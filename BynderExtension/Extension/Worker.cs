using Bynder.Names;
using Bynder.Utils.InRiver;
using Bynder.Workers;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;
using System.Linq;

namespace Bynder.Extension
{
    public class Worker : Extension, IEntityListener, ILinkListener
    {
        #region Methods

        public void EntityCommentAdded(int entityId, int commentId)
        {
            // Not implemented
        }

        /// <summary>
        /// when a resource entity is created in inRiver, we should process it as it is possible originated from bynder
        /// </summary>
        /// <param name="entityId"></param>
        public void EntityCreated(int entityId)
        {
            try
            {
                if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.DataOnly,
                    EntityTypeIds.Resource, out var entity)) return;

                Container.GetInstance<AssetDownloadWorker>().Execute(entity);
                Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
                Container.GetInstance<AssetUsageUpdateWorker>().Execute(entity);
            }
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        public void EntityDeleted(Entity deletedEntity)
        {
            try
            {
                foreach (var entityId in deletedEntity.OutboundLinks
                    .Where(l => l.Target.EntityType.Id.Equals(EntityTypeIds.Resource))
                    .Select(l => l.Target.Id))
                {
                    if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.DataOnly,
                        EntityTypeIds.Resource, out var entity)) return;

                    Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
                }
            }
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        public void EntityFieldSetUpdated(int entityId, string fieldSetId)
        {
            // Not implemented
        }

        public void EntityLocked(int entityId)
        {
            // Not implemented
        }

        public void EntitySpecificationFieldAdded(int entityId, string fieldName)
        {
            // Not implemented
        }

        public void EntitySpecificationFieldUpdated(int entityId, string fieldName)
        {
            // Not implemented
        }

        public void EntityUnlocked(int entityId)
        {
            // Not implemented
        }

        /// <summary>
        /// when a resource entity is updated in inRiver, we should process it as it is possible originated from bynder
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="fields"></param>
        public void EntityUpdated(int entityId, string[] fields)
        {
            try
            {
                var entity = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.Shallow);
                if (entity.EntityType.Id == EntityTypeIds.Resource)
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
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        public void LinkActivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            // Not implemented
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
            try
            {
                if (!Context.ExtensionManager.DataService.TryGetEntityOfType(targetId, LoadLevel.DataOnly,
                    EntityTypeIds.Resource, out var entity)) return;

                Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
            }
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        public void LinkDeleted(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            try
            {
                LinkCreated(linkId, sourceId, targetId, linkTypeId, linkEntityId);
            }
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        public void LinkInactivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            // Not implemented
        }

        public void LinkUpdated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            try
            {
                LinkCreated(linkId, sourceId, targetId, linkTypeId, linkEntityId);
            }
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        #endregion Methods
    }
}