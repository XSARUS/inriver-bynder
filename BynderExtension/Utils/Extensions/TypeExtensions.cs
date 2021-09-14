using System;
using System.Linq;

namespace Bynder.Utils.Extensions
{
    public static class TypeExtensions
    {
        #region Methods

        /// <summary>
        /// Get joined string for IEnumerable type data
        /// </summary>
        /// <param name="typeCode"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T GetValueForIEnumerableType<T>(this TypeCode typeCode, string input)
        {
            switch (typeCode)
            {
                case TypeCode.Int32:
                    return (T)input.ToIEnumerable<string>().Select(int.Parse);
                case TypeCode.String:
                    return (T)input.ToIEnumerable<string>().Select(x => x);
                default:
                    return default(T);
            }
        }
        #endregion Methods
    }
}
