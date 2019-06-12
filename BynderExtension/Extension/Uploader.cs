using Bynder.Names;
using Bynder.Utils.InRiver;
using Bynder.Workers;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;

namespace Bynder.Extension
{
    public class Uploader : Extension, IEntityListener
    {
        #region Methods

        public void EntityCreated(int entityId)
        {
            try
            {
                if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.DataOnly,
                    EntityTypeId.Resource, out var entity)) return;

                Container.GetInstance<AssetUploadWorker>().Execute(entity);
            }
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }
        #region NotImplementedMembers
        public void EntityCommentAdded(int entityId, int commentId)
        {
            // Not implemented
        }

        public void EntityDeleted(Entity deletedEntity)
        {
            // Not implemented
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
        public void EntityUpdated(int entityId, string[] fields)
        {
            try
            {
                if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.DataOnly,
                    EntityTypeId.Resource, out var entity)) return;

                Container.GetInstance<AssetUploadWorker>().Execute(entity);
            }
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }
        #endregion
        #endregion Methods
    }
}