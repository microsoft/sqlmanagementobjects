// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.XEvent;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for PredValueUnitTest
    /// </summary>
    [TestClass]    
    public class PredValueUnitTest
    {
        /// <summary>
        /// Tests the string constructor.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TestStringConstructor()
        {
            PredValue value = new PredValue("abcdefg");
            Assert.AreEqual("abcdefg", value.ToString());
            value = new PredValue("");
            Assert.AreEqual("", value.ToString());
        }
    }
}
