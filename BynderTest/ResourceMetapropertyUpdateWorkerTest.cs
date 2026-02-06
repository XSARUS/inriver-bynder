using Bynder.Workers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [TestClass, Ignore("Only for debugging!")]
    public class ResourceMetapropertyUpdateWorkerTest : TestBase
    {
        #region Fields

        private ResourceMetapropertyUpdateWorker _worker;

        #endregion Fields

        #region Methods

        [TestMethod]
        [DataRow(60333)]
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