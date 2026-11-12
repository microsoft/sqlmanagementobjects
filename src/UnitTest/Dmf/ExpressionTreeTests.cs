// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Globalization;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Test.DmfUnitTests
{
    [TestClass]
    public class ExpressionTreeTests : UnitTestBase
    {
        /// <summary>
        /// Verifies that deserializing and then serializing the XML definition of Operator nodes
        /// for an ExpressionTree result in the serialized value being the same as the initial value
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Verify_ExpressionTree_OperatorNodes_SerializeAndDeserializeCorrectly()
        {
            using (
                var nodeStream =
                    this.GetType().GetTypeInfo().Assembly.GetManifestResourceStream("ExpressionTree_OperatorNodes.xml"))
            {
                var doc = XDocument.Load(nodeStream);
                foreach (string nodeStr in doc.XPathSelectElements("//Operator").Select(e => e.ToString()))
                {
                    ExpressionNode node = ExpressionNode.Deserialize(nodeStr);
                    //The serialized value should be the same as the original XML used to create the node
                    Assert.That(nodeStr, Is.EqualTo(SerializeExpressionNode(node)));
                }
            }
        }

        [DataTestMethod]
        [TestCategory("Unit")]
        [DynamicData(nameof(SerializeTestCases), DynamicDataSourceType.Property,
            DynamicDataDisplayName = nameof(GetSerializeTestCaseDisplayName))]
        public void ExpressionNode_Parse(string expression, string expectedDisplay, string expectedXml)
        {
            var node = ExpressionNode.Parse(expression);
            var xml = SerializeExpressionNode(node);
            Assert.Multiple(() =>
            {
                Assert.That(node.ToString(), Is.EqualTo(expression), $"Should round trip expression [{expression}]");
                Assert.That(node.ToStringForDisplay(), Is.EqualTo(expectedDisplay), $"ToStringForDisplay of [{expression}]");
                Assert.That(xml, Is.EqualTo(expectedXml), $"SerializeExpressionNode for [{expression}]");
            });
        }

        /// <summary>
        /// Names each parser test using its expression rather than its expected XML.
        /// </summary>
        public static string GetSerializeTestCaseDisplayName(MethodInfo methodInfo, object[] data) =>
            $"{methodInfo.Name}({data[0]})";

        [DataTestMethod]
        [TestCategory("Unit")]
        [DataRow(typeof(DayOfWeek), nameof(DayOfWeek.Monday))]
        [DataRow(typeof(DayOfWeek), "42")]
        [DataRow(typeof(BindingFlags), "Public, Instance")]
        [DataRow(typeof(NameConditionType), nameof(NameConditionType.Like))]
        [DataRow(typeof(CompatibilityLevel), nameof(CompatibilityLevel.Version120))]
        [DataRow(typeof(LoginType), nameof(LoginType.SqlLogin))]
        [DataRow(typeof(DatabaseEngineType), nameof(DatabaseEngineType.Standalone))]
        [DataRow(typeof(Microsoft.SqlServer.Management.Smo.Broker.DialogEndPointState),
            nameof(Microsoft.SqlServer.Management.Smo.Broker.DialogEndPointState.Open))]
        public void ExpressionNode_TrustedEnums_RoundTripAndEvaluate(Type enumType, string enumValue)
        {
            object expected = Enum.Parse(enumType, enumValue);
            var constant = (ExpressionNodeConstant)ExpressionNode.Deserialize(
                SerializeExpressionNode(new ExpressionNodeConstant(expected)));
            Assert.Multiple(() =>
            {
                Assert.That(constant.Value, Is.EqualTo(expected), $"Constant should preserve {enumType.FullName}.{enumValue}");
                AssertSupportedEnumType(enumType.FullName, enumValue, expected);
            });
        }

        private static void AssertSupportedEnumType(string typeName, string enumValue, object expected)
        {
            var xml = new XElement("Constant",
                new XElement("TypeClass", "Numeric"),
                new XElement("ObjType", typeName),
                new XElement("Value", enumValue));
            var constant = (ExpressionNodeConstant)ExpressionNode.Deserialize(xml.ToString());
            var function = new ExpressionNodeFunction(ExpressionNodeFunction.Function.Enum,
                new ExpressionNodeConstant(typeName), new ExpressionNodeConstant(enumValue));
            var deserializedFunction = ExpressionNode.Deserialize(SerializeExpressionNode(function));
            Assert.Multiple(() =>
            {
                Assert.That(constant.Value, Is.EqualTo(expected), $"Constant should preserve {typeName}.{enumValue}");
                Assert.That(deserializedFunction.Evaluate(null), Is.EqualTo(expected),
                    $"Enum function should preserve {typeName}.{enumValue}");
                Assert.That(deserializedFunction.ToStringForUrn(),
                    Is.EqualTo(Convert.ToInt32(expected).ToString(CultureInfo.InvariantCulture)),
                    $"Enum filter should preserve {typeName}.{enumValue}");

                foreach (bool isArray in new[] { false, true })
                {
                    var resultNode = ExpressionNode.DeserializeWithResult(CreateResultXml(typeName, enumValue, isArray));
                    object expectedResult = isArray ? new[] { expected } : expected;
                    Assert.That(resultNode.GetResult(), Is.EqualTo(expectedResult),
                        $"Result should preserve {typeName}.{enumValue} (array: {isArray})");
                }
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ExpressionNode_PrimitiveConstants_DeserializeCorrectly()
        {
            object[] values =
            {
                "value", 42, true, 1.25d, 42L, (short)42, (byte)42,
                new DateTime(2026, 1, 1), new Guid("3dbd4254-e010-42b9-8cbe-782a54746f5b"),
                1.25m, 'x', 1.25f
            };
            Assert.Multiple(() =>
            {
                foreach (object value in values)
                {
                    string typeClass = value is string || value is char || value is Guid ? "String" :
                        value is bool ? "Bool" : value is DateTime ? "DateTime" : "Numeric";
                    string serializedValue = value is DateTime dateTime ?
                        dateTime.ToBinary().ToString(CultureInfo.InvariantCulture) :
                        Convert.ToString(value, CultureInfo.InvariantCulture);
                    var xml = new XElement("Constant",
                        new XElement("TypeClass", typeClass),
                        new XElement("ObjType", value.GetType().FullName),
                        new XElement("Value", serializedValue));
                    var node = (ExpressionNodeConstant)ExpressionNode.Deserialize(xml.ToString());
                    Assert.That(node.Value, Is.EqualTo(value), $"Constant should preserve {value.GetType().FullName}");
                    Assert.That(node.Value?.GetType(), Is.EqualTo(value.GetType()), "Constant type should be preserved");
                }
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ExpressionNode_UnapprovedQualifiedEnums_AreRejected()
        {
            Assert.Multiple(() =>
            {
                AssertUnsupportedEnumType(typeof(XmlNodeType).AssemblyQualifiedName, nameof(XmlNodeType.Element));
                AssertUnsupportedEnumType(
                    $"{typeof(DayOfWeek).FullName}, {typeof(NameConditionType).Assembly.FullName}",
                    nameof(DayOfWeek.Monday));
            });
        }

        [DataTestMethod]
        [TestCategory("Unit")]
        [DataRow(typeof(DayOfWeek), nameof(DayOfWeek.Monday))]
        [DataRow(typeof(NameConditionType), nameof(NameConditionType.Like))]
        [DataRow(typeof(CompatibilityLevel), nameof(CompatibilityLevel.Version120))]
        [DataRow(typeof(LoginType), nameof(LoginType.SqlLogin))]
        [DataRow(typeof(DatabaseEngineType), nameof(DatabaseEngineType.Standalone))]
        [DataRow(typeof(Microsoft.SqlServer.Management.Smo.Broker.DialogEndPointState),
            nameof(Microsoft.SqlServer.Management.Smo.Broker.DialogEndPointState.Open))]
        public void ExpressionNode_QualifiedEnums_AreRejected(Type enumType, string enumValue)
        {
            Assert.Multiple(() =>
            {
                AssertUnsupportedEnumType($"{enumType.FullName}, {enumType.Assembly.GetName().Name}", enumValue);
                AssertUnsupportedEnumType(enumType.AssemblyQualifiedName, enumValue);
            });
        }

        [DataTestMethod]
        [TestCategory("Unit")]
        [DataRow("System.Object")]
        [DataRow("System.Collections.Generic.List`1[[System.DayOfWeek, UntrustedPolicyAssembly]]")]
        [DataRow("System.DayOfWeek[]")]
        [DataRow(@"System.DayOfWeek\, UntrustedPolicyAssembly")]
        [DataRow("UntrustedPolicy.Enum, UntrustedPolicyAssembly")]
        [DataRow("System.DayOfWeek, UntrustedPolicyAssembly")]
        [DataRow("System.DayOfWeek, UntrustedPolicyAssembly, Version=0.0.0.0")]
        [DataRow("System.DayOfWeek, UntrustedPolicyAssembly, Version=65535.65535.65535.65535")]
        [DataRow("System.DayOfWeek, mscorlib, Version=invalid")]
        [DataRow("System.DayOfWeek, MSCORLIB")]
        [DataRow("System.DayOfWeek, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
        [DataRow("System.DayOfWeek,")]
        [DataRow("System.Object, mscorlib")]
        [DataRow("System.Collections.Generic.List`1[[System.DayOfWeek, mscorlib]], mscorlib")]
        public void ExpressionNode_UntrustedEnumTypes_DoNotResolveAssemblies(string typeName)
        {
            bool attemptedUntrustedAssemblyLoad = false;
            ResolveEventHandler handler = (sender, args) =>
            {
                if (args.Name.StartsWith("UntrustedPolicyAssembly", StringComparison.Ordinal))
                {
                    attemptedUntrustedAssemblyLoad = true;
                }
                return null;
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;
            try
            {
                Assert.Multiple(() =>
                {
                    AssertUnsupportedEnumType(typeName, "0");
                    Assert.That(attemptedUntrustedAssemblyLoad, Is.False,
                        $"Type name '{typeName}' must not initiate an untrusted assembly load");
                });
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= handler;
            }
        }

        private static void AssertUnsupportedEnumType(string typeName, string enumValue)
        {
            var xml = new XElement("Constant",
                new XElement("TypeClass", "Numeric"),
                new XElement("ObjType", typeName),
                new XElement("Value", enumValue));
            var function = new ExpressionNodeFunction(ExpressionNodeFunction.Function.Enum,
                new ExpressionNodeConstant(typeName), new ExpressionNodeConstant(enumValue));
            Assert.Multiple(() =>
            {
                Assert.That(() => ExpressionNode.Deserialize(xml.ToString()), Throws.TypeOf<UnsupportedTypeException>(),
                    $"Constant type '{typeName}' should be rejected");
                Assert.That(() => function.Evaluate(null), Throws.TypeOf<UnsupportedObjectTypeException>(),
                    $"Enum function type '{typeName}' should be rejected");
                Assert.That(() => function.ConvertToFilterNode(), Throws.TypeOf<UnsupportedObjectTypeException>(),
                    $"Enum filter type '{typeName}' should be rejected rather than converted to zero");

                foreach (bool isArray in typeName.EndsWith("[]", StringComparison.Ordinal) ? new[] { true } : new[] { false, true })
                {
                    Assert.That(() => ExpressionNode.DeserializeWithResult(CreateResultXml(typeName, enumValue, isArray)),
                        Throws.TypeOf<UnsupportedTypeException>(),
                        $"Result type '{typeName}' should be rejected (array: {isArray})");
                }
            });
        }

        private static string CreateResultXml(string typeName, string enumValue, bool isArray)
        {
            var resultValue = new XElement("ResultValue", enumValue);
            if (isArray)
            {
                resultValue = new XElement("ResultValue",
                    new XElement("Count", 1),
                    new XElement("ResultObjType", typeName),
                    new XElement("ResultValue", enumValue));
            }
            return new XElement("Attribute",
                new XElement("TypeClass", isArray ? "Array" : "Numeric"),
                new XElement("Name", "TestProperty"),
                new XElement("ResultObjType", isArray ? "System.Object[]" : typeName),
                resultValue).ToString();
        }
        /// <summary>
        /// Serializes a <see cref="ExpressionNode"/> and returns the result as a XML-Indented formatted string
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static string SerializeExpressionNode(ExpressionNode node)
        {
            using (var writer = new XmlTextWriter(new MemoryStream(), Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                node.Serialize(writer);
                writer.Flush();
                writer.BaseStream.Position = 0;
                return new StreamReader(writer.BaseStream).ReadToEnd();
            }
        }

        /// <summary>
        /// Provides expressions, expected display text, and expected serialized XML.
        /// </summary>
        public static IEnumerable<object[]> SerializeTestCases =>
        [
            ["Upper(ExecuteSql('String', 'select top 1 name from sys.databases'))", null, @"<Function>
  <TypeClass>String</TypeClass>
  <FunctionType>Upper</FunctionType>
  <ReturnType>String</ReturnType>
  <Count>1</Count>
  <Function>
    <TypeClass>String</TypeClass>
    <FunctionType>ExecuteSql</FunctionType>
    <ReturnType>String</ReturnType>
    <Count>2</Count>
    <Constant>
      <TypeClass>String</TypeClass>
      <ObjType>System.String</ObjType>
      <Value>String</Value>
    </Constant>
    <Constant>
      <TypeClass>String</TypeClass>
      <ObjType>System.String</ObjType>
      <Value>select top 1 name from sys.databases</Value>
    </Constant>
  </Function>
</Function>"],
            ["True()", "True", @"<Function>
  <TypeClass>Bool</TypeClass>
  <FunctionType>True</FunctionType>
  <ReturnType>Bool</ReturnType>
  <Count>0</Count>
</Function>"],
            ["False()", "False", @"<Function>
  <TypeClass>Bool</TypeClass>
  <FunctionType>False</FunctionType>
  <ReturnType>Bool</ReturnType>
  <Count>0</Count>
</Function>"],
            ["Add(Avg(1, 3), Count(1, 3, 4))", null, @"<Function>
  <TypeClass>Numeric</TypeClass>
  <FunctionType>Add</FunctionType>
  <ReturnType>Numeric</ReturnType>
  <Count>2</Count>
  <Function>
    <TypeClass>Numeric</TypeClass>
    <FunctionType>Avg</FunctionType>
    <ReturnType>Numeric</ReturnType>
    <Count>2</Count>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>1</Value>
    </Constant>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>3</Value>
    </Constant>
  </Function>
  <Function>
    <TypeClass>Numeric</TypeClass>
    <FunctionType>Count</FunctionType>
    <ReturnType>Numeric</ReturnType>
    <Count>3</Count>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>1</Value>
    </Constant>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>3</Value>
    </Constant>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>4</Value>
    </Constant>
  </Function>
</Function>"],
            ["Sum(Array(4, 5, 6))", null, @"<Function>
  <TypeClass>Numeric</TypeClass>
  <FunctionType>Sum</FunctionType>
  <ReturnType>Numeric</ReturnType>
  <Count>1</Count>
  <Function>
    <TypeClass>Array</TypeClass>
    <FunctionType>Array</FunctionType>
    <ReturnType>Array</ReturnType>
    <Count>3</Count>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>4</Value>
    </Constant>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>5</Value>
    </Constant>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>6</Value>
    </Constant>
  </Function>
</Function>"],
            ["Subtract(Round(100.1, 2), Power(2, 3))", null, @"<Function>
  <TypeClass>Numeric</TypeClass>
  <FunctionType>Subtract</FunctionType>
  <ReturnType>Numeric</ReturnType>
  <Count>2</Count>
  <Function>
    <TypeClass>Numeric</TypeClass>
    <FunctionType>Round</FunctionType>
    <ReturnType>Numeric</ReturnType>
    <Count>2</Count>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>100.1</Value>
    </Constant>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>2</Value>
    </Constant>
  </Function>
  <Function>
    <TypeClass>Numeric</TypeClass>
    <FunctionType>Power</FunctionType>
    <ReturnType>Numeric</ReturnType>
    <Count>2</Count>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>2</Value>
    </Constant>
    <Constant>
      <TypeClass>Numeric</TypeClass>
      <ObjType>System.Double</ObjType>
      <Value>3</Value>
    </Constant>
  </Function>
</Function>"],
            ["Enum('Microsoft.SqlServer.Management.Smo.CompatibilityLevel', 'Version120')", "Version120", @"<Function>
  <TypeClass>Numeric</TypeClass>
  <FunctionType>Enum</FunctionType>
  <ReturnType>Numeric</ReturnType>
  <Count>2</Count>
  <Constant>
    <TypeClass>String</TypeClass>
    <ObjType>System.String</ObjType>
    <Value>Microsoft.SqlServer.Management.Smo.CompatibilityLevel</Value>
  </Constant>
  <Constant>
    <TypeClass>String</TypeClass>
    <ObjType>System.String</ObjType>
    <Value>Version120</Value>
  </Constant>
</Function>"],
            ["DateTime('2022-01-07T00:00:00.0000000')", "2022-01-07T00:00:00.0000000", @"<Function>
  <TypeClass>DateTime</TypeClass>
  <FunctionType>DateTime</FunctionType>
  <ReturnType>DateTime</ReturnType>
  <Count>1</Count>
  <Constant>
    <TypeClass>String</TypeClass>
    <ObjType>System.String</ObjType>
    <Value>2022-01-07T00:00:00.0000000</Value>
  </Constant>
</Function>"],
            ["Guid('cf77998a-2935-4bc6-b149-cb52249ecb9c')", "cf77998a-2935-4bc6-b149-cb52249ecb9c", @"<Function>
  <TypeClass>Guid</TypeClass>
  <FunctionType>Guid</FunctionType>
  <ReturnType>Guid</ReturnType>
  <Count>1</Count>
  <Constant>
    <TypeClass>String</TypeClass>
    <ObjType>System.String</ObjType>
    <Value>cf77998a-2935-4bc6-b149-cb52249ecb9c</Value>
  </Constant>
</Function>"],
        ];
    }
}
