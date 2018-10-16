using System;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    public class Logger : IExtensionLog
    {
        private readonly TestContext _testContext;

        public Logger(TestContext testContext)
        {
            _testContext = testContext;
        }

        public void Log(string message)
        {
            _testContext.WriteLine("{0}", message);
        }

        public void Log(LogLevel level, string message)
        {
            _testContext.WriteLine("{0}: {1}", level, message);
        }

        public void Log(LogLevel level, string message, Exception ex)
        {
            _testContext.WriteLine("{0}: {1} [{2}]", level, message, ex.Message + ex.StackTrace);
        }
    }
}
