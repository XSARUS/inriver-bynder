using System;
using System.Collections.Generic;

namespace Bynder.Api.Model
{
    /// <summary>
    /// pure representation of the option
    /// </summary>
    public class MetapropertyOption
    {
        public string DisplayLabel { get; set; }
        public DateTime? Date { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public string Label { get; set; }
        public Dictionary<string, string> Labels { get; set; }
    }
}
