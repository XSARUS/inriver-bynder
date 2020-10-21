using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Converters
{
    using Api.Model;
    using Utils.Helpers;

    public class AssetJsonConverter : JsonConverter
    {
        private const string _propertyPrefix = "property_";

        private Type _type => typeof(Asset);

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

        private MetapropertyList GetMetapropertyList(List<JProperty> properties)
        {
            var propertyTokens = properties.Where(x => x.Name.StartsWith(_propertyPrefix));
            var metaProperties= propertyTokens.Select(x => 
                // save values as string
                new Metaproperty { Name = x.Name.Substring(x.Name.IndexOf(_propertyPrefix) + _propertyPrefix.Length), Value = JsonHelper.GetValueAsString(x) });
            return new MetapropertyList(metaProperties);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // will execute default behaviour
            throw new NotImplementedException();
        }
    }
}
