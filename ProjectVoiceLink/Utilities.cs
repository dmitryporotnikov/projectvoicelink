using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ProjectVoiceLink
{
    public static class Utilities
    {
        public static string hashvalue { get; set; }
        public static string calculate_checksum_of_thefile(string filepath)
        {
            var md5 = MD5.Create();
            {
                try
                {
                    var readstream = System.IO.File.OpenRead(filepath);
                    
                    
                        var hash = md5.ComputeHash(readstream);
                        hashvalue = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    
                    readstream.Close();
                }
                catch
                {
                    hashvalue = "error";
                    return hashvalue;
                }
            }

            return hashvalue;
        }
    }
   
}
