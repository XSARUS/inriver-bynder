using Bynder.Workers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [Ignore("Only for debugging!")]
    [TestClass]
    public class ResourceMetapropertyUpdateWorkerTest : TestBase
    {
        #region Fields

        private ResourceMetapropertyUpdateWorker _worker;

        #endregion Fields

        #region Methods

        [TestMethod]
        [DataRow(307757)]
        public void Debug(int resourceEntityId)
        {
            var resource = InRiverContext.ExtensionManager.DataService.GetEntity(resourceEntityId, inRiver.Remoting.Objects.LoadLevel.DataOnly);
            _worker.Execute(resource);
        }

        [TestInitialize]
        public void Init()
        {
            _worker = new ResourceMetapropertyUpdateWorker(InRiverContext, _bynderClient);
        }

        #endregion Methods
    }
}