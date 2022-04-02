using System.Text.RegularExpressions;

namespace CoralClient.Helpers
{
    public static class MinecraftHelper
    {
        private const string COLOR_REGEX = @"(§(\d|\w))+";

        public static string RemoveColorCodes(this string text)
            => Regex.Replace(text, COLOR_REGEX, string.Empty, RegexOptions.Compiled);
    }
}