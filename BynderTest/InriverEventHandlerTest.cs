using Bynder.Extension;
using Bynder.Models;
using Bynder.Names;
using Bynder.Utils.Helpers;
using inRiver.Remoting.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BynderTest
{
    [Ignore("Only use for debugging")]
    [TestClass]
    public class InriverEventHandlerTest : TestBase
    {
        private void AddConnectorState(InriverEvent inriverEvent)
        {
            var connectorStateName = SettingHelper.GetConnectorStateName(InRiverContext.Settings, InRiverContext.Logger, ConnectorStateIds.BynderInriverEvents);

            InRiverContext.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState
            {
                ConnectorId = connectorStateName,
                Data = JsonConvert.SerializeObject(inriverEvent)
            });
        }

        [TestMethod]
        public void Debug()
        {
            AddConnectorState(new InriverEvent
            {
                EntityId = 426398,
                IsLink = false,
                IsResource = true,
                Attempt = 0
            });

            var extension = new InriverEventHandler()
            {
                Context = InRiverContext
            };

            extension.Execute(true);
        }

    }
}
