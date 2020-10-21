using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Bynder.Utils.Extensions
{
    public static class StringParseExtensions
    {
        public static bool TryGetBool(string input)
        {
            switch (input.ToLower())
            {
                case "ja":
                case "yes":
                case "true":
                case "t":
                case "1":
                    return true;

                case "nee":
                case "no":
                case "-1":
                case "0":
                case "false":
                case "f":
                    return false;

                default:
                    throw new InvalidCastException($"Could parse value '{input}' to a Boolean");
            }
        }

        /// <summary>
        /// Extension method to return an enum value of type T for the given string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        /// <summary>
        /// Extension method to return an enum value of type T for the given int.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this int value)
        {
            var name = Enum.GetName(typeof(T), value);
            return name.ToEnum<T>();
        }

        /// <summary>
        /// Tries to Convert object to given type. Returns true when the conversion succeeded. 
        /// Returns default value when it couldn't convert or when value is null, but the type is non-nullable
        /// Use a nullable type if you want the default value to be null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="returnValue"></param>
        /// <param name="numberFormatInfo"></param>
        /// <param name="culture"></param>
        /// <param name="dateTimeFormat"></param>
        /// <returns></returns>
        public static bool TryToConvert<T>(this object input, out T returnValue, NumberFormatInfo numberFormatInfo = null, CultureInfo culture = null, string dateTimeFormat = null)
        {
            returnValue = default(T);
            try
            {
                returnValue = input.ConvertTo<T>(numberFormatInfo, culture, dateTimeFormat);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Converts object to given type. 
        /// Returns default value when value is null, but the type is non-nullable
        /// use a nullable type if you want the default value to be null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="numberFormatInfo">fill when you want your own floating type format</param>
        /// <param name="culture">null when you want to use Generics.CultureInfo, for backwards compatibility</param>
        /// <param name="dateTimeFormat">null when you want to use Generics.DateTimeFormat, for backwards compatibility. Empty string when you want to use the default dateTimeFormat</param>
        /// <returns></returns>
        public static T ConvertTo<T>(this object input, NumberFormatInfo numberFormatInfo = null, CultureInfo culture = null, string dateTimeFormat = null)
        {
            // check if type is nullable
            var inputConvertType = typeof(T);
            bool isNullable = inputConvertType.IsGenericType && inputConvertType.GetGenericTypeDefinition() == typeof(Nullable<>);

            // set type to convert to
            Type convertType;
            if (isNullable)
            {
                convertType = Nullable.GetUnderlyingType(inputConvertType);
            }
            else
            {
                convertType = typeof(T);
            }

            // return null immediately if nullable and input is null
            if (isNullable && input == null)
            {
                return default(T);
            }

            // create string for later usage
            var inputAsString = input as string;

            return GetConvertedValue<T>(convertType, input, inputAsString, numberFormatInfo, culture, dateTimeFormat);
        }

        private static T ConvertEnum<T>(object input, string inputAsString)
        {
            if (input == null)
                return default(T);

            if (!string.IsNullOrWhiteSpace(inputAsString))
            {
                if (int.TryParse(inputAsString, out var enumNumber))
                    enumNumber.ToEnum<T>();

                return inputAsString.ToEnum<T>();
            }

            return default(T);
        }

        /// <summary>
        /// Currently only supports string to IEnumerable 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="convertType"></param>
        /// <param name="input"></param>
        /// <param name="inputAsString"></param>
        /// <returns></returns>
        private static T ConvertToIEnumerable<T>(Type convertType, object input, string inputAsString)
        {
            if (input == null)
            {
                return default(T);
            }

            // return if already the right type
            var inputType = input.GetType();
            if (inputType == convertType)
            {
                return (T)input;
            }

            // only supports string to Ienumerable now
            if (!string.IsNullOrWhiteSpace(inputAsString))
            {
                //if something between <> like List<int>
                var genericArguments = convertType.GetGenericArguments();
                if (genericArguments.Length > 0)
                {
                    var CollectionTypeArgument = Type.GetTypeCode(convertType.GetGenericArguments()[0]);
                    return CollectionTypeArgument.GetValueForIEnumerableType<T>(inputAsString);
                }
                else
                {
                    //only IEnumerable and List are allowed
                    var CollectionTypeArgument = Type.GetTypeCode(convertType.GetElementType());
                    return CollectionTypeArgument.GetValueForIEnumerableType<T>(inputAsString);
                }
            }

            return default(T);
        }

        private static T GetConvertedValue<T>(Type convertType, object input, string inputAsString, NumberFormatInfo numberFormatInfo, CultureInfo culture, string dateTimeFormat)
        {
            try
            {
                if (convertType.IsEnum)
                {
                    return ConvertEnum<T>(input, inputAsString);
                }

                //IEnumerable handling: type is ienumerable or class implements type IEnumerable 
                bool isIEnumerable = convertType.IsGenericType && convertType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                    convertType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                // string is a list of chars, then convert to valuetype
                if (isIEnumerable && convertType != typeof(string))
                {
                    return ConvertToIEnumerable<T>(convertType, input, inputAsString);
                }

                return ConvertToValueType<T>(convertType, input, inputAsString, numberFormatInfo, culture, dateTimeFormat);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"The conversion of '{input}' to {convertType.Name} is not possible", ex);
            }
        }

        private static T ConvertToValueType<T>(Type convertType, object input, string inputAsString, NumberFormatInfo numberFormatInfo, CultureInfo culture, string dateTimeFormat)
        {
            //typecode
            switch (Type.GetTypeCode(convertType))
            {
                case TypeCode.Int16:
                    return (T)(object)Convert.ToInt16(input);
                case TypeCode.Int32:
                    return (T)(object)Convert.ToInt32(input);
                case TypeCode.Int64:
                    return (T)(object)Convert.ToInt64(input);
                case TypeCode.UInt16:
                    return (T)(object)Convert.ToUInt16(input);
                case TypeCode.UInt32:
                    return (T)(object)Convert.ToUInt32(input);
                case TypeCode.UInt64:
                    return (T)(object)Convert.ToUInt64(input);
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return ParseFloatingType<T>(convertType, input, numberFormatInfo, culture);
                case TypeCode.DateTime:
                    return ParseDateTime<T>(input, inputAsString, culture, dateTimeFormat);
                case TypeCode.Boolean:
                    return ParseBoolean<T>(convertType, input, inputAsString);
                case TypeCode.Char:
                    return (T)(object)Convert.ToChar(input);
                case TypeCode.String:
                    return ParseString<T>(convertType, input, numberFormatInfo, culture, dateTimeFormat);
            }

            return default(T);
        }

        private static T ParseString<T>(Type convertType, object input, NumberFormatInfo numberFormatInfo, CultureInfo culture, string dateTimeFormat)
        {
            if (input == null)
            {
                return default(T);
            }

            var inputType = input.GetType();

            switch (Type.GetTypeCode(inputType))
            {
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return ParseFloatingType<T>(convertType, input, numberFormatInfo, culture);
                case TypeCode.DateTime:
                    return ParseDateTimeToString<T>(convertType, input, culture, dateTimeFormat);
            }

            return (T)Convert.ChangeType(input, convertType);
        }

        private static T ParseDateTimeToString<T>(Type convertType, object input, CultureInfo culture, string dateTimeFormat)
        {
            if (dateTimeFormat == null)
            {
                dateTimeFormat = Generics.GENERAL_FORMAT_DATETIME;
            }
            if (culture == null)
            {
                culture = Generics.CultureInfo;
            }
            var formattedDateTime = ((DateTime)input).ToString(dateTimeFormat, culture);
            return (T)Convert.ChangeType(formattedDateTime, convertType);
        }

        private static T ParseBoolean<T>(Type convertType, object input, string inputAsString)
        {
            if (input is bool)
            {
                return (T)input;
            }
            if (!string.IsNullOrWhiteSpace(inputAsString))
            {
                return (T)Convert.ChangeType(TryGetBool(inputAsString), convertType);
            }
            return default(T);
        }

        private static T ParseFloatingType<T>(Type convertType, object input, NumberFormatInfo numberFormatInfo, CultureInfo culture)
        {
            // cant convert null, so return this way 
            if (input == null)
            {
                return default(T);
            }

            if (numberFormatInfo != null)
            {
                return (T)Convert.ChangeType(input, convertType, numberFormatInfo);
            }
            else if (culture != null)
            {
                return (T)Convert.ChangeType(input, convertType, culture);
            }
            else
            {
                return (T)Convert.ChangeType(input, convertType, Generics.CultureInfo);
            }
        }

        private static T ParseDateTime<T>(object input, string inputAsString, CultureInfo dateTimeCulture, string dateTimeFormat)
        {
            if (input is DateTime)
            {
                return (T)input;
            }
            if (!string.IsNullOrWhiteSpace(inputAsString))
            {
                if (dateTimeFormat == null)
                {
                    dateTimeFormat = Generics.GENERAL_FORMAT_DATETIME;
                }
                if (dateTimeCulture == null)
                {
                    dateTimeCulture = Generics.CultureInfo;
                }
                return (T)(object)DateTime.ParseExact(inputAsString, dateTimeFormat, dateTimeCulture);
            }

            return default(T);
        }

        /// <summary>
        /// Converts data for inRiver datatype
        /// All datatypes are nullable
        /// </summary>
        /// <param name="input"></param>
        /// <param name="dataType">inRiver datatype</param>
        /// <returns></returns>
        public static object ConvertTo(this object input, string dataType)
        {
            switch (dataType.ToLower())
            {
                //String
                //The data type for a string value.Represented in clients as edit boxes.

                //Xml
                //A data type for storing and validating data as xml.

                //CVL
                //Means that the field type is associated with an existing CVL from the marketing model that also must be assigned to the field type.The property Multivalue(true / false) on a field type decides if one ore more values can be selected from the CVL.In clients the CVL: s are normally presented as dropdown controls.

                case "string":
                case "xml":
                case "cvl":
                    return ConvertTo<string>(input);

                //Double
                //Represents a numerical value with decimals.
                case "double":
                    return ConvertTo<double?>(input);

                //Integer
                //Represents a numerical value without decimals.

                //File
                //This is a special data type(integer) used for the field type "ResourceFileId" on the "Resource" entity type.It is a unique integer pointing to a resource file in the iPMC database.
                case "integer":
                case "file":
                    return ConvertTo<int?>(input);

                //Boolean
                //Can only have two values.Normally represented as check boxes in clients, in code by false(unchecked) / true(checked).
                case "boolean":
                    return ConvertTo<bool?>(input);

                //DateTime
                //Represents a specific date and time.Special calendar controls are used in clients to make it easy to pick dates.
                case "datetime":
                    return ConvertTo<DateTime?>(input);

                //LocaleString
                //This is a data type that can hold string values for a field in all available languages in the current marketing model.In code the languages are indexed using CultureInfo objects.
                // todo: add logic for localestring. fe. nl-NL:voorbeeld,en-US:example

                default:
                    return null;
            }
        }

        /// <summary>
        /// Tries to convert data for inRiver datatype
        /// returns false when conversion not succesful
        /// </summary>
        /// <param name="input"></param>
        /// <param name="dataType">inRiver datatype</param>
        /// <param name="returnValue">converted data</param>
        public static bool TryToConvert(this object input, string dataType, out object returnValue)
        {
            bool success = false;
            switch (dataType.ToLower())
            {
                //String
                //The data type for a string value.Represented in clients as edit boxes.

                //Xml
                //A data type for storing and validating data as xml.

                //CVL
                //Means that the field type is associated with an existing CVL from the marketing model that also must be assigned to the field type.The property Multivalue(true / false) on a field type decides if one ore more values can be selected from the CVL.In clients the CVL: s are normally presented as dropdown controls.

                case "string":
                case "xml":
                case "cvl":
                    success = TryToConvert(input, out string tryStringVal);
                    returnValue = tryStringVal;
                    break;

                //Double
                //Represents a numerical value with decimals.
                case "double":
                    success = TryToConvert(input, out double tryDoubleVal);
                    returnValue = tryDoubleVal;
                    break;

                //Integer
                //Represents a numerical value without decimals.

                //File
                //This is a special data type(integer) used for the field type "ResourceFileId" on the "Resource" entity type.It is a unique integer pointing to a resource file in the iPMC database.
                case "integer":
                case "file":
                    success = TryToConvert(input, out int outIntVal);
                    returnValue = outIntVal;
                    break;

                //Boolean
                //Can only have two values.Normally represented as check boxes in clients, in code by false(unchecked) / true(checked).
                case "boolean":
                    success = TryToConvert(input, out bool outBoolVal);
                    returnValue = outBoolVal;
                    break;

                //DateTime
                //Represents a specific date and time.Special calendar controls are used in clients to make it easy to pick dates.
                case "datetime":
                    success = TryToConvert(input, out DateTime outDtVal);
                    returnValue = outDtVal;
                    break;

                //LocaleString
                //This is a data type that can hold string values for a field in all available languages in the current marketing model.In code the languages are indexed using CultureInfo objects.
                // todo: add logic for localestring. fe. nl-NL:voorbeeld,en-US:example
                default:
                    returnValue = default(object);
                    break;
            }

            return success;
        }
    }
}