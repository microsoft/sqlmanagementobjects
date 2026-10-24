using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class SerializationTests : UnitTestBase
    {
        [TestMethod]
        public void VersionSerializationAdapter_reads_and_writes_xml()
        {
            var version = new Version(1, 2, 3, 4);
            var output = new StringBuilder();
            var serializer = new VersionSerializationAdapter();
            using (var writer = XmlWriter.Create(output,
                new XmlWriterSettings() {Indent = false, OmitXmlDeclaration = true}))
            {
                serializer.WriteXml(writer, version);
            }

            var xml = output.ToString();
            Assert.That(xml, Is.EqualTo("<System.Version>1.2.3.4</System.Version>"), "Version XML");
            object deserializedObject;
            using (var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings()))
            {
                serializer.ReadXml(reader, out deserializedObject);
            }

            Assert.That((Version) deserializedObject, Is.EqualTo(version), "Deserialized Version");
        }

        /// <summary>
        /// SqlVariantSerializationAdapter is used for properties like PartitionFunction.RangeValues.
        /// The property is typically of type object or of type object[].
        /// The serializer needs to handle the various types and whether it's an array or a single object
        /// </summary>
        [TestMethod]
        public void SqlVariantSerializationAdapter_writes_xml_for_object_array()
        {
            var byteArray = new byte[] {2, 4};
            var dateTimeOffset = new DateTimeOffset(2019, 12, 3, 0, 0, 0, TimeSpan.Zero);
            var objects = new object[]
            {
                32,
                dateTimeOffset,
                new SqlBinary(byteArray)
            };
            var output = new StringBuilder();
            var serializer = new SqlVariantSerializationAdapter();
            using (var writer = XmlWriter.Create(output, new XmlWriterSettings() {Indent = false, OmitXmlDeclaration = true}))
            {
                serializer.WriteXml(writer, objects);
            }

            var xml = output.ToString();
            Assert.That(xml, Is.EqualTo($"<SqlVariantArray><SqlVariant Type=\"{typeof(Int32).FullName}\">32</SqlVariant><SqlVariant Type=\"{typeof(DateTimeOffset).FullName}\">{dateTimeOffset.ToString(CultureInfo.InvariantCulture)}</SqlVariant><SqlVariant Type=\"{typeof(SqlBinary).FullName}\">{Convert.ToBase64String(byteArray)}</SqlVariant></SqlVariantArray>"), "Variant array XML");

        }

        [TestMethod]
        public void SqlVariantSerializationAdapter_writes_xml_for_object()
        {
            var output = new StringBuilder();
            var serializer = new SqlVariantSerializationAdapter();
            using (var writer = XmlWriter.Create(output, new XmlWriterSettings() { Indent = false, OmitXmlDeclaration = true }))
            {
                serializer.WriteXml(writer, "MyObject");
            }

            var xml = output.ToString();
            Assert.That(xml, Is.EqualTo($"<SqlVariant Type=\"{typeof(string).FullName}\">MyObject</SqlVariant>"), "Variant XML");

        }
    }
}