using Bynder.Workers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [Ignore("Only for debugging!")]
    [TestClass]
    public class NonResourceMetapropertyWorkerTest : TestBase
    {
        #region Fields

        private NonResourceMetapropertyWorker _worker;

        #endregion Fields

        #region Methods

        [TestMethod]
        [DataRow(15081)]
        public void Debug(int entityId)
        {
            var entity = InRiverContext.ExtensionManager.DataService.GetEntity(entityId, inRiver.Remoting.Objects.LoadLevel.DataOnly);
            _worker.Execute(entity, new[] { "ProductModelName" } );
        }

        [TestInitialize]
        public void Init()
        {
            _worker = new Bynder.Workers.NonResourceMetapropertyWorker(InRiverContext, new ResourceMetapropertyUpdateWorker(InRiverContext, _bynderClient));
        }

        #endregion Methods
    }
}