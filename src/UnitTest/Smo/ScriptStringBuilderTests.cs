// Copyright (c) Microsoft Corporation
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class ScriptStringBuilderTests : UnitTestBase
    {
        [TestMethod]
        public void When_parameters_are_empty_ToString_returns_statement_only()
        {
            var builder = new ScriptStringBuilder("Statement 1");
            var script = builder.ToString();
            Assert.That(script, Is.EqualTo("Statement 1 ;"));
        }

        [TestMethod]
        public void With_parameters_ToString_returns_valid_TSQL_comma_separated_list_with_params_in_order_added()
        {
            var script = new ScriptStringBuilder("statement foo")
                .SetParameter("param1", "val1")
                .SetParameter("param2", "val2")
                .SetParameter("param3", "val3")
                .ToString();
            Assert.That(script, Is.EqualTo(@"statement foo (param1 = 'val1', param2 = 'val2', param3 = 'val3');"));
        }

        [TestMethod]
        public void When_parameter_names_are_duplicated_ToString_includes_just_the_last_value()
        {
            var script = new ScriptStringBuilder("statement foo")
                .SetParameter("param1", "val1")
                .SetParameter("param2", "val2")
                .SetParameter("param1", "val3")
                .ToString();
            Assert.That(script, Is.EqualTo(@"statement foo (param1 = 'val3', param2 = 'val2');"));

        }

        [TestMethod]
        public void When_parameter_is_unquoted_value_is_correct()
        {
            var script = new ScriptStringBuilder("statement foo modify")
                .SetParameter("MAXSIZE", "100 GB", ParameterValueFormat.NotString)
                .ToString();
            Assert.That(script, Is.EqualTo(@"statement foo modify (MAXSIZE = 100 GB);"));
        }

        [TestMethod]
        public void When_parameters_include_object_parameter_script_is_correct()
        {
            var script = new ScriptStringBuilder("statement complex")
                .SetParameter("key1", "value1")
                .SetParameter("key2", "value2", ParameterValueFormat.NotString)
                .SetParameter("key3", new List<IScriptStringBuilderParameter>() 
                { 
                    new ScriptStringBuilderParameter("innerKey4", "innerValue4"),
                    new ScriptStringBuilderParameter("innerKey5", "innerValue5", ParameterValueFormat.NotString)
                })
                .ToString();
            Assert.That(script, Is.EqualTo(@"statement complex (key1 = 'value1', key2 = value2, key3 (innerKey4 = 'innerValue4', innerKey5 = innerValue5));"));
        }

        [TestMethod]
        public void When_pretty_printing_script_formatting_is_correct()
        {
            var script = new ScriptStringBuilder("statement complex", new ScriptingPreferences())
                .SetParameter("key1", "value1")
                .SetParameter("key2", "value2", ParameterValueFormat.NotString)
                .SetParameter("key3", new List<IScriptStringBuilderParameter>() 
                { 
                    new ScriptStringBuilderParameter("innerKey4", "innerValue4"),
                    new ScriptStringBuilderParameter("innerKey5", "innerValue5", ParameterValueFormat.NotString)
                })
                .ToString(scriptSemiColon: true, pretty: true);

            string expected = @"statement complex(
	key1 = 'value1',
	key2 = value2,
	key3 (innerKey4 = 'innerValue4', innerKey5 = innerValue5)
);".FixNewLines();

            Assert.That(script, Is.EqualTo(expected));
        }
    }
}