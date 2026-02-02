using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;
using System.Collections.Generic;

namespace Bynder.Extension
{
    using Bynder.Config;
    using Bynder.Utils.Helpers;
    using Names;
    using System;
    using System.Text;
    using Utils.InRiver;
    using Workers;

    public class Uploader : Extension, IEntityListener
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
                    Settings.BynderLocaleForMetapropertyOptionLabel,
                    Settings.CreateMissingCvlKeys,
                    Settings.CronExpression,
                    Settings.CvlMetapropertyMapping,
                    Settings.DeleteResourceOnDeleteEvent,
                    Settings.DownloadMediaType,
                    Settings.FilenameExtensionMediaTypeMapping,
                    Settings.FieldValuesToSetOnArchiveEvent,
                    Settings.ImportConditions,
                    Settings.InitialAssetLoadUrlQuery,
                    Settings.InitialAssetLoadLimit,
                    Settings.InRiverEntityUrl,
                    Settings.InRiverIntegrationId,
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

        public void EntityCreated(int entityId)
        {
            try
            {
                if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.DataOnly,
                    EntityTypeIds.Resource, out var entity)) return;

                Container.GetInstance<AssetUploadWorker>().Execute(entity);
                Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
            }
            catch (System.Exception ex)
            {
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, ex.GetBaseException().Message, ex);
            }
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
                    EntityTypeIds.Resource, out var entity)) return;

                Container.GetInstance<AssetUploadWorker>().Execute(entity);
                Container.GetInstance<ResourceMetapropertyUpdateWorker>().Execute(entity);
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