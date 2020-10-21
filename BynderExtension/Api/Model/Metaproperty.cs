using System;

namespace Bynder.Api.Model
{
    public class Metaproperty
    {
        /// <summary>
        /// Unique GUID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the metaproperty, should be alphanumeric only. Cannot be modified after the metaproperty has been created.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value as string, multiple values comma separated.
        /// 
        /// todo: check out if string is ok or needs to change to an other datatype.
        /// Note that the list of (metaproperty) options should include all the (metaproperty) options available in the lower hierarchy; 
        /// meaning it should include the (metaproperty) options of the (metaproperty) options etc.
        /// </summary>
        public string Value { get; set; }

        public Metaproperty() {}

        [Obsolete("Use an Object Initializer instead")]
        public Metaproperty(string id, string value)
        {
            Id = id;
            Value = value;
        }
    }
}
