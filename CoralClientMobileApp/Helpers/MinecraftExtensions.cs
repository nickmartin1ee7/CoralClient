using System.Text.RegularExpressions;

namespace CoralClientMobileApp.Helpers
{
    public static class MinecraftExtensions
    {
        private const string COLOR_REGEX = @"(§(\d|\w))+";

        public static string RemoveColorCodes(this string text)
            => Regex.Replace(text, COLOR_REGEX, string.Empty, RegexOptions.Compiled);
    }
}