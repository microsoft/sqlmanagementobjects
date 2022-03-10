// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Unit test for PredOperandUnitTest
    /// </summary>
    [TestClass]
    public class PredOperandUnitTest : DbScopedXEventTestBase
    {
        
        private PredOperand predOperand = null;

        [TestMethod]        
        public void DbScoped_PredOperand_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestConstructorEventColumn();
                TestConstructorEventColumnNull();
                TestConstructorPredSource();
                TestConstructorPredSourceNull();
                TestPredOperand();
            }));
    
        }

        /// <summary>
        /// Tests the constructor with pred_source.
        /// </summary>
        public void TestConstructorPredSource()
        {
            predOperand = new PredOperand(store.Package0Package.PredSourceInfoSet["current_thread_id"]);
            Assert.IsNotNull(predOperand);
            Assert.That(predOperand, Is.InstanceOf<Predicate>());
            Assert.AreEqual("[package0].[current_thread_id]", predOperand.ToString());
        }

        /// <summary>
        /// Tests the constructor with event column.
        /// </summary>
        public void TestConstructorEventColumn()
        {
            predOperand = new PredOperand(store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_completed").DataEventColumnInfoSet["writes"]);
            Assert.IsNotNull(predOperand);
            Assert.That(predOperand, Is.InstanceOf<Predicate>(), "Unexpected type for predOperand");
            Assert.AreEqual("[writes]", predOperand.ToString());
        }

        /// <summary>
        /// Tests the operand object property.
        /// </summary>
        public void TestPredOperand()
        {
            DataEventColumnInfo eventColumnInfo = store.ObjectInfoSet.Get<EventInfo>("sqlos.wait_info").DataEventColumnInfoSet["duration"];
            predOperand = new PredOperand(eventColumnInfo);
            Assert.IsNotNull(predOperand);
            Assert.AreEqual(eventColumnInfo, predOperand.OperandObject);
        }

        /// <summary>
        /// Tests the constructor with null. Exception is expected.
        /// </summary>
        public void TestConstructorPredSourceNull()
        {            
            //the user may pass an non-exist event column to the ctor
            Assert.Throws<ArgumentNullException>(() => new PredOperand(store.Package0Package.PredSourceInfoSet["do not exist source"]), "PredOperand constructor should throw");
        }

        /// <summary>
        /// Tests the constructor with null. Exception is expected.
        /// </summary>
        public void TestConstructorEventColumnNull()
        {
           //the user may pass an non-exist event column to the ctor
            Assert.Throws<ArgumentNullException>(() => new PredOperand(store.ObjectInfoSet.Get<EventInfo>("sqlserver.lock_acquired").DataEventColumnInfoSet["do not exist"]), "PredOperand constructor should throw");
        }
    }
}
