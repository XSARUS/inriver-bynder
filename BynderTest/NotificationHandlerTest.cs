using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [Ignore("Only use for debugging")]
    [TestClass]
    public class ScheduledNotificationHandlerTest : TestBase
    {
        [TestMethod]
        public void Debug()
        {
            var extension = new ScheduledNotificationHandler()
            {
                Context = InRiverContext
            };

            extension.Execute(true);
        }
    }
}
