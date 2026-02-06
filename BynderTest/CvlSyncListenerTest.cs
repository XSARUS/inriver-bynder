using Bynder.Config;
using Bynder.Extension;
using inRiver.Remoting;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [TestClass, Ignore("Use for debugging")]
    public class CvlSyncListenerTest : TestBase
    {
        #region Fields

        private readonly string _localestringCvlId = "BynderTestCVL";
        private readonly string _stringCvlId = "InriverCvlOne"; // string CVL
                                                                // localestring CVL

        private CvlSyncListener _extension;
        private IModelService _modelService;
        private IUtilityService _utilityService;

        #endregion Fields

        #region Methods

        public void CVLValueCreatedTest()
        {
            _extension.CVLValueCreated(_localestringCvlId, "test");
        }

        public void CVLValueDeletedAllTest()
        {
            _extension.CVLValueDeletedAll(_localestringCvlId);
        }

        public void CVLValueDeletedTest()
        {
            _extension.CVLValueDeleted(_localestringCvlId, "test");
        }

        [TestMethod]
        public void CVLValueLifeCycleFlowTest()
        {
            var CVLValue = CreateLocaleStringCVLValue();
            Assert.IsGreaterThan(0, CVLValue.Id);
            CVLValueCreatedTest();
            UpdateLocaleStringCVLValue(CVLValue);
            CVLValueUpdatedTest();
            DeleteLocaleStringCVLValue(CVLValue.Id);
            CVLValueDeletedTest();
        }

        public void CVLValueUpdatedTest()
        {
            _extension.CVLValueUpdated(_localestringCvlId, "test");
        }

        [TestInitialize]
        public void Init()
        {
            _extension = new CvlSyncListener
            {
                Context = InRiverContext
            };
            // inriver locale > bynder locale
            TestSettings[Settings.LocaleMappingInriverToBynder] = "{\"nl-NL\":\"nl_NL\", \"en-GB\": \"en_GB\", \"it-IT\":\"it_IT\" }";
            // Bynder locale
            TestSettings[Settings.BynderLocaleForMetapropertyOptionLabel] = "nl_NL";
            TestSettings[Settings.CvlMetapropertyMapping] = "{\"InriverCvlOne\":[\"1BE89552-0A02-44A3-BEF806C1E15B39A2\"], \"BynderTestCVL\":[\"FC3C52EB-A700-4964-9C3142A8C999DB0D\"]}";

            _extension.Context.Settings = TestSettings;

            _modelService = _extension.Context.ExtensionManager.ModelService;
            _utilityService = _extension.Context.ExtensionManager.UtilityService;
        }

        [TestMethod, Ignore("Missing scope: current.user:read")]
        public void Test()
        {
            var result = _extension.Test();
            Assert.DoesNotContain("error", result);
            Assert.DoesNotContain("exception", result);
        }

        internal CVLValue CreateLocaleStringCVLValue()
        {
            var value = new LocaleString(_utilityService.GetAllLanguages());
            foreach (var language in value.Languages)
            {
                value[language] = "testvalue_" + language.ThreeLetterISOLanguageName;
            }
            var testCvlValue = new CVLValue
            {
                Key = "test",
                CVLId = _localestringCvlId,
                Value = value
            };

            testCvlValue = _modelService.AddCVLValue(testCvlValue);
            Logger.Log(LogLevel.Information, $"CVLValue created with id {testCvlValue.Id} for CVL {_localestringCvlId}");

            return testCvlValue;
        }

        internal void DeleteLocaleStringCVLValue(int cvlValueId)
        {
            var status = _modelService.DeleteCVLValue(cvlValueId);
            Assert.IsTrue(status);
            Logger.Log(LogLevel.Information, $"CVLValue with id {cvlValueId} deleted for CVL {_stringCvlId}");
        }

        internal void UpdateLocaleStringCVLValue(CVLValue testCvlValue)
        {
            var value = testCvlValue.Value as LocaleString;
            foreach (var language in value.Languages)
            {
                value[language] = "updated__testvalue_" + language.ThreeLetterISOLanguageName;
            }
            testCvlValue.Value = value;

            testCvlValue = _modelService.UpdateCVLValue(testCvlValue);
            Logger.Log(LogLevel.Information, $"CVLValue with id {testCvlValue.Id} updated for CVL {_stringCvlId}");
        }

        #endregion Methods
    }
}