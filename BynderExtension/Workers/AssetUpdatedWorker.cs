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
    using Models;
    using Names;
    using Utils;
    using Utils.Extensions;

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

        private static void SetResourceFilenameData(Entity resourceEntity, Asset asset, Dictionary<FieldType, string> filenameData)
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

        private WorkerResult CreateOrUpdateEntityAndRelations(string bynderAssetId, WorkerResult result, Asset asset, FilenameEvaluator.Result evaluatorResult, Entity resourceEntity)
        {
            _inRiverContext.Log(LogLevel.Verbose, "Create or update entity, metadata and relations");

            if (resourceEntity == null)
            {
                resourceEntity = CreateResourceEntity(bynderAssetId, asset);
            }

            SetAssetProperties(resourceEntity, asset, result);
            SetMetapropertyData(resourceEntity, asset, result);

            var filenameData = evaluatorResult.GetResourceDataInFilename();
            SetResourceFilenameData(resourceEntity, asset, filenameData);

            // todo why stringbuilder, why not add the messages to the workerResult?
            var resultString = new StringBuilder();
            resourceEntity = AddOrUpdateEntityInInRiver(resourceEntity, resultString);

            var relatedEntityData = evaluatorResult.GetRelatedEntityDataInFilename();
            AddRelations(relatedEntityData, resourceEntity, resultString);

            result.Messages.Add(resultString.ToString());
            return result;
        }

        private Entity CreateResourceEntity(string bynderAssetId, Asset asset)
        {
            Entity resourceEntity;
            EntityType resourceType = _inRiverContext.ExtensionManager.ModelService.GetEntityType(EntityTypeIds.Resource);
            resourceEntity = Entity.CreateEntity(resourceType);

            // add asset id to new ResourceEntity
            resourceEntity.GetField(FieldTypeIds.ResourceBynderId).Data = bynderAssetId;

            // set filename (only for *new* resource)
            resourceEntity.GetField(FieldTypeIds.ResourceFilename).Data = $"{bynderAssetId}_{asset.GetOriginalFileName()}";
            return resourceEntity;
        }

        private Dictionary<string, string> GetConfiguredAssetPropertyMap()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.AssetPropertyMap))
            {
                return _inRiverContext.Settings[Settings.AssetPropertyMap].ToDictionary<string, string>(',', '=');
            }
            _inRiverContext.Logger.Log(LogLevel.Error, "Could not find configured asset property Map");
            return new Dictionary<string, string>();
        }
        private List<ImportCondition> GetImportConditions()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.ImportConditions))
            {
                return JsonConvert.DeserializeObject<List<ImportCondition>>(_inRiverContext.Settings[Settings.ImportConditions]);
            }
            _inRiverContext.Logger.Log(LogLevel.Error, $"Could not find configured {Settings.ImportConditions}");
            return new List<ImportCondition>();
        }

        private Dictionary<string, string> GetConfiguredMetaPropertyMap()
        {
            if (_inRiverContext.Settings.ContainsKey(Config.Settings.MetapropertyMap))
            {
                return _inRiverContext.Settings[Config.Settings.MetapropertyMap].ToDictionary<string, string>(',', '=');
            }
            _inRiverContext.Logger.Log(LogLevel.Error, "Could not find configured metaproperty Map");
            return new Dictionary<string, string>();
        }

        private IEnumerable<string> GetLanguagesToSet()
        {
            if (_inRiverContext.Settings.ContainsKey(Config.Settings.LocaleStringLanguagesToSet))
            {
                return _inRiverContext.Settings[Config.Settings.LocaleStringLanguagesToSet].ConvertTo<IEnumerable<string>>() ?? new List<string>();
            }
            _inRiverContext.Logger.Log(LogLevel.Error, "Could not find LocaleString languages to set");
            return new List<string>();
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
                bool isIEnumerable = propertyVal != null && propertyVal.GetType().IsIEnumerable() && propertyVal.GetType() != typeof(string);
                List<string> values;
                if (isIEnumerable)
                {
                    values = propertyVal as List<string>;
                }
                else
                {
                    values = new List<string> { propertyVal.ConvertTo<string>() };
                }

                var mergedVal = string.Join(_inRiverContext.Settings[Settings.MultivalueSeparator], values);
                var singleVal = values.FirstOrDefault();

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

                        field.Data = ls;
                        break;
                    case "string":
                        field.Data = mergedVal;
                        break;
                    case "cvl":
                        if (field.FieldType.Multivalue)
                        {
                            field.Data = string.Join(";", values);
                        }
                        else
                        {
                            LogMessageIfMultipleValuesForSingleField(result, assetProperty.Name, field, values, singleVal, mergedVal);
                            field.Data = singleVal;
                        }
                        break;
                    case "datetime":
                        LogMessageIfMultipleValuesForSingleField(result, assetProperty.Name, field, values, singleVal, mergedVal);
                        if (string.IsNullOrEmpty(singleVal))
                        {
                            field.Data = null;
                        }
                        else
                        {
                            // 2017-03-28T14:28:56Z yyyy-mm-ddThh:mm:ssZ (ISO 8601)
                            // grab the UTC variant of the culture invariant datetime, because Bynder writes the DateTimes for its selected culture. So the given value of the datetime is the whole truth,
                            // and does not have to be converted. The DateTime parse takes the local time so we need to grab ourself the UTC time.
                            field.Data = singleVal.ConvertTo<DateTime?>()?.ToUniversalTime();
                        }
                        break;
                    default:
                        LogMessageIfMultipleValuesForSingleField(result, assetProperty.Name, field, values, singleVal, mergedVal);
                        field.Data = singleVal.ConvertTo(field.FieldType.DataType);
                        break;
                }
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

                field.Data = GetParsedMetadataValueForField(result, property, field);
            }
        }

        /// <summary>
        /// All values need to match.
        /// 
        /// When a value is null for a metaproperty on the Asset, then we don't receive the metaproperty from Bynder('s API response). 
        /// When the metaproperty is not found and the condition for this property has no values or any value is null, then it will return true.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        private bool AssetAppliesToConditions(Asset asset)
        {
            var conditions = GetImportConditions();

            // return true if no conditions found. Conditions are optional.
            if (conditions.Count == 0) return true;

            foreach(var condition in conditions)
            {
                if (!GetConditionResult(asset, condition)) return false;
            }

            return true;
        }

        private bool GetConditionResult(Asset asset, ImportCondition condition)
        {
            var metaproperty = asset.MetaProperties.FirstOrDefault(x => x.Name.Equals(condition.PropertyName));

            // metaproperty is not included in asset, when the value is null
            if (metaproperty == null)
            {
                // check if there are conditions or if a condition value is null
                if (condition.Values.Count == 0 || condition.Values.Any(x=> x == null)) return true;

                // return false, because the metaproperty does not have a value, but the condition does
                return false;
            }

            // sort the values
            metaproperty.Values.Sort();
            condition.Values.Sort();

            // check if the sorted values are equal
            return Enumerable.SequenceEqual(metaproperty.Values, condition.Values);
        }

        private object GetParsedMetadataValueForField(WorkerResult result, Metaproperty property, Field field)
        {
            var mergedVal = property.Values == null ? null : string.Join(_inRiverContext.Settings[Settings.MultivalueSeparator], property.Values);
            var singleVal = property.Values.FirstOrDefault();

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
                    if (field.FieldType.Multivalue)
                    {
                        return property.Values == null ? null : string.Join(";", property.Values);
                    }
                    else
                    {
                        LogMessageIfMultipleValuesForSingleField(result, property.Name, field, property.Values, singleVal, mergedVal);
                        return singleVal;
                    }
                case "datetime":
                    LogMessageIfMultipleValuesForSingleField(result, property.Name, field, property.Values, singleVal, mergedVal);
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
                    LogMessageIfMultipleValuesForSingleField(result, property.Name, field, property.Values, singleVal, mergedVal);
                    return singleVal.ConvertTo(field.FieldType.DataType);
            }
        }

        private WorkerResult UpdateMetadata(WorkerResult result, Asset asset, Entity resourceEntity)
        {
            _inRiverContext.Log(LogLevel.Verbose, $"Update metadata only for Resource {resourceEntity.Id}");
            SetAssetProperties(resourceEntity, asset, result);
            SetMetapropertyData(resourceEntity, asset, result);
            resourceEntity = _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
            result.Messages.Add($"Resource {resourceEntity.Id} updated");
            return result;
        }

        /// <summary>
        /// Main method of the worker
        /// </summary>
        /// <param name="bynderAssetId"></param>
        /// <param name="onlyUpdateMetadataHasUpdated"></param>
        /// <returns></returns>
        public WorkerResult Execute(string bynderAssetId, bool onlyUpdateMetadataHasUpdated)
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

            // find resourceEntity based on bynderAssetId
            Entity resourceEntity =
                _inRiverContext.ExtensionManager.DataService.GetEntityByUniqueValue(FieldTypeIds.ResourceBynderId, bynderAssetId,
                    LoadLevel.DataAndLinks);

            // only update metadata
            if (resourceEntity != null && onlyUpdateMetadataHasUpdated)
            {
                return UpdateMetadata(result, asset, resourceEntity);
            }

            return CreateOrUpdateEntityAndRelations(bynderAssetId, result, asset, evaluatorResult, resourceEntity);
        }

        #endregion Methods

    }
}
