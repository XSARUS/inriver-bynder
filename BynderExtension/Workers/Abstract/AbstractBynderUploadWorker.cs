using Bynder.Models;
using Bynder.Utils.Extensions;
using Bynder.Utils.Helpers;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Workers
{
    using SdkIBynderClient = Sdk.Service.IBynderClient;

    public class AbstractBynderUploadWorker: AbstractBynderWorker
    {

        #region Constructors

        public AbstractBynderUploadWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null)
            : base(inRiverContext, bynderClient)
        {
        }

        #endregion Constructors

        #region Methods

        protected static List<string> GetValuesForField(Field field)
        {
            var values = new List<string>();

            if (field == null || string.IsNullOrWhiteSpace(field?.Data?.ToString()))
            {
                return values;
            }

            if (field.FieldType.DataType.Equals(DataType.CVL) && field.FieldType.Multivalue)
            {
                var keys = field.Data.ToString().ToIEnumerable<string>(';');
                if (keys.Any())
                {
                    values.AddRange(keys);
                }
            }
            else
            {
                values.Add(field.Data.ToString());
            }

            return values;
        }

        protected bool EntityAppliesToConditions(Entity entity)
        {
            var conditions = SettingHelper.GetExportConditions(InRiverContext.Settings, InRiverContext.Logger);

            // return true if no conditions found. Conditions are optional.
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach (var condition in conditions)
            {
                if (GetConditionResult(entity, condition))
                    continue;

                /*var field = entity.GetField(condition.InRiverFieldTypeId);
                var value = field?.Data?.ToString();

                InRiverContext.Log(
                    LogLevel.Debug,
                    $"Resource {entity.Id} does not apply to condition on field {condition.InRiverFieldTypeId} [value: {value}], skipping metaproperty update; Condition values: {string.Join(";", condition.Values)}"
                );*/

                return false;
            }

            return true;
        }

        private static bool GetConditionResult(Entity entity, ExportCondition condition)
        {
            var field = entity.GetField(condition.InRiverFieldTypeId);

            var values = condition.Values;
            int valueCount = values?.Count ?? 0;

            // Geen field of leeg field
            if (field == null || field.IsEmpty())
            {
                // true als:
                // - geen condition values
                // - of expliciet null/empty condition
                return valueCount == 0 || (valueCount == 1 && string.IsNullOrEmpty(values[0]));
            }

            // Alleen nu pas field values ophalen (lazy)
            var fieldValues = GetValuesForField(field);

            return ConditionHelper.ValuesApplyToCondition(fieldValues, condition);
        }

        #endregion Methods
    }
}