using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ProjectVoiceLink
{
    public enum Language
    {
        English,
        Russian,
        Spanish,
        German,
        Ukrainian,
        Portuguese,
        Polish
    }

    public static class Localization
    {
        private static readonly ConcurrentDictionary<string, Language> UserLanguageOverrides = new();

        public static Language ResolveLanguage(string? preferredLangCode, string? userTelegramLangCode)
        {
            // 1. Check if user set an explicit preference
            if (!string.IsNullOrWhiteSpace(preferredLangCode))
            {
                var lang = ParseLanguageCode(preferredLangCode);
                if (lang.HasValue) return lang.Value;
            }

            // 2. Fallback to Telegram client's language code
            if (!string.IsNullOrWhiteSpace(userTelegramLangCode))
            {
                var lang = ParseLanguageCode(userTelegramLangCode);
                if (lang.HasValue) return lang.Value;
            }

            // 3. Default fallback
            return Language.Russian; // Fallback to Russian or English
        }

        public static Language? ParseLanguageCode(string code)
        {
            var normalized = code.Trim().ToLowerInvariant();
            if (normalized.StartsWith("ru")) return Language.Russian;
            if (normalized.StartsWith("en")) return Language.English;
            if (normalized.StartsWith("es")) return Language.Spanish;
            if (normalized.StartsWith("de")) return Language.German;
            if (normalized.StartsWith("uk") || normalized.StartsWith("ua")) return Language.Ukrainian;
            if (normalized.StartsWith("pt")) return Language.Portuguese;
            if (normalized.StartsWith("pl")) return Language.Polish;
            return null;
        }

        public static string Get(string key, Language language, params object[] args)
        {
            if (!Strings.TryGetValue(key, out var dict))
            {
                return key;
            }

            if (!dict.TryGetValue(language, out var format))
            {
                // Fallback: Russian, then English
                if (!dict.TryGetValue(Language.Russian, out format))
                {
                    dict.TryGetValue(Language.English, out format);
                }
            }

            if (string.IsNullOrEmpty(format)) return key;

            return args.Length > 0 ? string.Format(format, args) : format;
        }

        private static readonly Dictionary<string, Dictionary<Language, string>> Strings = new()
        {
            ["welcome"] = new()
            {
                [Language.Russian] = "🌊 Добро пожаловать в бот по обмену голосовыми сообщениями «Послание в бутылке»!\n\nОтправьте мне голосовое сообщение, и вы получите случайное голосовое от другого пользователя.\n\nКоманды:\n/random — получить случайное сообщение\n/last — получить последнее сообщение\n/lang — сменить язык / change language",
                [Language.English] = "🌊 Welcome to the 'Message in a Bottle' voice exchange bot!\n\nSend me a voice message, and in return you will receive a random voice message from another user.\n\nCommands:\n/random — receive a random voice message\n/last — receive the latest voice message\n/lang — change language",
                [Language.Spanish] = "🌊 ¡Bienvenido al bot de intercambio de notas de voz 'Mensaje en una botella'!\n\nEnvíame un mensaje de voz y recibirás una nota de voz aleatoria de otro usuario.\n\nComandos:\n/random — recibir un mensaje aleatorio\n/last — recibir el último mensaje\n/lang — cambiar idioma",
                [Language.German] = "🌊 Willkommen beim Sprachnachrichten-Austauschbot „Flaschenpost“!\n\nSende mir eine Sprachnachricht und du erhältst eine zufällige Sprachnachricht eines anderen Nutzers.\n\nBefehle:\n/random — zufällige Nachricht abrufen\n/last — neueste Nachricht abrufen\n/lang — Sprache ändern",
                [Language.Ukrainian] = "🌊 Ласкаво просимо до бота обміну голосовими повідомленнями «Послання у пляшці»!\n\nНадішліть мені голосове повідомлення, і ви отримаєте випадкове голосове від іншого користувача.\n\nКоманди:\n/random — отримати випадкове повідомлення\n/last — отримати останнє повідомлення\n/lang — змінити мову",
                [Language.Portuguese] = "🌊 Bem-vindo ao bot de troca de mensagens de voz «Mensagem na Garrafa»!\n\nEnvie-me uma mensagem de voz e você receberá uma mensagem aleatória de outro usuário.\n\nComandos:\n/random — receber mensagem aleatória\n/last — receber a mensagem mais recente\n/lang — alterar idioma",
                [Language.Polish] = "🌊 Witaj w bocie do wymiany wiadomości głosowych „Wiadomość w butelce”!\n\nWyślij mi wiadomość głosową, a w zamian otrzymasz losowe nagranie od innego użytkownika.\n\nKomendy:\n/random — odbierz losową wiadomość\n/last — odbierz najnowszą wiadomość\n/lang — zmień język"
            },
            ["voice_received"] = new()
            {
                [Language.Russian] = "🍾 Принял ваше голосовое! Запечатываю в бутылку и отправляю в океан...",
                [Language.English] = "🍾 Received your voice message! Sealing it in a bottle and casting it into the ocean...",
                [Language.Spanish] = "🍾 ¡Recibí tu nota de voz! Sellándola en una botella y lanzándola al océano...",
                [Language.German] = "🍾 Sprachnachricht empfangen! In eine Flasche versiegelt und ins Meer geworfen...",
                [Language.Ukrainian] = "🍾 Прийняв ваше голосове! Запечатую в пляшку і кидаю в океан...",
                [Language.Portuguese] = "🍾 Mensagem de voz recebida! Selando na garrafa e lançando ao oceano...",
                [Language.Polish] = "🍾 Otrzymałem Twoją wiadomość głosową! Zamykam ją w butelce i wrzucam do oceanu..."
            },
            ["duplicate_voice"] = new()
            {
                [Language.Russian] = "⚠️ Вы или кто-то другой уже отправляли эту запись. Пожалуйста, запишите новое уникальное голосовое сообщение.",
                [Language.English] = "⚠️ You or another user have already sent this recording. Please record a new, unique voice message.",
                [Language.Spanish] = "⚠️ Tú u otro usuario ya enviaron esta grabación. Por favor, graba un mensaje de voz nuevo y único.",
                [Language.German] = "⚠️ Sie oder ein anderer Nutzer haben diese Aufnahme bereits gesendet. Bitte nehmen Sie eine neue, einzigartige Nachricht auf.",
                [Language.Ukrainian] = "⚠️ Ви або хтось інший вже надсилали цей запис. Будь ласка, запишіть нове унікальне голосове повідомлення.",
                [Language.Portuguese] = "⚠️ Você ou outra pessoa já enviou esta gravação. Por favor, grave uma mensagem de voz nova e única.",
                [Language.Polish] = "⚠️ Ty lub ktoś inny już wysłał to nagranie. Nagraj nową, unikalną wiadomość głosową."
            },
            ["too_short"] = new()
            {
                [Language.Russian] = "⏳ Аудио сообщение слишком короткое. Минимальная длительность: {0} сек.",
                [Language.English] = "⏳ Voice message is too short. Minimum duration: {0} sec.",
                [Language.Spanish] = "⏳ El mensaje de voz es demasiado corto. Duración mínima: {0} seg.",
                [Language.German] = "⏳ Die Sprachnachricht ist zu kurz. Mindestdauer: {0} Sek.",
                [Language.Ukrainian] = "⏳ Голосове повідомлення занадто коротке. Мінімальна тривалість: {0} сек.",
                [Language.Portuguese] = "⏳ A mensagem de voz é muito curta. Duração mínima: {0} seg.",
                [Language.Polish] = "⏳ Wiadomość głosowa jest za krótka. Minimalny czas trwania: {0} sek."
            },
            ["too_long"] = new()
            {
                [Language.Russian] = "⏳ Аудио сообщение слишком длинное. Максимальная длительность: {0} сек.",
                [Language.English] = "⏳ Voice message is too long. Maximum duration: {0} sec.",
                [Language.Spanish] = "⏳ El mensaje de voz es demasiado largo. Duración máxima: {0} seg.",
                [Language.German] = "⏳ Die Sprachnachricht ist zu lang. Maximale Dauer: {0} Sek.",
                [Language.Ukrainian] = "⏳ Голосове повідомлення занадто довге. Максимальна тривалість: {0} сек.",
                [Language.Portuguese] = "⏳ A mensagem de voz é muito longa. Duração máxima: {0} seg.",
                [Language.Polish] = "⏳ Wiadomość głosowa jest za długa. Maksymalny czas trwania: {0} sek."
            },
            ["rate_limit"] = new()
            {
                [Language.Russian] = "⏳ Пожалуйста, не так быстро! Подождите {0} сек. перед следующей отправкой.",
                [Language.English] = "⏳ Slow down! Please wait {0} sec. before sending another message.",
                [Language.Spanish] = "⏳ ¡Más despacio! Por favor espera {0} seg. antes de enviar otro mensaje.",
                [Language.German] = "⏳ Bitte nicht so schnell! Warte {0} Sek. bis zur nächsten Nachricht.",
                [Language.Ukrainian] = "⏳ Будь ласка, не так швидко! Зачекайте {0} сек. перед наступним повідомленням.",
                [Language.Portuguese] = "⏳ Por favor, não tão rápido! Aguarde {0} seg. antes de enviar outra mensagem.",
                [Language.Polish] = "⏳ Zwolnij! Odczekaj {0} sek. przed wysłaniem kolejnej wiadomości."
            },
            ["own_message"] = new()
            {
                [Language.Russian] = "🌊 Вот это да! Вы выловили свою же бутылку. Кто бы мог подумать?",
                [Language.English] = "🌊 What are the odds! You fished out your own bottle from the ocean.",
                [Language.Spanish] = "🌊 ¡Vaya coincidencia! Has pescado tu propia botella del océano.",
                [Language.German] = "🌊 Was für ein Zufall! Du hast deine eigene Flaschenpost aus dem Meer gefischt.",
                [Language.Ukrainian] = "🌊 Оце так збіг! Ви виловили свою власну пляшку з океану.",
                [Language.Portuguese] = "🌊 Que coincidência! Você pescou sua própria garrafa do oceano.",
                [Language.Polish] = "🌊 Niesamowite! Wyłowiłeś z oceanu swoją własną butelkę."
            },
            ["no_voices"] = new()
            {
                [Language.Russian] = "🌊 В океане пока нет других бутылок с посланиями. Вы первый! Попробуйте позже.",
                [Language.English] = "🌊 There are no other bottles in the ocean yet. You are the first! Check back later.",
                [Language.Spanish] = "🌊 Todavía no hay otras botellas en el océano. ¡Eres el primero! Vuelve a intentar más tarde.",
                [Language.German] = "🌊 Es gibt noch keine weiteren Flaschenpost-Nachrichten im Meer. Du bist der Erste! Schau später wieder vorbei.",
                [Language.Ukrainian] = "🌊 В океані поки немає інших пляшок з посланнями. Ви перший! Спробуйте пізніше.",
                [Language.Portuguese] = "🌊 Ainda não há outras garrafas no oceano. Você é o primeiro! Tente novamente mais tarde.",
                [Language.Polish] = "🌊 W oceanie nie ma jeszcze innych butelek z wiadomościami. Jesteś pierwszy! Sprawdź ponownie później."
            },
            ["non_voice_guidance"] = new()
            {
                [Language.Russian] = "🎤 Пожалуйста, отправьте голосовое сообщение. Бот предназначен исключительно для обмена голосовыми записями.",
                [Language.English] = "🎤 Please send a voice message. This bot is specifically designed for exchanging voice recordings.",
                [Language.Spanish] = "🎤 Por favor envía una nota de voz. Este bot está diseñado exclusivamente para intercambiar mensajes de voz.",
                [Language.German] = "🎤 Bitte senden Sie eine Sprachnachricht. Dieser Bot ist ausschließlich für den Austausch von Sprachaufnahmen gedacht.",
                [Language.Ukrainian] = "🎤 Будь ласка, надішліть голосове повідомлення. Цей бот призначений виключно для обміну голосовими записами.",
                [Language.Portuguese] = "🎤 Por favor, envie uma mensagem de voz. Este bot foi criado exclusivamente para a troca de gravações de voz.",
                [Language.Polish] = "🎤 Proszę wysłać wiadomość głosową. Ten bot służy wyłącznie do wymiany nagrań głosowych."
            },
            ["sticker_refusal"] = new()
            {
                [Language.Russian] = "🎨 Стикеры не помещаются в бутылку! Пожалуйста, запишите голосовое сообщение.",
                [Language.English] = "🎨 Stickers don't fit in bottles! Please record a voice message instead.",
                [Language.Spanish] = "🎨 ¡Los stickers no caben en una botella! Por favor envía una nota de voz.",
                [Language.German] = "🎨 Sticker passen nicht in Flaschen! Bitte nimm stattdessen eine Sprachnachricht auf.",
                [Language.Ukrainian] = "🎨 Стікери не поміщаються у пляшку! Будь ласка, запишіть голосове повідомлення.",
                [Language.Portuguese] = "🎨 Figurinhas não cabem na garrafa! Por favor, grave uma mensagem de voz.",
                [Language.Polish] = "🎨 Naklejki nie mieszczą się w butelce! Proszę nagrać wiadomość głosową."
            },
            ["audio_refusal"] = new()
            {
                [Language.Russian] = "🎵 Пожалуйста, отправьте именно голосовое сообщение, записанное в микрофон, а не аудиофайл или музыку.",
                [Language.English] = "🎵 Please send a live voice message recorded with your microphone, not a music file or pre-recorded audio.",
                [Language.Spanish] = "🎵 Por favor envía una nota de voz grabada con tu micrófono, no un archivo de música o audio precargado.",
                [Language.German] = "🎵 Bitte sende eine echte Sprachnachricht per Mikrofon, keine Musikdatei oder hochgeladene Audiodatei.",
                [Language.Ukrainian] = "🎵 Будь ласка, надішліть саме голосове повідомлення, записане з мікрофона, а не музику чи файл.",
                [Language.Portuguese] = "🎵 Por favor, envie uma mensagem de voz gravada no microfone, não um arquivo de áudio ou música.",
                [Language.Polish] = "🎵 Proszę wysłać wiadomość głosową nagraną z mikrofonu, a nie plik muzyczny lub audio."
            },
            ["other_media_refusal"] = new()
            {
                [Language.Russian] = "📦 Бот принимает только голосовые сообщения. Попробуйте записать голос!",
                [Language.English] = "📦 The bot only accepts voice messages. Try recording your voice!",
                [Language.Spanish] = "📦 El bot solo acepta notas de voz. ¡Intenta grabar tu voz!",
                [Language.German] = "📦 Der Bot akzeptiert nur Sprachnachrichten. Nimm einfach deine Stimme auf!",
                [Language.Ukrainian] = "📦 Бот приймає лише голосові повідомлення. Спробуйте записати свій голос!",
                [Language.Portuguese] = "📦 O bot aceita apenas mensagens de voz. Experimente gravar sua voz!",
                [Language.Polish] = "📦 Bot przyjmuje tylko wiadomości głosowe. Spróbuj nagrać swój głos!"
            },
            ["lang_prompt"] = new()
            {
                [Language.Russian] = "🌐 Выберите язык / Select language:\n\n/lang ru — Русский\n/lang en — English\n/lang es — Español\n/lang de — Deutsch\n/lang uk — Українська\n/lang pt — Português\n/lang pl — Polski",
                [Language.English] = "🌐 Select language / Выберите язык:\n\n/lang en — English\n/lang ru — Russian\n/lang es — Spanish\n/lang de — German\n/lang uk — Ukrainian\n/lang pt — Portuguese\n/lang pl — Polish",
                [Language.Spanish] = "🌐 Selecciona idioma:\n\n/lang es — Español\n/lang en — English\n/lang ru — Ruso\n/lang de — Alemán\n/lang uk — Ucraniano\n/lang pt — Portugués\n/lang pl — Polaco",
                [Language.German] = "🌐 Sprache wählen:\n\n/lang de — Deutsch\n/lang en — Englisch\n/lang ru — Russisch\n/lang es — Spanisch\n/lang uk — Ukrainisch\n/lang pt — Portugiesisch\n/lang pl — Polnisch",
                [Language.Ukrainian] = "🌐 Оберіть мову:\n\n/lang uk — Українська\n/lang en — English\n/lang ru — Русский\n/lang es — Español\n/lang de — Deutsch\n/lang pt — Португальська\n/lang pl — Польська",
                [Language.Portuguese] = "🌐 Selecione o idioma:\n\n/lang pt — Português\n/lang en — English\n/lang ru — Russo\n/lang es — Espanhol\n/lang de — Alemão\n/lang uk — Ucraniano\n/lang pl — Polonês",
                [Language.Polish] = "🌐 Wybierz język:\n\n/lang pl — Polski\n/lang en — English\n/lang ru — Rosyjski\n/lang es — Hiszpański\n/lang de — Niemiecki\n/lang uk — Ukraiński\n/lang pt — Portugalski"
            },
            ["lang_changed"] = new()
            {
                [Language.Russian] = "✅ Язык успешно изменен на Русский!",
                [Language.English] = "✅ Language successfully changed to English!",
                [Language.Spanish] = "✅ ¡Idioma cambiado a Español correctamente!",
                [Language.German] = "✅ Sprache erfolgreich auf Deutsch geändert!",
                [Language.Ukrainian] = "✅ Мову успішно змінено на Українську!",
                [Language.Portuguese] = "✅ Idioma alterado com sucesso para Português!",
                [Language.Polish] = "✅ Język został pomyślnie zmieniony na Polski!"
            },
            ["admin_only"] = new()
            {
                [Language.Russian] = "⛔ Эта команда доступна только администраторам бота.",
                [Language.English] = "⛔ This command is restricted to bot administrators.",
                [Language.Spanish] = "⛔ Este comando solo está disponible para administradores.",
                [Language.German] = "⛔ Dieser Befehl ist Administratoren vorbehalten.",
                [Language.Ukrainian] = "⛔ Ця команда доступна лише адміністраторам бота.",
                [Language.Portuguese] = "⛔ Este comando é restrito aos administradores do bot.",
                [Language.Polish] = "⛔ Ta komenda jest dostępna tylko dla administratorów bota."
            },
            ["banned"] = new()
            {
                [Language.Russian] = "⛔ Ваш аккаунт заблокирован за нарушение правил.",
                [Language.English] = "⛔ Your account has been banned for violating rules.",
                [Language.Spanish] = "⛔ Tu cuenta ha sido bloqueada por violar las normas.",
                [Language.German] = "⛔ Ihr Konto wurde wegen Regelverstößen gesperrt.",
                [Language.Ukrainian] = "⛔ Ваш обліковий запис заблоковано через порушення правил.",
                [Language.Portuguese] = "⛔ Sua conta foi suspensa por violação das regras.",
                [Language.Polish] = "⛔ Twoje konto zostało zablokowane z powodu naruszenia zasad."
            }
        };
    }
}