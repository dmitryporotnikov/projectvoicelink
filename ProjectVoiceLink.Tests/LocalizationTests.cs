using ProjectVoiceLink;
using Xunit;

namespace ProjectVoiceLink.Tests
{
    public class LocalizationTests
    {
        [Theory]
        [InlineData("ru", Language.Russian)]
        [InlineData("en", Language.English)]
        [InlineData("es", Language.Spanish)]
        [InlineData("de", Language.German)]
        [InlineData("uk", Language.Ukrainian)]
        [InlineData("ua", Language.Ukrainian)]
        [InlineData("pt", Language.Portuguese)]
        [InlineData("pl", Language.Polish)]
        public void ParseLanguageCode_ValidCodes_ReturnsExpected(string code, Language expected)
        {
            var result = Localization.ParseLanguageCode(code);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ResolveLanguage_PreferredTakesPrecedence()
        {
            var lang = Localization.ResolveLanguage("pt", "ru");
            Assert.Equal(Language.Portuguese, lang);
        }

        [Fact]
        public void ResolveLanguage_FallbackToTelegramCodeWhenNoPreferred()
        {
            var lang = Localization.ResolveLanguage(null, "pl");
            Assert.Equal(Language.Polish, lang);
        }

        [Fact]
        public void ResolveLanguage_FallbackToDefaultWhenUnknown()
        {
            var lang = Localization.ResolveLanguage(null, "xyz");
            Assert.Equal(Language.Russian, lang);
        }

        [Theory]
        [InlineData(Language.Russian)]
        [InlineData(Language.English)]
        [InlineData(Language.Spanish)]
        [InlineData(Language.German)]
        [InlineData(Language.Ukrainian)]
        [InlineData(Language.Portuguese)]
        [InlineData(Language.Polish)]
        public void Get_AllSupportedLanguages_HaveWelcomeAndReceiptMessages(Language lang)
        {
            var welcome = Localization.Get("welcome", lang);
            Assert.False(string.IsNullOrWhiteSpace(welcome));

            var receipt = Localization.Get("voice_received", lang);
            Assert.False(string.IsNullOrWhiteSpace(receipt));

            var duplicate = Localization.Get("duplicate_voice", lang);
            Assert.False(string.IsNullOrWhiteSpace(duplicate));

            var langChanged = Localization.Get("lang_changed", lang);
            Assert.False(string.IsNullOrWhiteSpace(langChanged));
        }

        [Fact]
        public void Get_FormattedArguments_AreInsertedCorrectly()
        {
            var msg = Localization.Get("too_short", Language.Portuguese, 5);
            Assert.Contains("5 seg", msg);

            var plMsg = Localization.Get("too_short", Language.Polish, 5);
            Assert.Contains("5 sek", plMsg);

            var rateMsg = Localization.Get("rate_limit", Language.Polish, 10);
            Assert.Contains("10 sek", rateMsg);
        }
    }
}