using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BynderTest
{
    [TestClass, Ignore("Only use for debugging")]
    public class InriverEventHandlerTest : TestBase
    {

        [TestMethod]
        public void Debug()
        {
            var extension = new InriverEventHandler()
            {
                Context = InRiverContext
            };

            extension.Execute(true);
        }

    }
}
