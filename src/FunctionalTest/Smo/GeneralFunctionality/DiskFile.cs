// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using System;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// Tests for the DiskFile enumerator
    /// </summary>
    [TestClass]
    [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, HostPlatform = HostPlatformNames.Windows, MinMajor = 13)]
    public class DiskFileTests : SqlTestBase
    {
        // This test confirms that the workaround we put in place in DiskFile.xml to avoid issues
        // while trying to execute (on Windows) something like:
        //    select file_exists, file_is_a_directory, parent_directory_exists from sys.dm_os_file_exists('nul')
        [DataTestMethod]
        [DataRow("nul")]
        [DataRow("NUL")]
        [DataRow("nUl")]
        public void DiskFile_Can_Enumerate_Special_File_NUL(string nihil)
        {
            ExecuteFromDbPool("DiskFileTests", db =>
            {
                var en = new Enumerator();

                var req = new Request() { Urn = $"Server/File[@FullName='{nihil}']" };

                var ds = (DataSet)en.Process(db.Parent.ConnectionContext.SqlConnectionObject, req);

                Assert.Multiple(() =>
                {
                    // There should be no exception during the execution and exactly 1 row
                    Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(1), "One record should be returned!");

                    // The execution should report that NUL is indeed a file
                    Assert.That(Convert.ToBoolean(ds.Tables[0].Rows[0]["IsFile"]), Is.True, $"{nihil} should be reported as a file!");
                });
            });
        }
    }
}
