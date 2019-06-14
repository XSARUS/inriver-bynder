using Bynder.Api;
using Bynder.Names;
using Bynder.Utils;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using System.Linq;
using System.Text;

namespace Bynder.Workers
{
    class AssetUpdatedWorker : IWorker
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

        public WorkerResult Execute(string bynderAssetId)
        {
            var result = new WorkerResult();

            // get original filename, as we need to evaluate this for further processing
            var asset = _bynderClient.GetAssetByAssetId(bynderAssetId);

            string originalFileName = asset.GetOriginalFileName();

            // evaluate filename
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

            if (resourceEntity == null)
            {
                EntityType resourceType = _inRiverContext.ExtensionManager.ModelService.GetEntityType(EntityTypeIds.Resource);
                resourceEntity = Entity.CreateEntity(resourceType);

                // add asset id to new ResourceEntity
                resourceEntity.GetField(FieldTypeIds.ResourceBynderId).Data = bynderAssetId;

                // set filename (only for *new* resource)
                resourceEntity.GetField(FieldTypeIds.ResourceFilename).Data = $"{bynderAssetId}_{asset.GetOriginalFileName()}";
            }

            // status for new and existing ResourceEntity
            resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState).Data = BynderStates.Todo;

            // resource fields from regular expression created from filename
            foreach (var keyValuePair in evaluatorResult.GetResourceDataInFilename())
            {
                resourceEntity.GetField(keyValuePair.Key.Id).Data = keyValuePair.Value;
            }

            // save IdHash for re-creation of public CDN Urls in inRiver
            resourceEntity.GetField(FieldTypeIds.ResourceBynderIdHash).Data = asset.IdHash;

            var resultString = new StringBuilder();
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

            // get related entity data found in filename so we can create or update link to these entities
            // all found field/values are supposed to be unique fields in the correspondent entitytype
            // get all *inbound* linktypes towards the Resource entitytype in the model (e.g. ProductResource, ItemResource NOT ResourceOtherEntity)
            var inboundResourceLinkTypes = _inRiverContext.ExtensionManager.ModelService.GetLinkTypesForEntityType(EntityTypeIds.Resource)
                .Where(lt => lt.TargetEntityTypeId == EntityTypeIds.Resource).OrderBy(lt => lt.Index).ToList();

            foreach (var keyValuePair in evaluatorResult.GetRelatedEntityDataInFilename())
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

            result.Messages.Add(resultString.ToString());
            return result;
        }
    }
}
