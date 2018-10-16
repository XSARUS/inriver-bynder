using System.Linq;
using Bynder.Names;
using Bynder.Utils.InRiver;
using Bynder.Workers;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;

namespace Bynder.Extension
{
    public class Worker : Extension, IEntityListener, ILinkListener
    {
        /// <summary>
        /// when a resource entity is created in inRiver, we should process it as it is possible originated from bynder
        /// </summary>
        /// <param name="entityId"></param>
        public void EntityCreated(int entityId)
        {
            if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.DataOnly,
                EntityTypeId.Resource, out var entity)) return;

            Container.GetInstance<AssetDownloadWorker>().Execute(entity);
            Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
            Container.GetInstance<AssetUsageUpdateWorker>().Execute(entity);
        }

        /// <summary>
        /// when a resource entity is updated in inRiver, we should process it as it is possible originated from bynder
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="fields"></param>
        public void EntityUpdated(int entityId, string[] fields)
        {
            var entity = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.Shallow);
            if (entity.EntityType.Id == EntityTypeId.Resource)
            {
                Container.GetInstance<AssetDownloadWorker>().Execute(entity);
                Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
                Container.GetInstance<AssetUsageUpdateWorker>().Execute(entity);
            }
            else
            {
                // if other entitytype than resource update metaproperties based on modified fields
                Container.GetInstance<NonResourceMetapropertyWorker>().Execute(entity, fields);
            }
        }
        
        /// <summary>
        /// if a link is created with resource as target, we should check if we inform bynder
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="sourceId"></param>
        /// <param name="targetId"></param>
        /// <param name="linkTypeId"></param>
        /// <param name="linkEntityId"></param>
        public void LinkCreated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            if (!Context.ExtensionManager.DataService.TryGetEntityOfType(targetId, LoadLevel.DataOnly,
                EntityTypeId.Resource, out var entity)) return;

            Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
        }

        public void LinkUpdated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            LinkCreated(linkId, sourceId, targetId, linkTypeId, linkEntityId);
        }

        public void LinkDeleted(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            LinkCreated(linkId, sourceId, targetId, linkTypeId, linkEntityId);
        }

        public void EntityDeleted(Entity deletedEntity)
        {
            foreach (var entityId in deletedEntity.OutboundLinks
                .Where(l => l.Target.EntityType.Id.Equals(EntityTypeId.Resource))
                .Select(l => l.Target.Id))
            {
                if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.DataOnly,
                    EntityTypeId.Resource, out var entity)) return;

                Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
            }
        }
        
        #region Not Implemented IEntityListener, ILinkListener Members
        public void LinkActivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
        }

        public void LinkInactivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
        }


        public void EntityLocked(int entityId)
        {
        }

        public void EntityUnlocked(int entityId)
        {
        }

        public void EntityFieldSetUpdated(int entityId, string fieldSetId)
        {
        }

        public void EntityCommentAdded(int entityId, int commentId)
        {
        }

        public void EntitySpecificationFieldAdded(int entityId, string fieldName)
        {
        }

        public void EntitySpecificationFieldUpdated(int entityId, string fieldName)
        {
        }
        #endregion

    }
}
