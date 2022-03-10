// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.SqlServer.Management.XEvent;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Summary description for PredLogicalExprUnitTest
    /// </summary>
    [TestClass]    
    public class PredLogicalExprUnitTest : DbScopedXEventTestBase
    {

        [TestMethod]
        public void DbScoped_PredLogicalExpr_Tests()
        {
            ExecuteFromDbPool(PoolName,  (db) => ExecuteTest(db, () =>
            {
                TestAnd();
                TestComplicatedExpression();
                TestNot();
                TestOr();                
            }));
        }

        /// <summary>
        /// Tests LogicalOperatorType.Not
        /// </summary>
        public void TestNot()
        {
            PredOperand operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.database_id"));
            PredValue value = new PredValue(2);
            PredCompareInfo func = store.Package0Package.PredCompareInfoSet["divides_by_uint64"];
            PredFunctionExpr expr = new PredFunctionExpr(func, operand, value);

            PredLogicalExpr logicalExpr = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Not, expr, null);

            Assert.AreEqual("(NOT ([package0].[divides_by_uint64]([sqlserver].[database_id],(2))))", store.FormatPredicateExpression(logicalExpr));
        }

        /// <summary>
        /// Tests LogicalOperatorType.And.
        /// </summary>
        public void TestAnd()
        {
            PredOperand operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.database_id"));
            PredValue value = new PredValue(4);
            PredCompareExpr expr1 = new PredCompareExpr(PredCompareExpr.ComparatorType.LE, operand, value);

            operand = new PredOperand(store.ObjectInfoSet.Get<EventInfo>("sqlserver.lock_acquired").DataEventColumnInfoSet["mode"]);
            value = new PredValue(2);
            PredCompareInfo func = store.Package0Package.PredCompareInfoSet["divides_by_uint64"];
            PredFunctionExpr expr2 = new PredFunctionExpr(func, operand, value);

            PredLogicalExpr logicalExpr = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.And, expr1, expr2);

            Assert.AreEqual("(([sqlserver].[database_id]<=(4)) AND ([package0].[divides_by_uint64]([mode],(2))))", store.FormatPredicateExpression(logicalExpr));
        }

        /// <summary>
        /// Tests LogicalOperatorType.Or.
        /// </summary>
        public void TestOr()
        {        
            PredOperand operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.nt_domain"));
            PredValue value = new PredValue("eastdomain");
            PredCompareExpr expr1 = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);

            operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.nt_user"));
            value = new PredValue(@"mydomain\myuser");
            PredCompareExpr expr2 = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);
            
            PredLogicalExpr logicalExpr = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Or, expr1, expr2);
            Assert.AreEqual(@"(([sqlserver].[nt_domain]=N'eastdomain') OR ([sqlserver].[nt_user]=N'mydomain\myuser'))", store.FormatPredicateExpression(logicalExpr));
        }

        /// <summary>
        /// Tests the complicated logical expression.
        /// </summary>
        public void TestComplicatedExpression()
        {
            PredOperand operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.nt_domain"));
            PredValue value = new PredValue("eastdomain");
            PredCompareExpr expr1 = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);

            operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.nt_user"));
            value = new PredValue(@"mydomain\myuser");
            PredCompareExpr expr2 = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, value);

            PredLogicalExpr logicalExpr1 = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Or, expr1, expr2);


            operand = new PredOperand(store.ObjectInfoSet.Get<EventInfo>("sqlserver.lock_acquired").DataEventColumnInfoSet["mode"]);
            value = new PredValue(2);
            PredCompareInfo func = store.Package0Package.PredCompareInfoSet["divides_by_uint64"];
            PredFunctionExpr expr3 = new PredFunctionExpr(func, operand, value);

            PredLogicalExpr logicalExpr2 = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Not, expr3, null);

            PredLogicalExpr logicalExpr3 = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.And, logicalExpr1, logicalExpr2);

            operand = new PredOperand(store.ObjectInfoSet.Get<PredSourceInfo>("sqlserver.database_id"));
            value = new PredValue(4);
            PredCompareExpr expr4 = new PredCompareExpr(PredCompareExpr.ComparatorType.LE, operand, value);
            PredLogicalExpr logicalExpr = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Or, logicalExpr3, expr4);

            Assert.AreEqual(@"(((([sqlserver].[nt_domain]=N'eastdomain') OR ([sqlserver].[nt_user]=N'mydomain\myuser')) AND (NOT ([package0].[divides_by_uint64]([mode],(2))))) OR ([sqlserver].[database_id]<=(4)))",
                store.FormatPredicateExpression(logicalExpr));
            Assert.AreEqual(PredLogicalExpr.LogicalOperatorType.Or, logicalExpr.Operator);
            Assert.AreEqual(logicalExpr3, logicalExpr.LeftExpr);
            Assert.AreEqual(expr4, logicalExpr.RightExpr);

            Assert.AreEqual(PredLogicalExpr.LogicalOperatorType.And, logicalExpr3.Operator);
            Assert.AreEqual(logicalExpr1, logicalExpr3.LeftExpr);
            Assert.AreEqual(logicalExpr2, logicalExpr3.RightExpr);

            Assert.AreEqual(PredLogicalExpr.LogicalOperatorType.Not, logicalExpr2.Operator);
            Assert.AreEqual(expr3, logicalExpr2.LeftExpr);
            Assert.AreEqual(null, logicalExpr2.RightExpr);

            PredExpr tempexpr = logicalExpr.RightExpr;
            Assert.AreEqual(PredCompareExpr.ComparatorType.LE, ((PredCompareExpr)tempexpr).Operator);
            Assert.AreEqual(operand, ((PredCompareExpr)tempexpr).Operand);
            Assert.AreEqual(value, ((PredCompareExpr)tempexpr).Value);
        }
    }
}
