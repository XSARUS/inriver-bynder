using System.Collections.Generic;
using System.Linq;

namespace Bynder.Utils.Extensions
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// IEnumerable of strings to dictionary with a List of values. Grouped keys.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TListVal"></typeparam>
        /// <param name="input"></param>
        /// <param name="keyValueSeparator"></param>
        /// <returns></returns>
        public static Dictionary<TKey, List<TListVal>> ToDictionaryWithListOfValues<TKey, TListVal>(this IEnumerable<string> input, char keyValueSeparator)
        {
            var output = new Dictionary<TKey, List<TListVal>>();
            if (input == null)
            {
                return output;
            }

            IEnumerable<List<string>> keyValueLists = from row in input
                                                      select row.ToList<string>(keyValueSeparator);

            // grouped by 0 which is the key
            var groupedKeyValueLists = keyValueLists.GroupBy(x => x[0]);
            output = groupedKeyValueLists.ToDictionary(
                            pair => pair.Key.ConvertTo<TKey>(),
                            // 0 is the key, 1 is the value
                            pair => pair.Select(g => g[1].ConvertTo<TListVal>()).Distinct().ToList());
            return output;
        }
    }
}
