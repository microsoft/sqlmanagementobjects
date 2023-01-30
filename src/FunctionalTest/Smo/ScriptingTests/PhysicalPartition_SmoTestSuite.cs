// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Physical Partition properties and scripting
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class PhysicalPartition_SmoTestSuite : SmoTestBase
    {
    
 
        /// <summary>
        /// Tests accessing and setting Physical Partition properties on SQL2022+
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16, Edition = DatabaseEngineEdition.Enterprise)]
        public void SmoPhysicalPartition_Enable_XmlCompression()
        {
            this.ExecuteWithDbDrop(
           database =>
           {
               Table table = CreatePartitionedTable(database);
               table.PhysicalPartitions[1].XmlCompression = XmlCompressionType.On;
               Assert.That(table.Rebuild, Throws.Nothing, "rebuild table should succeed");
                // Rebuild calls Refresh on the collection, but Refresh doesn't update the property bag of objects that are already in the collection.
               database.Parent.SetDefaultInitFields(typeof(PhysicalPartition), true);
               table.PhysicalPartitions.ClearAndInitialize(string.Empty, new string[0]);
               var physicalPartition = table.PhysicalPartitions[1];
               Assert.Multiple(() =>
               {
                   Assert.That(table.PhysicalPartitions.Cast<PhysicalPartition>().Select(p => p.XmlCompression), Is.EqualTo(new XmlCompressionType[] { XmlCompressionType.Off, XmlCompressionType.On, XmlCompressionType.Off, XmlCompressionType.Off }), "XmlCompression after first Rebuild");
                   Assert.That(physicalPartition.DataCompression, Is.EqualTo(DataCompressionType.None), "Data Compression should be off");
               });
               table.PhysicalPartitions[1].XmlCompression = XmlCompressionType.Off;
               table.PhysicalPartitions[2].XmlCompression = XmlCompressionType.On;
               table.PhysicalPartitions[1].DataCompression = DataCompressionType.Page;
               table.PhysicalPartitions[3].DataCompression = DataCompressionType.Row;
               table.Rebuild();
               table.PhysicalPartitions.ClearAndInitialize(string.Empty, new string[0]);
               Assert.Multiple(() =>
               {
                   Assert.That(table.PhysicalPartitions.Cast<PhysicalPartition>().Select(p => p.XmlCompression), Is.EqualTo(new XmlCompressionType[] { XmlCompressionType.Off, XmlCompressionType.Off, XmlCompressionType.On, XmlCompressionType.Off }), "XmlCompression after second Rebuild");
                   Assert.That(table.PhysicalPartitions.Cast<PhysicalPartition>().Select(p => p.DataCompression), Is.EqualTo(new DataCompressionType[] { DataCompressionType.None, DataCompressionType.Page, DataCompressionType.None, DataCompressionType.Row }), "DataCompression after second Rebuild");
               });
               table.PhysicalPartitions[3].DataCompression = DataCompressionType.None;
               table.OnlineHeapOperation = true;
               table.Rebuild(4);
               table.PhysicalPartitions.ClearAndInitialize(string.Empty, new string[0]);
               Assert.Multiple(() =>
               {
                   Assert.That(table.PhysicalPartitions.Cast<PhysicalPartition>().Select(p => p.XmlCompression), Is.EqualTo(new XmlCompressionType[] { XmlCompressionType.Off, XmlCompressionType.Off, XmlCompressionType.On, XmlCompressionType.Off }), "XmlCompression after third Rebuild");
                   Assert.That(table.PhysicalPartitions.Cast<PhysicalPartition>().Select(p => p.DataCompression), Is.EqualTo(new DataCompressionType[] { DataCompressionType.None, DataCompressionType.Page, DataCompressionType.None, DataCompressionType.None }), "DataCompression after third Rebuild");
               });
               table.PhysicalPartitions[0].XmlCompression = XmlCompressionType.On;
               table.OnlineHeapOperation = true;
               table.Rebuild(1);
               table.PhysicalPartitions.ClearAndInitialize(string.Empty, new string[0]);
               Assert.Multiple(() =>
               {
                   Assert.That(table.PhysicalPartitions.Cast<PhysicalPartition>().Select(p => p.XmlCompression), Is.EqualTo(new XmlCompressionType[] { XmlCompressionType.On, XmlCompressionType.Off, XmlCompressionType.On, XmlCompressionType.Off }), "XmlCompression after fourth Rebuild");
                   Assert.That(table.PhysicalPartitions.Cast<PhysicalPartition>().Select(p => p.DataCompression), Is.EqualTo(new DataCompressionType[] { DataCompressionType.None, DataCompressionType.Page, DataCompressionType.None, DataCompressionType.None }), "DataCompression after fourth Rebuild");
               });

           });
        }

        /// <summary>
        /// Tests to ensure that data and xml compression options are not scripted
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        public void VerifyNoXmlCompressionScripted()
        {
            this.ExecuteWithDbDrop(
            database =>
            {
                string tableName = "mytable";
                Regex dataCompressionPattern = new Regex("DATA_COMPRESSION\\s*=\\s*PAGE");
                Regex xmlCompressionPattern = new Regex("XML_COMPRESSION\\s*=\\s*ON");

                // Create a table without data compression
                Table table = createTableForCompressionTest(database, tableName, DataCompressionType.None, XmlCompressionType.Off);
                string script = scriptTableForDataAndXmlCompressionTest(table);

                // Verify data and xml compression are not scripted.
                Assert.IsFalse(dataCompressionPattern.IsMatch(script), "DATA_COMPRESSION is scripted. " + script);
                Assert.IsFalse(xmlCompressionPattern.IsMatch(script), "XML_COMPRESSION is scripted. " + script);

                // Run the script and verify it creates a table
                table.Drop();
                database.ExecuteNonQuery(script);
                database.Tables.Refresh();
                table = database.Tables[tableName];
                Assert.IsNotNull(table, "Database table " + "\"" + tableName + "\" was not created by the script: " + script);
            });
        }

        /// <summary>
        /// Tests to ensure that data and xml compression options are scripted
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        public void VerifyDataAndXmlCompressionScripted()
        {
            this.ExecuteWithDbDrop(
            database =>
            {
                string tableName = "mytable";
                Regex dataCompressionPattern = new Regex("DATA_COMPRESSION\\s*=\\s*PAGE");
                Regex xmlCompressionPattern = new Regex("XML_COMPRESSION\\s*=\\s*ON");

                // Create a table without data compression
                Table table = createTableForCompressionTest(database, tableName, DataCompressionType.Page, XmlCompressionType.On);
                bool fHasXmlCompressedPartitions = table.HasXmlCompressedPartitions;
                string script = scriptTableForDataAndXmlCompressionTest(table);

                Assert.Multiple(() =>
                {
                    Assert.That(fHasXmlCompressedPartitions, Is.True, "table.HasXmlCompressedPartitions");
                    Assert.That(dataCompressionPattern.IsMatch(script), Is.True, "DATA_COMPRESSION is scripted. " + script);
                    Assert.That(xmlCompressionPattern.IsMatch(script),  Is.True, "XML_COMPRESSION is scripted. " + script);
                });
                // Run the script and verify it creates a table
                table.Drop();
                database.ExecuteNonQuery(script);
                database.Tables.Refresh();
                table = database.Tables[tableName];
                Assert.That(table, Is.Not.Null, "Database table " + "\"" + tableName + "\" was not created by the script: " + script);
                Assert.That(table.HasXmlCompressedPartitions, Is.True, "Script should create table with xml compressed partition");
                Assert.That(table.PhysicalPartitions.Cast<PhysicalPartition>().Select(p => p.XmlCompression), Is.EqualTo(new XmlCompressionType[] { XmlCompressionType.On }), "Table should have 1 partition");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        public void SmoPhysicalPartition_Table_Create_succeeds_when_XmlCompression_not_set()
        {
            // Simulate creating the staging table like the SSMS Manage Partitions wizard
            ExecuteWithDbDrop(database =>
            {
                var table = CreatePartitionedTable(database);
                var partitionScheme = database.PartitionSchemes[table.PartitionScheme];
                var newTable = new Table(database, "newTable", table.Schema);
                newTable.Columns.Add(new Column(newTable, table.Columns[0].Name, table.Columns[0].DataType));
                newTable.FileGroup = partitionScheme.FileGroups[0];
                // Note XmlCompression on the new PhysicalPartition is not set
                newTable.PhysicalPartitions.Add(new PhysicalPartition(newTable, 1, table.PhysicalPartitions[0].DataCompression));
                var script = TSqlScriptingHelper.GenerateScriptForAction(database.Parent, newTable.Create);
                Assert.That(script, Does.Contain("CREATE TABLE [dbo].[newTable]"), "Incorrect generated script");
            });
        }

        private Table createTableForCompressionTest(Database db, string tableName, DataCompressionType dataCompression, XmlCompressionType xmlCompression)
		{
			const string xmlSchemaObjName = "XmlSchema1";

			var xmlSchema1 = new XmlSchemaCollection(db, xmlSchemaObjName, "dbo");
			xmlSchema1.Text = "<xsd:schema xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"NS2\"> <xsd:element name=\"elem1\" type=\"xsd:integer\"/></xsd:schema>";
			xmlSchema1.Create();

			Table tb = new Table(db, tableName);
			Column col = new Column(tb, "Col1", DataType.Int);
			col.Nullable = false;

            PhysicalPartition pp = new PhysicalPartition(tb, 0, dataCompression, xmlCompression);
            tb.PhysicalPartitions.Add(pp);

            tb.Columns.Add(col);
			tb.Columns.Add(new Column(tb, "c2", DataType.Xml(xmlSchemaObjName)) { Nullable = false });

            Assert.That(tb.Create, Throws.Nothing, "Create table should succeed");

            return tb;
		}

        private string scriptTableForDataAndXmlCompressionTest(Table tb)
        {
            ScriptingOptions sp = new ScriptingOptions();
            sp.IncludeHeaders = true;
            sp.ScriptDataCompression = true;
            sp.ScriptXmlCompression = true;
            sp.ClusteredIndexes = true;
            sp.DriAllConstraints = true;
            sp.IncludeDatabaseContext = true;

            StringCollection sc = tb.Script(sp);
            StringBuilder sb = new StringBuilder();
            foreach (string s in sc)
            {
                sb.AppendLine(s);
            }

            return sb.ToString();
        }

        private Table CreatePartitionedTable(Database db)
        {

            var prefix = $"Partitiontest{new Random().Next()}";
            var partitionScheme = db.CreatePartitionSchemeWithFileGroups(prefix,
                new object[] { "20200201", "20200301", "20200401" }, DataType.DateTime);
            var partitionedTable1 = new Table(db, "partitionedTable1" + new Random().Next())
            {
                PartitionScheme = partitionScheme.Name
            };
            partitionedTable1.Columns.Add(new Column(partitionedTable1, "ItemDate", DataType.DateTime));
            partitionedTable1.Columns.Add(new Column(partitionedTable1, "ItemName", DataType.SysName));
            partitionedTable1.Columns.Add(new Column(partitionedTable1, "c2", DataType.Xml(string.Empty)) { Nullable = false });

            partitionedTable1.PartitionSchemeParameters.Add(new PartitionSchemeParameter() { Name = "ItemDate" });
            partitionedTable1.Create();
            db.ExecuteNonQuery($@"INSERT INTO {partitionedTable1.Name}(ItemDate, ItemName,c2) VALUES
                                      ('20200110', 'Partition1January', '<data><elem>1</elem></data>'),
                                      ('20200215', 'Partition1February', '<data><elem>2</elem></data>'),
                                      ('20200320', 'Partition1March', '<data><elem>3</elem></data>'),
                                      ('20200402', 'Partition1April', '<data><elem>4</elem></data>')");
            return partitionedTable1;

        }
    }
}
