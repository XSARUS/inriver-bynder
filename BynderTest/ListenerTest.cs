using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [TestClass]
    public class ListenerTest : TestBase
    {
        #region Methods

        [Ignore("Temporary disabled because since 1.7.0 we only store a ConnectorState and handle it by a ScheduledExtension")]
        [TestMethod]
        public void TestAwsNotification()
        {
            var listener = new NotificationListener
            {
                Context = InRiverContext
            };
            listener.Context.Settings = TestSettings;

            // todo: fill in your SNS settings in this test message
            var result = listener.Add(@"{
                  ""Type"" : ""Notification"",
                  ""MessageId"" : ""da41e39f-ea4d-435a-b922-c6aae3915ebe"",
                  ""TopicArn"" : ""arn:aws:sns:us-west-2:123456789012:MyTopic"",
                  ""Subject"" : ""asset_bank.media.uploaded"",
                  ""Message"" : ""{\""media_id\"": \""9542A933-2DF5-4999-9AB52701F33613C0\""}"",
                  ""Timestamp"" : ""2012-04-25T21:49:25.719Z"",
                  ""SignatureVersion"" : ""1"",
                  ""Signature"" : ""EXAMPLElDMXvB8r9R83tGoNn0ecwd5UjllzsvSvbItzfaMpN2nk5HVSw7XnOn/49IkxDKz8YrlH2qJXj2iZB0Zo2O71c4qQk1fMUDi3LGpij7RCW7AW9vYYsSqIKRnFS94ilu7NFhUzLiieYr4BKHpdTmdD6c0esKEYBpabxDSc="",
                  ""SigningCertURL"" : ""https://sns.us-west-2.amazonaws.com/SimpleNotificationService-f3ecfb7224c7233fe7bb5f59f96de52f.pem"",
                  ""UnsubscribeURL"" : ""https://sns.us-west-2.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:us-west-2:123456789012:MyTopic:2bcfbf39-05c3-41de-beaa-fcfcc21c8f55""
                } ");

            Logger.Log(result);
            Assert.AreNotEqual(string.Empty, result, "Got no result");
        }

        [TestMethod]
        public void TestTestMethod()
        {
            var listener = new NotificationListener
            {
                Context = InRiverContext
            };
            listener.Context.Settings = TestSettings;

            var result = listener.Test();
            Logger.Log(result);
            Assert.AreNotEqual(string.Empty, result, "Got no result");
        }

        #endregion Methods
    }
}