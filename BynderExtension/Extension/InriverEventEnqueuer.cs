using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bynder.Extension
{
    using Config;
    using Models;
    using Names;
    using Utils.Helpers;
    using Utils.InRiver;

    public class InriverEventEnqueuer : AbstractExtension, IEntityListener, ILinkListener
    {

        #region Properties

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = new Dictionary<string, string>()
                {
                    { Settings.ConnectorStateName, ConnectorStateIds.BynderInriverEvents }
                };

                return settings;
            }
        }

        #endregion Properties

        #region Methods

        public void EntityCommentAdded(int entityId, int commentId)
        {
            // Not implemented
        }

        /// <summary>
        /// when a Resource-Entity is created in inRiver, we should process it as it is possible originated from Bynder
        /// </summary>
        /// <param name="entityId"></param>
        public void EntityCreated(int entityId)
        {
            try
            {
                if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.Shallow, EntityTypeIds.Resource, out var entity))
                {
                    return;
                }

                AddConnectorState(new InriverEvent { EntityId = entity.Id, IsResource = true });
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
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
                    if (!Context.ExtensionManager.DataService.TryGetEntityOfType(entityId, LoadLevel.Shallow,
                        EntityTypeIds.Resource, out var entity)) return;

                    AddConnectorState(new InriverEvent { EntityId = entity.Id, IsResource = true, IsLink = true });
                }
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
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
                if (entity == null) return;

                AddConnectorState(new InriverEvent { EntityId = entityId, IsResource = entity.EntityType.Id == EntityTypeIds.Resource, FieldTypeIds = fields });
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
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
            => HandleLink(targetId);

        public void LinkDeleted(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
            => HandleLink(targetId);

        public void LinkInactivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            // Not implemented
        }

        public void LinkUpdated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
            => HandleLink(targetId);

        public override string Test()
        {
            var sb = new StringBuilder();

            try
            {
                var connectorStateName = SettingHelper.GetConnectorStateName(Context.Settings, Context.Logger);
                List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(connectorStateName);
                sb.AppendLine($"Number of connectorstates currently: {states.Count}");
            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
        }

        private void AddConnectorState(InriverEvent inriverEvent)
        {
            var connectorStateName = SettingHelper.GetConnectorStateName(Context.Settings, Context.Logger);

            Context.ExtensionManager.UtilityService.AddConnectorState( new ConnectorState {
                    ConnectorId = connectorStateName,
                    Data = JsonConvert.SerializeObject(inriverEvent)
            });
        }
        /// <summary>
        /// we should check if we inform bynder of the link create/update/delete
        /// </summary>
        /// <param name="targetId"></param>
        private void HandleLink(int targetId)
        {
            try
            {
                if (!Context.ExtensionManager.DataService.TryGetEntityOfType(targetId, LoadLevel.Shallow,
                    EntityTypeIds.Resource, out var entity)) return;

                AddConnectorState(new InriverEvent { EntityId = entity.Id, IsResource = true, IsLink = true });
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.GetBaseException().Message, ex);
            }
        }

        #endregion Methods

    }
}
