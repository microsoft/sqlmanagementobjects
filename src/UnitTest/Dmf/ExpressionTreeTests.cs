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

        [TestMethod]
        [TestCategory("Unit")]
        public void ExpressionNode_Parse()
        {
            Assert.Multiple(() =>
           {
               foreach (var testCase in SerializeTestCases)
               {
                   var node = ExpressionNode.Parse(testCase[0]);
                   var xml = SerializeExpressionNode(node);
                   Assert.That(node.ToString(), Is.EqualTo(testCase[0]), $"Should round trip expression [{testCase[0]}]");
                   Assert.That(node.ToStringForDisplay(), Is.EqualTo(testCase[1]), $"ToStringForDisplay of [{testCase[0]}]");
                   Assert.That(xml, Is.EqualTo(testCase[2]), $"SerializeExpressionNode for [{testCase[0]}]");
               }
           });
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

        
        // Test cases are 3 parts
        // 0 - the expression to parse
        // 1 - the expected return from ToStringForDisplay
        // 2 - the expected return from Serialize
        static readonly string[][] SerializeTestCases = new string[][] {
            new [] {"Upper(ExecuteSql('String', 'select top 1 name from sys.databases'))", null, @"<Function>
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
</Function>" },
            new [] {"True()", "True", @"<Function>
  <TypeClass>Bool</TypeClass>
  <FunctionType>True</FunctionType>
  <ReturnType>Bool</ReturnType>
  <Count>0</Count>
</Function>"},
            new [] {"False()", "False", @"<Function>
  <TypeClass>Bool</TypeClass>
  <FunctionType>False</FunctionType>
  <ReturnType>Bool</ReturnType>
  <Count>0</Count>
</Function>"},
            new [] { "Add(Avg(1, 3), Count(1, 3, 4))", null, @"<Function>
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
</Function>" },
        new[] { "Sum(Array(4, 5, 6))", null, @"<Function>
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
</Function>" },
        new [] { "Subtract(Round(100.1, 2), Power(2, 3))", null, @"<Function>
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
</Function>" },
        new [] { "Enum('Microsoft.SqlServer.Management.Smo.CompatibilityLevel', 'Version120')", "Version120", @"<Function>
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
</Function>" },
        new [] { "DateTime('2022-01-07T00:00:00.0000000')", "2022-01-07T00:00:00.0000000", @"<Function>
  <TypeClass>DateTime</TypeClass>
  <FunctionType>DateTime</FunctionType>
  <ReturnType>DateTime</ReturnType>
  <Count>1</Count>
  <Constant>
    <TypeClass>String</TypeClass>
    <ObjType>System.String</ObjType>
    <Value>2022-01-07T00:00:00.0000000</Value>
  </Constant>
</Function>" },
        new [] { "Guid('cf77998a-2935-4bc6-b149-cb52249ecb9c')", "cf77998a-2935-4bc6-b149-cb52249ecb9c", @"<Function>
  <TypeClass>Guid</TypeClass>
  <FunctionType>Guid</FunctionType>
  <ReturnType>Guid</ReturnType>
  <Count>1</Count>
  <Constant>
    <TypeClass>String</TypeClass>
    <ObjType>System.String</ObjType>
    <Value>cf77998a-2935-4bc6-b149-cb52249ecb9c</Value>
  </Constant>
</Function>" },
        };
    }
}
