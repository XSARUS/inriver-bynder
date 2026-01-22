using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bynder.Sdk.Api.Converters;
using Bynder.Sdk.Query.Asset;
using Bynder.Sdk.Query.Decoder;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Bynder.Api.Mappers
{
    public static class ApiQueryMapper
    {
        public static T FromDictionary<T>(IDictionary<string, string> values) where T : new()
        {
            var instance = new T();
            var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var apiField = prop.GetCustomAttribute<ApiField>();
                if (apiField == null)
                    continue;

                if (!values.TryGetValue(apiField.ApiName, out var rawValue))
                    continue;

                if (string.IsNullOrWhiteSpace(rawValue))
                    continue;

                var converted = ConvertValue(prop.PropertyType, rawValue);
                prop.SetValue(instance, converted);

                usedKeys.Add(apiField.ApiName);
            }

            // 🔑 Unknow keys → MetaProperties
            if (instance is MediaQuery mq)
            {
                foreach (var kvp in values)
                {
                    if (usedKeys.Contains(kvp.Key))
                        continue;

                    // waarde kan CSV zijn
                    var valuesList = kvp.Value
                        .Split(',')
                        .Select(v => v.Trim())
                        .Where(v => v.Length > 0)
                        .ToList();

                    if (valuesList.Count == 0)
                        continue;

                    mq.MetaProperties[kvp.Key] = valuesList;
                }
            }

            return instance;
        }


        private static object ConvertUsingConverter(Type converterType, string rawValue)
        {
            // Bynder converters zijn stateless
            var converter = Activator.CreateInstance(converterType);

            // conventie in SDK: Convert(string value)
            var method = converterType.GetMethod("Convert", new[] { typeof(string) });
            if (method == null)
                throw new InvalidOperationException(
                    $"Converter '{converterType.FullName}' has no Convert(string) method");

            return method.Invoke(converter, new object[] { rawValue });
        }

        private static object ConvertValue(Type targetType, string value)
        {
            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
                return ConvertValue(underlying, value);

            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int))
                return int.Parse(value, CultureInfo.InvariantCulture);

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, ignoreCase: true);

            if (typeof(IEnumerable<string>).IsAssignableFrom(targetType))
                return value.Split(';').ToList();

            throw new NotSupportedException(
                $"Type '{targetType.FullName}' is not supported");
        }
    }
}