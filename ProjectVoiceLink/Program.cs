using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ProjectVoiceLink 
{
    public class Program
    {
        private static ITelegramBotClient bot = null!;
        private static DatabaseService databaseService = null!;
        private static SpamProtectionService spamProtection = null!;
        private static MaintenanceService maintenanceService = null!;
        private static readonly DateTime AppStartTime = DateTime.UtcNow;

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message == null)
            {
                return;
            }

            var message = update.Message;
            var user = ExtractUser(message);

            // 1. Check if user is banned
            if (spamProtection.IsUserBanned(user.id))
            {
                return;
            }

            // 2. Resolve User Language
            var preferredLangCode = await databaseService.GetUserPreferredLanguageAsync(user.id, cancellationToken);
            var lang = Localization.ResolveLanguage(preferredLangCode, user.languagecode);

            // 3. Handle Message Types
            // 3.1 Replies to other messages
            if (message.ReplyToMessage != null)
            {
                switch (message.Type)
                {
                    case MessageType.Sticker:
                        await botClient.SendTextMessageAsync(message.Chat.Id, Localization.Get("sticker_refusal", lang), cancellationToken: cancellationToken);
                        break;
                    case MessageType.Audio:
                        await botClient.SendTextMessageAsync(message.Chat.Id, Localization.Get("audio_refusal", lang), cancellationToken: cancellationToken);
                        break;
                    case MessageType.Location:
                    case MessageType.Video:
                    case MessageType.VideoNote:
                        await botClient.SendTextMessageAsync(message.Chat.Id, Localization.Get("other_media_refusal", lang), cancellationToken: cancellationToken);
                        break;
                }
                return;
            }

            // 3.2 Voice message handling
            if (message.Type == MessageType.Voice && message.Voice != null)
            {
                await HandleVoiceMessageAsync(botClient, message, user, lang, cancellationToken);
                return;
            }

            // 3.3 Other non-voice media
            if (message.Type == MessageType.Sticker)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, Localization.Get("sticker_refusal", lang), cancellationToken: cancellationToken);
                return;
            }

            if (message.Type == MessageType.Audio)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, Localization.Get("audio_refusal", lang), cancellationToken: cancellationToken);
                return;
            }

            if (message.Type == MessageType.Location || message.Type == MessageType.Video || message.Type == MessageType.VideoNote)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, Localization.Get("other_media_refusal", lang), cancellationToken: cancellationToken);
                return;
            }

            // 3.4 Text commands
            if (message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                await HandleTextMessageAsync(botClient, message, user, lang, cancellationToken);
            }
        }

        private static async Task HandleVoiceMessageAsync(
            ITelegramBotClient botClient, 
            Message message, 
            UserClass user, 
            Language lang, 
            CancellationToken cancellationToken)
        {
            var voice = message.Voice!;

            // 1. Rate Limiting for Voice
            var rateResult = spamProtection.CheckVoiceRateLimit(user.id);
            if (!rateResult.Allowed)
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    Localization.Get("rate_limit", lang, rateResult.RetryAfterSeconds), 
                    cancellationToken: cancellationToken);
                return;
            }

            // 2. Validate Duration
            if (!spamProtection.ValidateVoiceDuration(voice.Duration, out var errorKey, out var boundaryVal))
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    Localization.Get(errorKey!, lang, boundaryVal), 
                    cancellationToken: cancellationToken);
                return;
            }

            // Acknowledge receipt
            await botClient.SendTextMessageAsync(
                message.Chat.Id, 
                Localization.Get("voice_received", lang), 
                cancellationToken: cancellationToken);

            // 3. Download Audio to disk with server-friendly visible filename
            var fileId = voice.FileId;
            var fileInfo = await botClient.GetFileAsync(fileId, cancellationToken);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var destinationFileName = $"{timestamp}_{user.id}_{fileId}.ogg";
            var destinationFilePath = Path.Combine(Configuration.AudioStoragePath, destinationFileName);

            if (!string.IsNullOrEmpty(fileInfo.FilePath))
            {
                await using var fileStream = new FileStream(
                    destinationFilePath, 
                    FileMode.Create, 
                    FileAccess.Write, 
                    FileShare.None, 
                    bufferSize: 64 * 1024, 
                    useAsync: true);

                await botClient.DownloadFileAsync(fileInfo.FilePath, fileStream, cancellationToken);
            }

            // 4. Calculate Checksum
            var hashvalue = await Utilities.calculate_checksum_async(destinationFilePath, cancellationToken);

            // 5. Duplicate Check
            var isDuplicate = await spamProtection.IsDuplicateVoiceAsync(hashvalue, cancellationToken);
            if (isDuplicate)
            {
                // Delete duplicate file from disk so it doesn't consume server space
                try
                {
                    if (System.IO.File.Exists(destinationFilePath))
                    {
                        System.IO.File.Delete(destinationFilePath);
                    }
                }
                catch
                {
                    // Ignore deletion error
                }

                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    Localization.Get("duplicate_voice", lang), 
                    cancellationToken: cancellationToken);
                return;
            }

            // 6. Save Voice in Database
            await databaseService.InsertVoiceAsync(user, destinationFilePath, hashvalue, cancellationToken);

            // 7. Pick a Random Voice to Send Back
            var randomVoicePath = await databaseService.GetRandomVoiceAsync(hashvalue, user.id, cancellationToken);

            if (!string.IsNullOrEmpty(randomVoicePath) && System.IO.File.Exists(randomVoicePath))
            {
                // Check if user caught their own message
                var randomHash = await Utilities.calculate_checksum_async(randomVoicePath, cancellationToken);
                if (string.Equals(hashvalue, randomHash, StringComparison.OrdinalIgnoreCase))
                {
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id, 
                        Localization.Get("own_message", lang), 
                        cancellationToken: cancellationToken);
                }

                try
                {
                    await using var stream = new FileStream(
                        randomVoicePath, 
                        FileMode.Open, 
                        FileAccess.Read, 
                        FileShare.Read, 
                        bufferSize: 64 * 1024, 
                        useAsync: true);

                    var inputFile = InputFile.FromStream(stream, Path.GetFileName(randomVoicePath));
                    await botClient.SendVoiceAsync(
                        chatId: message.Chat.Id,
                        voice: inputFile,
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id, 
                        Localization.Get("no_voices", lang), 
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    Localization.Get("no_voices", lang), 
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleTextMessageAsync(
            ITelegramBotClient botClient, 
            Message message, 
            UserClass user, 
            Language lang, 
            CancellationToken cancellationToken)
        {
            var text = message.Text!.Trim();
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();

            // Rate limit text commands
            var rateResult = spamProtection.CheckCommandRateLimit(user.id);
            if (!rateResult.Allowed)
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    Localization.Get("rate_limit", lang, rateResult.RetryAfterSeconds), 
                    cancellationToken: cancellationToken);
                return;
            }

            // /start
            if (cmd == "/start")
            {
                await databaseService.LogUserStartAsync(user, cancellationToken);
                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    Localization.Get("welcome", lang), 
                    cancellationToken: cancellationToken);
                return;
            }

            // /lang or /language
            if (cmd == "/lang" || cmd == "/language")
            {
                if (parts.Length > 1)
                {
                    var chosenCode = parts[1];
                    var parsed = Localization.ParseLanguageCode(chosenCode);
                    if (parsed.HasValue)
                    {
                        await databaseService.SetUserPreferredLanguageAsync(user.id, chosenCode, cancellationToken);
                        await botClient.SendTextMessageAsync(
                            message.Chat.Id, 
                            Localization.Get("lang_changed", parsed.Value), 
                            cancellationToken: cancellationToken);
                        return;
                    }
                }

                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    Localization.Get("lang_prompt", lang), 
                    cancellationToken: cancellationToken);
                return;
            }

            // /last
            if (cmd == "/last")
            {
                var lastVoice = await databaseService.GetLastVoiceAsync(cancellationToken);
                if (!string.IsNullOrEmpty(lastVoice) && System.IO.File.Exists(lastVoice))
                {
                    try
                    {
                        await using var stream = new FileStream(
                            lastVoice, 
                            FileMode.Open, 
                            FileAccess.Read, 
                            FileShare.Read, 
                            bufferSize: 64 * 1024, 
                            useAsync: true);

                        var inputFile = InputFile.FromStream(stream, Path.GetFileName(lastVoice));
                        await botClient.SendVoiceAsync(
                            chatId: message.Chat.Id,
                            voice: inputFile,
                            cancellationToken: cancellationToken);
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(
                            message.Chat.Id, 
                            Localization.Get("no_voices", lang), 
                            cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id, 
                        Localization.Get("no_voices", lang), 
                        cancellationToken: cancellationToken);
                }
                return;
            }

            // /random
            if (cmd == "/random")
            {
                var randomVoice = await databaseService.GetRandomVoiceAsync("", user.id, cancellationToken);
                if (!string.IsNullOrEmpty(randomVoice) && System.IO.File.Exists(randomVoice))
                {
                    try
                    {
                        await using var stream = new FileStream(
                            randomVoice, 
                            FileMode.Open, 
                            FileAccess.Read, 
                            FileShare.Read, 
                            bufferSize: 64 * 1024, 
                            useAsync: true);

                        var inputFile = InputFile.FromStream(stream, Path.GetFileName(randomVoice));
                        await botClient.SendVoiceAsync(
                            chatId: message.Chat.Id,
                            voice: inputFile,
                            cancellationToken: cancellationToken);
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(
                            message.Chat.Id, 
                            Localization.Get("no_voices", lang), 
                            cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id, 
                        Localization.Get("no_voices", lang), 
                        cancellationToken: cancellationToken);
                }
                return;
            }

            // Admin command: /stats
            if (cmd == "/stats")
            {
                if (!Configuration.AdminUserIds.Contains(user.id))
                {
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id, 
                        Localization.Get("admin_only", lang), 
                        cancellationToken: cancellationToken);
                    return;
                }

                var stats = await maintenanceService.GetStorageStatsAsync(cancellationToken);
                var uptime = DateTime.UtcNow - AppStartTime;

                var statsMsg = 
                    $"📊 <b>ProjectVoiceLink Status & Storage</b>\n\n" +
                    $"• Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s\n" +
                    $"• Total Voices in DB: <b>{stats.TotalVoices}</b>\n" +
                    $"• Audio Files on Disk: <b>{stats.AudioFilesCount}</b> ({Utilities.FormatBytes(stats.AudioDirectorySizeBytes)})\n" +
                    $"• SQLite DB Size: <b>{Utilities.FormatBytes(stats.DatabaseSizeBytes)}</b>\n" +
                    $"• Total Registered Users: <b>{stats.TotalUsers}</b>\n" +
                    $"• Total Log Entries: <b>{stats.TotalLogs}</b>\n" +
                    $"• Retention Policy: <b>{Configuration.RetentionDays} days</b>\n" +
                    $"• Audio Storage Path: <code>{Configuration.AudioStoragePath}</code>";

                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    statsMsg, 
                    parseMode: ParseMode.Html, 
                    cancellationToken: cancellationToken);
                return;
            }

            // Admin command: /purge [days]
            if (cmd == "/purge")
            {
                if (!Configuration.AdminUserIds.Contains(user.id))
                {
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id, 
                        Localization.Get("admin_only", lang), 
                        cancellationToken: cancellationToken);
                    return;
                }

                int days = Configuration.RetentionDays;
                if (parts.Length > 1 && int.TryParse(parts[1], out var parsedDays) && parsedDays > 0)
                {
                    days = parsedDays;
                }

                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    $"🧹 Starting maintenance purge for items older than {days} days...", 
                    cancellationToken: cancellationToken);

                var result = await maintenanceService.PurgeOlderThanDaysAsync(days, cancellationToken);

                var purgeMsg = 
                    $"✅ <b>Maintenance Purge Completed</b> in {result.Duration.TotalSeconds:F2}s\n\n" +
                    $"• Database Records Deleted: <b>{result.VoicesDeleted}</b>\n" +
                    $"• Audio Files Deleted: <b>{result.FilesDeleted}</b>\n" +
                    $"• Orphaned Files Cleaned: <b>{result.OrphanFilesDeleted}</b>\n" +
                    $"• Disk Space Reclaimed: <b>{Utilities.FormatBytes(result.BytesFreed)}</b>";

                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    purgeMsg, 
                    parseMode: ParseMode.Html, 
                    cancellationToken: cancellationToken);
                return;
            }

            // Non-command text message: log and reply with guidance
            await databaseService.LogMessageAsync(user, cancellationToken);
            try
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    Localization.Get("non_voice_guidance", lang), 
                    cancellationToken: cancellationToken);
            }
            catch
            {
                // Ignore delivery failure
            }
        }

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                Console.Error.WriteLine(JsonSerializer.Serialize(new
                {
                    exception.Message,
                    exception.StackTrace
                }));
            }
            catch
            {
                Console.Error.WriteLine(exception.ToString());
            }

            return Task.CompletedTask;
        }

        private static UserClass ExtractUser(Message message)
        {
            var user = new UserClass();
            try
            {
                user.first_name = message.From?.FirstName ?? "null";
                user.last_name = message.From?.LastName ?? "null";
                user.username = message.From?.Username ?? "null";
                user.id = message.From?.Id.ToString() ?? "0";
                user.languagecode = message.From?.LanguageCode ?? "unknown";
                user.date = DateTime.UtcNow.ToString("o");
                user.isbot = (message.From?.IsBot ?? false).ToString();
                user.messageid = message.MessageId.ToString();
                user.text = message.Text ?? string.Empty;
            }
            catch
            {
                user.first_name = "";
                user.last_name = "";
                user.username = "";
                user.id = message.From?.Id.ToString() ?? "0";
                user.languagecode = message.From?.LanguageCode ?? "";
                user.date = DateTime.UtcNow.ToString("o");
                user.isbot = (message.From?.IsBot ?? false).ToString();
                user.messageid = message.MessageId.ToString();
                user.text = message.Text ?? string.Empty;
            }
            return user;
        }

        static async Task Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(Configuration.BotToken) || Configuration.BotToken == "YOURBOTTOKEN")
            {
                Console.WriteLine("Warning: BOT_TOKEN is not configured. Set the BOT_TOKEN environment variable.");
            }

            // Initialize Services
            databaseService = new DatabaseService();
            await databaseService.InitializeAsync();

            spamProtection = new SpamProtectionService(databaseService);
            maintenanceService = new MaintenanceService(databaseService);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                cts.Cancel();
            };

            // Start scheduled background maintenance
            maintenanceService.StartScheduledMaintenance(cts.Token);

            bot = new TelegramBotClient(Configuration.BotToken);

            try
            {
                var me = await bot.GetMeAsync(cts.Token);
                Console.WriteLine($"Bot is running: @{me.Username} ({me.FirstName})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not connect to Telegram API: {ex.Message}");
            }

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cts.Token
            );

            Console.WriteLine("Bot is receiving updates. Press Ctrl+C to shut down.");

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Shutting down gracefully...");
            }
        }
    }
}