using System.Linq;
using System.Text;
using Bynder.Api;
using Bynder.Names;
using Bynder.Utils;
using Bynder.Utils.InRiver;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;

namespace Bynder.Workers
{
    class AssetUpdatedWorker : IWorker
    {
        private readonly inRiverContext _inRiverContext;
        private readonly IBynderClient _bynderClient;
        private readonly FileNameEvaluator _fileNameEvaluator;

        public AssetUpdatedWorker(inRiverContext inRiverContext, IBynderClient bynderClient, FileNameEvaluator fileNameEvaluator)
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
                _inRiverContext.ExtensionManager.DataService.GetEntityByUniqueValue(FieldTypeId.ResourceBynderId, bynderAssetId,
                    LoadLevel.DataAndLinks);

            if (resourceEntity == null)
            {
                EntityType resourceType = _inRiverContext.ExtensionManager.ModelService.GetEntityType(EntityTypeId.Resource);
                resourceEntity = Entity.CreateEntity(resourceType);

                // add asset id to new ResourceEntity
                resourceEntity.GetField(FieldTypeId.ResourceBynderId).Data = bynderAssetId;

                // set filename (only for *new* resource)
                resourceEntity.GetField(FieldTypeId.ResourceFileName).Data = $"{bynderAssetId}_{asset.GetOriginalFileName()}";
            }

            // status for new and existing ResourceEntity
            resourceEntity.GetField(FieldTypeId.ResourceBynderDownloadState).Data = BynderState.Todo;

            // resource fields from regular expression created from filename
            foreach (var keyValuePair in evaluatorResult.GetResourceDataInFilename())
            {
                resourceEntity.GetField(keyValuePair.Key.Id).Data = keyValuePair.Value;
            }

            // save IdHash for re-creation of public CDN Urls in inRiver
            resourceEntity.GetField(FieldTypeId.ResourceBynderIdHash).Data = asset.IdHash;

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
            var inboundResourceLinkTypes = _inRiverContext.ExtensionManager.ModelService.GetLinkTypesForEntityType(EntityTypeId.Resource)
                .Where(lt => lt.TargetEntityTypeId == EntityTypeId.Resource).OrderBy(lt => lt.Index).ToList();

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

                _inRiverContext.ExtensionManager.DataService.CreateLinkIfNotExists(sourceEntity, resourceEntity, linkType);
                resultString.Append($"; {sourceEntity.EntityType.Id} entity {sourceEntity.Id} found and linked");
            }

            result.Messages.Add(resultString.ToString());
            return result;
        }
    }
}
