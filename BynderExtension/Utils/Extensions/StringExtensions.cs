using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bynder.Utils.Extensions
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string value)
        {
            Regex pattern = new Regex(@"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");
            var matches = pattern.Matches(value).OfType<Match>().Select(m => m.Groups[0].Value).ToArray();

            return new string(
              new CultureInfo("en-US", false)
                .TextInfo
                .ToTitleCase(
                  string.Join(" ", matches).ToLower()
                )
                .Replace(@" ", "")
                .Select((x, i) => i == 0 ? char.ToLower(x) : x)
                .ToArray()
            );
        }
    }
}
