using System;
using Newtonsoft.Json;

namespace Bynder.Sdk.Api.Converters
{
    public sealed class ObjectToJsonStringConverter : ITypeToStringConverter
    {
        public bool CanConvert(Type typeToConvert) => true;

        public string Convert(object value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}