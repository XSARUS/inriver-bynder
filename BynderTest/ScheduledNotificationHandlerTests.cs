using Bynder.Extension;
using Bynder.Models;
using Bynder.Names;
using Bynder.Utils.Helpers;
using inRiver.Remoting.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace BynderTest
{
    [Ignore("Only use for debugging")]
    [TestClass]
    public class ScheduledNotificationHandlerTests : TestBase
    {
        [TestMethod]
        public void Debug()
        {
            AttemptSNSMessageWrapper data = new AttemptSNSMessageWrapper
            {
                Attempt = 1,
                OriginalMessageJson = @"{
                  ""Type"" : ""Notification"",
                  ""MessageId"" : ""da41e39f-ea4d-435a-b922-c6aae3915ebe"",
                  ""TopicArn"" : ""arn:aws:sns:us-west-2:123456789012:MyTopic"",
                  ""Subject"" : ""asset_bank.media.uploaded"",
                  ""Message"" : ""{\""media_id\"": \""17F01106-6F26-4A9D-884ABECE523D1129\""}"", //replace media id here with asset id to test
                  ""Timestamp"" : ""2012-04-25T21:49:25.719Z"",
                  ""SignatureVersion"" : ""1"",
                  ""Signature"" : ""EXAMPLElDMXvB8r9R83tGoNn0ecwd5UjllzsvSvbItzfaMpN2nk5HVSw7XnOn/49IkxDKz8YrlH2qJXj2iZB0Zo2O71c4qQk1fMUDi3LGpij7RCW7AW9vYYsSqIKRnFS94ilu7NFhUzLiieYr4BKHpdTmdD6c0esKEYBpabxDSc="",
                  ""SigningCertURL"" : ""https://sns.us-west-2.amazonaws.com/SimpleNotificationService-f3ecfb7224c7233fe7bb5f59f96de52f.pem"",
                  ""UnsubscribeURL"" : ""https://sns.us-west-2.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:us-west-2:123456789012:MyTopic:2bcfbf39-05c3-41de-beaa-fcfcc21c8f55""
                } "
            };

            var connectorStateName = SettingHelper.GetConnectorStateName(InRiverContext.Settings, InRiverContext.Logger, ConnectorStateIds.BynderNotificationListener);
            ConnectorState state = new ConnectorState
            {
                ConnectorId = connectorStateName,
                Data = JsonConvert.SerializeObject(data)
            };
            InRiverContext.ExtensionManager.UtilityService.AddConnectorState(state);


            var extension = new ScheduledNotificationHandler()
            {
                Context = InRiverContext
            };

            extension.Execute(true);
        }

    }
}
