using Newtonsoft.Json.Linq;
using System;

namespace Bynder.Utils.Helpers
{
    public static class JsonHelper
    {
        #region Methods

        public static string GetValueAsString(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            switch (token.Type)
            {
                case JTokenType.Null:
                    return null;

                case JTokenType.Array:
                    var arr = (JArray)token;
                    return string.Join(",", arr);

                case JTokenType.Date:
                    var isoDateTimeString = GetIsoDateTimeString(token);
                    return isoDateTimeString;

                case JTokenType.Float:
                    return GetFloatString(token);

                case JTokenType.Boolean:
                    return GetBooleanString(token);

                // We do not need to implement this for now.
                // It depends on what will be send as value for the metaproperty types in bynder. We have not seen the code reach the object yet.
                case JTokenType.Object:
                    throw new NotImplementedException("no implementation for object yet");

                default:
                    return token.Value<string>();
            }
        }

        public static string GetValueAsString(JProperty property) => GetValueAsString(property.Value);

        private static string GetBooleanString(JToken token, bool forceType = true)
        {
            if (forceType && token.Type != JTokenType.Boolean)
            {
                return null;
            }

            bool boolValue = token.Value<bool>();

            return boolValue.ToString();
        }

        private static string GetFloatString(JToken token)
        {
            if (token.Type != JTokenType.Float)
            {
                return null;
            }

            decimal number = token.Value<decimal>();

            return number.ToString("0.0######", System.Globalization.CultureInfo.InvariantCulture); // At least 1 decimal and dot as decimal separator
        }

        /// <summary>
        /// Gets the Iso dateTime from the token
        /// </summary>
        private static string GetIsoDateTimeString(JToken token)
        {
            if (token.Type != JTokenType.Date)
            {
                return null;
            }

            return ((DateTime)((JValue)token).Value).ToString(Generics.GENERAL_FORMAT_DATETIME);
        }

        #endregion Methods
    }
}