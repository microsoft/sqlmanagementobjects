// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Test.Manageability.Utils;
using _VSUT = Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.Common;
using _SMO = Microsoft.SqlServer.Management.Smo;
using System;
using System.Text;
using System.Threading;
#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using System.Collections.Specialized;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using SFC = Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Index properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [_VSUT.TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Index_SmoTestSuite : SmoObjectTestBase
    {
        /// <summary>
        /// Tests accessing and setting index properties on Azure v12 (Sterling)
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void SmoIndexProperties_AzureSterlingV12AndAfterCloud()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    var result = new SqlTestResult();
                    _SMO.Table table = database.CreateTable("tbl_" + this.TestContext.TestName);
                    _SMO.Index index = table.CreateIndex("idx_" + this.TestContext.TestName);

                    //Read-Only properties
                    result &= SqlTestHelpers.TestReadProperty(index, "HasCompressedPartitions", false);
                    result &= SqlTestHelpers.TestReadProperty(index, "FileGroup", "PRIMARY");
                    result &= SqlTestHelpers.TestReadProperty(index, "PartitionScheme", "");
                    result &= SqlTestHelpers.TestReadProperty(index, "IsPartitioned", false);
                    result &= SqlTestHelpers.TestReadProperty(index, "FileStreamFileGroup", "");
                    result &= SqlTestHelpers.TestReadProperty(index, "FileStreamPartitionScheme", "");

                    //Read-only after creation properties

                    //Read/Write Properties


                    Assert.IsTrue(result.Succeeded, result.FailureReasons);
                });
        }

        /// <summary>
        /// Tests accessing and setting index properties on SQL2014
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 12, MaxMajor = 12)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 12, MaxMajor = 12)]
        public void SmoIndexProperties_Sql2014()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    var result = new SqlTestResult();

                    _SMO.Table table = database.CreateTable("tbl_" + this.TestContext.TestName);
                    _SMO.Index index = table.CreateIndex("idx_" + this.TestContext.TestName);

                    //Read-Only properties
                    result &= SqlTestHelpers.TestReadProperty(index, "HasCompressedPartitions", false);
                    result &= SqlTestHelpers.TestReadProperty(index, "FileGroup", "PRIMARY");
                    result &= SqlTestHelpers.TestReadProperty(index, "PartitionScheme", "");
                    result &= SqlTestHelpers.TestReadProperty(index, "IsPartitioned", false);
                    result &= SqlTestHelpers.TestReadProperty(index, "FileStreamFileGroup", "");
                    result &= SqlTestHelpers.TestReadProperty(index, "FileStreamPartitionScheme", "");

                    //Read-only after creation properties

                    //Read/Write Properties


                    Assert.IsTrue(result.Succeeded, result.FailureReasons);

                });
        }

        /// <summary>
        /// Tests accessing and setting index properties on SQL15
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoIndexProperties_Sql2016AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    var result = new SqlTestResult();

                    _SMO.Table table = database.CreateTable("tbl_" + this.TestContext.TestName);
                    _SMO.Index index = table.CreateIndex("idx_" + this.TestContext.TestName);

                    //Read-Only properties
                    result &= SqlTestHelpers.TestReadProperty(index, "HasCompressedPartitions", false);
                    result &= SqlTestHelpers.TestReadProperty(index, "FileGroup", "PRIMARY");
                    result &= SqlTestHelpers.TestReadProperty(index, "PartitionScheme", "");
                    result &= SqlTestHelpers.TestReadProperty(index, "IsPartitioned", false);
                    result &= SqlTestHelpers.TestReadProperty(index, "FileStreamFileGroup", "");
                    result &= SqlTestHelpers.TestReadProperty(index, "FileStreamPartitionScheme", "");

                    //Read-only after creation properties
                    //Read/Write Properties
                    Assert.IsTrue(result.Succeeded, result.FailureReasons);
                });

        }

        /// <summary>
        /// Tests to check XmlCompression property on index
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16, Edition = DatabaseEngineEdition.Enterprise)]
        public void SmoIndexProperties_Sql2022AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
            db =>
            {
                string tableName = "myXmlTable";
                _SMO.Table tb = new _SMO.Table(db, tableName);
                tb.Columns.Add(new _SMO.Column(tb, "ItemDate", _SMO.DataType.DateTime));
                tb.Columns.Add(new _SMO.Column(tb, "ItemName", _SMO.DataType.SysName));
                tb.Columns.Add(new _SMO.Column(tb, "c2", _SMO.DataType.Xml(string.Empty)) { Nullable = false });

                NUnit.Framework.Assert.That(tb.Create, NUnit.Framework.Throws.Nothing, "Create table should succeed");

                db.ExecuteNonQuery($@"INSERT INTO {tb.Name}(ItemDate, ItemName,c2) VALUES
                                      ('20200110', 'Partition1January', '<data><elem>1</elem></data>'),
                                      ('20200215', 'Partition1February', '<data><elem>2</elem></data>'),
                                      ('20200320', 'Partition1March', '<data><elem>3</elem></data>'),
                                      ('20200402', 'Partition1April', '<data><elem>4</elem></data>')");

                _SMO.Index index = CreatePartitionedIndex(db, tb);

                index.PhysicalPartitions[1].XmlCompression = _SMO.XmlCompressionType.On;
                Assert.That(index.Rebuild, Throws.Nothing, "rebuild index should succeed");

                db.Parent.SetDefaultInitFields(typeof(_SMO.PhysicalPartition), true);
                index.PhysicalPartitions.ClearAndInitialize(string.Empty, new string[0]);
                var physicalPartition = index.PhysicalPartitions[1];
                Assert.Multiple(() =>
                {
                    Assert.That(physicalPartition.XmlCompression, Is.EqualTo(_SMO.XmlCompressionType.On), "Xml Compression should be on");
                    Assert.That(physicalPartition.DataCompression, Is.EqualTo(_SMO.DataCompressionType.None), "Data Compression should be off");
                });

                // script the index and make sure creation succeeds
                //
                string script = ScriptSmoObject(index);
                index.Drop();
                try
                {
                    db.ExecuteNonQuery(script);
                    db.Tables.Refresh();
                }
                catch(Exception e)
                {
                    Assert.Fail($"Index creation failed. {e}\r\n{script}");
                }
            });
        }

        private _SMO.Index CreatePartitionedIndex(_SMO.Database db, _SMO.Table tb)
        {
            var prefix = $"Partitiontest{new Random().Next()}";
            var partitionScheme = db.CreatePartitionSchemeWithFileGroups(prefix,
                new object[] { "20200201", "20200301", "20200401" }, _SMO.DataType.DateTime);

            var partitionedIndex = new _SMO.Index(tb, "partitionedIndex1" + new Random().Next());
            partitionedIndex.IndexedColumns.Add(new _SMO.IndexedColumn(partitionedIndex, tb.Columns[0].Name));
            partitionedIndex.IndexType = _SMO.IndexType.NonClusteredIndex;
            partitionedIndex.PartitionScheme = partitionScheme.Name;
            partitionedIndex.PartitionSchemeParameters.Add(new _SMO.PartitionSchemeParameter() { Name = "ItemDate" });
            partitionedIndex.Create();

            return partitionedIndex;
        }

        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Index index = (_SMO.Index)obj;
            _SMO.Table table = (_SMO.Table)objVerify;

            table.Indexes.Refresh();
            Assert.IsNull(table.Indexes[index.Name],
                          "Current index not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an index with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Index_Sql16AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);
                    _SMO.Index index = new _SMO.Index(table, GenerateSmoObjectName("idx"));

                    index.IndexedColumns.Add(new _SMO.IndexedColumn(index, table.Columns[0].Name));
                    index.IndexKeyType = _SMO.IndexKeyType.None;

                    string indexScriptDropIfExistsTemplate = "DROP INDEX IF EXISTS [{0}]";
                    string indexScriptDropIfExists = string.Format(indexScriptDropIfExistsTemplate, index.Name);

                    VerifySmoObjectDropIfExists(index, table, indexScriptDropIfExists);
                });
        }

        /// <summary>
        /// Tests dropping a primary key with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_PrimaryKey_Sql16AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);
                    _SMO.Index pk = new _SMO.Index(table, GenerateSmoObjectName("pk"));

                    _SMO.Column column = table.Columns[0];
                    column.Nullable = false;
                    column.Alter();

                    _SMO.IndexedColumn pkc = new _SMO.IndexedColumn(pk, table.Columns[0].Name);
                    pk.IndexedColumns.Add(pkc);
                    pk.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;

                    const string primaryKeyScriptDropIfExistsTemplate =
                        "ALTER TABLE {0} DROP CONSTRAINT IF EXISTS {1}";
                   string primaryKeyScriptDropIfExists = string.Format(primaryKeyScriptDropIfExistsTemplate,
                        table.FormatFullNameForScripting(new _SMO.ScriptingPreferences()),
                        pk.FormatFullNameForScripting(new _SMO.ScriptingPreferences()));

                   VerifySmoObjectDropIfExists(pk, table, primaryKeyScriptDropIfExists);
                });
        }

        /// <summary>
        /// Validates an in-memory table with a clustered columnstore index can be correctly scripted.
        /// </summary>
        [_VSUT.TestMethod]
        [SqlTestArea(SqlTestArea.Hekaton)]
        [SqlRequiredFeature(SqlFeature.Hekaton)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void HekatonColumnstoreScripting()
        {
            ExecuteWithDbDrop(
            database =>
            {
                if (database.DatabaseEngineEdition != DatabaseEngineEdition.SqlDatabase)
                {
                    // We may be running against Express, where AUTO_CLOSE is on.
                    // That option does not work for in-memory databases, so turning it off,
                    // just to be on the safe side.
                    //
                    database.DatabaseOptions.AutoClose = false;
                    database.Alter();

                    // Add Hekaton support
                    //
                    _SMO.FileGroup memoryOptimizedFg = new _SMO.FileGroup(database, String.Format("{0}_hkfg", database.Name),
                            Microsoft.SqlServer.Management.Smo.FileGroupType.MemoryOptimizedDataFileGroup);

                    memoryOptimizedFg.Create();

                    _SMO.DataFile dataFile = new _SMO.DataFile(memoryOptimizedFg, String.Format("{0}_hkfg", database.Name))
                    {
                        FileName =
                            _SMO.PathWrapper.Combine(_SMO.PathWrapper.GetDirectoryName(database.FileGroups["PRIMARY"].Files[0].FileName),
                                String.Format("{0}_hkfg", database.Name))
                    };

                    dataFile.Create();
                    database.FileGroups.Refresh();
                }
                _SMO.Table table = new _SMO.Table(database, "HekatonColumnstoreScripting_testTable")
                {
                    IsMemoryOptimized = true,
                    Durability = _SMO.DurabilityType.SchemaAndData,
                };

                table.Columns.Add(new _SMO.Column(table, "c1", _SMO.DataType.Int) { Nullable = false });

                _SMO.Index pkIdx = new _SMO.Index(table, "c1_pk") { IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey, IndexType = _SMO.IndexType.NonClusteredIndex };
                pkIdx.IndexedColumns.Add(new _SMO.IndexedColumn(pkIdx, "c1"));
                table.Indexes.Add(pkIdx);

                _SMO.Index cci = new _SMO.Index(table, "cci") { IndexKeyType = _SMO.IndexKeyType.None, IndexType = _SMO.IndexType.ClusteredColumnStoreIndex };
                table.Indexes.Add(cci);

                table.Create();

                string script = ScriptSmoObject(table);

                var expectedScriptFragment =
$"CREATE TABLE [dbo].[HekatonColumnstoreScripting_testTable]{Environment.NewLine}({Environment.NewLine}\t[c1] [int] NOT NULL,{Environment.NewLine}{Environment.NewLine} CONSTRAINT [c1_pk]  PRIMARY KEY NONCLUSTERED {Environment.NewLine}({Environment.NewLine}\t[c1] ASC{Environment.NewLine}),{Environment.NewLine}INDEX [cci] CLUSTERED COLUMNSTORE WITH (COMPRESSION_DELAY = 0){Environment.NewLine})WITH ( MEMORY_OPTIMIZED = ON , DURABILITY = SCHEMA_AND_DATA )";

                Assert.That(script, Does.Contain(expectedScriptFragment));
            }, AzureDatabaseEdition.Premium);
        }

        /// <summary>
        /// Verify spatial index create script.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlDatabaseEdge)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Verify_Create_Spatial_Index()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    StringDictionary createIndexScriptList = new StringDictionary();

                    _SMO.Table table1 = new _SMO.Table(database, "table1" + Guid.NewGuid());
                    createIndexScriptList.Add("GEOGRAPHY_AUTO_GRID",
                        CreateSpatialIndexScript(table1, "geography", _SMO.DataType.Geography,
                            _SMO.SpatialIndexType.GeographyAutoGrid));

                    _SMO.Table table2 = new _SMO.Table(database, "table2" + Guid.NewGuid());
                    createIndexScriptList.Add("GEOGRAPHY_GRID",
                        CreateSpatialIndexScript(table2, "geography", _SMO.DataType.Geography,
                            _SMO.SpatialIndexType.GeographyGrid));

                    _SMO.Table table3 = new _SMO.Table(database, "table3" + Guid.NewGuid());
                    createIndexScriptList.Add("GEOMETRY_AUTO_GRID",
                        CreateSpatialIndexScript(table3, "geometry", _SMO.DataType.Geometry,
                            _SMO.SpatialIndexType.GeometryAutoGrid));

                    _SMO.Table table4 = new _SMO.Table(database, "table4" + Guid.NewGuid());
                    createIndexScriptList.Add("GEOMETRY_GRID",
                        CreateSpatialIndexScript(table4, "geometry", _SMO.DataType.Geometry,
                            _SMO.SpatialIndexType.GeometryGrid));

                    Assert.That(createIndexScriptList["GEOGRAPHY_AUTO_GRID"], Does.Contain("GEOGRAPHY_AUTO_GRID"));
                    Assert.That(createIndexScriptList["GEOGRAPHY_GRID"], Does.Contain("GEOGRAPHY_GRID"));
                    Assert.That(createIndexScriptList["GEOMETRY_AUTO_GRID"], Does.Contain("GEOMETRY_AUTO_GRID"));
                    Assert.That(createIndexScriptList["GEOMETRY_GRID"], Does.Contain("GEOMETRY_GRID"));
                });
        }

        /// <summary>
        /// Create Spatial Index script
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="dataType"></param>
        /// <param name="spatialIndexType"></param>
        /// <returns></returns>
        private string CreateSpatialIndexScript(_SMO.Table table, string columnName, _SMO.DataType dataType, _SMO.SpatialIndexType spatialIndexType)
        {
            //Create table with Primary key Clustered.
            CreateTableWithPrimaryKeyClustered(table, columnName, dataType);

            _SMO.Index index = new _SMO.Index(table, "idx_" + spatialIndexType);
            _SMO.IndexedColumn pkc = new _SMO.IndexedColumn(index, columnName);
            index.IndexedColumns.Add(pkc);
            index.IndexKeyType = _SMO.IndexKeyType.None;
            index.IndexType = _SMO.IndexType.SpatialIndex;
            index.SpatialIndexType = spatialIndexType;
            if (index.SpatialIndexType == _SMO.SpatialIndexType.GeometryGrid || index.SpatialIndexType == _SMO.SpatialIndexType.GeometryAutoGrid)
            {
                //Sample values
                index.BoundingBoxXMax = -19;
                index.BoundingBoxYMax = 51;
                index.BoundingBoxXMin = -20;
                index.BoundingBoxYMin = 50;
            }

            index.Create();

            return ScriptSmoObject(index);
        }

        /// <summary>
        /// Create Table with Primary key clustered.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="dataType"></param>
        private void CreateTableWithPrimaryKeyClustered(_SMO.Table table, string columnName, _SMO.DataType dataType)
        {
            table.Columns.Add(new _SMO.Column(table, "pk", _SMO.DataType.UniqueIdentifier) { Nullable = false });
            table.Columns.Add(new _SMO.Column(table, columnName, dataType) { Nullable = false });

            _SMO.Index pkIdx = new _SMO.Index(table, "PK_" + table.Name) { IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey, IndexType = _SMO.IndexType.ClusteredIndex };
            pkIdx.IndexedColumns.Add(new _SMO.IndexedColumn(pkIdx, "pk"));
            table.Indexes.Add(pkIdx);

            table.Create();
        }

        /// <summary>
        /// Verify vector index create script with just required parameters
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Verify_CreateVectorIndex_RequiredProps()
        {
            Verify_Create_Vector_Index("cosine", "DiskANN");
        }

        /// <summary>
        /// Verify vector index create script with filegroup specified
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Verify_CreateVectorIndex_WithFilegroup()
        {
            Verify_Create_Vector_Index("cosine", "DiskANN", filegroup: "PRIMARY");
        }

        /// <summary>
        /// Verify spatial index create script.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Verify_CreateVectorIndex_AllProps()
        {
            Verify_Create_Vector_Index("euclidean", "DiskANN", m: 42, r: 43, l: 44);
        }

        private void Verify_Create_Vector_Index(string metric, string type, int? m = null, int? r = null, int? l = null, string filegroup = null)
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = null;
                    database.ExecuteNonQuery("ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON");
                    try
                    {
                        // Create our table
                        table = new _SMO.Table(database, "VectorIndexTable_" + Guid.NewGuid());
                        table.Columns.Add(new _SMO.Column(table, "pk", _SMO.DataType.Int) { Nullable = false });
                        table.Columns.Add(new _SMO.Column(table, "vecCol", _SMO.DataType.Vector(3)) { Nullable = false });

                        _SMO.Index pkIdx = new _SMO.Index(table, "PK_" + table.Name) { IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey, IndexType = _SMO.IndexType.ClusteredIndex };
                        pkIdx.IndexedColumns.Add(new _SMO.IndexedColumn(pkIdx, "pk"));
                        table.Indexes.Add(pkIdx);
                        table.Create();

                        // Create the vector index
                        _SMO.Index index = new _SMO.Index(table, "VectorIndex_" + table.Name)
                        {
                            IndexType = _SMO.IndexType.VectorIndex,
                            VectorIndexMetric = metric,
                            VectorIndexType = type
                        };
                        _SMO.IndexedColumn vectorColumn = new _SMO.IndexedColumn(index, "vecCol");
                        index.IndexedColumns.Add(vectorColumn);
                        if (filegroup != null)
                        {
                            index.FileGroup = filegroup;
                        }
                        index.Create();

                        // Refresh to get a clean copy of the object from the server
                        table.Indexes.ClearAndInitialize($"[@Name='{SFC.Urn.EscapeString(index.Name)}']", new string[] { nameof(_SMO.Index.VectorIndexMetric), nameof(_SMO.Index.VectorIndexType) });
                        var newIndex = table.Indexes[index.Name];
                        Assert.That(newIndex.IndexType, Is.EqualTo(IndexType.VectorIndex));
                        Assert.That(newIndex.VectorIndexMetric, Is.EqualTo(index.VectorIndexMetric));
                        Assert.That(newIndex.VectorIndexType, Is.EqualTo(index.VectorIndexType));
                        Assert.That(newIndex.FileGroup, Is.EqualTo(index.FileGroup));
                    }
                    finally
                    {
                        table?.Drop();
                    }
            });
        }

        /// <summary>
        /// Verify JSON index create script without optimized_for_array_search option
        /// </summary>
        [DataRow(true)]
        [DataRow(false)]
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Verify_CreateJsonIndex(bool optimizeForArraySearch)
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = null;

                    // Create our table with JSON column
                    table = new _SMO.Table(database, "JsonIndexTable_" + Guid.NewGuid());
                    table.Columns.Add(new _SMO.Column(table, "id", _SMO.DataType.Int) { Nullable = false });
                    table.Columns.Add(new _SMO.Column(table, "jsonCol", _SMO.DataType.Json) { Nullable = true });
                    table.Columns.Add(new _SMO.Column(table, "name", _SMO.DataType.NVarChar(100)) { Nullable = true });

                    _SMO.Index pkIdx = new _SMO.Index(table, "PK_" + table.Name) 
                    { 
                        IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey, 
                        IndexType = _SMO.IndexType.ClusteredIndex 
                    };
                    pkIdx.IndexedColumns.Add(new _SMO.IndexedColumn(pkIdx, "id"));
                    table.Indexes.Add(pkIdx);
                    table.Create();

                    // Create the JSON index
                    _SMO.Index index = new _SMO.Index(table, "JsonIndex_" + table.Name)
                    {
                        IndexType = _SMO.IndexType.JsonIndex
                    };
                    _SMO.IndexedColumn jsonColumn = new _SMO.IndexedColumn(index, "jsonCol");
                    index.IndexedColumns.Add(jsonColumn);

                    if (optimizeForArraySearch)
                    {
                        index.OptimizeForArraySearch = true;
                    }
                    
                    try
                    {
                        index.Create();

                        // Refresh to get a clean copy of the object from the server
                        table.Indexes.ClearAndInitialize($"[@Name='{SFC.Urn.EscapeString(index.Name)}']", 
                            new string[] { nameof(_SMO.Index.OptimizeForArraySearch) });
                        var newIndex = table.Indexes[index.Name];
                        Assert.That(newIndex.IndexType, Is.EqualTo(IndexType.JsonIndex));
                        
                        Assert.That(newIndex.OptimizeForArraySearch, Is.EqualTo(optimizeForArraySearch), 
                                "optimizeForArraySearch should be true");

                        // Verify the script contains expected options
                        string script = ScriptSmoObject(newIndex);
                        Assert.That(script, Does.Contain("CREATE JSON INDEX"), 
                            "Script should contain CREATE JSON INDEX");
                        Assert.That(script, Does.Contain("[jsonCol]"), 
                            "Script should contain the JSON column");

                        if (optimizeForArraySearch)
                        {
                            Assert.That(script, Does.Contain("OPTIMIZE_FOR_ARRAY_SEARCH = ON"),
                                "Script should contain OPTIMIZE_FOR_ARRAY_SEARCH = ON");
                        }
                        else
                        {
                            Assert.That(script, Does.Not.Contain("OPTIMIZE_FOR_ARRAY_SEARCH"),
                                "Script should not contain OPTIMIZE_FOR_ARRAY_SEARCH");
                        }
                    }
                    finally
                    {
                        table?.Drop();
                    }
            });
        }

        /// <summary>
        /// Verify JSON index create script with JSON paths
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Verify_CreateJsonIndex_With_Paths()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = null;

                    // Create our table with JSON column
                    table = new _SMO.Table(database, "JsonIndexPathTable_" + Guid.NewGuid());
                    table.Columns.Add(new _SMO.Column(table, "id", _SMO.DataType.Int) { Nullable = false });
                    table.Columns.Add(new _SMO.Column(table, "jsonCol", _SMO.DataType.Json) { Nullable = true });

                    _SMO.Index pkIdx = new _SMO.Index(table, "PK_" + table.Name)
                    {
                        IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey,
                        IndexType = _SMO.IndexType.ClusteredIndex
                    };
                    pkIdx.IndexedColumns.Add(new _SMO.IndexedColumn(pkIdx, "id"));
                    table.Indexes.Add(pkIdx);
                    table.Create();

                    // Create the JSON index with paths using INCLUDING clause
                    _SMO.Index index = new _SMO.Index(table, "JsonIndex_" + table.Name)
                    {
                        IndexType = _SMO.IndexType.JsonIndex,
                        OptimizeForArraySearch = true
                    };
                    _SMO.IndexedColumn jsonColumn = new _SMO.IndexedColumn(index, "jsonCol");
                    index.IndexedColumns.Add(jsonColumn);

                    // Add multiple JSON paths to the index
                    var jsonPath1 = new _SMO.IndexedJsonPath(index, "$.customer.name");
                    var jsonPath2 = new _SMO.IndexedJsonPath(index, "$.order.items");
                    var jsonPath3 = new _SMO.IndexedJsonPath(index, "$.\"믱吨굮뿁霋\"");
                    index.IndexedJsonPaths.Add(jsonPath1);
                    index.IndexedJsonPaths.Add(jsonPath2);
                    index.IndexedJsonPaths.Add(jsonPath3);

                    try
                    {
                        index.Create();
                        table.Indexes.ClearAndInitialize($"[@Name='{SFC.Urn.EscapeString(index.Name)}']",
                            new string[] { nameof(_SMO.Index.OptimizeForArraySearch) });
                        var newIndex = table.Indexes[index.Name];
                        Assert.That(newIndex.IndexType, Is.EqualTo(IndexType.JsonIndex));
                        Assert.That(newIndex.OptimizeForArraySearch, Is.EqualTo(true));

                        // Verify the script
                        string script = ScriptSmoObject(newIndex);
                        Assert.That(script, Does.Contain("CREATE JSON INDEX"));
                        Assert.That(script, Does.Contain("OPTIMIZE_FOR_ARRAY_SEARCH = ON"));

                        // Verify that all paths are included in the script
                        Assert.That(script, Does.Contain("'$.customer.name'"));
                        Assert.That(script, Does.Contain("'$.order.items'"));
                        Assert.That(script, Does.Contain("'$.\"믱吨굮뿁霋\"'"));

                    }
                    finally
                    {
                        table?.Drop();
                    }
                });
        }

        /// <summary>
        /// Verify JSON index create script with multiple options combined
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Verify_CreateJsonIndex_WithMultipleOptions()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = null;

                    // Create our table with JSON column
                    table = new _SMO.Table(database, "JsonIndexTable_" + Guid.NewGuid());
                    table.Columns.Add(new _SMO.Column(table, "id", _SMO.DataType.Int) { Nullable = false });
                    table.Columns.Add(new _SMO.Column(table, "jsonCol", _SMO.DataType.Json) { Nullable = true });

                    _SMO.Index pkIdx = new _SMO.Index(table, "PK_" + table.Name) 
                    { 
                        IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey, 
                        IndexType = _SMO.IndexType.ClusteredIndex 
                    };
                    pkIdx.IndexedColumns.Add(new _SMO.IndexedColumn(pkIdx, "id"));
                    table.Indexes.Add(pkIdx);
                    table.Create();

                    // Create the JSON index with multiple options
                    _SMO.Index index = new _SMO.Index(table, "JsonIndex_" + table.Name)
                    {
                        IndexType = _SMO.IndexType.JsonIndex,
                        OptimizeForArraySearch = true,
                        FillFactor = 75,
                        DisallowPageLocks = false,
                        DisallowRowLocks = false
                    };
                    _SMO.IndexedColumn jsonColumn = new _SMO.IndexedColumn(index, "jsonCol");
                    index.IndexedColumns.Add(jsonColumn);
 
                    try
                    {                       
                        index.Create();

                        // Refresh and verify
                        table.Indexes.ClearAndInitialize($"[@Name='{SFC.Urn.EscapeString(index.Name)}']", 
                            new string[] { 
                                nameof(_SMO.Index.OptimizeForArraySearch),
                                nameof(_SMO.Index.PadIndex), 
                                nameof(_SMO.Index.DisallowPageLocks),
                                nameof(_SMO.Index.DisallowRowLocks)
                            });
                        var newIndex = table.Indexes[index.Name];
                        Assert.That(newIndex.IndexType, Is.EqualTo(IndexType.JsonIndex));
                        Assert.That(newIndex.OptimizeForArraySearch, Is.EqualTo(true), "OptimizeForArraySearch should be true");
                        //Assert.That(newIndex.PadIndex, Is.EqualTo(true), "PadIndex should be true");
                        Assert.That(newIndex.FillFactor, Is.EqualTo(75), "FillFactor should be 75");

                        // Verify the script contains expected options
                        string script = ScriptSmoObject(newIndex);
                        Assert.That(script, Does.Contain("CREATE JSON INDEX"), 
                            "Script should contain CREATE JSON INDEX");
                        Assert.That(script, Does.Contain("OPTIMIZE_FOR_ARRAY_SEARCH = ON"), 
                            "Script should contain OPTIMIZE_FOR_ARRAY_SEARCH = ON");
                        Assert.That(script, Does.Contain("FILLFACTOR = 75"), 
                            "Script should contain FILLFACTOR = 75");
                        Assert.That(script, Does.Contain("ALLOW_PAGE_LOCKS = ON"), 
                            "Script should contain ALLOW_PAGE_LOCKS = ON");
                        Assert.That(script, Does.Contain("ALLOW_ROW_LOCKS = ON"), 
                            "Script should contain ALLOW_ROW_LOCKS = ON");
                    }
                    finally
                    {
                        table?.Drop();
                    }
            });
        }

        /// <summary>
        /// Verify that attempting to alter a JSON index throws an exception.
        /// Tests Alter(), Alter(IndexOperation), and scripting ALTER operations.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Verify_JsonIndex_Alter_ThrowsException()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = null;

                    try
                    {
                        // Create table with JSON column
                        table = new _SMO.Table(database, "JsonIndexTable_" + Guid.NewGuid());
                        table.Columns.Add(new _SMO.Column(table, "id", _SMO.DataType.Int) { Nullable = false });
                        table.Columns.Add(new _SMO.Column(table, "jsonCol", _SMO.DataType.Json) { Nullable = true });

                        _SMO.Index pkIdx = new _SMO.Index(table, "PK_" + table.Name)
                        {
                            IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey,
                            IndexType = _SMO.IndexType.ClusteredIndex
                        };
                        pkIdx.IndexedColumns.Add(new _SMO.IndexedColumn(pkIdx, "id"));
                        table.Indexes.Add(pkIdx);
                        table.Create();

                        // Create the JSON index
                        _SMO.Index index = new _SMO.Index(table, "JsonIndex_" + table.Name)
                        {
                            IndexType = _SMO.IndexType.JsonIndex
                        };
                        _SMO.IndexedColumn jsonColumn = new _SMO.IndexedColumn(index, "jsonCol");
                        index.IndexedColumns.Add(jsonColumn);
                        index.Create();

                        // Refresh the index to simulate it being loaded from the database
                        table.Indexes.Refresh();
                        var createdIndex = table.Indexes[index.Name];

                        // Test 1: Try to alter the index via Alter() - should throw FailedOperationException
                        createdIndex.FillFactor = 80;
                        var ex1 = Assert.Throws<_SMO.FailedOperationException>(() => createdIndex.Alter(),
                            "Altering a JSON index should throw FailedOperationException");

                        Assert.That(ex1.Message, Does.Contain("Altering JSON indexes is not currently supported").IgnoreCase,
                            "Exception message should mention Altering JSON indexes is not currently supported");

                        // Test 2: Try to alter with Rebuild operation - should throw FailedOperationException
                        var ex2 = Assert.Throws<_SMO.FailedOperationException>(() => createdIndex.Alter(_SMO.IndexOperation.Rebuild),
                            "Altering a JSON index with Rebuild operation should throw FailedOperationException");

                        Assert.That(ex2.Message, Does.Contain("Altering JSON indexes is not currently supported").IgnoreCase,
                            "Exception message should mention Altering JSON indexes is not currently supported");

                        // Test 3: Try to script alter for the index - should throw FailedOperationException
                        var scripter = new _SMO.Scripter(database.Parent);
                        scripter.Options.ScriptForAlter = true;
                        var ex3 = Assert.Throws<_SMO.FailedOperationException>(() => scripter.Script(new _SMO.SqlSmoObject[] { createdIndex }),
                            "Scripting ALTER for a JSON index should throw FailedOperationException");

                        Assert.That(ex3.Message, Does.Contain("Script failed for Index").IgnoreCase,
                            "Exception message should mention script failed");
                    }
                    finally
                    {
                        table?.Drop();
                    }
                });
        }
        #endregion

        #region Resumable Operations Tests

        /// <summary>
        /// Test the options and operations of resumable index rebuild for SMO on SQL2017 and later.
        /// Can't run this test against SQL Azure DB, as it relies on capturing SQL Text,
        /// however there is no difference in scripting between Azure and on-premises for this feature.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express)]
        [SupportedServerVersionRange(MinMajor = 14)]
        public void Index_ResumableRebuildOperation_SQL2017AndAfterOnPrem()
        {
            // Test SMO Resumable Index rebuild through both the index, and resumable index objects.
            //
            this.ExecuteFromDbPool(
            database =>
            {
                TestResumableRebuildOnPrem(database, useResumableIndex: false);
                TestResumableRebuildOnPrem(database, useResumableIndex: true);
            });
        }

        /// <summary>
        /// Tests the options and operations of resumable index rebuild, using either the resumable options
        /// on the index object itself, or the standalone Resumable index object.
        /// </summary>
        /// <param name="database">The test database.</param>
        /// <param name="useResumableIndex">Whether to use the Resumable Index Object or not.</param>
        private void TestResumableRebuildOnPrem(_SMO.Database database, bool useResumableIndex)
        {
            // Prepare: Create a table with the specified rows and columns data
            _SMO.Table table = CreateBasicTable(database, 2, 50);

            // Prepare: Create a clustered index for test.
            _SMO.Index index = table.CreateIndex("idx_" + this.TestContext.TestName, new IndexProperties() {IsClustered = true});
            _SMO.Server server = database.Parent;
            server.SetDefaultInitFields(typeof(_SMO.ResumableIndex), "ResumableOperationState", "MaxDOP", "PercentComplete");

            var initialExcutionModes = server.ConnectionContext.SqlExecutionModes;
            server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;
            _SMO.ResumableIndex resumableIndex = null;

            try
            {
                // Check 1: Rebuild the normal index.
                TraceHelper.TraceInformation("Resumable Index Testing - Rebuilding the normal index");
                index.OnlineIndexOperation = true;
                index.Rebuild();
                index.Refresh();
                AssertResumableOperationState(index.ResumableOperationState, _SMO.ResumableOperationStateType.None);

                // Check 2: Rebuild the resumable index.
                TraceHelper.TraceInformation("Resumable Index Testing - Rebuilding the resumable index");
                index.OnlineIndexOperation = true;
                index.ResumableIndexOperation = true;
                server.ConnectionContext.CapturedSql.Clear();
                index.Rebuild();

                string rebuildScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                AssertResumableOperationScript(rebuildScript, index.Name, table.FullQualifiedName, "REBUILD");
                AssertResumableOperationOption(rebuildScript, "ONLINE", index.OnlineIndexOperation ? "ON" : "OFF");
                AssertResumableOperationOption(rebuildScript, "RESUMABLE", index.ResumableIndexOperation ? "ON" : "OFF");

                // Rebuild the index with the MAXDOP option
                index.MaximumDegreeOfParallelism = 2;
                server.ConnectionContext.CapturedSql.Clear();
                index.Rebuild();

                rebuildScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                AssertResumableOperationOption(rebuildScript, "MAXDOP", index.MaximumDegreeOfParallelism.ToString());

                // Rebuild the index with the MAXDOP and MAX_DURATION options
                index.MaximumDegreeOfParallelism = 3;
                index.ResumableMaxDuration = 1;
                server.ConnectionContext.CapturedSql.Clear();
                index.Rebuild();

                rebuildScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                AssertResumableOperationOption(rebuildScript, "MAXDOP", index.MaximumDegreeOfParallelism.ToString());
                AssertResumableOperationOption(rebuildScript, "MAX_DURATION", index.ResumableMaxDuration.ToString() + " MINUTES");

                // Verify the index state after the rebuild operation.
                index.Refresh();
                AssertResumableOperationState(index.ResumableOperationState, _SMO.ResumableOperationStateType.None);

                // Check 3: Resume the resumable index that is paused currently
                TraceHelper.TraceInformation("Resumable Index Testing - Resuming the resumable index that paused currently");
                PutResumableIndexInPausedState(database, table, index);
                server.ConnectionContext.CapturedSql.Clear();

                // Validate that we found the entry in the list of resumable index operations.
                //
                table.Refresh();
                resumableIndex = table.ResumableIndexes[index.Name];
                AssertResumableOperationState(resumableIndex.ResumableOperationState, _SMO.ResumableOperationStateType.Paused);
                Assert.AreEqual(resumableIndex.PercentComplete, 100.0, "Unexpected value for percent complete after pausing.");

                // Resume the operation either via the index or ResumableIndex object.
                //
                if (!useResumableIndex)
                {
                    index.Resume();
                }
                else
                {
                    resumableIndex.Resume();
                }

                index.Refresh();
                table.ResumableIndexes.Refresh();

                Assert.That(!table.ResumableIndexes.Contains(index.Name), "Should not have found a resumable index for a completed operation.");
                AssertResumableOperationState(index.ResumableOperationState, _SMO.ResumableOperationStateType.None);
                string resumeScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                AssertResumableOperationScript(resumeScript, index.Name, table.FullQualifiedName, "RESUME");

                // Resume the index with low priority
                PutResumableIndexInPausedState(database, table, index);
                table.ResumableIndexes.Refresh();
                resumableIndex = table.ResumableIndexes[index.Name];
                server.ConnectionContext.CapturedSql.Clear();

                // Resume the operation either via the index or ResumableIndex object.
                //
                if (!useResumableIndex)
                {
                    index.LowPriorityMaxDuration = 2;
                    index.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Blockers;
                    index.Resume();
                    resumeScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                    AssertResumableOperationScript(resumeScript, index.Name, table.FullQualifiedName, "RESUME");
                    AssertResumableOperationOption(resumeScript, "WAIT_AT_LOW_PRIORITY (MAX_DURATION", index.LowPriorityMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "ABORT_AFTER_WAIT", index.LowPriorityAbortAfterWait.ToString().ToUpper());
                }
                else
                {
                    resumableIndex.LowPriorityMaxDuration = 2;
                    resumableIndex.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Blockers;
                    resumableIndex.Resume();
                    resumeScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                    AssertResumableOperationScript(resumeScript, index.Name, table.FullQualifiedName, "RESUME");
                    AssertResumableOperationOption(resumeScript, "WAIT_AT_LOW_PRIORITY (MAX_DURATION", resumableIndex.LowPriorityMaxDuration + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "ABORT_AFTER_WAIT", resumableIndex.LowPriorityAbortAfterWait.ToString().ToUpper());
                }

                // Resume the index with MAX_DURATION and low priority
                //
                PutResumableIndexInPausedState(database, table, index);
                server.ConnectionContext.CapturedSql.Clear();

                // Resume the operation either via the index or ResumableIndex object.
                //
                if (!useResumableIndex)
                {
                    index.ResumableMaxDuration = 4;
                    index.LowPriorityMaxDuration = 5;
                    index.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Self;
                    index.Resume();
                    resumeScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                    AssertResumableOperationScript(resumeScript, index.Name, table.FullQualifiedName, "RESUME");
                    AssertResumableOperationOption(resumeScript, "MAX_DURATION", index.ResumableMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "WAIT_AT_LOW_PRIORITY (MAX_DURATION", index.LowPriorityMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "ABORT_AFTER_WAIT", index.LowPriorityAbortAfterWait.ToString().ToUpper());
                }
                else
                {
                    table.ResumableIndexes.Refresh();
                    resumableIndex = table.ResumableIndexes[index.Name];
                    resumableIndex.ResumableMaxDuration = 4;
                    resumableIndex.LowPriorityMaxDuration = 5;
                    resumableIndex.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Self;
                    resumableIndex.Resume();
                    resumeScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                    AssertResumableOperationScript(resumeScript, index.Name, table.FullQualifiedName, "RESUME");
                    AssertResumableOperationOption(resumeScript, "MAX_DURATION", resumableIndex.ResumableMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "WAIT_AT_LOW_PRIORITY (MAX_DURATION", resumableIndex.LowPriorityMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "ABORT_AFTER_WAIT", resumableIndex.LowPriorityAbortAfterWait.ToString().ToUpper());
                }

                // Resume the index with MAXDOP, MAX_DURATION and low priority
                PutResumableIndexInPausedState(database, table, index);
                server.ConnectionContext.CapturedSql.Clear();

                // Resume the operation either via the index or ResumableIndex object.
                //
                if (!useResumableIndex)
                {
                    index.MaximumDegreeOfParallelism = 2;
                    index.ResumableMaxDuration = 1;
                    index.LowPriorityMaxDuration = 2;
                    index.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Blockers;
                    index.Resume();
                    resumeScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                    AssertResumableOperationScript(resumeScript, index.Name, table.FullQualifiedName, "RESUME");
                    AssertResumableOperationOption(resumeScript, "MAXDOP", index.MaximumDegreeOfParallelism.ToString());
                    AssertResumableOperationOption(resumeScript, "MAX_DURATION", index.ResumableMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "WAIT_AT_LOW_PRIORITY (MAX_DURATION", index.LowPriorityMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "ABORT_AFTER_WAIT", index.LowPriorityAbortAfterWait.ToString().ToUpper());

                }
                else
                {
                    table.ResumableIndexes.Refresh();
                    resumableIndex = table.ResumableIndexes[index.Name];
                    resumableIndex.MaxDOP = 2;
                    resumableIndex.ResumableMaxDuration = 1;
                    resumableIndex.LowPriorityMaxDuration = 2;
                    resumableIndex.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Blockers;
                    resumableIndex.Resume();
                    resumeScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                    AssertResumableOperationScript(resumeScript, index.Name, table.FullQualifiedName, "RESUME");
                    AssertResumableOperationOption(resumeScript, "MAXDOP", resumableIndex.MaxDOP.ToString());
                    AssertResumableOperationOption(resumeScript, "MAX_DURATION", resumableIndex.ResumableMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "WAIT_AT_LOW_PRIORITY (MAX_DURATION", resumableIndex.LowPriorityMaxDuration.ToString() + " MINUTES");
                    AssertResumableOperationOption(resumeScript, "ABORT_AFTER_WAIT", resumableIndex.LowPriorityAbortAfterWait.ToString().ToUpper());
                }

                // Check 4: Abort the resumable index that is paused currently
                TraceHelper.TraceInformation("Resumable Index Testing - Abort the resumable index that paused currently");
                PutResumableIndexInPausedState(database, table, index);
                server.ConnectionContext.CapturedSql.Clear();

                // Abort the operation either via the index or ResumableIndex object.
                //
                if (!useResumableIndex)
                {
                    index.Abort();
                }
                else
                {
                    table.ResumableIndexes.Refresh();
                    resumableIndex = table.ResumableIndexes[index.Name];
                    resumableIndex.Abort();
                }

                index.Refresh();
                string abortScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                AssertResumableOperationScript(abortScript, index.Name, table.FullQualifiedName, "ABORT");
                AssertResumableOperationState(index.ResumableOperationState, _SMO.ResumableOperationStateType.None);

                // Check 5: Verify the t-sql script of the pause operation.
                server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                server.ConnectionContext.CapturedSql.Clear();

                // Issue the pause operation either via the index or ResumableIndex object.
                //
                if (!useResumableIndex)
                {
                    index.Pause();
                }
                else
                {
                    resumableIndex.Pause();
                }

                string pauseScript = server.ConnectionContext.CapturedSql.Text.ToSingleString();
                Assert.That(pauseScript, Does.Contain(String.Format("ALTER INDEX {0} ON {1} PAUSE", SmoObjectHelpers.SqlBracketQuoteString(index.Name), table.FullQualifiedName)),
                    "Unexpected script of the pause index operation.");
            }
            finally
            {
                server.ConnectionContext.SqlExecutionModes = initialExcutionModes;
                try
                {
                    table.Drop();
                }
                catch
                { }
            }
        }

        /// <summary>
        /// Tests resumable index rebuild operations against SQL Azure DB.  Does not perform script validation, but
        /// checks the effective functional value of the operation.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_ResumableRebuildOperation_Azure()
        {
            // Test SMO Resumable Index rebuild through both the index, and resumable index objects.
            //
            this.ExecuteFromDbPool(
            database =>
            {
                TestResumableRebuildOnAzure(database, useResumableIndex: false);
                TestResumableRebuildOnAzure(database, useResumableIndex: true);
            });
        }

        /// <summary>
        /// Tests the options and operations of resumable index rebuild, using either the resumable options
        /// on the index object itself, or the standalone Resumable index object for Azure (i.e. no text capture)
        /// </summary>
        /// <param name="database">The test database.</param>
        /// <param name="useResumableIndex">Whether to use the Resumable Index Object or not.</param>
        private void TestResumableRebuildOnAzure(_SMO.Database database, bool useResumableIndex)
        {
            // Prepare: Create a table with the specified rows and columns data
            _SMO.Table table = CreateBasicTable(database, 2, 50);

            // Prepare: Create a clustered index for test.
            _SMO.Index index = table.CreateIndex("idx_" + this.TestContext.TestName, new IndexProperties() { IsClustered = true });
            _SMO.Server server = database.Parent;
            server.SetDefaultInitFields(typeof(_SMO.ResumableIndex), "ResumableOperationState", "MaxDOP", "PercentComplete");
            _SMO.ResumableIndex resumableIndex = null;

            // Check 1: Rebuild the index resumably.
            TraceHelper.TraceInformation("Resumable Index Testing - Rebuilding the resumable index");
            index.OnlineIndexOperation = true;
            index.ResumableIndexOperation = true;
            index.Rebuild();

            // Verify the index state after the rebuild operation completes.
            index.Refresh();
            AssertResumableOperationState(index.ResumableOperationState, _SMO.ResumableOperationStateType.None);

            // Check 2: Resume a paused resumable index operation.
            TraceHelper.TraceInformation("Resumable Index Testing - Resuming a paused resumable index operation");
            PutResumableIndexInPausedState(database, table, index);

            // Validate that we found the entry in the list of resumable index operations.
            //
            table.ResumableIndexes.Refresh();
            resumableIndex = table.ResumableIndexes[index.Name];
            AssertResumableOperationState(resumableIndex.ResumableOperationState, _SMO.ResumableOperationStateType.Paused);
            Assert.AreEqual(resumableIndex.PercentComplete, 100.0, "Unexpected value for percent complete after pausing.");

            // Resume the operation either via the index or ResumableIndex object.
            //
            if (!useResumableIndex)
            {
                index.Resume();
            }
            else
            {
                resumableIndex.Resume();
            }

            index.Refresh();
            table.ResumableIndexes.Refresh();

            Assert.That(!table.ResumableIndexes.Contains(index.Name), "Should not have found a resumable index for a completed operation.");
            AssertResumableOperationState(index.ResumableOperationState, _SMO.ResumableOperationStateType.None);

            // Resume the index with low priority
            PutResumableIndexInPausedState(database, table, index);
            table.ResumableIndexes.Refresh();
            resumableIndex = table.ResumableIndexes[index.Name];

            // Resume the operation either via the index or ResumableIndex object.
            if (!useResumableIndex)
            {
                index.LowPriorityMaxDuration = 2;
                index.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Blockers;
                index.Resume();
            }
            else
            {
                resumableIndex.LowPriorityMaxDuration = 2;
                resumableIndex.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Blockers;
                resumableIndex.Resume();
            }

            // Resume the index with MAX_DURATION and low priority
            PutResumableIndexInPausedState(database, table, index);

            // Resume the operation either via the index or ResumableIndex object.
            if (!useResumableIndex)
            {
                index.ResumableMaxDuration = 4;
                index.LowPriorityMaxDuration = 5;
                index.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Self;
                index.Resume();
            }
            else
            {
                table.ResumableIndexes.Refresh();
                resumableIndex = table.ResumableIndexes[index.Name];
                resumableIndex.ResumableMaxDuration = 4;
                resumableIndex.LowPriorityMaxDuration = 5;
                resumableIndex.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Self;
                resumableIndex.Resume();
            }

            // Resume the index with MAXDOP, MAX_DURATION and low priority
            PutResumableIndexInPausedState(database, table, index);

            // Resume the operation either via the index or ResumableIndex object.
            if (!useResumableIndex)
            {
                index.MaximumDegreeOfParallelism = 2;
                index.ResumableMaxDuration = 1;
                index.LowPriorityMaxDuration = 2;
                index.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Blockers;
                index.Resume();
            }
            else
            {
                table.ResumableIndexes.Refresh();
                resumableIndex = table.ResumableIndexes[index.Name];
                resumableIndex.MaxDOP = 2;
                resumableIndex.ResumableMaxDuration = 1;
                resumableIndex.LowPriorityMaxDuration = 2;
                resumableIndex.LowPriorityAbortAfterWait = _SMO.AbortAfterWait.Blockers;
                resumableIndex.Resume();
            }

            // Check 4: Abort the resumable index that is paused currently
            TraceHelper.TraceInformation("Resumable Index Testing - Abort the resumable index that paused currently");
            PutResumableIndexInPausedState(database, table, index);

            // Abort the operation either via the index or ResumableIndex object.
            if (!useResumableIndex)
            {
                index.Abort();
            }
            else
            {
                table.ResumableIndexes.Refresh();
                resumableIndex = table.ResumableIndexes[index.Name];
                resumableIndex.Abort();
            }

            index.Refresh();
                AssertResumableOperationState(index.ResumableOperationState, _SMO.ResumableOperationStateType.None);
        }

        /// <summary>
        /// Tests that we correctly script the resumable option for create index operations if specified.
        ///
        /// TODO altran: After public preview lightup, remove the dependency on TF 3865.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlDataWarehouse)]
        [SupportedServerVersionRange(MinMajor = 15)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_ResumableCreateOperation_SQL2018AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
            database =>
            {
                // Prepare: Create a table with the specified rows and columns data
                _SMO.Table table = CreateBasicTable(database, 2, 50);
                _SMO.Server server = database.Parent;

                // Enable the TF that allows us to run the index creation with the resumable setting enabled.
                // Only do this for on-prem DBs, as treaceflags are not support in Azure.
                //
                bool enabledTf = false;
                if (server.DatabaseEngineType != DatabaseEngineType.SqlAzureDatabase && server.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance && !server.IsTraceFlagOn(3865, true))
                {
                    database.ExecutionManager.ConnectionContext.ExecuteNonQuery("DBCC TRACEON (3865, -1)");
                    enabledTf = true;
                }

                // Create a clustered index on the table.
                //
                _SMO.Index clIdx = table.CreateIndex("clIdx_" + this.TestContext.TestName, new IndexProperties() { IsClustered = true});

                try
                {
                    // Create a simple non-clustered index on the first column of the table.
                    //
                    _SMO.Index ncIdx = new _SMO.Index(table, "ncIdx_" + this.TestContext.TestName);
                    ncIdx.IndexedColumns.Add(new _SMO.IndexedColumn(ncIdx, table.Columns[0].Name));
                    ncIdx.OnlineIndexOperation = true;
                    ncIdx.IndexType = _SMO.IndexType.NonClusteredIndex;
                    ncIdx.ResumableIndexOperation = true;
                    ncIdx.ResumableMaxDuration = 5;

                    StringCollection stringColl = ncIdx.Script();
                    StringBuilder sb = new StringBuilder();
                    foreach (string statement in stringColl)
                    {
                        sb.AppendLine(statement);
                        TraceHelper.TraceInformation(statement);
                    }
                    string createScript = sb.ToString();

                    // Verify the create script contains the resumable option, as well as the expected maxduration option
                    //
                    Assert.That(createScript, Does.Contain(String.Format("CREATE NONCLUSTERED INDEX {0} ON {1}",
                        SmoObjectHelpers.SqlBracketQuoteString(ncIdx.Name), table.FullQualifiedName)),
                        String.Format("Unexpected script of the create index operation."));
                    AssertResumableOperationOption(createScript, "RESUMABLE", "ON");
                    AssertResumableOperationOption(createScript, "MAX_DURATION", ncIdx.ResumableMaxDuration.ToString() + " MINUTES");

                    // Finally verify that the create actually succeeds when executed.
                    // Only do this for on-prem for now, until this is fully lit up in Azure.
                    //
                    if (server.DatabaseEngineType == DatabaseEngineType.Standalone)
                    {
                        ncIdx.Create();
                    }
                }
                finally
                {
                    // Disable the traceflag allowing clustered index creation resumably if we had to enable it.
                    //
                    if (enabledTf)
                    {
                        database.ExecutionManager.ConnectionContext.ExecuteNonQuery("DBCC TRACEOFF (3865, -1)");
                    }
                }
            });
        }

        /// <summary>
        /// Tests that we correctly script the resumable option for create constraint operation.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_ResumableCreateConstraintOperation()
        {
            this.ExecuteFromDbPool(
            database =>
            {
                // Prepare: Create a table with the specified rows and columns data
                var columns = new ColumnProperties[2];
                for (int columnId = 0; columnId < 2; columnId++)
                {
                    columns[columnId] = new ColumnProperties($"C{columnId.ToString()}") { Nullable = false };
                }

                _SMO.Table table = DatabaseObjectHelpers.CreateTable(
                    database: database,
                    tableNamePrefix: "tbl",
                    columnProperties: columns);
                var scriptOptionsForAlter = new _SMO.ScriptingOptions() { ScriptForAlter = true };

                // Create a simple non-clustered index on the first column of the table.
                //
                _SMO.Index index = new _SMO.Index(table, "ncIdx_" + this.TestContext.TestName)
                {
                    OnlineIndexOperation = true,
                    ResumableIndexOperation = true,
                    ResumableMaxDuration = 5,
                    IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey,
                    IndexType = _SMO.IndexType.NonClusteredIndex
                };
                index.IndexedColumns.Add(new _SMO.IndexedColumn(index, table.Columns[0].Name));
                table.Indexes.Add(index);

                string alterScript1 = ScriptSmoObject(table, scriptOptionsForAlter);

                // Verify the create script contains the resumable option, as well as the expected maxduration option
                //
                Assert.That(alterScript1, Does.Contain($"ALTER TABLE {table.FullQualifiedName} ADD  CONSTRAINT {SmoObjectHelpers.SqlBracketQuoteString(index.Name)}"),
                    "Unexpected script of the create index operation.");
                AssertResumableOperationOption(alterScript1, "RESUMABLE", "ON");
                AssertResumableOperationOption(alterScript1, "MAX_DURATION", index.ResumableMaxDuration.ToString() + " MINUTES");

                table.Alter();

                table.Refresh();
                Assert.IsNotNull(table.Indexes["ncIdx_" + this.TestContext.TestName], "Index not created");
                Assert.That(table.Indexes["ncIdx_" + this.TestContext.TestName].IndexKeyType == _SMO.IndexKeyType.DriPrimaryKey, "Incorrect key type.");

            });
        }

        /// <summary>
        /// Transform the state of the resumable index from the None into the Paused state.
        /// </summary>
        private void PutResumableIndexInPausedState(_SMO.Database database, _SMO.Table table, _SMO.Index index)
        {
            TraceHelper.TraceInformation("Resumable Index Testing - Generating a paused resumable index");

            // Create trigger that will delay for 10 minutes after the rebuild of an index. This will leave the index in the
            // "running" state for those 10 minutes, during which we can cancel the rebuild and then get the paused index.
            _SMO.Server server = database.Parent;
            TraceHelper.TraceInformation("Creating the trigger on database.");
            _SMO.DatabaseDdlTrigger triggerDb = DatabaseObjectHelpers.CreateDatabaseDdlTrigger(database,
                "trg", "AFTER ALTER_INDEX", "WAITFOR DELAY '00:10:00'"); // the delay time is set as 10 minutes.

            // Execute the index rebuild on a separate thread so that we can cancel the operation after starting the rebuild,
            // which will cause the trigger created above to fire and then delay the execution for 10 minutes
            var query = $"ALTER INDEX {index.FullQualifiedName} ON {table} REBUILD WITH (ONLINE = ON, RESUMABLE = ON)";
            var thread = new Thread(() => { try { _ = database.ExecutionManager.ConnectionContext.ExecuteNonQuery(query, ExecutionTypes.ContinueOnError); } catch { } });
            thread.Start();

            // Wait for 10 seconds to give it plenty of time to start the rebuild operation.
            thread.Join(new TimeSpan(0, 0, 10));

            // Cancel the rebuild operation so that the index status will be paused.
            database.ExecutionManager.ConnectionContext.Cancel();
            thread.Join();

            // Refresh the index to update its state correctly after transforming the state of the index.
            index.Refresh();
            AssertResumableOperationState(index.ResumableOperationState, _SMO.ResumableOperationStateType.Paused);

            // Drop the trigger
            triggerDb.Drop();
        }

        /// <summary>
        /// Assert that the current state of the resumable index is right. </summary>
        /// <param name="current_state">The current state of the resumable index. </param>
        /// <param name="expected_state">The expected state of the resumable index.</param>
        private void AssertResumableOperationState(_SMO.ResumableOperationStateType current_state, _SMO.ResumableOperationStateType expected_state)
        {
            Assert.That(current_state, Is.EqualTo(expected_state), "Unexpected state of the resumable index.");
        }

        /// <summary>
        /// Assert the option of the resumable index operation in the script. </summary>
        /// <param name="script">The script is verified. </param>
        /// <param name="optionName">The option name is verified.</param>
        /// <param name="optionValue">The option value is verified. </param>
        private void AssertResumableOperationOption(string script, string optionName, string optionValue)
        {
            Assert.That(script, Does.Contain(String.Format("{0} = {1}", optionName, optionValue)), "Unexpected option of the resumable index operation.");
        }

        /// <summary>
        /// Assert the t-sql script of the resumable index operation. </summary>
        /// <param name="script"></param>
        /// <param name="indexName"></param>
        /// <param name="tableName"></param>
        /// <param name="operationType"></param>
        private void AssertResumableOperationScript(string script, string indexName, string tableName, string operationType)
        {
            Assert.That(script, Does.Contain(String.Format("ALTER INDEX {0} ON {1} {2}", SmoObjectHelpers.SqlBracketQuoteString(indexName), tableName, operationType)),
                        String.Format("Unexpected script of the {0} index operation.", operationType));
        }

        /// <summary>
        /// Create a basic table and insert data with the specified column and row numbers.
        /// Optionally allows specifying the nullable property of every table column.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="columnCount"></param>
        /// <param name="rowCount"></param>
        /// <param name="nullable"></param>
        /// <returns></returns>
        private static _SMO.Table CreateBasicTable(_SMO.Database database, int columnCount, int rowCount, bool nullable = true)
        {
            // Set the column properties with the default column names (C0, C1, ...)
            var columns = new ColumnProperties[columnCount];
            for (int columnId = 0; columnId < columnCount; columnId++)
            {
                columns[columnId] = new ColumnProperties(String.Format("C{0}", columnId.ToString())) { Nullable = nullable };
            }

            _SMO.Table table = DatabaseObjectHelpers.CreateTable(
                database: database,
                tableNamePrefix: "tbl",
                columnProperties: columns);

            TableObjectHelpers.InsertDataToTable(table, rowCount);

            return table;
        }

        #endregion

        #region WaitAtLowPriority Tests
        /// <summary>
        /// Test the index create with the low priority option through SMO.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void Index_CreateWaitAtLowPriority()
        {
            this.ExecuteWithDbDrop(
            database =>
            {
                // Prepare: Create a table with the specified rows and columns data
                _SMO.Table table = CreateBasicTable(database, 2, 50);
                _SMO.Server server = database.Parent;
                int maxDuration = 2;
                _SMO.AbortAfterWait[] abortTypeValues = (_SMO.AbortAfterWait[])Enum.GetValues(typeof(_SMO.AbortAfterWait));
                foreach (_SMO.AbortAfterWait abortType in abortTypeValues)
                {
                    database.Parent.ConnectionContext.CapturedSql.Clear();
                    // Create a simple non-clustered index on the first column of the table.
                    //
                    _SMO.Index ncIdx = new _SMO.Index(table, "ncIdx_" + Guid.NewGuid() + this.TestContext.TestName);
                    ncIdx.IndexedColumns.Add(new _SMO.IndexedColumn(ncIdx, table.Columns[0].Name));
                    ncIdx.IndexType = _SMO.IndexType.NonClusteredIndex;
                    ncIdx.OnlineIndexOperation = true;
                    ncIdx.LowPriorityMaxDuration = maxDuration;
                    ncIdx.LowPriorityAbortAfterWait = abortType;
                    ncIdx.DropExistingIndex = false;

                    StringCollection stringColl = ncIdx.Script();
                    StringBuilder sb = new StringBuilder();
                    foreach (string statement in stringColl)
                    {
                        sb.AppendLine(statement);
                        TraceHelper.TraceInformation(statement);
                    }
                    string createScript = sb.ToString();

                    // Verify the create script contains the resumable option, as well as the expected maxduration option
                    //
                    Assert.That(createScript, Does.Contain(String.Format("CREATE NONCLUSTERED INDEX {0} ON {1}",
                        SmoObjectHelpers.SqlBracketQuoteString(ncIdx.Name), table.FullQualifiedName)),
                        String.Format("Unexpected script of the create index operation."));
                    Assert.That(createScript, Does.Contain(String.Format("ONLINE = ON (WAIT_AT_LOW_PRIORITY (MAX_DURATION = {0} MINUTES", maxDuration.ToString())),
                            "Unexpected option of the WAIT_AT_LOW_PRIORITY property");
                    Assert.That(createScript, Does.Contain(String.Format("ABORT_AFTER_WAIT = {0}", abortType.ToString().ToUpper())),
                            "Unexpected option of the WAIT_AT_LOW_PRIORITY property");
                     ncIdx.Create();
                }
            });
        }


        /// <summary>
        /// Test the index drop and constraint drop with the low priority option through SMO on SQL2017 and later.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        [SupportedServerVersionRange(MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express)]
        public void Index_DropWaitAtLowPriority_Sql2017AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // Prepare: Create table and insert some data;
                    _SMO.Table table = CreateBasicTable(database, 1, 10, false);

                    // Prepare: Set the execution mode to ExecuteAndCaptureSql for the connection.
                    _SMO.Server server = database.Parent;
                    var initialExcutionModes = server.ConnectionContext.SqlExecutionModes;
                    server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;

                    // Test the index drop and constraint drop with low priority option with considering all possible properties of the tested index.
                    // The index properties considered here include IndexType (NonClusteredIndex, ClusteredIndex), IsClustered (true, false),
                    // OnlineIndexOperation (ture, false) and KeyType (None, DriUniqueKey, DriPrimaryKey).
                    // Specifically, the clustered and offline index is not supported to drop with the low priority option.
                    try
                    {
                        // Check 1: Check the non_clustered index dropped with the low priority option
                        CheckIndexDroppingAtLowPriority(database, table, "non_cluster_index", true,
                            new IndexProperties() { IndexType = _SMO.IndexType.NonClusteredIndex, IsClustered = false, OnlineIndexOperation = true });

                        // Check 2: Check the clustered and offline/online index dropped with the low priority option, where the online index drop should not success.
                        CheckIndexDroppingAtLowPriority(database, table, "cluster_index", false,
                            new IndexProperties() { IndexType = _SMO.IndexType.ClusteredIndex, IsClustered = true, OnlineIndexOperation = false });

                        CheckIndexDroppingAtLowPriority(database, table, "cluster_online_index", true,
                            new IndexProperties() { IndexType = _SMO.IndexType.ClusteredIndex, IsClustered = true, OnlineIndexOperation = true });

                        // Check 3: Check the unique key and non_clustered constraint dropped with the low priority option.
                        CheckIndexDroppingAtLowPriority(database, table, "unique_non_cluster_index", true,
                            new IndexProperties() { IndexType = _SMO.IndexType.NonClusteredIndex, IsClustered = false, OnlineIndexOperation = false, KeyType = _SMO.IndexKeyType.DriUniqueKey });

                        // Check 4: Check the unique key, clustered and offline/online constraint dropped with the low priority option, where the online constraint drop should not success.
                        CheckIndexDroppingAtLowPriority(database, table, "unique_cluster_index", false,
                            new IndexProperties() { IsClustered = true, OnlineIndexOperation = false, KeyType = _SMO.IndexKeyType.DriUniqueKey });

                        CheckIndexDroppingAtLowPriority(database, table, "unique_cluster_online_index",true,
                            new IndexProperties() { IsClustered = true, OnlineIndexOperation = true, KeyType = _SMO.IndexKeyType.DriUniqueKey });

                        // Check 5: Check the primary key and non_clustered constraint dropped with the low priority option.
                        CheckIndexDroppingAtLowPriority(database, table, "primary_non_cluster_index", true,
                            new IndexProperties() { IndexType = _SMO.IndexType.NonClusteredIndex, IsClustered = false, OnlineIndexOperation = false, KeyType = _SMO.IndexKeyType.DriPrimaryKey });

                        // Check 6: Check the primary key, clustered and offline/online constraint dropped with the low priority option, where the online constraint drop should not success.
                        CheckIndexDroppingAtLowPriority(database, table, "primary_cluster_index", false,
                            new IndexProperties() { IsClustered = true, OnlineIndexOperation = false, KeyType = _SMO.IndexKeyType.DriPrimaryKey });

                        CheckIndexDroppingAtLowPriority(database, table, "primary_cluster_online_index", true,
                            new IndexProperties() { IsClustered = true, OnlineIndexOperation = true, KeyType = _SMO.IndexKeyType.DriPrimaryKey });
                    }
                    finally
                    {
                        server.ConnectionContext.SqlExecutionModes = initialExcutionModes;
                    }
              });
        }

        /// <summary>
        /// This helper function is used to verify the low priority option and its related sub-options for the index drop.
        /// The tested index properties are specified by the indexProperties that includes the IndexType, IsClustered, OnlineIndexOperation and IndexkeyType properties.
        /// Specifically, the clustered and offline index is not supported to drop with the low priority option. </summary>
        /// <param name="database"></param>
        /// <param name="table"></param>
        /// <param name="namePrefix">The prefix name of the index that will be created.</param>
        /// <param name="shouldSucceed">The boolean value validates whether the low priority was as expected </param>
        /// <param name="indexProperties">The specified properties of the index.</param>
        private void CheckIndexDroppingAtLowPriority(_SMO.Database database, _SMO.Table table, string namePrefix, bool shouldSucceed, IndexProperties indexProperties)
        {
            // the sub-options of the low priority where all values of the abort type will be enumerated and then tested in the index drop.
            int maxDuration = 2;
            _SMO.AbortAfterWait[] abortTypeValues = (_SMO.AbortAfterWait[])Enum.GetValues(typeof(_SMO.AbortAfterWait));

            foreach (_SMO.AbortAfterWait abortType in abortTypeValues)
            {
                // Create an index whose properties are specified by the IndexProperties.
                _SMO.Index index = table.CreateIndex(namePrefix, indexProperties);

                // Save the default values of the sub-options related with the low priority option.
                int defaultMaxDuration = index.LowPriorityMaxDuration;
                _SMO.AbortAfterWait defaultAbortType = index.LowPriorityAbortAfterWait;

                // Set the sub-options values of the low priority so that the index will be dropped by low priority.
                index.LowPriorityMaxDuration = maxDuration;
                index.LowPriorityAbortAfterWait = abortType;

                database.Parent.ConnectionContext.CapturedSql.Clear();

                try
                {
                    index.Drop();

                    // Throw an error if the index drop is successful but it is expected to fail.
                    Assert.True(shouldSucceed, String.Format("Dropping index with low priority should not be successed. IsClustered:{0}, IsOnline:{1}, KeyType{2}",
                            indexProperties.IsClustered ? "True" : "False",
                            indexProperties.OnlineIndexOperation ? "On" : "Off",
                            indexProperties.KeyType.ToString()));

                    // Verify the sub-options values of the low priority that is used for the index drop.
                    string script = database.Parent.ConnectionContext.CapturedSql.Text.ToSingleString();
                    TraceHelper.TraceInformation(script);
                    AssertDropOperationScript(script, table, index, indexProperties);
                    Assert.That(script, Does.Contain(String.Format("WAIT_AT_LOW_PRIORITY (MAX_DURATION = {0} MINUTES", maxDuration.ToString())),
                            "Unexpected option of the WAIT_AT_LOW_PRIORITY property");
                    Assert.That(script, Does.Contain(String.Format("ABORT_AFTER_WAIT = {0}", abortType.ToString().ToUpper())),
                            "Unexpected option of the WAIT_AT_LOW_PRIORITY property");
                }
                catch (_SMO.SmoException e)
                {
                    // Throw an error if the index drop is failed but it is expected to success.
                    Assert.True(!shouldSucceed, String.Format("Dropping index with low priority should be successed. IsClustered:{0}, IsOnline:{1}, KeyType{2}\nException:{3}",
                            indexProperties.IsClustered ? "True" : "False",
                            indexProperties.OnlineIndexOperation ? "On" : "Off",
                            indexProperties.KeyType.ToString(),
                            e.Message));

                    // Set the sub-options into the default values so that the index will be dropped without the low priority option.
                    index.LowPriorityMaxDuration = defaultMaxDuration;
                    index.LowPriorityAbortAfterWait = defaultAbortType;

                    index.Drop();
                }
            }
        }

        /// <summary>
        /// Assert the basic t-sql script of the index drop operation. </summary>
        /// <param name="script"></param>
        /// <param name="table"></param>
        /// <param name="index"></param>
        /// <param name="indexProperties"></param>
        private void AssertDropOperationScript(string script, _SMO.Table table, _SMO.Index index, IndexProperties indexProperties)
        {
            // Verify the tsql script of the index drop.
            if (indexProperties.KeyType == _SMO.IndexKeyType.None)
            {
                Assert.That(script, Does.Contain(String.Format("DROP INDEX {0} ON {1}", SmoObjectHelpers.SqlBracketQuoteString(index.Name), table.FullQualifiedName)));
            }
            else
            {
                Assert.That(script, Does.Contain(String.Format("ALTER TABLE {0} DROP CONSTRAINT {1}", table.FullQualifiedName, SmoObjectHelpers.SqlBracketQuoteString(index.Name))));
            }
        }

        #endregion

        #region Drop options Tests

        /// <summary>
        /// Test the drop operation with different options for the regular index and the constraint through SMO on all onprem server.
        /// where the options include with the "maxdop/online/move to".
        /// </summary>
        [_VSUT.TestMethod]
        [_VSUT.Timeout(3600000)]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        public void Index_DropOptions_ALL_ONPREM_SERVERS()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // Prepare: Create table and insert some data;
                    _SMO.Table table = CreateBasicTable(database, 1, 10, false);

                    // Prepare: Set the execution mode to ExecuteAndCaptureSql for the connection.
                    _SMO.Server server = database.Parent;
                    var initialExcutionModes = server.ConnectionContext.SqlExecutionModes;
                    server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;

                    // Test the index drop operation with the helper function, where the type of the tested indexes include
                    // the non_clustered regular index, the clustered regular index, the unique key constraint and the primary key constraint.
                    try
                    {
                        // Check 1: Check the non_clustered regular index dropped with the different options
                        DropIndexWithDifferentOptions(database, table, "non_cluster_index",
                            new IndexProperties() { IndexType = _SMO.IndexType.NonClusteredIndex, IsClustered = false });

                        // Check 2: Check the clustered regular index dropped with the different options.
                        DropIndexWithDifferentOptions(database, table, "cluster_index",
                            new IndexProperties() { IndexType = _SMO.IndexType.ClusteredIndex, IsClustered = true });

                        // Check 3: Check the unique key constraint dropped with the different options.
                        DropIndexWithDifferentOptions(database, table, "unique_cluster_index",
                            new IndexProperties() { IndexType = _SMO.IndexType.ClusteredIndex, IsClustered = true, KeyType = _SMO.IndexKeyType.DriUniqueKey });

                        // Check 4: Check the primary key constraint dropped with thedifferent options.
                        DropIndexWithDifferentOptions(database, table, "primary_cluster_index",
                            new IndexProperties() { IndexType = _SMO.IndexType.ClusteredIndex, IsClustered = true, KeyType = _SMO.IndexKeyType.DriPrimaryKey });
                    }
                    finally
                    {
                        server.ConnectionContext.SqlExecutionModes = initialExcutionModes;
                    }


                });
        }

        /// <summary>
        /// This helper function is used to test the index drop operations and its related options.
        /// The tested index properties are specified by the indexProperties that includes the IndexType, IsClustered, OnlineIndexOperation and IndexkeyType properties.
        /// Specifically, if the index is regular and clustered, it could also support the DropAndMove operation. </summary>
        /// <param name="database"></param>
        /// <param name="table"></param>
        /// <param name="namePrefix">The prefix name of the index that will be created.</param>
        /// <param name="indexProperties">The specified properties of the index.</param>
        private void DropIndexWithDifferentOptions(_SMO.Database database, _SMO.Table table, string namePrefix, IndexProperties indexProperties)
        {
            // The enumerated values for the online/maxdop/move-to options are set as the remainders of the iterator divided by 2, 5, 3 respectively.
            // These three numbers are relatively prime, so if the iteratorCount is setted as the lowest common multiple of them (30),
            // all combinations values of three dimensions ([0,1]*[0,4]*[0,2]) would be enumerated in the following loop.
            const int iteratorCount = 30;
            for (int iterator = 0; iterator < iteratorCount; iterator++)
            {
                // Create an index whose properties are specified by the IndexProperties.
                _SMO.Index index = table.CreateIndex(namePrefix, indexProperties);

                // Generate the option values based on the iterator.
                // For the value of the dropType here, 0 represents the Drop operation, 1 represents the DropAndMove operation with
                // the file group parameter, 2 represents the DropAndMove operation with the partition scheme parameter.
                bool isOnline = index.IsOnlineRebuildSupported ? (iterator % 2 == 0) : false;
                int maxDegreeOfParallelism = iterator % 5;
                int dropType = iterator % 3;

                index.OnlineIndexOperation = isOnline;
                index.MaximumDegreeOfParallelism = maxDegreeOfParallelism;

                string dropScript;
                bool isClusteredRegularIndex = (indexProperties.IsClustered == true && indexProperties.KeyType == _SMO.IndexKeyType.None);

                // Only the regular and clustered index supports the DropAndMove operation for now.
                if (isClusteredRegularIndex && dropType == 1)
                {
                    // drop index and move data to a new fileGroup.
                    _SMO.FileGroup fileGroup = DatabaseObjectHelpers.CreateFileGroupWithDataFile(database, index.Name);
                    database.Parent.ConnectionContext.CapturedSql.Clear();
                    index.DropAndMove(fileGroup.Name);
                    dropScript = database.Parent.ConnectionContext.CapturedSql.Text.ToSingleString();

                    // verify the "MOVE TO" option of the index drop.
                    Assert.That(dropScript, Does.Contain(String.Format("MOVE TO {0}", fileGroup.FullQualifiedName)));
                }
                else if (isClusteredRegularIndex && dropType == 2)
                {
                    // drop index and move data to a new partition scheme.
                    object[] val = new object[] { "3", "5"};
                    _SMO.PartitionScheme partitionScheme = DatabaseObjectHelpers.CreatePartitionSchemeWithFileGroups(database, index.Name, val, _SMO.DataType.Int);
                    database.Parent.ConnectionContext.CapturedSql.Clear();
                    index.DropAndMove(partitionScheme.Name, new StringCollection { table.Columns[0].Name });
                    dropScript = database.Parent.ConnectionContext.CapturedSql.Text.ToSingleString();

                    // verify the "MOVE TO" option of the index drop.
                    Assert.That(dropScript, Does.Contain(String.Format("MOVE TO {0}", partitionScheme.FullQualifiedName)));
                }
                else{
                    // drop index
                    database.Parent.ConnectionContext.CapturedSql.Clear();
                    index.Drop();
                    dropScript = database.Parent.ConnectionContext.CapturedSql.Text.ToSingleString();
                }

                // Verify the tsql script of the index drop.
                AssertDropOperationScript(dropScript, table, index, indexProperties);

                // Verify the ONLINE and MAXDOP options of the index drop.
                if (indexProperties.IsClustered == true)
                {
                    Assert.That(dropScript, Does.Contain(String.Format("ONLINE = {0}", isOnline? "ON":"OFF")));

                    if (maxDegreeOfParallelism != 0)
                    {
                        Assert.That(dropScript, Does.Contain(String.Format("MAXDOP = {0}", maxDegreeOfParallelism)));
                    }
                }
            }
        }

        #endregion

        #region IsOptimizedForSequentialKey Property Tests

        /// <summary>
        /// Test the index property IsOptimizedForSequentialKey which is introduced in SQL2019.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_TestIsOptimizedForSequentialKeyPropertyForRegularIndex()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    // CREATE INDEX
                    TestIsOptimizedForSequentialKey_CreateIndex(database);

                    // ALTER INDEX
                    TestIsOptimizedForSequentialKey_AlterIndex(database);
                });
        }

        /// <summary>
        /// Test the index property IsOptimizedForSequentialKey which is introduced in SQL2019.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_TestIsOptimizedForSequentialKeyPropertyForKeyConstraint()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    var t1 = new _SMO.Table(database, GenerateSmoObjectName("cnst_t1"));
                    var t2 = new _SMO.Table(database, GenerateSmoObjectName("cnst_t2"));


                    // CREATE TABLE WITH CONSTRAINT
                    TestIsOptimizedForSequentialKey_CreateTableAddConstraint(database, t1, t2);

                    // ALTER TABLE ADD CONSTRAINT
                    TestIsOptimizedForSequentialKey_AlterTableAddConstraint(database, t1, t2);
                });
        }

        /// <summary>
        /// Test the index property IsOptimizedForSequentialKey which is introduced in SQL2019.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_TestIsOptimizedForSequentialKeyPropertyForUnsupportedIndexType()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    // Negative: ColumnStore indexes
                    TestIsOptimizedForSequentialKey_ColumnstoreIndexNegative(database);

                    // Negative: XML indexes
                    TestIsOptimizedForSequentialKey_XmlIndexNegative(database);

                    // Negative: Spatial indexes
                    try
                    {
                        TestIsOptimizedForSequentialKey_SpatialIndexNegative(database);
                    }
                    catch(Exception) when (this.ServerContext.DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
                    {
                        // Spatial Index uses CLR (Common language Runtime) and this is not supported on
                        // SqlDatabaseEdge. Hence we can ignore this test for Edge
                        //
                    }
                });
        }

        /// <summary>
        /// Tests creating a clustered columnstore index
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 12)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_CreateClusteredColumnstoreIndex()
        {
            this.ExecuteFromDbPool(database =>
            {
                // Create a table with several columns
                var table = new _SMO.Table(database, "CreateClusteredColumnstoreIndex");
                table.Columns.Add(new _SMO.Column(table, "Id", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value1", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value2", _SMO.DataType.Int) { Nullable = false });
                table.Create();

                // Create a clustered columnstore index
                var index = new _SMO.Index(table, "CCS_Idx");
                index.IndexType = _SMO.IndexType.ClusteredColumnStoreIndex;

                // Add columns
                var col1 = new _SMO.IndexedColumn(index, "Value1");
                var col2 = new _SMO.IndexedColumn(index, "Value2");

                index.IndexedColumns.Add(col1);
                index.IndexedColumns.Add(col2);

                // Script and verify the index creation T-SQL
                var script = ScriptSmoObject(index);
                Assert.That(script.NormalizeWhitespace(), Does.Contain("CREATE CLUSTERED COLUMNSTORE INDEX [CCS_Idx] ON [dbo].[CreateClusteredColumnstoreIndex]"), "incorrect in index script.");

                index.Create();
                table.Indexes.ClearAndInitialize(string.Empty, new string[0]);
                Assert.That(table.Indexes.Select(i => i.Name), Has.Exactly(1).EqualTo("CCS_Idx"));

                index.Drop();
                table.Drop();
            });
        }

        /// <summary>
        /// Tests creating a clustered columnstore index with an ORDER clause using ColumnStoreOrderOrdinal.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_CreateClusteredColumnstoreIndex_WithOrderClause_ColumnStoreOrderOrdinal()
        {
            this.ExecuteFromDbPool(database =>
            {
                // Create a table with several columns
                var table = new _SMO.Table(database, "OCCI");
                table.Columns.Add(new _SMO.Column(table, "Id", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value1", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value2", _SMO.DataType.Int) { Nullable = false });
                table.Create();

                // Create a clustered columnstore index with an ORDER clause
                var index = new _SMO.Index(table, "CCI_Ordered_Idx");
                index.IndexType = _SMO.IndexType.ClusteredColumnStoreIndex;

                // Add columns and set ColumnStoreOrderOrdinal property
                var col1 = new _SMO.IndexedColumn(index, "Value1");
                var col2 = new _SMO.IndexedColumn(index, "Value2");

                col1.ColumnStoreOrderOrdinal = 1; // First column in the ORDER clause
                col2.ColumnStoreOrderOrdinal = 2; // Second column in the ORDER clause

                index.IndexedColumns.Add(col1);
                index.IndexedColumns.Add(col2);

                // Script and verify the index creation T-SQL
                var script = ScriptSmoObject(index);
                Assert.That(script.NormalizeWhitespace(), Does.Contain("CREATE CLUSTERED COLUMNSTORE INDEX [CCI_Ordered_Idx] ON [dbo].[OCCI] ORDER ([Value1],[Value2])"), "ORDER clause not found or incorrect in index script.");

                index.Create();
                table.Indexes.ClearAndInitialize(string.Empty, new string[0]);
                Assert.That(table.Indexes.Select(i => i.Name), Has.Exactly(1).EqualTo("CCI_Ordered_Idx"));

                index.Drop();
                table.Drop();
            });
        }

        /// <summary>
        /// Tests creating a nonClustered columnstore index with an ORDER clause using ascending order.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_CreateNonClusteredColumnstoreIndex_WithOrderClause_ASC()
        {
            this.ExecuteFromDbPool(database =>
            {
                // Create a table with several columns
                var table = new _SMO.Table(database, "ONCCIASC");
                table.Columns.Add(new _SMO.Column(table, "Id", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value1", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value2", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value3", _SMO.DataType.Int) { Nullable = false });
                table.Create();

                // Create a nonclustered columnstore index with an ORDER clause
                var index = new _SMO.Index(table, "NCCS_Ordered_Idx");
                index.IndexType = _SMO.IndexType.NonClusteredColumnStoreIndex;

                // Add columns and set ColumnStoreOrderOrdinal property
                var col1 = new _SMO.IndexedColumn(index, "Value1");
                var col2 = new _SMO.IndexedColumn(index, "Value2");
                var col3 = new _SMO.IndexedColumn(index, "Value3");

                col1.ColumnStoreOrderOrdinal = 1; // First column in the ORDER clause
                col2.ColumnStoreOrderOrdinal = 2; // Second column in the ORDER clause
                col3.ColumnStoreOrderOrdinal = 3; // Third column in the ORDER clause

                index.IndexedColumns.Add(col1);
                index.IndexedColumns.Add(col2);
                index.IndexedColumns.Add(col3);

                // Script and verify the index creation T-SQL
                var script = ScriptSmoObject(index);
                Assert.That(script.NormalizeWhitespace(), Does.Contain("CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCS_Ordered_Idx] ON [dbo].[ONCCIASC] ( [Value1], [Value2], [Value3] )ORDER ([Value1],[Value2],[Value3])"), "ORDER clause not found or incorrect in index script.");

                index.Create();
                table.Indexes.ClearAndInitialize(string.Empty, new string[0]);
                Assert.That(table.Indexes.Select(i => i.Name), Has.Exactly(1).EqualTo("NCCS_Ordered_Idx"));

                index.Drop();
                table.Drop();
            });
        }

        /// <summary>
        /// Tests creating a nonClustered columnstore index with an ORDER clause using descending order.
        /// </summary>
        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_CreateNonClusteredColumnstoreIndex_WithOrderClause_DES()
        {
            this.ExecuteFromDbPool(database =>
            {
                // Create a table with several columns
                var table = new _SMO.Table(database, "NCCSOrderedTable");
                table.Columns.Add(new _SMO.Column(table, "Id", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value1", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value2", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value3", _SMO.DataType.Int) { Nullable = false });
                table.Create();

                // Create a nonclustered columnstore index with an ORDER clause
                var index = new _SMO.Index(table, "NCCS_Ordered_Idx");
                index.IndexType = _SMO.IndexType.NonClusteredColumnStoreIndex;

                // Add columns and set ColumnStoreOrderOrdinal property
                var col1 = new _SMO.IndexedColumn(index, "Value1");
                var col2 = new _SMO.IndexedColumn(index, "Value2");
                var col3 = new _SMO.IndexedColumn(index, "Value3");

                col1.ColumnStoreOrderOrdinal = 3; // Thrid column in the ORDER clause
                col2.ColumnStoreOrderOrdinal = 2; // Second column in the ORDER clause
                col3.ColumnStoreOrderOrdinal = 1; // First column in the ORDER clause

                index.IndexedColumns.Add(col1);
                index.IndexedColumns.Add(col2);
                index.IndexedColumns.Add(col3);

                // Script and verify the index creation T-SQL
                var script = ScriptSmoObject(index);
                Assert.That(script.NormalizeWhitespace(), Does.Contain("CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCS_Ordered_Idx] ON [dbo].[NCCSOrderedTable] ( [Value1], [Value2], [Value3] )ORDER ([Value3],[Value2],[Value1])"), "ORDER clause not found or incorrect in index script.");

                index.Create();
                table.Indexes.ClearAndInitialize(string.Empty, new string[0]);
                Assert.That(table.Indexes.Select(i => i.Name), Has.Exactly(1).EqualTo("NCCS_Ordered_Idx"));

                index.Drop();
                table.Drop();
            });
        }

        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void Index_CreateClusteredColumnstoreIndex_WithCompressionDelay()
        {
            this.ExecuteFromDbPool(database =>
            {
                // Create a table
                var table = new _SMO.Table(database, "CompressedTableForCCI");
                table.Columns.Add(new _SMO.Column(table, "Id", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value", _SMO.DataType.Int) { Nullable = false });
                table.Create();

                // Create a clustered columnstore index with specific options
                var index = new _SMO.Index(table, "SmoBaselineVerification_CompressedColumnstoreIndexWithCompressionDelay");
                index.IndexType = _SMO.IndexType.ClusteredColumnStoreIndex;
                index.CompressionDelay = 0;
                index.DropExistingIndex = false;
                index.FileGroup = "PRIMARY";
                table.Indexes.Add(index);

                // Script and verify the index creation T-SQL
                var script = ScriptSmoObject(index);

                Assert.That(
                    script.NormalizeWhitespace(),
                    Does.Contain(
                        "CREATE CLUSTERED COLUMNSTORE INDEX [SmoBaselineVerification_CompressedColumnstoreIndexWithCompressionDelay] ON [dbo].[CompressedTableForCCI] WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0) ON [PRIMARY]"
                    ),
                    "Clustered columnstore index script does not match expected T-SQL."
                );

                index.Create();
                table.Indexes.ClearAndInitialize(string.Empty, new string[0]);
                Assert.That(table.Indexes.Select(i => i.Name), Has.Exactly(1).EqualTo("SmoBaselineVerification_CompressedColumnstoreIndexWithCompressionDelay"));

                index.Drop();
                table.Drop();
            });
        }

        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_CreateClusteredColumnstoreIndex_WithInclude_NoClauseGenerate()
        {
            this.ExecuteFromDbPool(database =>
            {
                // Create a table with several columns
                var table = new _SMO.Table(database, "CCIWithInclude");
                table.Columns.Add(new _SMO.Column(table, "Id", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value1", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value2", _SMO.DataType.Int) { Nullable = false });
                table.Create();

                // Create a clustered columnstore index with INCLUDE columns (not supported)
                var index = new _SMO.Index(table, "CCS_Idx");
                index.IndexType = _SMO.IndexType.ClusteredColumnStoreIndex;

                var col1 = new _SMO.IndexedColumn(index, "Value1");
                var col2 = new _SMO.IndexedColumn(index, "Value2");
                col1.IsIncluded = true;
                col2.IsIncluded = true;

                index.IndexedColumns.Add(col1);
                index.IndexedColumns.Add(col2);

                var script = ScriptSmoObject(index);

                Assert.That(
                    script.NormalizeWhitespace(),
                    Does.Contain(
                        "CREATE CLUSTERED COLUMNSTORE INDEX [CCS_Idx] ON [dbo].[CCIWithInclude] WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)"
                    ),
                    "Clustered columnstore index script does not match expected T-SQL."
                );

                index.Create();
                table.Indexes.ClearAndInitialize(string.Empty, new string[0]);
                Assert.That(table.Indexes.Select(i => i.Name), Has.Exactly(1).EqualTo("CCS_Idx"));

                index.Drop();
                table.Drop();
            });
        }

        [_VSUT.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Index_CreateNonClusteredColumnstoreIndex_WithInclude_NoClauseGenerate()
        {
            this.ExecuteFromDbPool(database =>
            {
                // Create a table with several columns
                var table = new _SMO.Table(database, "NCCIWithInclude");
                table.Columns.Add(new _SMO.Column(table, "Id", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value1", _SMO.DataType.Int) { Nullable = false });
                table.Columns.Add(new _SMO.Column(table, "Value2", _SMO.DataType.Int) { Nullable = false });
                table.Create();

                // Create a clustered columnstore index with INCLUDE columns (not supported)
                var index = new _SMO.Index(table, "CCS_Idx");
                index.IndexType = _SMO.IndexType.NonClusteredColumnStoreIndex;

                var col1 = new _SMO.IndexedColumn(index, "Value1");
                var col2 = new _SMO.IndexedColumn(index, "Value2");
                col1.IsIncluded = true;
                col2.IsIncluded = true;

                index.IndexedColumns.Add(col1);
                index.IndexedColumns.Add(col2);

                var script = ScriptSmoObject(index);

                Assert.That(
                    script.NormalizeWhitespace(),
                    Does.Contain(
                        "CREATE NONCLUSTERED COLUMNSTORE INDEX [CCS_Idx] ON [dbo].[NCCIWithInclude] ( [Value1], [Value2] )WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)"
                    ),
                    "NonClustered columnstore index script does not match expected T-SQL."
                );

                index.Create();
                table.Indexes.ClearAndInitialize(string.Empty, new string[0]);

                Assert.That(table.Indexes.Select(i => i.Name), Has.Exactly(1).EqualTo("CCS_Idx"));

                index.Drop();
                table.Drop();
            });
        }

        /// <summary>
        /// Test property IsOptimizedForSequentialKey in CREATE INDEX scenarios.
        /// </summary>
        private void TestIsOptimizedForSequentialKey_CreateIndex(_SMO.Database database)
        {
            const string tableName = "t1";

            var t1 = new _SMO.Table(database, tableName);

            t1.Columns.Add(new _SMO.Column(t1, "c1", _SMO.DataType.Int) { Nullable = false });
            t1.Columns.Add(new _SMO.Column(t1, "c2", _SMO.DataType.Int) { Nullable = false });
            t1.Columns.Add(new _SMO.Column(t1, "c3", _SMO.DataType.Int) { Nullable = false });

            t1.Create();

            // Create indexes with the property IsOptimizedForSequentialKey
            // and check if the generated scripts contain the T-SQL index option.

            // index1

            var index1 = new _SMO.Index(t1, "idx1");
            index1.IndexedColumns.Add(new _SMO.IndexedColumn(index1, t1.Columns[0].Name));
            index1.IsClustered = true;
            index1.DisallowPageLocks = true;
            index1.FillFactor = 80;
            index1.IsOptimizedForSequentialKey = true; // the property of interest

            AssertIsOptimizedForSequentialKeyOption(ScriptSmoObject(index1), expectedValue: true);
            Assert.That(() => { index1.Create(); }, Throws.Nothing, "index1 creation should succeed.");

            // index2

            var index2 = new _SMO.Index(t1, "idx2");
            index2.IndexedColumns.Add(new _SMO.IndexedColumn(index2, t1.Columns[1].Name));
            index2.IsClustered = false;
            index2.DisallowPageLocks = true;
            index2.FillFactor = 80;
            index2.IsOptimizedForSequentialKey = true; // the property of interest

            AssertIsOptimizedForSequentialKeyOption(ScriptSmoObject(index2), expectedValue: true);
            Assert.That(() => { index2.Create(); }, Throws.Nothing, "index2 creation should succeed.");

            // index3

            var index3 = new _SMO.Index(t1, "idx3");
            index3.IndexedColumns.Add(new _SMO.IndexedColumn(index3, t1.Columns[2].Name));
            index3.IsClustered = false;
            index3.IsOptimizedForSequentialKey = false; // the property of interest

            AssertIsOptimizedForSequentialKeyOption(ScriptSmoObject(index3), expectedValue: false);
            Assert.That(() => { index3.Create(); }, Throws.Nothing, "index3 creation should succeed.");
        }

        /// <summary>
        /// Test property IsOptimizedForSequentialKey in ALTER INDEX scenarios.
        /// </summary>
        private void TestIsOptimizedForSequentialKey_AlterIndex(_SMO.Database database)
        {
            const string tableName = "t1";

            var scriptOptionsForAlter = new _SMO.ScriptingOptions() { ScriptForAlter = true };

            var t1 = database.Tables[tableName];

            var index1 = t1.Indexes["idx1"];
            var index2 = t1.Indexes["idx2"];
            var index3 = t1.Indexes["idx3"];

            // Alter indexes with the property IsOptimizedForSequentialKey
            // and check if the generated scripts contain the T-SQL index option.

            // index1

            Assert.That(index1.IsOptimizedForSequentialKey, Is.True, "Unexpected IsOptimizedForSequentialKey value on index1");

            index1.DisallowPageLocks = false;
            index1.IsOptimizedForSequentialKey = false; // the property of interest

            string alterScript1 = ScriptSmoObject(index1, scriptOptionsForAlter);
            AssertIsOptimizedForSequentialKeyOption(alterScript1, expectedValue: false);
            Assert.That(() => { index1.Alter(); }, Throws.Nothing, "Alter index1 should succeed.");

            // index2

            Assert.That(index2.IsOptimizedForSequentialKey, Is.True, "Unexpected IsOptimizedForSequentialKey value on index2");

            index2.IsOptimizedForSequentialKey = false; // the property of interest

            string alterScript2 = ScriptSmoObject(index2, scriptOptionsForAlter);
            AssertIsOptimizedForSequentialKeyOption(alterScript2, expectedValue: false);
            Assert.That(() => { index2.Alter(); }, Throws.Nothing, "Alter index2 should succeed.");

            // index3

            Assert.That(index3.IsOptimizedForSequentialKey, Is.False, "Unexpected IsOptimizedForSequentialKey value on index3");

            index3.IsOptimizedForSequentialKey = true; // the property of interest

            string alterScript3 = ScriptSmoObject(index3, scriptOptionsForAlter);
            AssertIsOptimizedForSequentialKeyOption(alterScript3, expectedValue: true);
            Assert.That(() => { index3.Alter(); }, Throws.Nothing, "Alter index3 should succeed.");
        }

        /// <summary>
        /// Test property IsOptimizedForSequentialKey in CREATE TABLE WITH CONSTRAINT scenarios.
        /// </summary>
        private void TestIsOptimizedForSequentialKey_CreateTableAddConstraint(_SMO.Database database, _SMO.Table t1, _SMO.Table t2)
        {
            var scriptOptionsForCreate = new _SMO.ScriptingOptions()
            {
                DriPrimaryKey = true,
                DriUniqueKeys = true
            };

            // Create a table and the key constraint/index with property IsOptimizedForSequentialKey
            // and check if the generated scripts contain the T-SQL index option.

            {
                // Primary key constraint index option


                t1.Columns.Add(new _SMO.Column(t1, "c1", _SMO.DataType.Int) { Nullable = false });
                t1.Columns.Add(new _SMO.Column(t1, "c2", _SMO.DataType.Int) { Nullable = false });

                _SMO.Index index1 = new _SMO.Index(t1, "keyidx1");
                index1.IndexedColumns.Add(new _SMO.IndexedColumn(index1, t1.Columns[0].Name));
                index1.IsClustered = true;
                index1.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;
                index1.IsOptimizedForSequentialKey = true; // the property of interest

                t1.Indexes.Add(index1);

                string createScript = ScriptSmoObject(t1, scriptOptionsForCreate);
                AssertIsOptimizedForSequentialKeyOption(createScript, expectedValue: true);
                Assert.That(() => { t1.Create(); }, Throws.Nothing, "table creation should succeed.");
            }

            {
                // Unique key constraint index option

                t2.Columns.Add(new _SMO.Column(t2, "c1", _SMO.DataType.Int) { Nullable = false });
                t2.Columns.Add(new _SMO.Column(t2, "c2", _SMO.DataType.Int) { Nullable = false });

                _SMO.Index index2 = new _SMO.Index(t2, "keyidx2");
                index2.IndexedColumns.Add(new _SMO.IndexedColumn(index2, t2.Columns[0].Name));
                index2.IndexKeyType = _SMO.IndexKeyType.DriUniqueKey;
                index2.IsOptimizedForSequentialKey = true; // the property of interest

                t2.Indexes.Add(index2);

                string createScript = ScriptSmoObject(t2, scriptOptionsForCreate);
                AssertIsOptimizedForSequentialKeyOption(createScript, expectedValue: true);
                Assert.That(() => { t2.Create(); }, Throws.Nothing, "table creation should succeed.");
            }

            {
                // Unique key constraint index option

                var t3 = new _SMO.Table(database, GenerateSmoObjectName("cnst_t3"));

                t3.Columns.Add(new _SMO.Column(t3, "c1", _SMO.DataType.Int) { Nullable = false });
                t3.Columns.Add(new _SMO.Column(t3, "c2", _SMO.DataType.Int) { Nullable = false });

                _SMO.Index index3 = new _SMO.Index(t3, "keyidx3");
                index3.IndexedColumns.Add(new _SMO.IndexedColumn(index3, t3.Columns[0].Name));
                index3.IndexKeyType = _SMO.IndexKeyType.DriUniqueKey;
                index3.IsOptimizedForSequentialKey = false; // the property of interest

                t3.Indexes.Add(index3);

                string createScript = ScriptSmoObject(t3, scriptOptionsForCreate);
                AssertIsOptimizedForSequentialKeyOption(createScript, expectedValue: false);
                Assert.That(() => { t3.Create(); }, Throws.Nothing, "table creation should succeed.");
            }
        }

        /// <summary>
        /// Test property IsOptimizedForSequentialKey in ALTER TABLE ADD CONSTRAINT scenarios.
        /// </summary>
        private void TestIsOptimizedForSequentialKey_AlterTableAddConstraint(_SMO.Database database, _SMO.Table t1, _SMO.Table t2)
        {
            var scriptOptionsForAlter = new _SMO.ScriptingOptions() { ScriptForAlter = true };

            // Alter the table to add the key constraint/index with property IsOptimizedForSequentialKey
            // and check if the generated scripts contain the T-SQL index option.

            {
                // Add a new unique key constraint

                _SMO.Index index4 = new _SMO.Index(t1, "keyidx4");
                index4.IndexedColumns.Add(new _SMO.IndexedColumn(index4, t1.Columns[1].Name));
                index4.IndexKeyType = _SMO.IndexKeyType.DriUniqueKey;
                index4.IsOptimizedForSequentialKey = true; // the property of interest

                t1.Indexes.Add(index4);

                string alterScript = ScriptSmoObject(t1, scriptOptionsForAlter);
                AssertIsOptimizedForSequentialKeyOption(alterScript, expectedValue: true);
                Assert.That(() => { t1.Alter(); }, Throws.Nothing, "Alter table should succeed.");
            }

            {
                // Add a new primary key constraint

                _SMO.Index index5 = new _SMO.Index(t2, "keyidx5");
                index5.IndexedColumns.Add(new _SMO.IndexedColumn(index5, t2.Columns[1].Name));
                index5.IndexKeyType = _SMO.IndexKeyType.DriUniqueKey;
                index5.IsOptimizedForSequentialKey = true; // the property of interest

                t2.Indexes.Add(index5);

                string alterScript = ScriptSmoObject(t2, scriptOptionsForAlter);
                AssertIsOptimizedForSequentialKeyOption(alterScript, expectedValue: true);
                Assert.That(() => { t2.Alter(); }, Throws.Nothing, "Alter table should succeed.");
            }
        }

        /// <summary>
        /// Test property IsOptimizedForSequentialKey for ColumnStore indexes.
        /// This is a negative test validating that the option cannot be on for the index type.
        /// </summary>
        private void TestIsOptimizedForSequentialKey_ColumnstoreIndexNegative(_SMO.Database database)
        {
            // Create ColumnStore indexes with IsOptimizedForSequentialKey = true
            // which should fail.

            {
                // Create a clustered column-store index

                var t1 = new _SMO.Table(database, "ccitest1");

                t1.Columns.Add(new _SMO.Column(t1, "c1", _SMO.DataType.Int) { Nullable = false });
                t1.Columns.Add(new _SMO.Column(t1, "c2", _SMO.DataType.Int) { Nullable = false });

                t1.Create();

                var index1 = new _SMO.Index(t1, "idx1");
                index1.IndexType = _SMO.IndexType.ClusteredColumnStoreIndex;
                index1.IsClustered = true;
                index1.IsOptimizedForSequentialKey = true; // invalid

                Assert.That(() => { index1.Create(); }, Throws.InstanceOf<_SMO.SmoException>(),
                    "Columnstore index does not support the index option. This should have failed.");

                // Try again with IsOptimizedForSequentialKey = false

                index1.IsOptimizedForSequentialKey = false;

                Assert.That(() => { index1.Create(); }, Throws.Nothing,
                    "Create the columnstore index should succeed.");
            }

            {
                // Create a non-clustered column-store index

                var t2 = new _SMO.Table(database, "ccitest2");

                t2.Columns.Add(new _SMO.Column(t2, "c1", _SMO.DataType.Int) { Nullable = false });
                t2.Columns.Add(new _SMO.Column(t2, "c2", _SMO.DataType.Int) { Nullable = false });

                t2.Create();

                var index2 = new _SMO.Index(t2, "idx2");
                index2.IndexType = _SMO.IndexType.NonClusteredColumnStoreIndex;
                index2.IndexedColumns.Add(new _SMO.IndexedColumn(index2, t2.Columns[0].Name));
                index2.IsOptimizedForSequentialKey = true; // invalid

                Assert.That(() => { index2.Create(); }, Throws.InstanceOf<_SMO.SmoException>(),
                    "Columnstore index does not support the index option. This should have failed.");

                // Try again with IsOptimizedForSequentialKey = false

                index2.IsOptimizedForSequentialKey = false;

                Assert.That(() => { index2.Create(); }, Throws.Nothing,
                    "Create the columnstore index should succeed.");
            }
        }

        /// <summary>
        /// Test property IsOptimizedForSequentialKey for XML indexes.
        /// This is a negative test validating that the option cannot be on for the index type.
        /// </summary>
        private void TestIsOptimizedForSequentialKey_XmlIndexNegative(_SMO.Database database)
        {
            // Create XML indexes with IsOptimizedForSequentialKey = true
            // which should fail.

            const string xmlSchemaObjName = "XmlSchema1";

            var xmlSchema1 = new _SMO.XmlSchemaCollection(database, xmlSchemaObjName, "dbo");
            xmlSchema1.Text = "<xsd:schema xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"NS2\"> <xsd:element name=\"elem1\" type=\"xsd:integer\"/></xsd:schema>";
            xmlSchema1.Create();

            var t1 = new _SMO.Table(database, "xmltest1");

            t1.Columns.Add(new _SMO.Column(t1, "c1", _SMO.DataType.Int) { Nullable = false });
            t1.Columns.Add(new _SMO.Column(t1, "c2", _SMO.DataType.Xml(xmlSchemaObjName)) { Nullable = false });

            t1.Create();

            // Create a primary key first, which is required for an XML index.

            var index1 = new _SMO.Index(t1, "xmlidx1");
            index1.IsClustered = true;
            index1.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;
            index1.IndexedColumns.Add(new _SMO.IndexedColumn(index1, t1.Columns[0].Name));
            index1.Create();

            // Create a primary XML index.

            var index2 = new _SMO.Index(t1, "xmlidx2");
            index2.IndexedColumns.Add(new _SMO.IndexedColumn(index2, t1.Columns[1].Name));
            index2.DisallowPageLocks = true;
            index2.IsOptimizedForSequentialKey = true; // invalid

            Assert.That(() => { index2.Create(); }, Throws.InstanceOf<_SMO.SmoException>(),
                "XML index does not support the index option. This should have failed.");

            // Try again with IsOptimizedForSequentialKey = false

            index2.IsOptimizedForSequentialKey = false;

            Assert.That(() => { index2.Create(); }, Throws.Nothing,
                 "Create XML index should succeed.");

            // Create a secondary XML index.

            var index3 = new _SMO.Index(t1, "xmlidx3");
            index3.IndexedColumns.Add(new _SMO.IndexedColumn(index3, t1.Columns[1].Name));
            index3.ParentXmlIndex = "xmlidx2";
            index3.SecondaryXmlIndexType = _SMO.SecondaryXmlIndexType.Value;
            index3.DisallowPageLocks = true;
            index3.IsOptimizedForSequentialKey = true; // invalid

            Assert.That(() => { index3.Create(); }, Throws.InstanceOf<_SMO.SmoException>(),
                "XML index does not support the index option. This should have failed.");

            // Try again with IsOptimizedForSequentialKey = false

            index3.IsOptimizedForSequentialKey = false;

            Assert.That(() => { index3.Create(); }, Throws.Nothing,
                 "Create XML index should succeed.");
        }

        /// <summary>
        /// Test property IsOptimizedForSequentialKey for Spatial indexes.
        /// This is a negative test validating that the option cannot be on for the index type.
        /// </summary>
        private void TestIsOptimizedForSequentialKey_SpatialIndexNegative(_SMO.Database database)
        {
            // Create Spatial indexes with IsOptimizedForSequentialKey = true
            // which should fail.

            // Create a table with a primary key

            var t1 = new _SMO.Table(database, "spatialtest1");

            t1.Columns.Add(new _SMO.Column(t1, "c1", _SMO.DataType.Int) { Nullable = false });
            t1.Columns.Add(new _SMO.Column(t1, "c2", _SMO.DataType.Geometry) { Nullable = false });

            var index1 = new _SMO.Index(t1, "pkidx");
            index1.IndexedColumns.Add(new _SMO.IndexedColumn(index1, t1.Columns[0].Name));
            index1.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;
            index1.IndexType = _SMO.IndexType.ClusteredIndex;

            t1.Indexes.Add(index1);
            t1.Create();

            // Create a spatial index (geometry)

            var index2 = new _SMO.Index(t1, "geoidx1");
            index2.IndexedColumns.Add(new _SMO.IndexedColumn(index2, t1.Columns[1].Name));
            index2.IndexType = _SMO.IndexType.SpatialIndex;
            index2.SpatialIndexType = _SMO.SpatialIndexType.GeometryGrid;
            index2.BoundingBoxXMin = 0;
            index2.BoundingBoxXMax = 500;
            index2.BoundingBoxYMin = 0;
            index2.BoundingBoxYMax = 300;

            index2.DisallowPageLocks = true;
            index2.IsOptimizedForSequentialKey = true; // invalid

            Assert.That(() => { index2.Create(); }, Throws.InstanceOf<_SMO.SmoException>(),
                "Spatial index does not support the index option. This should have failed.");

            // Try again with IsOptimizedForSequentialKey = false

            index2.IsOptimizedForSequentialKey = false;

            Assert.That(() => { index2.Create(); }, Throws.Nothing,
                 "Create spatial index should succeed.");
        }

        /// <summary>
        /// Assert that the script contain or does not contain the IsOptimizedForSequentialKey T-SQL option.
        /// </summary>
        /// <param name="script">The script to be verified. </param>
        /// <param name="expectedValue">
        /// For true/false, checks if the script contains the option value with ON/OFF.
        /// For null, checks if the script does not contain the option value.
        /// </param>
        private void AssertIsOptimizedForSequentialKeyOption(string script, bool? expectedValue)
        {
            const string optionName = "OPTIMIZE_FOR_SEQUENTIAL_KEY";

            if (expectedValue.HasValue)
            {
                string optionValue = expectedValue.Value ? "ON" : "OFF";
                string expectedFragment = string.Format("{0} = {1}", optionName, optionValue);
                string errorMessage = string.Format("The generated script doesn't have the expected fragment {0}", expectedFragment);

                Assert.That(script, Does.Contain(expectedFragment), errorMessage);
            }
            else
            {
                // Check if the script does not contain the option at all.

                string unexpectedFragment = string.Format("{0} = ", optionName);
                string errorMessage = string.Format("The generated script should not have the index option {0}", optionName);

                Assert.That(script, Does.Not.Contain(unexpectedFragment), errorMessage);
            }
        }

        #endregion
    }
}
