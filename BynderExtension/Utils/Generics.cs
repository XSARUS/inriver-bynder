using System.Globalization;
using System.Text;

namespace Bynder.Utils
{
    public static class Generics
    {
        #region Fields

        public const string DecimalSeparator = ",";
        public const string GENERAL_FORMAT_DATETIME = "yyyy-MM-ddTHH:mm:ssZ"; // ISO8601, or "yyyy-MM-dd'T'HH:mm:ss'Z'"

        /// <summary>
        /// Magic threshold number for fastest processing in inRiver
        /// </summary>
        public const int MaxConcurrentThreadCount = 4;

        public const string ThousandsSeparator = ".";
        public static readonly CultureInfo CultureInfo = CultureInfo.InvariantCulture;
        private static NumberFormatInfo _decimalFormat;
        private static Encoding _encoding;

        #endregion Fields

        #region Properties

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

        #endregion Properties
    }
}