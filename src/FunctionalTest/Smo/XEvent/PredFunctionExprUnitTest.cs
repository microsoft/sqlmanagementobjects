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
    /// Summary description for PredFunctionExprUnitTest
    /// </summary>
    [TestClass]
    public class PredFunctionExprUnitTest : DbScopedXEventTestBase
    {
        
        private static PredFunctionExpr funcExpr = null;

        private static PredCompareInfo compare = null;

        private static PredOperand operand = null;

        private static PredValue value = null;

        [TestMethod]
        public void DbScoped_PredFunctionExpr_Tests()
        {
            
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                compare = store.Package0Package.PredCompareInfoSet["equal_uint64"];
                operand = new PredOperand(store.Package0Package.PredSourceInfoSet["current_thread_id"]);
                value = new PredValue(2);
                TestConstructor();
                TestConstructorNullCompare();
                TestConstructorNullOpValue();
                TestConstructorNullOperand();
            }));
        }

        public void TestConstructor()
        {
            //use PredOperand comes from a pred_source
            funcExpr = new PredFunctionExpr(compare, operand, value);
            Assert.IsNotNull(funcExpr);
            Assert.That(funcExpr, Is.InstanceOf<PredExpr>(), "Unexpected type for funcExpr");

            Assert.AreEqual("([package0].[equal_uint64]([package0].[current_thread_id],(2)))", store.FormatPredicateExpression(funcExpr));

            Assert.AreEqual(compare, funcExpr.Operator);
            Assert.AreEqual(operand, funcExpr.Operand);
            Assert.AreEqual(value, funcExpr.Value);

            //use PredOperand comes from a event column
            funcExpr = null;
            DataEventColumnInfo eventColumn = store.ObjectInfoSet.Get<EventInfo>("sqlserver.lock_acquired").DataEventColumnInfoSet["mode"];
            funcExpr = new PredFunctionExpr(compare, new PredOperand(eventColumn), value);
            Assert.IsNotNull(funcExpr);
            Assert.That(funcExpr, Is.InstanceOf<PredExpr>(), "Unexpected type for funcExpr");
            Assert.AreEqual("([package0].[equal_uint64]([mode],(2)))", store.FormatPredicateExpression(funcExpr));
        }

        public void TestConstructorNullCompare()
        {
            Assert.Throws<ArgumentNullException>(() => new PredFunctionExpr(null, operand, value),
                "PredFunctionExpr constructor should throw");
        }

        public void TestConstructorNullOperand()
        {                   
            Assert.Throws<ArgumentNullException>(() => new PredFunctionExpr(compare, null, value));
        }

        public void TestConstructorNullOpValue()
        {        
            Assert.Throws<ArgumentNullException>(() => new PredFunctionExpr(compare, operand, null));
        }        
    }
}
