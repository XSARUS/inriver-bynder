using System.Globalization;
using System.Text;

namespace Bynder.Utils
{
    public static class Generics
    {
        public const string GENERAL_FORMAT_CULTURE_NAME = "en-US";
        public const string GENERAL_FORMAT_DATETIME = "yyyy-MM-ddTHH:mm:ssZ"; // ISO8601, or "yyyy-MM-dd'T'HH:mm:ss'Z'"

        /// <summary>
        /// Magic threshold number for fastest processing in inRiver
        /// </summary>
        public const int MaxConcurrentThreadCount = 4;

        private static NumberFormatInfo _decimalFormat;
        public static NumberFormatInfo DecimalFormat
        {
            get
            {
                if (_decimalFormat != null)
                {
                    return _decimalFormat;
                }
                //no bom
                _decimalFormat = new NumberFormatInfo
                {
                    NumberGroupSeparator = ThousandsSeparator,
                    NumberDecimalSeparator = DecimalSeparator,
                };
                return _decimalFormat;
            }
        }

        public const string ThousandsSeparator = ".";
        public const string DecimalSeparator = ",";

        private static Encoding _encoding;
        public static Encoding Encoding
        {
            get
            {
                if (_encoding != null)
                {
                    return _encoding;
                }
                // UTF8 without byte order mark (bom)
                _encoding = new UTF8Encoding(false);
                return _encoding;
            }
        }

        /// <summary>
        /// Culture of general format (which is en-US)
        /// </summary>
        public static readonly CultureInfo CultureInfo = CultureInfo.GetCultureInfo(GENERAL_FORMAT_CULTURE_NAME);
    }
}
