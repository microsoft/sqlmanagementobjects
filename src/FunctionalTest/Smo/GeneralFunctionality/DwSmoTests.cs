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
    }
}
