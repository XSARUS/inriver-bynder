using System;
using System.Collections.Generic;

namespace Bynder.Utils.Helpers
{
    using Models;

    public static class MetaPropertyTraverseConfigHelper
    {

        /// <summary>
        /// Returns true if any of the provided fields match any InriverFieldTypeId
        /// defined anywhere inside the MetaPropertyMapTraverseConfig tree.
        /// </summary>
        public static bool HasAnyConfiguredField(
            IEnumerable<string> fields,
            MetaPropertyMapTraverseConfig config)
        {
            if (fields == null || config == null)
                return false;

            // Gather all MetaPropertyMapping entries in the entire config tree
            var allMaps = GetAllMetaPropertyMaps(config);

            foreach (var field in fields)
            {
                foreach (var map in allMaps)
                {
                    if (string.Equals(field, map.InriverFieldTypeId, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Collects all MetaPropertyMaps from config and its nested inbound/outbound.
        /// </summary>
        private static List<MetaPropertyMap> GetAllMetaPropertyMaps(MetaPropertyMapTraverseConfig root)
        {
            var result = new List<MetaPropertyMap>();

            foreach (var node in Flatten(root))
            {
                if (node.MetaPropertyMapping != null)
                    result.AddRange(node.MetaPropertyMapping);
            }

            return result;
        }

        /// <summary>
        /// Flattens config tree (inbound + outbound).
        /// </summary>
        private static IEnumerable<MetaPropertyMapTraverseConfig> Flatten(MetaPropertyMapTraverseConfig root)
        {
            var stack = new Stack<MetaPropertyMapTraverseConfig>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return node;

                if (node.Inbound != null)
                {
                    foreach (var child in node.Inbound)
                        stack.Push(child);
                }

                if (node.Outbound != null)
                {
                    foreach (var child in node.Outbound)
                        stack.Push(child);
                }
            }
        }
    }

}
}
