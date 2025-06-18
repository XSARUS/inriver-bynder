using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Bynder.Workers
{
    using Api;
    using Api.Model;
    using Config;
    using Enums;
    using Models;
    using Names;
    using Utils;
    using Utils.Extensions;
    using Utils.Helpers;

    public class AssetUpdatedWorker : IWorker
    {
        #region Fields

        private readonly IBynderClient _bynderClient;
        private readonly FilenameEvaluator _fileNameEvaluator;
        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public AssetUpdatedWorker(inRiverContext inRiverContext, IBynderClient bynderClient, FilenameEvaluator fileNameEvaluator)
        {
            _inRiverContext = inRiverContext;
            _bynderClient = bynderClient;
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
            var asset = _bynderClient.GetAssetByAssetId(bynderAssetId);

            // evaluate filename
            string originalFileName = asset.GetOriginalFileName();
            var evaluatorResult = _fileNameEvaluator.Evaluate(originalFileName);
            if (!evaluatorResult.IsMatch())
            {
                result.Messages.Add($"Not processing '{originalFileName}'; does not match regex.");
                return result;
            }

            // evaluate conditions
            if (!AssetAppliesToConditions(asset))
            {
                _inRiverContext.Log(LogLevel.Debug, $"Asset {bynderAssetId} does not apply to the conditions");

                result.Messages.Add($"Not processing '{originalFileName}'; does not apply to import conditions.");
                return result;
            }

            _inRiverContext.Log(LogLevel.Debug, $"Asset {asset.Id} applies to conditions.");

            var resourceSearchType = GetResourceSearchType();
            Entity resourceEntity = EntityHelper.GetResourceByAsset(asset, resourceSearchType, _inRiverContext.ExtensionManager.DataService, LoadLevel.DataAndLinks);

            // handle notification logic
            switch (notificationType)
            {
                case NotificationType.DataUpsert:
                    return CreateOrUpdateEntityAndRelations(result, asset, evaluatorResult, resourceEntity);

                case NotificationType.MetadataUpdated:
                    if (resourceEntity != null)
                    {
                        return UpdateMetadata(result, asset, resourceEntity);
                    }
                    else
                    {
                        return CreateOrUpdateEntityAndRelations(result, asset, evaluatorResult, resourceEntity);
                    }

                case NotificationType.IsArchived:
                    if (resourceEntity != null)
                    {
                        return SetValuesOnResource(result, bynderAssetId, resourceEntity);
                    }
                    else
                    {
                        _inRiverContext.Log(LogLevel.Debug, $"Archived asset {bynderAssetId}, does not exist in inRiver as Resource.");
                        result.Messages.Add($"Archived asset {bynderAssetId}, does not exist in inRiver as Resource.");

                        return result;
                    }

                default:
                    _inRiverContext.Log(LogLevel.Warning, $"Notification type {notificationType} is not implemented yet! This notification will not be processed for asset {bynderAssetId}.");
                    result.Messages.Add($"Notification type {notificationType} is not implemented yet! This notification will not be processed for asset {bynderAssetId}.");

                    return result;
            }
        }

        private Entity AddOrUpdateEntityInInRiver(Entity resourceEntity, StringBuilder resultString)
        {
            if (resourceEntity.Id == 0)
            {
                resourceEntity = _inRiverContext.ExtensionManager.DataService.AddEntity(resourceEntity);
                resultString.Append($"Resource {resourceEntity.Id} added");
            }
            else
            {
                resourceEntity = _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
                resultString.Append($"Resource {resourceEntity.Id} updated");
            }

            return resourceEntity;
        }

        /// <summary>
        /// get related entity data found in filename so we can create or update link to these entities
        /// all found field/values are supposed to be unique fields in the correspondent entitytype
        /// </summary>
        /// <param name="evaluatorResult"></param>
        /// <param name="resourceEntity"></param>
        /// <param name="resultString"></param>
        private void AddRelations(Dictionary<FieldType, string> relatedEntityData, Entity resourceEntity, StringBuilder resultString)
        {
            if (relatedEntityData.Count == 0)
            {
                return;
            }

            // get all *inbound* linktypes towards the Resource entitytype in the model(e.g.ProductResource, ItemResource NOT ResourceOtherEntity)
            var inboundResourceLinkTypes = _inRiverContext.ExtensionManager.ModelService.GetLinkTypesForEntityType(EntityTypeIds.Resource)
                .Where(lt => lt.TargetEntityTypeId == EntityTypeIds.Resource).OrderBy(lt => lt.Index).ToList();

            foreach (var keyValuePair in relatedEntityData)
            {
                var fieldTypeId = keyValuePair.Key.Id;
                var value = keyValuePair.Value;

                // find sourcentity (e.g. Product)
                var sourceEntity = _inRiverContext.ExtensionManager.DataService.GetEntityByUniqueValue(fieldTypeId, value, LoadLevel.Shallow);
                if (sourceEntity == null) continue;

                // find linktype in our previously found list
                var linkType =
                    inboundResourceLinkTypes.FirstOrDefault(lt => lt.SourceEntityTypeId == sourceEntity.EntityType.Id);
                if (linkType == null) continue;

                if (!_inRiverContext.ExtensionManager.DataService.LinkAlreadyExists(sourceEntity.Id, resourceEntity.Id, null, linkType.Id))
                {
                    _inRiverContext.ExtensionManager.DataService.AddLink(new Link()
                    {
                        Source = sourceEntity,
                        Target = resourceEntity,
                        LinkType = linkType
                    });
                }

                resultString.Append($"; {sourceEntity.EntityType.Id} entity {sourceEntity.Id} found and linked");
            }
        }

        /// <summary>
        /// All values need to match.
        ///
        /// When a value is null for a metaproperty on the Asset, then we don't receive the metaproperty from Bynder('s API response).
        /// When the metaproperty is not found and the condition for this property has no values or the only value is null, then it will return true.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        private bool AssetAppliesToConditions(Asset asset)
        {
            var conditions = GetImportConditions();

            // return true if no conditions found. Conditions are optional.
            if (conditions.Count == 0) return true;

            foreach (var condition in conditions)
            {
                if (!GetConditionResult(asset, condition)) return false;
            }

            return true;
        }

        private WorkerResult CreateOrUpdateEntityAndRelations(WorkerResult result, Asset asset, FilenameEvaluator.Result evaluatorResult, Entity resourceEntity)
        {
            _inRiverContext.Log(LogLevel.Verbose, $"Create or update entity, metadata and relations for bynder asset {asset.Id}");

            if (resourceEntity == null)
            {
                resourceEntity = CreateResourceEntity(asset);
            }

            // always set the asset id
            resourceEntity.GetField(FieldTypeIds.ResourceBynderId).Data = asset.Id;

            SetAssetProperties(resourceEntity, asset, result);
            SetMetapropertyData(resourceEntity, asset, result);

            var filenameData = evaluatorResult.GetResourceDataInFilename();
            SetResourceFilenameData(resourceEntity, asset, filenameData);

            var resultString = new StringBuilder();
            resourceEntity = AddOrUpdateEntityInInRiver(resourceEntity, resultString);

            var relatedEntityData = evaluatorResult.GetRelatedEntityDataInFilename();
            AddRelations(relatedEntityData, resourceEntity, resultString);

            result.Messages.Add(resultString.ToString());
            return result;
        }

        private Entity CreateResourceEntity(Asset asset)
        {
            Entity resourceEntity;
            EntityType resourceType = _inRiverContext.ExtensionManager.ModelService.GetEntityType(EntityTypeIds.Resource);
            resourceEntity = Entity.CreateEntity(resourceType);

            // set filename (only for *new* resource)
            string filename = asset.GetOriginalFileName();
            if (string.IsNullOrEmpty(filename))
            {
                _inRiverContext.Log(LogLevel.Debug, $"Filename for asset {asset.Id} is empty");
            }

            if (ShouldAddAssetIdPrefixToFilename())
            {
                filename = $"{asset.Id}_{filename}";
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
            List<CVLValue> cvlValues = _inRiverContext.ExtensionManager.ModelService.GetCVLValuesForCVL(field.FieldType.CVLId, true);

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
                if (GetConfiguredCreateMissingCvlKeys())
                {
                    CVLValue newCvlValue = new CVLValue()
                    {
                        CVLId = field.FieldType.CVLId,
                        Key = cleanCvlKey,
                        Value = cvlKey
                    };

                    try
                    {
                        newCvlValue = _inRiverContext.ExtensionManager.ModelService.AddCVLValue(newCvlValue);
                        cvlValues.Add(newCvlValue);
                        validatedCvlKeys.Add(newCvlValue.Key);
                    }
                    catch (Exception e)
                    {
                        result.Messages.Add($"Could not add CVLKey '{cleanCvlKey}' ({cvlKey}).");
                        _inRiverContext.Log(LogLevel.Error, $"Could not add CVLKey '{cleanCvlKey}' ({cvlKey}).", e);
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

        private bool GetConditionResult(Asset asset, ImportCondition condition)
        {
            var metaproperty = asset.MetaProperties.FirstOrDefault(x => x.Name.Equals(condition.PropertyName));

            // metaproperty is not included in asset, when the value is null
            if (metaproperty == null)
            {
                // check if there are conditions or if the only condition value is null
                if (condition.Values.Count == 0 || (condition.Values.Count == 1 && condition.Values.First() == null)) return true;

                // return false, because the metaproperty does not have a value, but the condition does
                return false;
            }

            switch (condition.MatchType)
            {
                case MatchType.EqualSorted:
                    // sort the values
                    metaproperty.Values.Sort();
                    condition.Values.Sort();
                    // check if lists are equal
                    return Enumerable.SequenceEqual(metaproperty.Values, condition.Values, StringComparer.Ordinal);

                case MatchType.EqualSortedCaseInsensitive:
                    // sort the values
                    metaproperty.Values.Sort();
                    condition.Values.Sort();
                    // check if lists are equal
                    return Enumerable.SequenceEqual(metaproperty.Values, condition.Values, StringComparer.OrdinalIgnoreCase);

                case MatchType.Equal:
                    return Enumerable.SequenceEqual(metaproperty.Values, condition.Values, StringComparer.Ordinal);

                case MatchType.EqualCaseInsensitive:
                    return Enumerable.SequenceEqual(metaproperty.Values, condition.Values, StringComparer.OrdinalIgnoreCase);

                case MatchType.ContainsAny:
                    return metaproperty.Values.Intersect(condition.Values).Any();

                case MatchType.ContainsAnyCaseInsensitive:
                    return metaproperty.Values.Select(x => x.ToLower()).Intersect(condition.Values.Select(x => x.ToLower())).Any();

                case MatchType.ContainsAll:
                    return condition.Values.All(x => metaproperty.Values.Contains(x));

                case MatchType.ContainsAllCaseInsensitive:
                    var metapropertyValuesLowerCase = metaproperty.Values.Select(x => x.ToLower());
                    var conditionValuesLowerCase = condition.Values.Select(x => x.ToLower());
                    return conditionValuesLowerCase.All(x => metapropertyValuesLowerCase.Contains(x));

                default:
                    throw new NotSupportedException($"MatchType {condition.MatchType} is not yet supported to use for the import conditions!");
            }
        }

        /// <summary>
        /// Optional setting. Default is an empty dictionary
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetConfiguredAssetPropertyMap()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.AssetPropertyMap))
            {
                return _inRiverContext.Settings[Settings.AssetPropertyMap].ToDictionary<string, string>(',', '=');
            }
            _inRiverContext.Logger.Log(LogLevel.Verbose, "Could not find configured asset property Map");
            return new Dictionary<string, string>();
        }

        private bool ShouldAddAssetIdPrefixToFilename()
        {
            if (_inRiverContext.Settings.TryGetValue(Settings.AddAssetIdPrefixToFilenameOfNewResource, out string setting))
            {
                return string.Equals(setting, true.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }

            // default true for backwards compatiblity
            return true;
        }

        private ResourceSearchType GetResourceSearchType()
        {
            if (_inRiverContext.Settings.TryGetValue(Settings.ResourceSearchType, out string setting))
            {
                return setting.ToEnum<ResourceSearchType>();
            }

            // default ResourceSearchType.AssetId for backwards compatiblity
            return ResourceSearchType.AssetId;
        }

        /// <summary>
        /// Optional setting. Default is false.
        /// </summary>
        /// <returns></returns>
        private bool GetConfiguredCreateMissingCvlKeys()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.CreateMissingCvlKeys))
            {
                return string.Equals(_inRiverContext.Settings[Settings.CreateMissingCvlKeys], true.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }

            _inRiverContext.Logger.Log(LogLevel.Error, $"Could not find configuration for '{Settings.CreateMissingCvlKeys}'");

            return false;
        }

        /// <summary>
        /// Optional setting. Default is an empty dictionary.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetConfiguredMetaPropertyMap()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.MetapropertyMap))
            {
                return _inRiverContext.Settings[Settings.MetapropertyMap].ToDictionary<string, string>(',', '=');
            }
            _inRiverContext.Logger.Log(LogLevel.Verbose, "Could not find configured metaproperty Map");
            return new Dictionary<string, string>();
        }

        private DateTimeSettings GetDateTimeSettings()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.TimestampSettings))
            {
                return JsonConvert.DeserializeObject<DateTimeSettings>(_inRiverContext.Settings[Settings.TimestampSettings]);
            }
            _inRiverContext.Logger.Log(LogLevel.Verbose, $"Could not find configured {Settings.TimestampSettings}");
            return null;
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        private List<FieldValueCombination> GetFieldValueCombinations()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.FieldValuesToSetOnArchiveEvent))
            {
                return JsonConvert.DeserializeObject<List<FieldValueCombination>>(_inRiverContext.Settings[Settings.FieldValuesToSetOnArchiveEvent]);
            }
            _inRiverContext.Logger.Log(LogLevel.Verbose, $"Could not find configured {Settings.FieldValuesToSetOnArchiveEvent}");
            return new List<FieldValueCombination>();
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        private List<ImportCondition> GetImportConditions()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.ImportConditions))
            {
                return JsonConvert.DeserializeObject<List<ImportCondition>>(_inRiverContext.Settings[Settings.ImportConditions]);
            }
            _inRiverContext.Logger.Log(LogLevel.Verbose, $"Could not find configured {Settings.ImportConditions}");
            return new List<ImportCondition>();
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetLanguagesToSet()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.LocaleStringLanguagesToSet))
            {
                return _inRiverContext.Settings[Settings.LocaleStringLanguagesToSet].ConvertTo<IEnumerable<string>>() ?? new List<string>();
            }
            _inRiverContext.Logger.Log(LogLevel.Verbose, "Could not find LocaleString languages to set");
            return new List<string>();
        }

        /// <summary>
        /// Optional setting. Default is an empty string.
        /// </summary>
        /// <returns></returns>
        private string GetMultivalueSeparator()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.MultivalueSeparator))
            {
                return _inRiverContext.Settings[Settings.MultivalueSeparator];
            }
            _inRiverContext.Logger.Log(LogLevel.Verbose, "Could not find configured multivalue separator");
            return string.Empty;
        }

        private object GetParsedValueForField(WorkerResult result, string propertyName, List<string> values, Field field)
        {
            var mergedVal = values == null ? null : string.Join(GetMultivalueSeparator(), values);
            var singleVal = values?.FirstOrDefault();

            switch (field.FieldType.DataType.ToLower())
            {
                case "localestring":
                    var languagesToSet = GetLanguagesToSet();
                    var ls = (LocaleString)field.Data;
                    if (ls == null)
                    {
                        ls = new LocaleString(_inRiverContext.ExtensionManager.UtilityService.GetAllLanguages());
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

        private void LogMessageIfMultipleValuesForSingleField(WorkerResult result, string propertyName, Field field, List<string> values, string firstVal, string mergedVal)
        {
            if (values != null && values.Count > 1)
            {
                result.Messages.Add($"Property '{propertyName}' contains multiple values, while the Field '{field.FieldType.Id}' and datatype {field.FieldType.DataType} only needs one. Taking the value '{firstVal}' of the list of values '{mergedVal}'.");
            }
        }

        private void SetAssetProperties(Entity resourceEntity, Asset asset, WorkerResult result)
        {
            _inRiverContext.Log(LogLevel.Verbose, $"Setting asset properties on entity {resourceEntity.Id}");

            var propertyMap = GetConfiguredAssetPropertyMap();
            var assetProperties = asset.GetType().GetProperties();

            foreach (var kvp in propertyMap)
            {
                var assetProperty = assetProperties.FirstOrDefault(x => x.Name.ToCamelCase().Equals(kvp.Key));
                if (assetProperty == null)
                {
                    var message = $"Property '{kvp.Key}' does not exist on an Asset!";
                    result.Messages.Add(message);
                    _inRiverContext.Log(LogLevel.Warning, message);
                    continue;
                }

                var field = resourceEntity.GetField(kvp.Value);
                if (field == null)
                {
                    var message = $"Field '{kvp.Value}' does not exist on a Resource!";
                    result.Messages.Add(message);
                    _inRiverContext.Log(LogLevel.Warning, message);
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
        }

        private void SetMetapropertyData(Entity resourceEntity, Asset asset, WorkerResult result)
        {
            _inRiverContext.Log(LogLevel.Verbose, $"Setting metaproperties on entity {resourceEntity.Id}");

            var metaPropertyMapping = GetConfiguredMetaPropertyMap();
            var metaPropertiesToProcess = asset.MetaProperties.Where(property => metaPropertyMapping.ContainsKey(property.Name) && metaPropertyMapping[property.Name] != null);
            foreach (var property in metaPropertiesToProcess)
            {
                var fieldTypeId = metaPropertyMapping[property.Name];
                var field = resourceEntity.GetField(fieldTypeId);
                if (field == null)
                {
                    result.Messages.Add($"FieldType '{fieldTypeId}' in MetaPropertyMapping does not exist on Resource EntityType");
                    _inRiverContext.Logger.Log(LogLevel.Warning, $"FieldType '{fieldTypeId}' does not exist on Resource EntityType");
                    continue;
                }

                field.Data = GetParsedValueForField(result, property.Name, property.Values, field);
            }
        }

        private void SetResourceFilenameData(Entity resourceEntity, Asset asset, Dictionary<FieldType, string> filenameData)
        {
            // status for new and existing ResourceEntity
            resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState).Data = BynderStates.Todo;

            // resource fields from regular expression created from filename
            foreach (var keyValuePair in filenameData)
            {
                resourceEntity.GetField(keyValuePair.Key.Id).Data = keyValuePair.Value;
            }

            // save IdHash for re-creation of public CDN Urls in inRiver
            resourceEntity.GetField(FieldTypeIds.ResourceBynderIdHash).Data = asset.IdHash;
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
            var fieldValueCombinations = GetFieldValueCombinations();
            if (fieldValueCombinations.Count == 0)
            {
                _inRiverContext.Log(LogLevel.Verbose, $"No fieldvalue combinations found. Not updating resource for archived bynder asset {bynderAssetId}");
                return result;
            }

            var fieldsToUpdate = new List<Field>();
            var dateTimeSettings = GetDateTimeSettings();

            foreach (var fvc in fieldValueCombinations)
            {
                if (string.IsNullOrWhiteSpace(fvc.FieldTypeId))
                {
                    _inRiverContext.Log(LogLevel.Verbose, $"Field value combination found without FieldTypeId setting filled in setting '{Settings.FieldValuesToSetOnArchiveEvent}'!");
                    continue;
                }

                var field = resourceEntity.GetField(fvc.FieldTypeId);
                if (field == null)
                {
                    _inRiverContext.Log(LogLevel.Verbose, $"Field '{fvc.FieldTypeId}' used in the setting '{Settings.FieldValuesToSetOnArchiveEvent}' does not exist on Resource!");
                    continue;
                }

                if (fvc.SetTimestamp)
                {
                    if (dateTimeSettings == null)
                    {
                        _inRiverContext.Log(LogLevel.Verbose, $"Field value combination found with {nameof(FieldValueCombination.SetTimestamp)} on true, but the setting '{Settings.TimestampSettings}' is empty!");
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
                _inRiverContext.Log(LogLevel.Verbose, $"Setting values on Resource {resourceEntity.Id} for archived bynder asset {bynderAssetId}");
                resourceEntity = _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(fieldsToUpdate);
                result.Messages.Add($"Updated field(s) on Resource {resourceEntity.Id} for archived bynder asset {bynderAssetId}");
            }
            else
            {
                _inRiverContext.Log(LogLevel.Verbose, $"No fields to update on Resource {resourceEntity.Id} for archived bynder asset {bynderAssetId}");
            }

            return result;
        }

        private WorkerResult UpdateMetadata(WorkerResult result, Asset asset, Entity resourceEntity)
        {
            _inRiverContext.Log(LogLevel.Verbose, $"Update metadata only for Resource {resourceEntity.Id}");

            // always set the asset id
            resourceEntity.GetField(FieldTypeIds.ResourceBynderId).Data = asset.Id;

            SetAssetProperties(resourceEntity, asset, result);
            SetMetapropertyData(resourceEntity, asset, result);
            resourceEntity = _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
            result.Messages.Add($"Resource {resourceEntity.Id} updated");
            return result;
        }

        #endregion Methods
    }
}