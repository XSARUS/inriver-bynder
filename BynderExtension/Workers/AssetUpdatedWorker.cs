using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Bynder.Workers
{
    using Api;
    using Api.Model;
    using Names;
    using Utils;
    using Utils.Extensions;

    public class AssetUpdatedWorker : IWorker
    {
        private readonly inRiverContext _inRiverContext;
        private readonly IBynderClient _bynderClient;
        private readonly FilenameEvaluator _fileNameEvaluator;

        public AssetUpdatedWorker(inRiverContext inRiverContext, IBynderClient bynderClient, FilenameEvaluator fileNameEvaluator)
        {
            _inRiverContext = inRiverContext;
            _bynderClient = bynderClient;
            _fileNameEvaluator = fileNameEvaluator;
        }


        public void SetMetapropertyData(Entity resourceEntity, Asset asset, WorkerResult result)
        {
            _inRiverContext.Log(LogLevel.Verbose, "Setting metaproperties on entity");

            var metaPropertyMapping = GetConfiguredMetaPropertyMap();
            var metaPropertiesToProcess = asset.MetaProperties.Where(property => metaPropertyMapping.ContainsKey(property.Name) && metaPropertyMapping[property.Name] != null);
            foreach(var property in metaPropertiesToProcess)
            {
                var fieldTypeId = metaPropertyMapping[property.Name];
                var field = resourceEntity.GetField(fieldTypeId);
                if (field == null)
                {
                    result.Messages.Add($"FieldType '{fieldTypeId}' in MetaPropertyMapping does not exist on Resource EntityType");
                    _inRiverContext.Logger.Log(LogLevel.Warning, $"FieldType '{fieldTypeId}' does not exist on Resource EntityType");
                    continue;
                }

                switch (field.FieldType.DataType.ToLower())
                {
                    case "localestring":
                        var languagesToSet = GetLanguagesToSet();
                        var ls = (LocaleString)field.Data;
                        if(ls == null)
                        {
                            ls = new LocaleString(_inRiverContext.ExtensionManager.UtilityService.GetAllLanguages());
                        }

                        foreach (var lang in languagesToSet)
                        {
                            var culture = new CultureInfo(lang);
                            if (!ls.ContainsCulture(culture)) continue;
                            ls[culture] = property.Value;
                        }

                        field.Data = ls;
                        break;
                    case "datetime":
                        if (property.Value.Contains('/')) // when using the date property
                        {
                            // 07/28/2017
                            field.Data = property.Value.ConvertTo<DateTime>(null, null, "MM/dd/yyyy");
                        }
                        else // added this just to be sure, it is used in example outputs of the Bynder API
                        {
                            //2017-03-28T14:28:56Z
                            field.Data = property.Value.ConvertTo<DateTime>(null, null, "yyyy-MM-ddTHH:mm:ssZ");
                        }
                        break;
                    default:
                        field.Data = property.Value.ConvertTo(field.FieldType.DataType);
                        break;
                }
            }
        }

        public WorkerResult Execute(string bynderAssetId, bool onlyUpdateMetadataHasUpdated)
        {
            _inRiverContext.Log(LogLevel.Verbose, "Only metadata updated: " + onlyUpdateMetadataHasUpdated);

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

        private WorkerResult UpdateMetadata(WorkerResult result, Asset asset, Entity resourceEntity)
        {
            _inRiverContext.Log(LogLevel.Verbose, "Update metadata only");

            SetMetapropertyData(resourceEntity, asset, result);
            resourceEntity = _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
            result.Messages.Add($"Resource {resourceEntity.Id} updated");
            return result;
        }

        private WorkerResult CreateOrUpdateEntityAndRelations(string bynderAssetId, WorkerResult result, Asset asset, FilenameEvaluator.Result evaluatorResult, Entity resourceEntity)
        {
            _inRiverContext.Log(LogLevel.Verbose, "Create or update entity, metadata and relations");

            if (resourceEntity == null)
            {
                resourceEntity = CreateResourceEntity(bynderAssetId, asset);
            }

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

        /// <summary>
        /// get related entity data found in filename so we can create or update link to these entities
        /// all found field/values are supposed to be unique fields in the correspondent entitytype
        /// </summary>
        /// <param name="evaluatorResult"></param>
        /// <param name="resourceEntity"></param>
        /// <param name="resultString"></param>
        private void AddRelations(Dictionary<FieldType, string> relatedEntityData, Entity resourceEntity, StringBuilder resultString)
        {
            if(relatedEntityData.Count == 0)
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

        public Dictionary<string, string> GetConfiguredMetaPropertyMap()
        {
            if (_inRiverContext.Settings.ContainsKey(Config.Settings.MetapropertyMap))
            {
                return _inRiverContext.Settings[Config.Settings.MetapropertyMap]
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split('='))
                    .ToDictionary(x => x[0], y => y[1]);
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
    }
}
