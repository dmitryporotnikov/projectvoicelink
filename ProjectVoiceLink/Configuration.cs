using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectVoiceLink
{
    internal static class Configuration
    {
        public static int minimum_voice_message_length_in_seconds = 1; //Minimum voice message length in seconds

        public static string DatabasePath = "Data Source=" + $"../ProjectVoiceLink.db"; //Database path

        public static string BotToken = "YOURBOTTOKEN"; //Bot token
    }
}
