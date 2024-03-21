// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Microsoft.SqlServer.Test.Manageability.Utils;
using System.Threading;
using Microsoft.SqlServer.Management.Smo;
using System.IO;
using System.Collections.Specialized;
using static Microsoft.SqlServer.Management.SqlParser.MetadataProvider.MetadataProviderUtils.Names;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// SMO Tests specific to DW instances
    /// </summary>
    [TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
    public class DwSmoTests : SqlTestBase
    {
        /// <summary>
        /// Regression test for 11125431. Requires a paused DW instance on the server to be complete
        /// </summary>
        [TestMethod]
        public void Server_Databases_collection_enumerates_without_exception()
        {
            ExecuteTest(() =>
            {
                var databases = new List<string>();
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);
                Assert.DoesNotThrow(() =>
                {
                    try
                    {
                        foreach (var db in server.Databases.OfType<_SMO.Database>())
                        {
                            try
                            {
                                databases.Add(string.Format("DB Name: {0}\tEdition:{1}", db.Name, db.DatabaseEngineEdition));
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Got exception accessing db properties:{0}", ex.GetType());
                                throw;
                            }

                        }
                    }
                    catch (ConnectionFailureException e)
                    {
                        var sqlException = e.InnerException as SqlException;
                        if (sqlException != null)
                        {
                            // We want the real message in the log like "Cannot connect to database when it is paused"
                            throw sqlException;
                        }
                        throw;
                    }

                });
            });
        }


        /// <summary>
        /// Tests that dw table with default ICC is scripted correctly.
        /// Also, we validate the ColumnStoreOrderOrdinal from prefetch index.
        ///
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        public void DWTable_ScriptData_succeed()
        {
            this.ExecuteWithDbDrop(db => { 
                var tableName = $"table{Guid.NewGuid()}";
                var table = new _SMO.Table(db, tableName);
                _SMO.Column c1 = new _SMO.Column(table, "col1", new _SMO.DataType(_SMO.SqlDataType.Int));
                _SMO.Column c2 = new _SMO.Column(table, "col2", new _SMO.DataType(_SMO.SqlDataType.Int));
                _SMO.Column c3 = new _SMO.Column(table, "col3", new _SMO.DataType(_SMO.SqlDataType.Int));
                table.Columns.Add(c1);
                table.Columns.Add(c2);
                table.Columns.Add(c3);
                table.Create();
                table = db.Tables[tableName];

                // Adding scrpiter. 
                var scripter = new _SMO.Scripter(db.Parent);
                scripter.Options.Indexes = false;
                scripter.Options.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlDataWarehouse;
                scripter.Options.TargetDatabaseEngineType = DatabaseEngineType.SqlAzureDatabase;

                // Validation of missing properties.
                var missingProperties = new MissingProperties();
                SqlSmoObject.PropertyMissing += missingProperties.OnPropertyMissing;
                StringCollection script = null;
                try
                {
                    script = scripter.Script(table);
                    Assert.That(missingProperties.Properties.Where(p => p.StartsWith(nameof(IndexedColumn))), Is.Empty, "Clustered indexed columns should have been fetched.");
                }
                finally
                {
                    SqlSmoObject.PropertyMissing -= missingProperties.OnPropertyMissing;
                    ServerContext.SetDefaultInitFields(allFields: false);
                }
               
                // Validation of Scripting result.
                var actualScript = script.ToSingleString().Trim().Replace("\r\n", " ").Replace("\n", " ");
                Assert.That(actualScript,
                        Does.EndWith(
                        String.Format(@"SET ANSI_NULLS ON SET QUOTED_IDENTIFIER ON CREATE TABLE [dbo].{0} ( 	[col1] [int] NULL, 	[col2] [int] NULL, 	[col3] [int] NULL ) WITH ( 	DISTRIBUTION = ROUND_ROBIN, 	CLUSTERED COLUMNSTORE INDEX )", 
                    SmoObjectHelpers.SqlBracketQuoteString(tableName))),"Wrong Creat script for DW table");
            }, AzureDatabaseEdition.DataWarehouse);
        }

        class MissingProperties
        {
            public readonly IList<string> Properties = new List<string>();
            private readonly int threadId = Thread.CurrentThread.ManagedThreadId;
            public void OnPropertyMissing(object sender, PropertyMissingEventArgs args)
            {
                if (Thread.CurrentThread.ManagedThreadId == threadId)
                {
                    Properties.Add($"{args.TypeName}.{args.PropertyName}");
                }
            }
        }
    }
}