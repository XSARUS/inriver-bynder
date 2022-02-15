using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Converters
{
    using Api.Model;

    public class AssetJsonConverter : JsonConverter
    {
        #region Fields

        private const string _propertyPrefix = "property_";

        #endregion Fields

        #region Properties

        public override bool CanWrite => false;
        private Type _type => typeof(Asset);

        #endregion Properties

        #region Methods

        public override bool CanConvert(Type objectType)
        {
            return _type.IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);
            var properties = jObject.Properties().ToList();

            // deserialize automaticly
            existingValue = existingValue ?? serializer.ContractResolver.ResolveContract(objectType).DefaultCreator();

            // populate with reader created from current jObject, so we can re-use the reader. The original reader can't be reset, because it's forward-only
            serializer.Populate(jObject.CreateReader(), existingValue);

            // deserialize metadataproperties
            var asset = (Asset)existingValue;
            asset.MetaProperties = GetMetapropertyList(properties);

            // Populate the object properties
            return asset;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // will execute default behaviour
            throw new NotImplementedException();
        }

        private static List<string> GetValueAsStringList(JToken token)
        {
            if (token == null) return new List<string>();

            switch (token.Type)
            {
                case JTokenType.Null:
                    return new List<string>();

                case JTokenType.Array:
                    var arr = (JArray)token;
                    return arr.ToObject<List<string>>();

                // We do not need to implement this for now.
                // It depends on what will be send as value for the metaproperty types in bynder. We have only seen strings and string arrays.
                case JTokenType.Object:
                    throw new NotImplementedException("No implementation to process JToken Object yet");

                default:
                    return new List<string> { token.Value<string>() };
            }
        }

        private MetapropertyList GetMetapropertyList(List<JProperty> properties)
        {
            var propertyTokens = properties.Where(x => x.Name.StartsWith(_propertyPrefix));
            var metaProperties = propertyTokens.Select(jProperty =>
                 // property values are always send as array
                 new Metaproperty
                 {
                     Name = jProperty.Name.Substring(jProperty.Name.IndexOf(_propertyPrefix) + _propertyPrefix.Length),
                     Values = GetValueAsStringList(jProperty.Value)
                 });
            return new MetapropertyList(metaProperties);
        }

        #endregion Methods
    }
}