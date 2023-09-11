using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ProjectVoiceLink
{
    internal class UserClass
    {
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string username { get; set; }
        public string id { get; set; }
        public string languagecode { get; set; }
        public string date { get; set; }
        public string isbot { get; set; }
        public string messageid { get; set; }
        public string text { get; set; }
    }
}
