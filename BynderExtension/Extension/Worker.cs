using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bynder.Extension
{
    using Bynder.Config;
    using Bynder.Utils.Helpers;
    using Names;
    using Sdk.Query.Asset;
    using System;
    using System.Text;
    using Utils.InRiver;
    using Workers;

    public class Worker : Extension, IEntityListener, ILinkListener
    {
        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = base.DefaultSettings;

                // Remove settings that are not used in this extension:
                var settingsToRemove = new List<string>(12)
                {
                    Settings.AddAssetIdPrefixToFilenameOfNewResource,
                    Settings.AssetPropertyMap,
                    Settings.BynderBrandName,
                    Settings.BynderLocaleForMetapropertyOptionLabel,
                    Settings.CreateMissingCvlKeys,
                    Settings.CronExpression,
                    Settings.CvlMetapropertyMapping,
                    Settings.DeleteResourceOnDeleteEvent,
                    Settings.FieldValuesToSetOnArchiveEvent,
                    Settings.ImportConditions,
                    Settings.InitialAssetLoadUrlQuery,
                    Settings.InitialAssetLoadLimit,
                    Settings.LocaleMappingInriverToBynder,
                    Settings.LocaleStringLanguagesToSet,
                    Settings.MaxRetryAttempts,
                    Settings.MultivalueSeparator,
                    Settings.RegularExpressionForFileName,
                    Settings.ResourceSearchType,
                    Settings.TimestampSettings,
                };

                settingsToRemove.ForEach(s => settings.Remove(s));

                return settings;
            }
        }

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

        public override string Test()
        {
            var sb = new StringBuilder();
            if (SettingHelper.ExecuteBaseTestMethod(Context.Settings, Context.Logger))
            {
                sb.AppendLine(base.Test());
            }

            try
            {
                // Not implemented yet, depends on worker's test-methods
            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
        }

        #endregion Methods
    }
}