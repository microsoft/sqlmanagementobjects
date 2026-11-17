// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.XEvent;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for PredCompareExprUnitTest
    /// </summary>
    [TestClass]
    public class PredCompareExprUnitTest : DbScopedXEventTestBase
    {


        [TestMethod]
        public void DbScoped_PredCompareExpr_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestEventColumn();
                TestPredSource();                
            }));
        }
        /// <summary>
        /// Tests the PredCompareExpr constructor with PredOperand(PredSourceInfo) and PredValue.
        /// </summary>
        public void TestPredSource()
        {
            PredOperand operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.database_id"));
            PredValue value = new PredValue(7);
            PredCompareExpr expr = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);
            Assert.AreEqual("([sqlserver].[database_id]=(7))", store.FormatPredicateExpression(expr));

            operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.nt_user"));
            value = new PredValue(@"fareast\jix");
            expr = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);
            Assert.AreEqual(@"([sqlserver].[nt_user]=N'fareast\jix')", store.FormatPredicateExpression(expr));

            operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.username"));
            value = new PredValue("sa");
            expr = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);
            Assert.AreEqual(@"([sqlserver].[username]=N'sa')", store.FormatPredicateExpression(expr));

            Assert.AreEqual(PredCompareExpr.ComparatorType.EQ, expr.Operator);

            Assert.AreEqual(operand, expr.Operand);

            Assert.AreEqual(value, expr.Value);
        }


        /// <summary>
        /// Tests the PredCompareExpr constructor with PredOperand(EventColumnInfo) and PredValue.
        /// </summary>
        public void TestEventColumn()
        { 
            PredOperand operand = new PredOperand(store.ObjectInfoSet.Get<EventInfo>("sqlserver.lock_acquired").DataEventColumnInfoSet["mode"]);
            PredValue value = new PredValue(5);
            PredCompareExpr expr = new PredCompareExpr(PredCompareExpr.ComparatorType.NE, operand, value);
            Assert.AreEqual("([mode]<>(5))", store.FormatPredicateExpression(expr));
            expr = new PredCompareExpr(PredCompareExpr.ComparatorType.GE, operand, value);
            Assert.AreEqual("([mode]>=(5))", store.FormatPredicateExpression(expr));

            Assert.AreEqual(PredCompareExpr.ComparatorType.GE, expr.Operator);
            Assert.AreEqual(value, expr.Value);
            Assert.AreEqual(operand, expr.Operand);

        }


    }
}
