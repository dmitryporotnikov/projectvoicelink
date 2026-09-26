using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectVoiceLink
{
    public static class Utilities
    {
        public static string calculate_checksum_of_thefile(string filepath)
        {
            try
            {
                if (!File.Exists(filepath))
                {
                    return "error";
                }

                using var readstream = new FileStream(
                    filepath, 
                    FileMode.Open, 
                    FileAccess.Read, 
                    FileShare.Read, 
                    bufferSize: 64 * 1024);

                var hash = MD5.HashData(readstream);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch
            {
                return "error";
            }
        }

        public static async Task<string> calculate_checksum_async(string filepath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(filepath))
                {
                    return "error";
                }

                await using var readstream = new FileStream(
                    filepath, 
                    FileMode.Open, 
                    FileAccess.Read, 
                    FileShare.Read, 
                    bufferSize: 64 * 1024, 
                    useAsync: true);

                var hash = await MD5.HashDataAsync(readstream, cancellationToken);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch
            {
                return "error";
            }
        }

        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dBytes = bytes;
            while (dBytes >= 1024 && i < suffixes.Length - 1)
            {
                dBytes /= 1024;
                i++;
            }
            return $"{dBytes:0.##} {suffixes[i]}";
        }
    }
}