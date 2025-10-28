using Bynder.Config;
using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [TestClass, Ignore("Use for debugging")]
    public class CvlSyncListenerTest : TestBase
    {
        private string _cvlId = "BynderTest";
        private CvlSyncListener _extension;

        [TestInitialize]
        public void Init()
        {
            _extension = new CvlSyncListener();
            _extension.Context = InRiverContext;
            TestSettings[Settings.LocaleMappingInriverToBynder] = "{\"nl\":\"nl_NL\", \"de\": \"de_DE\", \"fr\": \"fr_FR\", \"it\":\"it_IT\" }";
            TestSettings[Settings.CvlMetapropertyMapping] = "{\"InriverCvlOne\":[\"5A9H4A32-1F0B-582A-94KF9243CD178926\"], \"BynderTest\":[\"1U5EA4D7-53E1-4E0F-RB28E61BE3904F58\"]}";

            _extension.Context.Settings = TestSettings;
        }

        [TestMethod]
        public void Create()
        {
            _extension.CVLValueCreated(_cvlId, "TestOne");
        }

        [TestMethod]
        public void Update()
        {
            _extension.CVLValueUpdated(_cvlId, "TestOne");
        }

        [TestMethod]
        public void Delete()
        {
            _extension.CVLValueDeleted(_cvlId, "TestOne");
        }

        [TestMethod]
        public void DeleteAll()
        {
            _extension.CVLValueDeletedAll(_cvlId);
        }
    }
}
