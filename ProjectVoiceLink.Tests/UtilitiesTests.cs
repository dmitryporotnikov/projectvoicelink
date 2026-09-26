using System;
using System.IO;
using System.Text;
using ProjectVoiceLink;
using Xunit;

namespace ProjectVoiceLink.Tests
{
    public class UtilitiesTests
    {
        [Fact]
        public void CalculateChecksum_NonExistentFile_ReturnsError()
        {
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ogg");
            var result = Utilities.calculate_checksum_of_thefile(nonExistentPath);
            Assert.Equal("error", result);
        }

        [Fact]
        public void CalculateChecksum_ValidFile_ReturnsExpectedMd5Hex()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                // Write known byte content [0x01, 0x02, 0x03, 0x04]
                // MD5 of [0x01, 0x02, 0x03, 0x04]: 08d6c05a21512a79a1dfeb9d2a8f262f
                File.WriteAllBytes(tempFile, new byte[] { 0x01, 0x02, 0x03, 0x04 });
                
                var checksum = Utilities.calculate_checksum_of_thefile(tempFile);
                Assert.Equal("08d6c05a21512a79a1dfeb9d2a8f262f", checksum);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void UserClass_Defaults_AreInitializedToEmptyOrNonNull()
        {
            var user = new UserClass();
            Assert.NotNull(user.first_name);
            Assert.NotNull(user.last_name);
            Assert.NotNull(user.username);
            Assert.NotNull(user.id);
            Assert.NotNull(user.languagecode);
            Assert.NotNull(user.date);
            Assert.NotNull(user.isbot);
            Assert.NotNull(user.messageid);
            Assert.NotNull(user.text);
        }

        [Fact]
        public void Configuration_Defaults_HaveExpectedValues()
        {
            Assert.True(Configuration.minimum_voice_message_length_in_seconds >= 1);
            Assert.False(string.IsNullOrWhiteSpace(Configuration.DatabasePath));
            Assert.False(string.IsNullOrWhiteSpace(Configuration.AudioStoragePath));
        }
    }
}