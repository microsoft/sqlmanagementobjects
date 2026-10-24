// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SfcUnitTests
{
    [TestClass]
    public class FilterNodeTests
    {
        /// <summary>
        /// Regression test for sql injection bug 9473340
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void When_ObjectType_is_string_ToString_escapes_value()
        {
            var filterNodeConstant = new FilterNodeConstant(@"string with a ' in it",
                FilterNodeConstant.ObjectType.String);
            Assert.That(filterNodeConstant.ToString(), Is.EqualTo(@"'string with a '' in it'"),
                "ToString should provide the value escaped and surrounded by quotes");
            Assert.That((string)filterNodeConstant, Is.EqualTo(@"string with a '' in it"),
                "implicit operator should escape the value" );
            Assert.That(filterNodeConstant.ValueAsString, Is.EqualTo(@"string with a ' in it"), "ValueAsString should return unescaped value");
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void When_ObjectType_is_Boolean_ToString_converts_to_sql_functions()
        {
            var filterNodeConstant = new FilterNodeConstant(true, FilterNodeConstant.ObjectType.Boolean);
            Assert.That(filterNodeConstant.ToString(), Is.EqualTo("true()"), "ToString should convert true to true()");
            Assert.That((string)filterNodeConstant, Is.EqualTo("True"), "implicit operator should convert true to True");
            Assert.That(filterNodeConstant.ValueAsString, Is.EqualTo("True"), "ValueAsString should convert true to True");
            filterNodeConstant = new FilterNodeConstant(false, FilterNodeConstant.ObjectType.Boolean);
            Assert.That(filterNodeConstant.ToString(), Is.EqualTo("false()"), "ToString should convert false to false()");
            Assert.That((string)filterNodeConstant, Is.EqualTo("False"), "implicit operator should convert false to False");
            Assert.That(filterNodeConstant.ValueAsString, Is.EqualTo("False"), "ValueAsString should convert false to False");
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]       
        
        public void When_ObjectType_is_Number_ToString_converts_to_number()
        {
            var filterNodeConstant = new FilterNodeConstant(1024*1024, FilterNodeConstant.ObjectType.Number);
            Assert.That(filterNodeConstant.ToString(), Is.EqualTo("1048576"), "Unexpexted ToString result");
            Assert.That((string)filterNodeConstant, Is.EqualTo("1048576"), "Unexpected implicit operator result");
            Assert.That(filterNodeConstant.ValueAsString, Is.EqualTo("1048576"), "Unexpected ValueAsString result");
        }
    }
}
