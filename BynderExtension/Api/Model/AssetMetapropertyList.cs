using System.Collections.Generic;
using System.Linq;

namespace Bynder.Api.Model
{
    /// <summary>
    /// Metaproperties on the asset
    /// </summary>
    public class AssetMetapropertyList : List<AssetMetaproperty>
    {
        #region Constructors

        public AssetMetapropertyList(IEnumerable<AssetMetaproperty> metaproperties) : base(metaproperties)
        {
        }

        public AssetMetapropertyList()
        { }

        #endregion Constructors

        #region Methods

        public static AssetMetapropertyList CreateFromDictionary(Dictionary<string, List<string>> dictionary)
        {
            var metaproperyList = new AssetMetapropertyList();
            if (dictionary == null) return metaproperyList;

            foreach (var element in dictionary)
            {
                metaproperyList.Add(new AssetMetaproperty { Id = element.Key, Values = element.Value ?? new List<string>() });
            }

            return metaproperyList;
        }

        public string GetPostData()
        {
            return string.Join($"\n", GetPostKeyValuePairs().Select(kv => $"{kv.Key}={kv.Value}"));
        }

        public List<KeyValuePair<string, string>> GetPostKeyValuePairs()
        {
            var pairs = new List<KeyValuePair<string, string>>();
            foreach (var metaproperty in this)
            {
                pairs.Add(new KeyValuePair<string, string>("metaproperty." + metaproperty.Id, string.Join(",", metaproperty.Values.Distinct())));
            }
            return pairs;
        }

        #endregion Methods
    }
}