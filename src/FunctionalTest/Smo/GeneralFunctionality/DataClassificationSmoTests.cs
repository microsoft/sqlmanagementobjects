// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.SqlServer.Test.SMO.ScriptingTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
    public class DataClassificationSmoTests : SmoObjectTestBase
    {
        /// <summary>
        /// Verifies that table with classified columns is properly created
        /// </summary>
        [TestMethod]
        public void DataClassification_TableCreate()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName ("ClassifiedTable"));
                var column1 = new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityLabelName = "'LabelName_A'", SensitivityLabelId = "'LabelId_A'", SensitivityInformationTypeName = "'InfoTypeName_A'", SensitivityInformationTypeId = "'InfoTypeId_A'" };
                var column2 = new _SMO.Column(table, "b", _SMO.DataType.Int);
                var column3 = new _SMO.Column(table, "c", _SMO.DataType.Int) { SensitivityLabelName = "LabelName_C", SensitivityInformationTypeName = "InfoTypeName_C" };
                var column4 = new _SMO.Column(table, "d", _SMO.DataType.Int);

                var rank1 = SetSensitivityRank(column1, SensitivityRank.Low);
                var rank3 = SetSensitivityRank(column3, SensitivityRank.High);

                table.Columns.Add(column1);
                table.Columns.Add(column2);

                table.Create();

                column3.Create();
                column4.Create();

                Assert.That(table.HasClassifiedColumn, Is.True, "'table' HasClassifiedColumn");
                Assert.That(column1.IsClassified, Is.True, "'a' IsClassified");
                Assert.That(column1.SensitivityLabelName, Is.EqualTo("'LabelName_A'"), "'a' SensitivityLabelName");
                Assert.That(column1.SensitivityLabelId, Is.EqualTo("'LabelId_A'"), "'a' SensitivityLabelId");
                Assert.That(column1.SensitivityInformationTypeName, Is.EqualTo("'InfoTypeName_A'"), "'a' SensitivityInformationTypeName");
                Assert.That(column1.SensitivityInformationTypeId, Is.EqualTo("'InfoTypeId_A'"), "'a' SensitivityInformationTypeId");
                Assert.That(GetSensitivityRank(column1), Is.EqualTo(rank1), "'a' SensitivityRank");
                Assert.That(column2.IsClassified, Is.False, "'b' IsClassified");
                Assert.That(GetSensitivityRank(column2), Is.EqualTo(SensitivityRank.Undefined), "'b' SensitivityRank");
                Assert.That(column3.IsClassified, Is.True, "'c' IsClassified");
                Assert.That(column4.IsClassified, Is.False, "'d' IsClassified");
                Assert.That(column2.SensitivityLabelId, Is.Empty, "'b' SensitivityLabelId");
                Assert.That(column3.SensitivityLabelId, Is.Empty, "'c' SensitivityLabelId");
                Assert.That(GetSensitivityRank(column3), Is.EqualTo(rank3), "'c' SensitivityRank");
                Assert.That(column4.SensitivityInformationTypeId, Is.Empty, "'d' SensitivityInformationTypeId");
                Assert.That(GetSensitivityRank(column4), Is.EqualTo(SensitivityRank.Undefined), "'d' SensitivityRank");
            });
        }

        /// <summary>
        /// Verifies that table with classified columns is properly altered
        /// </summary>
        [TestMethod]
        public void DataClassification_TableAlter()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));

                table.Columns.Add(new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityLabelName = "LabelName_A", SensitivityLabelId = "LabelId_A", SensitivityInformationTypeName = "InfoTypeName_A", SensitivityInformationTypeId = "InfoTypeId_A" });
                table.Columns.Add(new _SMO.Column(table, "b", _SMO.DataType.Int));

                SetSensitivityRank(table.Columns[0], SensitivityRank.Low);

                table.Create();

                var column1 = table.Columns["a"];
                var column2 = table.Columns["b"];

                column1.SensitivityLabelName = "'LabelName_A_Changed'";
                column1.SensitivityLabelId = string.Empty;
                column1.SensitivityInformationTypeName = "InfoTypeName_A_Changed";
                column1.SensitivityInformationTypeId = string.Empty;
                var rank1 = SetSensitivityRank(column1, SensitivityRank.Medium);

                column1.Alter();

                column2.SensitivityInformationTypeName = "InfoTypeName_B_Changed";

                table.Alter();
                column2.Refresh();

                Assert.That(table.HasClassifiedColumn, Is.True, "'table' HasClassifiedColumn");
                Assert.That(column1.IsClassified, Is.True, "'a' IsClassified");
                Assert.That(column2.IsClassified, Is.True, "'b' IsClassified");
                Assert.That(column1.SensitivityLabelName, Is.EqualTo("'LabelName_A_Changed'"), "'a' SensitivityLabelName");
                Assert.That(column1.SensitivityLabelId, Is.Empty, "'a' SensitivityLabelId");
                Assert.That(column1.SensitivityInformationTypeName, Is.EqualTo("InfoTypeName_A_Changed"), "'a' SensitivityInformationTypeName");
                Assert.That(column1.SensitivityInformationTypeId, Is.Empty, "'a' SensitivityInformationTypeId");
                Assert.That(GetSensitivityRank(column1), Is.EqualTo(rank1), "'a' SensitivityRank");
                Assert.That(column2.SensitivityInformationTypeName, Is.EqualTo("InfoTypeName_B_Changed"), "'b' SensitivityInformationTypeName");
            });
        }

        /// <summary>
        /// Verifies data classification is properly dropped
        /// </summary>
        [TestMethod]
        public void DataClassification_Drop()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));

                table.Columns.Add(new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityLabelName = "LabelName_A", SensitivityLabelId = "LabelId_A", SensitivityInformationTypeName = "InfoTypeName_A", SensitivityInformationTypeId = "InfoTypeId_A" });
                table.Columns.Add(new _SMO.Column(table, "b", _SMO.DataType.Int));

                SetSensitivityRank(table.Columns[0], SensitivityRank.High);

                table.Create();

                var column1 = table.Columns["a"];
                var column2 = table.Columns["b"];

                column1.RemoveClassification();
                column1.Alter();
                column1.Refresh();

                // Verify bug when setting sensitivity option which was empty before to empty string.
                // The drop was failed to server versions [10, 14]. TFS bug: 586347
                column2.SensitivityLabelName = string.Empty;
                column2.Alter();
                column2.Refresh();

                Assert.That(column1.IsClassified, Is.False, "'a' IsClassified");
                Assert.That(column1.SensitivityLabelName, Is.Empty, "'a' SensitivityLabelName");
                Assert.That(column1.SensitivityLabelId, Is.Empty, "'a' SensitivityLabelId");
                Assert.That(column1.SensitivityInformationTypeName, Is.Empty, "'a' SensitivityInformationTypeName");
                Assert.That(column1.SensitivityInformationTypeId, Is.Empty, "'a' SensitivityInformationTypeId");
                Assert.That(GetSensitivityRank(column1), Is.EqualTo(SensitivityRank.Undefined), "'a' SensitivityRank");
            });
        }

        /// <summary>
        /// Verifies that column can be created with classified label id,
        /// when classified label name is empty
        /// Note: TASK 429910 Uncomment Azure and remove platform limitation once T45 will be deployed on Azure and Linux
        /// </summary>
        [TestMethod]
        public void DataClassification_CreateColumnWithEmptyLabelName()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));
                var column = new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityLabelId = "LabelId_A" };

                table.Columns.Add(column);

                table.Create();
                table.Refresh();

                Assert.That(column.IsClassified, Is.True, "'a' IsClassified");
                Assert.That(column.SensitivityLabelId, Is.EqualTo("LabelId_A"), "'a' SensitivityLabelId");
            });
        }

        /// <summary>
        /// Verifies that column can be created with classified information type id,
        /// when classified information type name is empty
        /// Note: TASK 429910 Uncomment Azure and remove platform limitation once T45 will be deployed on Azure and Linux
        /// </summary>
        [TestMethod]
        public void DataClassification_CreateColumnWithEmptyInformationTypeName()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));

                table.Columns.Add(new _SMO.Column(table, "a", _SMO.DataType.Int));
                table.Create();

                var column = new _SMO.Column(table, "b", _SMO.DataType.Int) { SensitivityInformationTypeId = "InfoTypeId_B" };

                column.Create();
                column.Refresh();

                Assert.That(column.IsClassified, Is.True, "'b' IsClassified");
                Assert.That(column.SensitivityInformationTypeId, Is.EqualTo("InfoTypeId_B"), "'b' SensitivityInformationTypeId");
            });
        }

        /// <summary>
        /// Verifies alter succeeds when setting classified label name to empty value, 
        /// but classified label id is not empty
        /// Note: TASK 429910 Uncomment Azure and remove platform limitation once T45 will be deployed on Azure and Linux
        /// </summary>
        [TestMethod]
        public void DataClassification_AlterColumnWithEmptyLabelName()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));

                table.Columns.Add(new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityLabelName = "LabelName_A", SensitivityLabelId = "LabelId_A" });
                table.Create();

                var column = table.Columns["a"];

                column.SensitivityLabelName = string.Empty;

                table.Alter();
                table.Refresh();

                Assert.That(column.IsClassified, Is.True, "'a' IsClassified");
                Assert.That(column.SensitivityLabelId, Is.EqualTo("LabelId_A"), "'a' SensitivityLabelId");
            });
        }

        /// <summary>
        /// Verifies alter succeeds when setting classified information type name to empty value, 
        /// but classified information type id is not empty
        /// Note: TASK 429910 Uncomment Azure and remove platform limitation once T45 will be deployed on Azure and Linux
        /// </summary>
        [TestMethod]
        public void DataClassification_AlterColumnWithEmptyInformationTypeName()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));
                var column = new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityInformationTypeName = "InfoTypeName_A", SensitivityInformationTypeId = "InfoTypeId_A" };

                table.Columns.Add(column);
                table.Create();

                column.SensitivityInformationTypeName = string.Empty;

                column.Alter();
                column.Refresh();

                Assert.That(column.IsClassified, Is.True, "'a' IsClassified");
                Assert.That(column.SensitivityInformationTypeId, Is.EqualTo("InfoTypeId_A"), "'a' SensitivityInformationTypeId");
            });
        }

        /// <summary>
        /// Verifies that data classification is not supported for created computed column
        /// </summary>
        [TestMethod]
        public void DataClassification_ComputedColumnCreate()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));

                table.Columns.Add(new _SMO.Column(table, "a", _SMO.DataType.Int));
                table.Columns.Add(new _SMO.Column(table, "b", _SMO.DataType.Int));
                table.Columns.Add(new _SMO.Column(table, "c", _SMO.DataType.Int) { Computed = true, ComputedText = "a * b", SensitivityLabelName = "LabelName_C", SensitivityLabelId = "LabelId_C", SensitivityInformationTypeName = "InfoTypeName_C", SensitivityInformationTypeId = "InfoTypeId_C" });

                SmoTestsUtility.AssertInnerException<WrongPropertyValueException>(() => table.Create(), ExceptionTemplates.NoDataClassificationOnComputedColumns);
            });
        }

        /// <summary>
        /// Verifies that data classification is not supported for altered computed column
        /// </summary>
        [TestMethod]
        public void DataClassification_ComputedColumnAlter()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));

                table.Columns.Add(new _SMO.Column(table, "a", _SMO.DataType.Int));
                table.Columns.Add(new _SMO.Column(table, "b", _SMO.DataType.Int));
                table.Columns.Add(new _SMO.Column(table, "c", _SMO.DataType.Int) { Computed = true, ComputedText = "a * b" });

                table.Create();

                var column = table.Columns["c"];

                column.SensitivityLabelName = "LabelName_C";
                column.SensitivityLabelId = "LabelId_C";
                column.SensitivityInformationTypeName = "InfoTypeName_C";
                column.SensitivityInformationTypeId = "InfoTypeId_C";

                SmoTestsUtility.AssertInnerException<WrongPropertyValueException>(() => column.Alter(), ExceptionTemplates.NoDataClassificationOnComputedColumns);
            });
        }

        /// <summary>
        /// Verifies that column becames classified by setting its sensitivity rank only
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void DataClassification_SetSensitivityRank()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));
                var column1 = new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityRank = SensitivityRank.High };

                table.Columns.Add(column1);
                table.Create();

                Assert.That(column1.IsClassified, Is.True, "'a' IsClassified");
                Assert.That(column1.SensitivityRank, Is.EqualTo(SensitivityRank.High), "'a' SensitivityRank");
            });
        }

        /// <summary>
        /// Verifies that column becames not classified by clearing its single sensitivity attribute - sensitivity rank
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void DataClassification_ClearSensitivityRank()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));
                var column1 = new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityRank = SensitivityRank.High };

                table.Columns.Add(column1);
                table.Create();

                column1.SensitivityRank = SensitivityRank.Undefined;

                column1.Alter();
                column1.Refresh();

                Assert.That(column1.IsClassified, Is.False, "'a' IsClassified");
                Assert.That(column1.SensitivityRank, Is.EqualTo(SensitivityRank.Undefined), "'a' SensitivityRank");
            });
        }

        /// <summary>
        /// Verifies GetClassifiedColumns returns empty list when database has no classified columns
        /// upon table and column create
        /// </summary>
        [TestMethod]
        public void DataClassification_GetClassifiedColumnsEmptyOnCreate()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));
                var column1 = new _SMO.Column(table, "a", _SMO.DataType.Int);
                var column2 = new _SMO.Column(table, "b", _SMO.DataType.Int);
                var column3 = new _SMO.Column(table, "c", _SMO.DataType.Int);
                var column4 = new _SMO.Column(table, "d", _SMO.DataType.Int);

                table.Columns.Add(column1);
                table.Columns.Add(column2);

                table.Create();

                column3.Create();
                column4.Create();

                db.InitializeClassifiedColumns();

                Assert.That(db.Tables, Is.Empty, "db.Tables must be empty");
            });
        }

        /// <summary>
        /// Verifies GetClassifiedColumns returns empty list when databasa has no classified columns
        /// upon table and column alter
        /// </summary>
        [TestMethod]
        public void DataClassification_GetClassifiedColumnsEmptyOnAlter()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));
                var column1 = new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityLabelName = "'LabelName_A'", SensitivityLabelId = "'LabelId_A'", SensitivityInformationTypeName = "'InfoTypeName_A'", SensitivityInformationTypeId = "'InfoTypeId_A'" };
                var column2 = new _SMO.Column(table, "b", _SMO.DataType.Int);
                var column3 = new _SMO.Column(table, "c", _SMO.DataType.Int) { SensitivityLabelName = "LabelName_C", SensitivityInformationTypeName = "InfoTypeName_C" };
                var column4 = new _SMO.Column(table, "d", _SMO.DataType.Int);

                table.Columns.Add(column1);
                table.Columns.Add(column2);

                table.Create();

                column3.Create();
                column4.Create();

                column1.RemoveClassification();
                column1.Alter();

                column3.RemoveClassification();
                column3.Alter();

                db.InitializeClassifiedColumns();

                Assert.That(db.Tables, Is.Empty, "db.Tables must be empty");
            });
        }

        /// <summary>
        /// Verifies GetClassifiedColumns returns non empty classified columns list when databasa has classified columns
        /// upon table and column create
        /// </summary>
        [TestMethod]
        public void DataClassification_GetClassifiedColumnsNonEmptyOnCreate()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));
                var column1 = new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityLabelName = "'LabelName_A'", SensitivityLabelId = "'LabelId_A'", SensitivityInformationTypeName = "'InfoTypeName_A'", SensitivityInformationTypeId = "'InfoTypeId_A'" };
                var column2 = new _SMO.Column(table, "b", _SMO.DataType.Int);
                var column3 = new _SMO.Column(table, "c", _SMO.DataType.Int) { SensitivityLabelName = "LabelName_C", SensitivityInformationTypeName = "InfoTypeName_C" };
                var column4 = new _SMO.Column(table, "d", _SMO.DataType.Int);

                var rank1 = SetSensitivityRank(column1, SensitivityRank.Low);
                var rank3 = SetSensitivityRank(column3, SensitivityRank.High);

                table.Columns.Add(column1);
                table.Columns.Add(column2);

                table.Create();

                column3.Create();
                column4.Create();

                db.InitializeClassifiedColumns();

                List<Column> classifiedColumns = new List<Column>();

                foreach (Table t in db.Tables)
                {
                    foreach (Column c in t.Columns)
                    {
                        classifiedColumns.Add(c);
                    }
                }

                Assert.That(classifiedColumns.Count, Is.EqualTo(2), "classifiedColumns");
                Assert.That(classifiedColumns[0].SensitivityLabelName, Is.EqualTo("'LabelName_A'"), "'a' SensitivityLabelName");
                Assert.That(classifiedColumns[0].SensitivityLabelId, Is.EqualTo("'LabelId_A'"), "'a' SensitivityLabelId");
                Assert.That(classifiedColumns[0].SensitivityInformationTypeName, Is.EqualTo("'InfoTypeName_A'"), "'a' SensitivityInformationTypeName");
                Assert.That(classifiedColumns[0].SensitivityInformationTypeId, Is.EqualTo("'InfoTypeId_A'"), "'a' SensitivityInformationTypeId");
                Assert.That(GetSensitivityRank(classifiedColumns[0]), Is.EqualTo(rank1), "'a' SensitivityRank");
                Assert.That(classifiedColumns[1].SensitivityLabelName, Is.EqualTo("LabelName_C"), "'c' SensitivityLabelName");
                Assert.That(classifiedColumns[1].SensitivityInformationTypeName, Is.EqualTo("InfoTypeName_C"), "'c' SensitivityInformationTypeName");
                Assert.That(GetSensitivityRank(classifiedColumns[1]), Is.EqualTo(rank3), "'c' SensitivityRank");
            });
        }

        /// <summary>
        /// Verifies GetClassifiedColumns returns non empty classified columns list when databasa has classified columns
        /// upon table and column alter
        /// </summary>
        [TestMethod]
        public void DataClassification_GetClassifiedColumnsNonEmptyOnAlter()
        {
            ExecuteWithClean(db =>
            {
                var table = new _SMO.Table(db, GenerateUniqueSmoObjectName("ClassifiedTable"));

                table.Columns.Add(new _SMO.Column(table, "a", _SMO.DataType.Int) { SensitivityLabelName = "LabelName_A", SensitivityLabelId = "LabelId_A", SensitivityInformationTypeName = "InfoTypeName_A", SensitivityInformationTypeId = "InfoTypeId_A" });
                table.Columns.Add(new _SMO.Column(table, "b", _SMO.DataType.Int));

                SetSensitivityRank(table.Columns[0], SensitivityRank.Low);

                table.Create();

                var column1 = table.Columns["a"];
                var column2 = table.Columns["b"];

                column1.SensitivityLabelName = "'LabelName_A_Changed'";
                column1.SensitivityLabelId = string.Empty;
                column1.SensitivityInformationTypeName = "InfoTypeName_A_Changed";
                column1.SensitivityInformationTypeId = string.Empty;
                var rank1 = SetSensitivityRank(column1, SensitivityRank.Medium);

                column1.Alter();

                column2.SensitivityInformationTypeName = "InfoTypeName_B_Changed";

                table.Alter();

                List<Column> classifiedColumns = new List<Column>();

                foreach (Table t in db.Tables)
                {
                    foreach (Column c in t.Columns)
                    {
                        classifiedColumns.Add(c);
                    }
                }

                Assert.That(classifiedColumns.Count, Is.EqualTo(2), "classifiedColumns");
                Assert.That(classifiedColumns[0].SensitivityLabelName, Is.EqualTo("'LabelName_A_Changed'"), "'a' SensitivityLabelName");
                Assert.That(classifiedColumns[0].SensitivityLabelId, Is.Empty, "'a' SensitivityLabelId");
                Assert.That(classifiedColumns[0].SensitivityInformationTypeName, Is.EqualTo("InfoTypeName_A_Changed"), "'a' SensitivityInformationTypeName");
                Assert.That(classifiedColumns[0].SensitivityInformationTypeId, Is.Empty, "'a' SensitivityInformationTypeId");
                Assert.That(GetSensitivityRank(classifiedColumns[0]), Is.EqualTo(rank1), "'a' SensitivityRank");
                Assert.That(classifiedColumns[1].SensitivityInformationTypeName, Is.EqualTo("InfoTypeName_B_Changed"), "'b' SensitivityInformationTypeName");
            });
        }

        private static SensitivityRank SetSensitivityRank(Column column, SensitivityRank rank)
        {
            if (column.IsSupportedProperty("SensitivityRank"))
            {
                column.SensitivityRank = rank;

                return column.SensitivityRank;
            }

            return SensitivityRank.Undefined;
        }

        private static SensitivityRank GetSensitivityRank(Column column)
        {
            return column.IsSupportedProperty("SensitivityRank") ? column.SensitivityRank : SensitivityRank.Undefined;
        }

        protected override void VerifyIsSmoObjectDropped(SqlSmoObject obj, SqlSmoObject objVerify)
        {
            
        }

        private void ExecuteWithClean(Action<Database> action)
        {
            ExecuteFromDbPool(db =>
            {
                List<Table> tables = new List<Table>();

                // avoid "Collection was modified; enumeration operation may not execute"
                foreach (Table t in db.Tables)
                {
                    tables.Add(t);
                }

                foreach (Table t in tables)
                {
                    t.Drop();
                }

                db.Refresh();

                action.Invoke(db);
            });
        }
    }
}