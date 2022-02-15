using System;
using System.Collections.Generic;

namespace Bynder.Api.Model
{
    public class Metaproperty
    {
        #region Properties

        /// <summary>
        /// Unique GUID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the metaproperty, should be alphanumeric only. Cannot be modified after the metaproperty has been created.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Values from bynder are always represented as array
        /// </summary>
        public List<string> Values { get; set; }

        #endregion Properties

        #region Constructors

        public Metaproperty()
        { }

        [Obsolete("Use an Object Initializer instead")]
        public Metaproperty(string id, string value)
        {
            Id = id;
            Values = new List<string> { value };
        }

        #endregion Constructors
    }
}