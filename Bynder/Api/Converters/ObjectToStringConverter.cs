using Newtonsoft.Json;
using System;

namespace Bynder.Sdk.Api.Converters
{
    public sealed class ObjectToJsonStringConverter : ITypeToStringConverter
    {
        #region Methods

        public bool CanConvert(Type typeToConvert) => true;

        public string Convert(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        #endregion Methods
    }
}