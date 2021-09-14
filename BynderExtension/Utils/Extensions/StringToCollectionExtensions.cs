using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Utils.Extensions
{
    public static class StringToCollectionExtensions
    {
        /// <summary>
        /// String to dictionary. One unique key per string.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="input"></param>
        /// <param name="parameterSeparator"></param>
        /// <param name="keyValueSeparator"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TVal> ToDictionary<TKey, TVal>(this string input, char parameterSeparator, char keyValueSeparator)
        {
            IEnumerable<List<string>> keyValueLists = from row in input.ToIEnumerable<string>(parameterSeparator)
                                                      select row.ToList<string>(keyValueSeparator);
            return keyValueLists.ToDictionary(
                    pair => pair[0].ConvertTo<TKey>(),
                    pair => pair.ElementAtOrDefault(1).ConvertTo<TVal>()
            );
        }

        /// <summary>
        /// Splits string into a collection. Empty List if string is null.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="parameterSeparator"></param>
        /// <returns>Collection of strings</returns>
        public static List<T> ToList<T>(this string input, char parameterSeparator)
        {
            return input.ToIEnumerable<T>(parameterSeparator).ToList();
        }

        /// <summary>
        /// Splits string into a collection. Empty IEnumerable if string is null.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="parameterSeparator"></param>
        /// <returns>Collection of strings</returns>
        public static IEnumerable<T> ToIEnumerable<T>(this string input, char parameterSeparator)
        {
            if (input == null)
            {
                return new List<T>(0);
            }
            return input
                    .Split(new[] { parameterSeparator }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x.ConvertTo<T>());
        }

        /// <summary>
        /// Splits string into a collection. Empty IEnumerable if string is null.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="parameterSeparators"></param>
        /// <returns>Collection of strings</returns>
        public static IEnumerable<T> ToIEnumerable<T>(this string input, char[] parameterSeparators = null)
        {
            if (input == null)
            {
                return new List<T>(0);
            }

            if (parameterSeparators == null)
            {
                parameterSeparators = new[] { ',', ';' };
            }

            return input
                    .Split(parameterSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x.ConvertTo<T>());
        }
    }
}
