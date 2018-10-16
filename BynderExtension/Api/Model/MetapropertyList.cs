using System.Collections.Generic;
using System.Linq;

namespace Bynder.Api.Model
{
    public class MetapropertyList : List<Metaproperty>
    {
        public List<KeyValuePair<string, string>> GetPostKeyValuePairs()
        {
            var pairs = new List<KeyValuePair<string, string>>();
            foreach (var metaproperty in this)
            {
                pairs.Add(new KeyValuePair<string, string>("metaproperty."+metaproperty.Id, metaproperty.Value));
            }
            return pairs;
        }

        public string GetPostData()
        {
            return string.Join($"\n", GetPostKeyValuePairs().Select(kv => $"{kv.Key}={kv.Value}"));
        }

        public static MetapropertyList CreateFromDictionary(Dictionary<string, string> dictionary)
        {
            var metaproperyList = new MetapropertyList();
            if (dictionary == null) return metaproperyList;

            foreach (var element in dictionary)
            {
                metaproperyList.Add(new Metaproperty(element.Key, element.Value));
            }

            return metaproperyList;
        }
    }
}
