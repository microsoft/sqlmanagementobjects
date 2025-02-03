// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.XEvent;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// UnitTest for Target.
    /// </summary>
    [TestClass]
    public class TargetUnitTest 
    {

        /// <summary>
        /// Test the Empty Target constructor.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TestTargetCtorEmpty()
        {
            Target target = new Target();
            Assert.IsNull(target.Name);
            Assert.AreEqual(-1, target.ID);
            Assert.IsNull(target.Description);
        }

        /// <summary>
        /// Test the Target constructor with Null DatabaseXEStore.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TestTargetCtorNullXEStore()
        {
            Session session = new Session();
            Assert.Throws<NullReferenceException>(() => new Target(session, "package0.event_file"));
        }

    }
}
