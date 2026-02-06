using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.Runtime.Serialization;

namespace Bynder.Sdk.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    [DataContract]
    public enum MetapropertyType
    {
        [EnumMember(Value = "select")]
        Select,

        [EnumMember(Value = "select2")]
        Select2,

        [EnumMember(Value = "autocomplete")]
        Autocomplete,

        [EnumMember(Value = "text")]
        Text,

        [EnumMember(Value = "longtext")]
        Longtext,

        [EnumMember(Value = "date")]
        Date,

        [EnumMember(Value = "linked_assets")]
        LinkedAssets
    }
}