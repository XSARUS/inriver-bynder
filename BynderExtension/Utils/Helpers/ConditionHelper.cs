using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Utils.Helpers
{
    using Enums;
    using Models;

    public static class ConditionHelper
    {
        #region Methods

        public static bool ValuesApplyToCondition(List<string> values, Condition condition)
        {
            switch (condition.MatchType)
            {
                case MatchType.EqualSorted:
                    // sort the values
                    values.Sort();
                    condition.Values.Sort();
                    // check if lists are equal
                    return Enumerable.SequenceEqual(values, condition.Values, StringComparer.Ordinal);

                case MatchType.EqualSortedCaseInsensitive:
                    // sort the values
                    values.Sort();
                    condition.Values.Sort();
                    // check if lists are equal
                    return Enumerable.SequenceEqual(values, condition.Values, StringComparer.OrdinalIgnoreCase);

                case MatchType.Equal:
                    return Enumerable.SequenceEqual(values, condition.Values, StringComparer.Ordinal);

                case MatchType.EqualCaseInsensitive:
                    return Enumerable.SequenceEqual(values, condition.Values, StringComparer.OrdinalIgnoreCase);

                case MatchType.ContainsAny:
                    return values.Intersect(condition.Values).Any();

                case MatchType.ContainsAnyCaseInsensitive:
                    return values.Select(x => x.ToLower()).Intersect(condition.Values.Select(x => x.ToLower())).Any();

                case MatchType.ContainsAll:
                    return condition.Values.All(x => values.Contains(x));

                case MatchType.ContainsAllCaseInsensitive:
                    var metapropertyValuesLowerCase = values.Select(x => x.ToLower());
                    var conditionValuesLowerCase = condition.Values.Select(x => x.ToLower());
                    return conditionValuesLowerCase.All(x => metapropertyValuesLowerCase.Contains(x));

                default:
                    throw new NotSupportedException($"MatchType {condition.MatchType} is not yet supported to use for the import conditions!");
            }
        }

        #endregion Methods
    }
}