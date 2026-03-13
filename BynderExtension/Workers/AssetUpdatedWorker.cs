using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bynder.Config;
using Bynder.Enums;
using Bynder.Models;
using Bynder.Names;
using Bynder.Sdk.Model;
using Bynder.SettingProviders;
using Bynder.Utils;
using Bynder.Utils.Extensions;
using Bynder.Utils.Helpers;
using SdkIBynderClient = Bynder.Sdk.Service.IBynderClient;

namespace Bynder.Workers
{
    public class AssetUpdatedWorker : AbstractBynderWorker, IWorker
    {
        #region Fields

        private const string FieldTypeSettingsRegExKey = "RegExp";
        private readonly FilenameEvaluator _fileNameEvaluator;
        private Regex ResourceFilenameRegEx;

        #endregion Fields

        #region Properties

        public override Dictionary<string, string> DefaultSettings => AssetUpdatedWorkerSettingsProvider.Create();
        private EntityType ResourceEntityType => InRiverContext.ExtensionManager.ModelService.GetEntityType(EntityTypeIds.Resource);

        #endregion Properties

        #region Constructors

        public AssetUpdatedWorker(inRiverContext inRiverContext, FilenameEvaluator fileNameEvaluator, SdkIBynderClient bynderClient = null) : base(inRiverContext, bynderClient)
        {
            _fileNameEvaluator = fileNameEvaluator;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Main method of the worker
        /// </summary>
        /// <param name="bynderAssetId"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        public WorkerResult Execute(string bynderAssetId, NotificationType notificationType)
        {
            var result = new WorkerResult();

            // get original filename, as we need to evaluate this for further processing
            Media media = GetMedia(bynderAssetId);
            if (media == null)
            {
                result.Messages.Add($"Not processing '{bynderAssetId}'; asset not found.");
                return result;
            }

            // evaluate filename
            var (url, filename) = MediaHelper.GetDownloadUrlAndFilename(InRiverContext, _bynderClient, media).GetAwaiter().GetResult();
            var evaluatorResult = _fileNameEvaluator.Evaluate(filename);
            if (!evaluatorResult.Match.Success)
            {
                result.Messages.Add($"Not processing '{filename}'; does not match regex.");
                return result;
            }

            // evaluate conditions
            if (!AssetAppliesToConditions(media))
            {
                InRiverContext.Log(LogLevel.Debug, $"Asset {bynderAssetId} does not apply to the conditions");

                result.Messages.Add($"Not processing '{filename}'; does not apply to import conditions.");
                return result;
            }

            InRiverContext.Log(LogLevel.Debug, $"Asset {media.Id} with filename {filename}, applies to conditions; handling notification-type: {notificationType}");
            result.Messages.Add($"Asset {media.Id} applies to conditions; handling notification-type: {notificationType}");

            var resourceSearchType = SettingHelper.GetResourceSearchType(InRiverContext.Settings, InRiverContext.Logger);
            InRiverContext.Log(LogLevel.Debug, $"Asset {media.Id} search by '{resourceSearchType}'");
            
            Entity resourceEntity = EntityHelper.GetResourceByAsset(media, resourceSearchType, InRiverContext, LoadLevel.DataAndLinks);
            InRiverContext.Log(LogLevel.Debug, $"Asset {media.Id} (notification-type: {notificationType}) belongs to Resource-Entity with id: '{resourceEntity?.Id}' (empty or 0 means that a new Entity should be created!)");

            // handle notification logic
            switch (notificationType)
            {
                case NotificationType.DataUpsert:
                    return CreateOrUpdateEntityAndRelations(result, media, evaluatorResult, resourceEntity, url, filename);

                case NotificationType.MetadataUpdated:
                    if (resourceEntity != null)
                    {
                        return UpdateMetadata(result, media, resourceEntity);
                    }
                    else
                    {
                        return CreateOrUpdateEntityAndRelations(result, media, evaluatorResult, resourceEntity, url, filename);
                    }

                case NotificationType.IsArchived:
                    if (resourceEntity != null)
                    {
                        return SetValuesOnResource(result, bynderAssetId, resourceEntity);
                    }
                    else
                    {
                        InRiverContext.Log(LogLevel.Debug, $"Archived asset {bynderAssetId}, does not exist in inRiver as Resource.");
                        result.Messages.Add($"Archived asset {bynderAssetId}, does not exist in inRiver as Resource.");

                        return result;
                    }

                default:
                    InRiverContext.Log(LogLevel.Warning, $"Notification type {notificationType} is not implemented yet! This notification will not be processed for asset {bynderAssetId}.");
                    result.Messages.Add($"Notification type {notificationType} is not implemented yet! This notification will not be processed for asset {bynderAssetId}.");

                    return result;
            }
        }

        public Media GetMedia(string bynderAssetId)
        {
            var media = _bynderClient.GetAssetService().GetAssetByMediaQuery(bynderAssetId).GetAwaiter().GetResult();
            var metaProperties = _bynderClient.GetAssetService().GetMetapropertiesAsync().GetAwaiter().GetResult();

            foreach (var mp in media.MetaProperties)
            {
                mp.Id = metaProperties[mp.Name].Id;
            }

            return media;
        }

        private static bool GetConditionResult(Media asset, ImportCondition condition)
        {
            var metaproperty = asset.MetaProperties.FirstOrDefault(mp => mp.Name.Equals(condition.PropertyName)) ?? null;

            if (metaproperty == null)
            {
                return false;
            }

            var metapropertyValues = metaproperty.Values;

            // metaproperty is not included in asset, when the value is null
            if (metapropertyValues == null)
            {
                // check if there are conditions or if the only condition value is null
                if (condition.Values.Count == 0 || (condition.Values.Count == 1 && string.IsNullOrEmpty(condition.Values[0]))) return true;

                // return false, because the metaproperty does not have a value, but the condition does
                return false;
            }

            return ConditionHelper.ValuesApplyToCondition(metapropertyValues, condition);
        }

        private static void LogMessageIfMultipleValuesForSingleField(WorkerResult result, string propertyName, Field field, List<string> values, string firstVal, string mergedVal)
        {
            if (values != null && values.Count > 1)
            {
                result.Messages.Add($"Property '{propertyName}' contains multiple values, while the Field '{field.FieldType.Id}' and datatype {field.FieldType.DataType} only needs one. Taking the value '{firstVal}' of the list of values '{mergedVal}'.");
            }
        }

        private static void SetResourceFilenameData(Entity resourceEntity, Dictionary<FieldType, string> filenameData)
        {
            // resource fields from regular expression created from filename
            foreach (var keyValuePair in filenameData)
            {
                resourceEntity.GetField(keyValuePair.Key.Id).Data = keyValuePair.Value;
            }
        }

        /// <summary>
        /// get related entity data found in filename so we can create or update link to these entities
        /// all found field/values are supposed to be unique fields in the correspondent entitytype
        /// </summary>
        /// <param name="evaluatorResult"></param>
        /// <param name="resourceEntity"></param>
        /// <param name="resultString"></param>
        private string AddRelations(Dictionary<FieldType, string> relatedEntityData, Entity resourceEntity)
        {
            StringBuilder resultString = new StringBuilder();

            if (relatedEntityData.Count == 0)
            {
                resultString.Append($"; Empty relatedEntityData for ResourceEntity {resourceEntity.Id}");
                return resultString.ToString();
            }

            // get all *inbound* linktypes towards the Resource entitytype in the model(e.g.ProductResource, ItemResource NOT ResourceOtherEntity)
            var inboundResourceLinkTypes = InRiverContext.ExtensionManager.ModelService.GetLinkTypesForEntityType(EntityTypeIds.Resource)
                .Where(lt => lt.TargetEntityTypeId == EntityTypeIds.Resource).OrderBy(lt => lt.Index).ToList();

            foreach (var keyValuePair in relatedEntityData)
            {
                var fieldTypeId = keyValuePair.Key.Id;
                var value = keyValuePair.Value;

                // find sourcentity (e.g. Product)
                var sourceEntity = InRiverContext.ExtensionManager.DataService.GetEntityByUniqueValue(fieldTypeId, value, LoadLevel.Shallow);
                if (sourceEntity == null)
                {
                    resultString.Append($"; Nothing found for FieldTypeID {fieldTypeId} found for {value}");
                    continue;
                }
                else
                {
                    resultString.Append($"; Found SourceEntity-id {sourceEntity.Id} for FieldTypeID {fieldTypeId} found for {value}");
                }

                // find linktype in our previously found list
                var linkType = inboundResourceLinkTypes.FirstOrDefault(lt => lt.SourceEntityTypeId == sourceEntity.EntityType.Id);
                if (linkType == null)
                {
                    resultString.Append($"; No LinkType found for SourceEntity-id {sourceEntity.Id} [{sourceEntity.EntityType.Id}]");
                    continue;
                }

                if (!InRiverContext.ExtensionManager.DataService.LinkAlreadyExists(sourceEntity.Id, resourceEntity.Id, null, linkType.Id))
                {
                    resultString.Append($"; Adding link!");
                    InRiverContext.ExtensionManager.DataService.AddLink(new Link()
                    {
                        Source = sourceEntity,
                        Target = resourceEntity,
                        LinkType = linkType
                    });
                }

                resultString.Append($"; {sourceEntity.EntityType.Id} entity {sourceEntity.Id} found and linked");
            }

            return resultString.ToString();
        }

        /// <summary>
        /// All values need to match.
        ///
        /// When a value is null for a metaproperty on the Asset, then we don't receive the metaproperty from Bynder('s API response).
        /// When the metaproperty is not found and the condition for this property has no values or the only value is null, then it will return true.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        private bool AssetAppliesToConditions(Media asset)
        {
            var conditions = SettingHelper.GetImportConditions(InRiverContext.Settings, InRiverContext.Logger);

            // return true if no conditions found. Conditions are optional.
            if (conditions == null || conditions.Count == 0)
            {
                InRiverContext.Log(LogLevel.Debug, $"Import conditions are empty > {asset.Id} applies to conditions immediately!");
                return true;
            }

            foreach (var condition in conditions)
            {
                if (!GetConditionResult(asset, condition))
                {
                    return false;
                }
            }

            return true;
        }

        private WorkerResult CreateOrUpdateEntityAndRelations(WorkerResult result, Media asset, FilenameEvaluator.Result evaluatorResult, Entity resourceEntity, string url, string filename)
        {
            StringBuilder resultString = new StringBuilder();
            string action = resourceEntity == null ? "Create Entity" : $"Update Entity {resourceEntity.Id}";
            InRiverContext.Log(LogLevel.Verbose, $"{action}, metadata and relations for Bynder asset {asset.Id}");

            if (resourceEntity == null)
            {
                resourceEntity = CreateResourceEntity(asset, url, filename);
                result.Messages.Add($"Resource entity creation initialized (not added to inriver yet!) for asset {asset.Id}");
            }

            // get current fieldvalues so we can check the updated fields later on
            var oldFieldValues = resourceEntity.Fields.Select(x => (Field)x.Clone()).ToList();

            // always set the asset id
            resourceEntity.GetField(FieldTypeIds.ResourceBynderId).Data = asset.Id;

            // save IdHash for re-creation of public CDN Urls in inRiver
            resourceEntity.GetField(FieldTypeIds.ResourceBynderIdHash).Data = asset.IdHash;

            // status for new and existing ResourceEntity
            resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState).Data = BynderStates.Todo;

            SetAssetProperties(resourceEntity, asset, result);
            SetMetapropertyData(resourceEntity, asset, result);

            var filenameData = evaluatorResult.GetResourceDataInFilename();
            SetResourceFilenameData(resourceEntity, filenameData);

            if (resourceEntity.Id == 0)
            {
                resourceEntity = InRiverContext.ExtensionManager.DataService.AddEntity(resourceEntity);
                resultString.Append($"Resource {resourceEntity.Id} added");
            }
            else
            {
                // get updated fields
                var updatedFields = resourceEntity.Fields.Where(x => oldFieldValues.First(y => Equals(y.FieldType.Id, x.FieldType.Id)).ValueHasBeenModified(x.Data)).ToList();
                if (updatedFields.Count > 0)
                {
                    resourceEntity = InRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(updatedFields);
                    resultString.Append($"Resource {resourceEntity.Id} updated");
                }
                else
                {
                    InRiverContext.Log(LogLevel.Verbose, $"No fields to update on Resource {resourceEntity.Id} for asset {asset.Id}");
                }
            }

            var relatedEntityData = evaluatorResult.GetRelatedEntityDataInFilename();
            resultString.Append(AddRelations(relatedEntityData, resourceEntity));

            result.Messages.Add(resultString.ToString());
            return result;
        }

        private Entity CreateResourceEntity(Media asset, string url, string filename)
        {
            Entity resourceEntity = Entity.CreateEntity(ResourceEntityType);

            // var (url, filename) = MediaHelper.GetDownloadUrlAndFilename(InRiverContext, _bynderClient, asset).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(filename))
            {
                InRiverContext.Log(LogLevel.Debug, $"Filename for asset {asset.Id} is empty");
                filename = asset.Id;
            }

            var fieldTypeSettings = resourceEntity.GetField(FieldTypeIds.ResourceFilename).FieldType.Settings;
            if (ResourceFilenameRegEx is null && fieldTypeSettings != null && fieldTypeSettings.ContainsKey(FieldTypeSettingsRegExKey) && !string.IsNullOrEmpty(fieldTypeSettings[FieldTypeSettingsRegExKey]))
            {
                ResourceFilenameRegEx = new Regex(fieldTypeSettings[FieldTypeSettingsRegExKey]);
            }

            if (ResourceFilenameRegEx != null && !ResourceFilenameRegEx.Match(filename).Success)
            {
                // This validation has been added because the native exception of inriver does not include enough details
                throw new FormatException($"Value '{filename}' for Field '{FieldTypeIds.ResourceFilename}' doesn't match the required RegExp format '{fieldTypeSettings[FieldTypeSettingsRegExKey]}'");
            }

            resourceEntity.GetField(FieldTypeIds.ResourceFilename).Data = filename;

            return resourceEntity;
        }

        /// <summary>
        /// Returns the parsed cvl value.
        /// Checks if the values exist in the CVL. If not they get added if the setting CREATE_MISSING_CVL_KEYS is true.
        /// returns valid CVL values or null.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <param name="result"></param>
        /// <param name="singleVal">returns the first valid CVL value</param>
        /// <returns></returns>
        private string GeParsedCvlValueForField(Field field, List<string> values, WorkerResult result, out string singleVal)
        {
            singleVal = string.Empty;

            if (values == null || values.Count == 0)
            {
                return null;
            }

            List<string> validatedCvlKeys = new List<string>(values.Count);
            List<CVLValue> cvlValues = InRiverContext.ExtensionManager.ModelService.GetCVLValuesForCVL(field.FieldType.CVLId, true);

            foreach (string cvlKey in values)
            {
                if (string.IsNullOrWhiteSpace(cvlKey))
                {
                    continue;
                }

                if (cvlValues.Any(c => c.Key.Equals(cvlKey)))
                {
                    validatedCvlKeys.Add(cvlKey);
                    continue;
                }

                string cleanCvlKey = cvlKey.ToStringWithoutControlCharactersForCvlKey();
                if (cvlValues.Any(c => c.Key.Equals(cleanCvlKey)))
                {
                    validatedCvlKeys.Add(cleanCvlKey);
                    continue;
                }

                // create new CVL value when the setting CREATE_MISSING_CVL_KEYS is true
                if (SettingHelper.GetConfiguredCreateMissingCvlKeys(InRiverContext.Settings, InRiverContext.Logger))
                {
                    CVLValue newCvlValue = new CVLValue()
                    {
                        CVLId = field.FieldType.CVLId,
                        Key = cleanCvlKey,
                        Value = cvlKey
                    };

                    try
                    {
                        newCvlValue = InRiverContext.ExtensionManager.ModelService.AddCVLValue(newCvlValue);
                        cvlValues.Add(newCvlValue);
                        validatedCvlKeys.Add(newCvlValue.Key);
                    }
                    catch (Exception e)
                    {
                        result.Messages.Add($"Could not add CVLKey '{cleanCvlKey}' ({cvlKey}).");
                        InRiverContext.Log(LogLevel.Error, $"Could not add CVLKey '{cleanCvlKey}' ({cvlKey}).", e);
                    }
                }
                else
                {
                    result.Messages.Add($"Missing CVLKey '{cleanCvlKey}' ({cvlKey}) has not been added.");
                }
            }

            if (validatedCvlKeys.Any())
            {
                if (field.FieldType.Multivalue)
                {
                    // cvlkeys are always separated by semicolon in inRiver
                    return string.Join(";", validatedCvlKeys);
                }
                else
                {
                    singleVal = validatedCvlKeys.FirstOrDefault();
                    return singleVal;
                }
            }

            return null;
        }

        private object GetParsedValueForField(WorkerResult result, string propertyName, List<string> values, Field field)
        {
            var mergedVal = values == null ? null : string.Join(SettingHelper.GetMultivalueSeparator(InRiverContext.Settings, InRiverContext.Logger), values);
            var singleVal = values?.FirstOrDefault();

            switch (field.FieldType.DataType.ToLower())
            {
                case "localestring":
                    var languagesToSet = SettingHelper.GetLanguagesToSet(InRiverContext.Settings, InRiverContext.Logger);
                    var ls = (LocaleString)field.Data;
                    if (ls == null)
                    {
                        ls = new LocaleString(InRiverContext.ExtensionManager.UtilityService.GetAllLanguages());
                    }

                    foreach (var lang in languagesToSet)
                    {
                        var culture = new CultureInfo(lang);
                        if (!ls.ContainsCulture(culture)) continue;
                        ls[culture] = mergedVal;
                    }

                    return ls;

                case "string":
                    return mergedVal;

                case "cvl":
                    var parsedCvlVal = GeParsedCvlValueForField(field, values, result, out singleVal);
                    if (!field.FieldType.Multivalue)
                    {
                        LogMessageIfMultipleValuesForSingleField(result, propertyName, field, values, singleVal, mergedVal);
                    }
                    return parsedCvlVal;

                case "datetime":
                    LogMessageIfMultipleValuesForSingleField(result, propertyName, field, values, singleVal, mergedVal);
                    if (string.IsNullOrEmpty(singleVal))
                    {
                        return null;
                    }
                    else if (singleVal.Contains('/')) // when using the date property
                    {
                        // 07/28/2017
                        return singleVal.ConvertTo<DateTime?>(dateTimeFormat: "MM/dd/yyyy");
                    }
                    else // added this just to be sure, it is used in example outputs of the Bynder API
                    {
                        //2017-03-28T14:28:56Z yyyy-mm-ddThh:mm:ssZ (ISO 8601)
                        // grab the UTC variant of the culture invariant datetime, because Bynder writes the DateTimes for its selected culture. So the given value of the datetime is the whole truth,
                        // and does not have to be converted. The DateTime parse takes the local time so we need to grab ourself the UTC time.
                        return singleVal.ConvertTo<DateTime?>()?.ToUniversalTime();
                    }
                default:
                    LogMessageIfMultipleValuesForSingleField(result, propertyName, field, values, singleVal, mergedVal);
                    return singleVal.ConvertTo(field.FieldType.DataType);
            }
        }

        private void SetAssetProperties(Entity resourceEntity, Media asset, WorkerResult result)
        {
            InRiverContext.Log(LogLevel.Verbose, $"Setting asset properties on entity {resourceEntity.Id}");

            var propertyMap = SettingHelper.GetConfiguredAssetPropertyMap(InRiverContext.Settings, InRiverContext.Logger);
            var assetProperties = asset.GetType().GetProperties();

            foreach (var kvp in propertyMap)
            {
                var assetProperty = assetProperties.FirstOrDefault(x => x.Name.ToCamelCase().Equals(kvp.Key));
                if (assetProperty == null)
                {
                    var message = $"Property '{kvp.Key}' does not exist on an Asset!";
                    result.Messages.Add(message);
                    InRiverContext.Log(LogLevel.Warning, message);
                    continue;
                }

                var field = resourceEntity.GetField(kvp.Value);
                if (field == null)
                {
                    var message = $"Field '{kvp.Value}' does not exist on a Resource!";
                    result.Messages.Add(message);
                    InRiverContext.Log(LogLevel.Warning, message);
                    continue;
                }

                object propertyVal = assetProperty.GetValue(asset, null);

                // for now we know that the Asset only holds List<string> so we do it this way, would be nicer to add this in the ConvertTo as well
                bool isIEnumerable = propertyVal != null && propertyVal.GetType().IsIEnumerable() && !(propertyVal is string);
                List<string> values;
                if (isIEnumerable)
                {
                    values = propertyVal as List<string>;
                }
                else
                {
                    values = new List<string> { propertyVal.ConvertTo<string>() };
                }

                field.Data = GetParsedValueForField(result, assetProperty.Name, values, field);
            }

            // thumbnails | ticket #208787
            var thumbnailMappings = SettingHelper.GetFieldTypeThumbnailMappings(InRiverContext.Settings, InRiverContext.Logger);
            foreach (var thumbnailMapping in thumbnailMappings)
            {
                var thumbnailField = resourceEntity.GetField(thumbnailMapping.FieldTypeId);
                if (thumbnailField == null)
                {
                    InRiverContext.Log(LogLevel.Warning, $"Thumbnail field with fieldtype {thumbnailMapping.FieldTypeId} not found on resource entity {resourceEntity.Id}!");
                    continue;
                }

                var thumbnailUrl = GetThumbnailUrl(asset, thumbnailMapping);

                if (thumbnailUrl == null)
                {
                    InRiverContext.Log(LogLevel.Warning, $"Thumbnail url for type '{thumbnailMapping.ThumbnailType}' or fallback-type '{thumbnailMapping.FallBackThumbnailType}' is not available on resource entity {resourceEntity.Id}!");
                    continue;
                }

                thumbnailField.Data = thumbnailUrl;
            }
        }

        private void SetMetapropertyData(Entity resourceEntity, Media asset, WorkerResult result)
        {
            var metaPropertyMapping = SettingHelper.GetConfiguredMetaPropertyMap(InRiverContext.Settings, InRiverContext.Logger);
            if (metaPropertyMapping.Count == 0)
            {
                InRiverContext.Log(LogLevel.Verbose, "Could not find configured metaproperty Map");
                return;
            }

            InRiverContext.Log(LogLevel.Verbose, $"Setting metaproperties on entity {resourceEntity.Id}");

            var matchedProperties =
                asset.MetaProperties.Join(
                    metaPropertyMapping,
                    property => property.Id,
                    map => map.BynderMetaProperty,
                    (property, map) => new { property, map },
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (var matchedProperty in matchedProperties)
            {
                InRiverContext.Log(LogLevel.Debug, $"{matchedProperty.property.Name} ({matchedProperty.property.Id}) -> {matchedProperty.map.InriverFieldTypeId}");
            }

            foreach (var match in matchedProperties)
            {
                var fieldTypeId = match.map.InriverFieldTypeId;
                var field = resourceEntity.GetField(fieldTypeId);
                if (field == null)
                {
                    result.Messages.Add($"FieldType '{fieldTypeId}' in MetaPropertyMapping does not exist on Resource EntityType");
                    InRiverContext.Log(LogLevel.Warning, $"FieldType '{fieldTypeId}' does not exist on Resource EntityType");
                    continue;
                }

                field.Data = GetParsedValueForField(result, match.property.Name, match.property.Values, field);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="result"></param>
        /// <param name="bynderAssetId"></param>
        /// <param name="resourceEntity"></param>
        /// <returns></returns>
        private WorkerResult SetValuesOnResource(WorkerResult result, string bynderAssetId, Entity resourceEntity)
        {
            var fieldValueCombinations = SettingHelper.GetFieldValueCombinations(InRiverContext.Settings, InRiverContext.Logger);
            if (fieldValueCombinations.Count == 0)
            {
                InRiverContext.Log(LogLevel.Verbose, $"No fieldvalue combinations found. Not updating resource for archived bynder asset {bynderAssetId}");
                return result;
            }

            var fieldsToUpdate = new List<Field>();
            var dateTimeSettings = SettingHelper.GetDateTimeSettings(InRiverContext.Settings, InRiverContext.Logger);

            foreach (var fvc in fieldValueCombinations)
            {
                if (string.IsNullOrWhiteSpace(fvc.FieldTypeId))
                {
                    InRiverContext.Log(LogLevel.Verbose, $"Field value combination found without FieldTypeId setting filled in setting '{Settings.FieldValuesToSetOnArchiveEvent}'!");
                    continue;
                }

                var field = resourceEntity.GetField(fvc.FieldTypeId);
                if (field == null)
                {
                    InRiverContext.Log(LogLevel.Verbose, $"Field '{fvc.FieldTypeId}' used in the setting '{Settings.FieldValuesToSetOnArchiveEvent}' does not exist on Resource!");
                    continue;
                }

                if (fvc.SetTimestamp)
                {
                    if (dateTimeSettings == null)
                    {
                        InRiverContext.Log(LogLevel.Verbose, $"Field value combination found with {nameof(FieldValueCombination.SetTimestamp)} on true, but the setting '{Settings.TimestampSettings}' is empty!");
                        continue;
                    }

                    field.Data = DateTimeHelper.GetTimestamp(dateTimeSettings);
                }
                else
                {
                    field.Data = fvc.Value.ConvertTo(field.FieldType.DataType);
                }

                fieldsToUpdate.Add(field);
            }

            if (fieldsToUpdate.Count > 0)
            {
                InRiverContext.Log(LogLevel.Verbose, $"Setting values on Resource {resourceEntity.Id} for archived bynder asset {bynderAssetId}");
                resourceEntity = InRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(fieldsToUpdate);
                result.Messages.Add($"Updated field(s) on Resource {resourceEntity.Id} for archived bynder asset {bynderAssetId}");
            }
            else
            {
                InRiverContext.Log(LogLevel.Verbose, $"No fields to update on Resource {resourceEntity.Id} for archived bynder asset {bynderAssetId}");
            }

            return result;
        }

        private WorkerResult UpdateMetadata(WorkerResult result, Media asset, Entity resourceEntity)
        {
            InRiverContext.Log(LogLevel.Verbose, $"Update metadata only for Resource {resourceEntity.Id}");

            // get current fieldvalues so we can check the updated fields later on
            var oldFieldValues = resourceEntity.Fields.Select(x => (Field)x.Clone()).ToList();

            // always set the asset id and hash
            resourceEntity.GetField(FieldTypeIds.ResourceBynderId).Data = asset.Id;
            resourceEntity.GetField(FieldTypeIds.ResourceBynderIdHash).Data = asset.IdHash;

            // set other fields
            SetAssetProperties(resourceEntity, asset, result);
            SetMetapropertyData(resourceEntity, asset, result);

            // get updated fields
            var updatedFields = resourceEntity.Fields.Where(x => oldFieldValues.First(y => Equals(y.FieldType.Id, x.FieldType.Id)).ValueHasBeenModified(x.Data)).ToList();
            if (updatedFields.Count > 0)
            {
                resourceEntity = InRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(updatedFields);
                result.Messages.Add($"Resource {resourceEntity.Id} updated");
            }
            else
            {
                InRiverContext.Log(LogLevel.Verbose, $"No fields to update on Resource {resourceEntity.Id} for asset {asset.Id}");
            }

            return result;
        }

        protected string GetThumbnailUrl(Media media, FieldTypeThumbnailMapping thumbnailMapping) =>
            MediaHelper.GetThumbnailUrl(InRiverContext, media, thumbnailMapping);

        #endregion Methods
    }
}