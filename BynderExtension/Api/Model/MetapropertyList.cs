using System.Collections.Generic;
using System.Linq;

namespace Bynder.Api.Model
{
    public class MetapropertyList : List<Metaproperty>
    {
        #region Constructors

        public MetapropertyList(IEnumerable<Metaproperty> metaproperties) : base(metaproperties)
        {
        }

        public MetapropertyList()
        { }

        #endregion Constructors

        #region Methods

        public static MetapropertyList CreateFromDictionary(Dictionary<string, List<string>> dictionary)
        {
            var metaproperyList = new MetapropertyList();
            if (dictionary == null) return metaproperyList;

            foreach (var element in dictionary)
            {
                metaproperyList.Add(new Metaproperty { Id = element.Key, Values = element.Value ?? new List<string>() });
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