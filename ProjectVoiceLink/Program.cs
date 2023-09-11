using System;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Microsoft.Data.Sqlite;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using static System.Net.WebRequestMethods;

namespace ProjectVoiceLink 
{
    internal class Program
    {
        //Creating new telegram both instance
        static ITelegramBotClient bot = new TelegramBotClient(Configuration.BotToken);

        //initializing sql lite connection
        static SqliteConnection conn = new SqliteConnection(Configuration.DatabasePath);

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            Console.WriteLine();
            UserClass User = new UserClass();
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;

                // Message Breakdown
                try {
                    if (update.Message.From.FirstName != null)
                    {
                        User.first_name = update.Message.From.FirstName.ToString();
                    }
                    else
                    {
                        User.first_name = "null";
                    }
                    if (update.Message.From.LastName != null)
                    {
                        User.last_name = update.Message.From.LastName.ToString();
                    }
                    else
                    {
                        User.last_name = "null";
                    }
                    if (update.Message.From.Username != null)
                    {
                        User.username = update.Message.From.Username.ToString();
                    }
                    else
                    {
                        User.username = "null";
                    }
                    User.id = update.Message.From.Id.ToString();
                    User.languagecode = update.Message.From.LanguageCode.ToString();
                    User.date = DateTime.Now.ToString();
                    User.isbot = update.Message.From.IsBot.ToString();
                    User.messageid = update.Message.MessageId.ToString();
                    User.text = "";
                }
                catch
                {
                    Console.WriteLine("There was an error parsing usign data, trying to zero the values and continure");
                    User.first_name = "";
                    User.last_name = "";
                    User.username = "";
                    User.id = update.Message.From.Id.ToString();
                    User.languagecode = update.Message.From.LanguageCode.ToString();
                    User.date = DateTime.Now.ToString();
                    User.isbot = update.Message.From.IsBot.ToString();
                    User.messageid = update.Message.MessageId.ToString();
                    User.text = "";
                }

                if (update.Message.ReplyToMessage != null) //if message is a reply to another message
                {
                    if (message.Type == Telegram.Bot.Types.Enums.MessageType.Sticker)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Ну и зачем мне ваш стикер?");
                    }

                    if (message.Type == Telegram.Bot.Types.Enums.MessageType.Audio)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Пожалуйста, отправьте голосовое сообщение, а не музыку или заранее записанный файл.");
                    }

                    if (message.Type == Telegram.Bot.Types.Enums.MessageType.Location)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Ну и зачем мне это?");
                    }
                    if (message.Type == Telegram.Bot.Types.Enums.MessageType.Video)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Ну и зачем мне это?");
                    }
                    if (message.Type == Telegram.Bot.Types.Enums.MessageType.VideoNote)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Ну и зачем мне это?");
                    }

                    return;
                }


                if (message.Type == Telegram.Bot.Types.Enums.MessageType.Voice)
                    {
                        
                        if (message.Voice.Duration > Configuration.minimum_voice_message_length_in_seconds) 
                        {
                            //try
                            //{
                                await botClient.SendTextMessageAsync(message.Chat, "Принял ваше голосовое!");

                        // Saving File
                        var fileId = update.Message.Voice.FileId;
                            var fileInfo = await botClient.GetFileAsync(fileId);
                            var filePath = fileInfo.FilePath;
                            string destinationFilePath = $"../audio_bottles/" + fileId + ".ogg";

                        await using FileStream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                            await botClient.DownloadFileAsync( filePath: filePath, destination: fileStream);
                        fileStream.Close();
                        // EOF_Saving_file

                        //Calculate file hash
                        string hashvalue = Utilities.calculate_checksum_of_thefile(destinationFilePath);
                        

                        //Compare_file_hashes
                        string stm = "SELECT filehash FROM Voices ORDER BY filehash DESC LIMIT 100;";
                        using var cmd = new SqliteCommand(stm, conn);
                        using Microsoft.Data.Sqlite.SqliteDataReader rdr = await cmd.ExecuteReaderAsync();
                        bool message_found_in_the_hash = false;
                        while (rdr.Read())
                        { 
                            if (hashvalue == (String.Format("{0}", rdr[0])))
                                {
                                //Console.WriteLine("MESSAGE FOUND IN THE HASH");
                                message_found_in_the_hash = true;
                                }
                            //Console.WriteLine(String.Format("{0}", rdr[0]));
                        }
                        //eofhashcompare

                        //replying
                        if (message_found_in_the_hash == false)
                        {
                            //Logging
                            string usersql = "INSERT INTO Voices (first_name,last_name,username,id,language_code,date,is_bot,message_id,audio_file,filehash) VALUES('" + User.first_name + "','" + User.last_name + "','" + User.username + "','" + User.id + "','" + User.languagecode + "','" + User.date + "','" + User.isbot + "','" + User.messageid + "','" + destinationFilePath + "','" + hashvalue + "')";
                            using var userlog = new SqliteCommand(usersql, conn);
                            await userlog.ExecuteNonQueryAsync();
                            //EOF-Logging
                            
                            // replying to the message

                            string random_voice = "SELECT audio_file FROM Voices ORDER BY RANDOM() LIMIT 1;";
                            using var sqlrandom_voice = new SqliteCommand(random_voice, conn);
                            string path_to_random_voice = sqlrandom_voice.ExecuteScalar().ToString();

                            //check to do not send the user his own message
                            //try
                            //{

                            if (String.Equals(hashvalue, "error") == true)
                            {
                                await botClient.SendTextMessageAsync(message.Chat, "Произошла ошибка при расчете хэша вашего голосового сообщения, попробуйте еще раз.");
                            }
                            else
                            {
                                if (String.Equals(hashvalue, Utilities.calculate_checksum_of_thefile(path_to_random_voice)) == true)
                                {
                                    await botClient.SendTextMessageAsync(message.Chat, "Вот это да! Вы получили свое же сообщение. Кто бы мог подумать?");
                                    //random_voice = "SELECT audio_file FROM Voices ORDER BY RANDOM() LIMIT 1;";
                                }
                            }
                            //}
                            //catch
                            // {
                            //     await botClient.SendTextMessageAsync(message.Chat, "Кажется еще никто не отправил боту сообщение, поздравляю, вы первый");
                            //     return;
                            // }
                            //eof check

                            try { 
                                 using (var stream = System.IO.File.OpenRead(path_to_random_voice))
                                    {
                                    message = await botClient.SendVoiceAsync(
                                    chatId: message.Chat.Id,
                                    voice: stream,
                                    //duration: 36,
                                    cancellationToken: cancellationToken);
                                    stream.Close();
                                }
                               }
                            catch
                            {
                                await botClient.SendTextMessageAsync(message.Chat, "Кажется еще никто не отправил боту сообщение, или я не могу найти подходящее. Попробуйте чуть позже, пожалуйста?");
                            }
                        }
                        if (message_found_in_the_hash == true)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Вы уже отправляли это голосовое сообщение. Пожалуйста, запишите новое.");
                        }
                        //eofreplying
                        //}

                        //catch
                        //{
                        //    Console.WriteLine("Failed to deliver message to the user + " + username + " on " + date);
                        //}
                    }
                    else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Аудио сообщение слишком короткое");
                        }
                    }

                    if (message.Type == Telegram.Bot.Types.Enums.MessageType.Sticker)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Ну и зачем мне ваш стикер?");
                    }

                    if (message.Type == Telegram.Bot.Types.Enums.MessageType.Audio)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Пожалуйста, отправьте голосовое сообщение, а не музыку или заранее записанный файл.");
                    }

                if (message.Type == Telegram.Bot.Types.Enums.MessageType.Location)
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Ну и зачем мне это?");
                }
                if (message.Type == Telegram.Bot.Types.Enums.MessageType.Video)
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Ну и зачем мне это?");
                }
                if (message.Type == Telegram.Bot.Types.Enums.MessageType.VideoNote)
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Ну и зачем мне это?");
                }


                if (message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
                    {
                    User.text = update.Message.Text.ToString();
                        
                        if (message.Text.ToLower() == "/start")
                            {
                                await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать в бот по обмену голосовыми сообщениями!");
                                //Logging
                                 string usersql = "INSERT INTO Users (first_name,last_name,username,id,language_code,date,is_bot,message_id,text) VALUES('" + User.first_name + "','" + User.last_name + "','" + User.username + "','" + User.id + "','" + User.languagecode + "','" + User.date + "','" + User.isbot + "','" + User.messageid + "','" + User.text + "')";
                                 using var userlog = new SqliteCommand(usersql, conn);
                                 await userlog.ExecuteNonQueryAsync();
                                //EOF-Logging
                                 return;
                            }
                    if (message.Text.ToLower() == "/last")
                    {
                        string random_voice = "SELECT audio_file FROM Voices ORDER BY KEYID DESC LIMIT 1;";
                        using var sqlrandom_voice = new SqliteCommand(random_voice, conn);
                        string path_to_random_voice = sqlrandom_voice.ExecuteScalar().ToString();

                        try
                        {
                            using (var stream = System.IO.File.OpenRead(path_to_random_voice))
                            {
                                message = await botClient.SendVoiceAsync(
                                chatId: message.Chat.Id,
                                voice: stream,
                                //duration: 36,
                                cancellationToken: cancellationToken);
                                stream.Close();
                            }
                        }
                        catch
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Кажется еще никто не отправил боту сообщение, или я не могу найти подходящее. Попробуйте чуть позже, пожалуйста?");
                        }
                        return;
                    }
                    if (message.Text.ToLower() == "/random")
                    {
                        string random_voice = "SELECT audio_file FROM Voices ORDER BY RANDOM() LIMIT 1;";
                        using var sqlrandom_voice = new SqliteCommand(random_voice, conn);
                        string path_to_random_voice = sqlrandom_voice.ExecuteScalar().ToString();

                        try
                        {
                            using (var stream = System.IO.File.OpenRead(path_to_random_voice))
                            {
                                message = await botClient.SendVoiceAsync(
                                chatId: message.Chat.Id,
                                voice: stream,
                                //duration: 36,
                                cancellationToken: cancellationToken);
                                stream.Close();
                            }
                        }
                        catch
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Кажется еще никто не отправил боту сообщение, или я не могу найти подходящее. Попробуйте чуть позже, пожалуйста?");
                        }
                        return;
                    }

                    else
                            {
                            //Logging
                            string messagelogsql = "INSERT INTO Logs (first_name,last_name,username,id,language_code,date,is_bot,message_id,text) VALUES('" + User.first_name + "','" + User.last_name + "','" + User.username + "','" + User.id + "','" + User.languagecode + "','" + User.date + "','" + User.isbot + "','" + User.messageid + "','" + User.text + "')";
                            using var messagelog = new SqliteCommand(messagelogsql, conn);
                            await messagelog.ExecuteNonQueryAsync();
                            //EOF-Logging

                            //default reply
                            try { await botClient.SendTextMessageAsync(message.Chat, "Пожалуйста, отправьте голосовое сообщение. Бот не принимает ничего, кроме голосовых."); }
                            catch { Console.WriteLine("Failed to deliver message to the user + " + User.username + " on " + User.date); }
                            }
                      }
                

            }

        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }


        static void Main(string[] args)
        {
            //Connecting to the SQLite
            conn.Open();
            string stm = "SELECT SQLITE_VERSION()";
            using var cmd = new SqliteCommand(stm, conn);
            string version = cmd.ExecuteScalar().ToString();
            
            Console.WriteLine($"SQLite version: {version} initialized");

 

            //Starting Telegram Bot
            Console.WriteLine("Bot is running: " + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }

    }
}