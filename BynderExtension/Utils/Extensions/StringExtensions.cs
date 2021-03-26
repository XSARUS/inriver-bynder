using System;
using System.Collections.Generic;
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

        private static readonly List<char> ControlChars = new List<char>()
        {
            '\u0000',
            '\u0001',
            '\u0000',
            '\u0001',
            '\u0002',
            '\u0003',
            '\u0004',
            '\u0005',
            '\u0006',
            '\u0007',
            '\u0008',
            '\u0009',
            '\u000B',
            '\u000C',
            '\u000E',
            '\u000F',
            '\u0010',
            '\u0011',
            '\u0012',
            '\u0013',
            '\u0014',
            '\u0015',
            '\u0016',
            '\u0017',
            '\u0018',
            '\u0019',
            '\u001A',
            '\u001B',
            '\u001C',
            '\u001D',
            '\u001E',
            '\u001F',
        };

        public static string RemoveControlCharacters(this string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            IEnumerable<char> filtered = input.Where(currentChar => !ControlChars.Contains(currentChar));

            return new string(filtered.ToArray());
        }

        public static string ToStringWithoutControlCharactersForCvlKey(this string input)
        {
            string value = input.ToString().RemoveControlCharacters();

            string pattern = @"[^a-z0-9]";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            value = regex.Replace(value, string.Empty);

            return value;
        }
    }
}
