using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BynderTest
{
    public class Logger : IExtensionLog
    {
        #region Fields

        private readonly TestContext _testContext;

        #endregion Fields

        #region Constructors

        public Logger(TestContext testContext)
        {
            _testContext = testContext;
        }

        #endregion Constructors

        #region Methods

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

        #endregion Methods
    }
}