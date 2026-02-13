// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for SessionUnitTest
    /// </summary>
    [TestClass]
    public class SessionUnitTest 
    {
                
        /// <summary>
        /// Tests the default value of a newly create session.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TestDefaultValue()
        {
            Session session = new Session();
            Assert.AreEqual(-1, session.ID);
            Assert.IsNull(session.Parent);
            Assert.IsNull(session.Name);
            Assert.IsFalse(session.IsRunning);
            Assert.AreEqual(Session.EventRetentionModeEnum.AllowSingleEventLoss, session.EventRetentionMode);
            Assert.AreEqual(Session.DefaultDispatchLatency, session.MaxDispatchLatency);
            Assert.AreEqual(Session.DefaultMaxMemory, session.MaxMemory);
            Assert.AreEqual(0, session.MaxEventSize);
            Assert.AreEqual(Session.MemoryPartitionModeEnum.None, session.MemoryPartitionMode);
            Assert.IsFalse(session.TrackCausality);
            Assert.IsFalse(session.AutoStart);
            Assert.AreEqual(Session.NotStarted, session.StartTime);
            Assert.AreEqual(Session.DefaultMaxDuration, session.MaxDuration);

            session = new Session(null, "ut1");
            Assert.AreEqual(-1, session.ID);
            Assert.AreEqual(null, session.Parent);
            Assert.AreEqual("ut1", session.Name);
            Assert.IsFalse(session.IsRunning);
            Assert.AreEqual(Session.EventRetentionModeEnum.AllowSingleEventLoss, session.EventRetentionMode);
            Assert.AreEqual(Session.DefaultDispatchLatency, session.MaxDispatchLatency);
            Assert.AreEqual(Session.DefaultMaxMemory, session.MaxMemory);
            Assert.AreEqual(0, session.MaxEventSize);
            Assert.AreEqual(Session.MemoryPartitionModeEnum.None, session.MemoryPartitionMode);
            Assert.IsFalse(session.TrackCausality);
            Assert.IsFalse(session.AutoStart);
            Assert.AreEqual(Session.NotStarted, session.StartTime);
            Assert.AreEqual(Session.DefaultMaxDuration, session.MaxDuration);
        }

        /// <summary>
        /// Tests the MaxDuration property.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void TestMaxDuration()
        {
            Session session = new Session();
            
            // Test default value when no parent (version checking unavailable)
            Assert.AreEqual(Session.DefaultMaxDuration, session.MaxDuration);
            Assert.AreEqual(Session.UnlimitedDuration, session.MaxDuration);

            session.MaxDuration = 3600;
            session.MaxDuration = 86400;
            session.MaxDuration = Session.UnlimitedDuration;
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TestValidateByWrongMethodName()
        {
            Session session = new Session();
            Assert.Throws<XEventException>(() => session.Validate("invalid"), "Validate should throw");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TestValidateName()
        {
            Session session = new Session();
            Assert.Throws<XEventException>(() => session.Validate(ValidationMethod.Create), "Validate should throw");
        }
    }
}
