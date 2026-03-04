// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using TraceHelper = Microsoft.SqlServer.Test.Manageability.Utils.Helpers.TraceHelper;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// v14+ has new DMVs for enumerating files and folders and drives on the host.
    /// The FullName property is calculated differently
    ///  dependent on the version of sql. These tests help ensure that the file browser which 
    /// matches this version of SMO will function correctly.
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class File_SmoTestSuite : SqlTestBase
    {
        /// <summary>
        /// Verify presence of drive letter, \ separator, and IsFile
        /// </summary>
        [TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        public void When_host_is_Windows_FullFileName_is_Windows_path()
        {
            ExecuteWithDbDrop(db =>
            {
                var expectedFolderPath = Path.GetDirectoryName(db.PrimaryFilePath);
                var dataTable = db.Parent.EnumerateFilesAndFolders(expectedFolderPath);
                var rows = dataTable.Rows.OfType<DataRow>().ToArray();
                var fileNames = new List<string>();

                Assert.That(rows, Has.Length.AtLeast(1), "No rows returned from file enumeration");
                foreach (var row in rows)
                {
                    TraceHelper.TraceInformation("Enumerated file {0}, IsFile = {1}", row[ServerObjectHelpers.FullNameColumn] ?? "<None>", row[ServerObjectHelpers.IsFileColumn]);
                    var fullFileName = (string) row[ServerObjectHelpers.FullNameColumn];
                    var isFile = Convert.ToBoolean(row[ServerObjectHelpers.IsFileColumn], CultureInfo.InvariantCulture);
                    fileNames.Add(fullFileName);
                    Assert.That(fullFileName, Does.Contain(@"\"), "Path should have the Windows path separator");
                    var driveLetter = Path.GetPathRoot(fullFileName);
                    Assert.That(driveLetter, Does.Match(@"^[A-Za-z]{1}:\\$"), "Unexpected drive root");
                    var folderPath = Path.GetDirectoryName(fullFileName);
                    Assert.That(folderPath, Is.EqualTo(expectedFolderPath).IgnoreCase,
                        "Folder path of enumerated file doesn't match source directory");
                    var expectedIsFile = !string.IsNullOrEmpty(Path.GetExtension(fullFileName));

                    Assert.That(isFile, Is.EqualTo(expectedIsFile), "Unexpected value of IsFile for " + fullFileName);
                }
                Assert.That(fileNames, Has.Member(db.PrimaryFilePath), "Database folder not enumerated");
            });
        }

        /// <summary>
        /// the primaryfilepath in linux is still in windows form, so for now we'll hard code /var/opt/mssql
        /// Also, some folders in linux have a period in their name, so we can't easily validate IsFile except
        /// perhaps by trying to query children of non-files
        /// </summary>        
        [TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Windows)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        public void When_host_is_Linux_FullFileName_is_Linux_path()
        {
            const string expectedFolderPath = "/var/opt/mssql/log";
            ExecuteTest(() =>
            {
                var dataTable = this.ServerContext.EnumerateFilesAndFolders(expectedFolderPath);
                var rows = dataTable.Rows.OfType<DataRow>().ToArray();
                var fileNames = new List<string>();

                Assert.That(rows, Has.Length.AtLeast(1), "No rows returned from file enumeration");
                foreach (var row in rows)
                {
                    TraceHelper.TraceInformation("Enumerated file {0}, IsFile = {1}", row[ServerObjectHelpers.FullNameColumn] ?? "<None>", row[ServerObjectHelpers.IsFileColumn]);
                    var fullFileName = (string) row[ServerObjectHelpers.FullNameColumn];
                    var isFile = Convert.ToBoolean(row[ServerObjectHelpers.IsFileColumn], CultureInfo.InvariantCulture);
                    if (isFile)
                    {
                        fileNames.Add(fullFileName);
                    }
                    Assert.That(fullFileName, Does.Contain(@"/"), "Path should have the Linux path separator");
                    Assert.That(fullFileName, Does.StartWith(expectedFolderPath),
                        "Folder path of enumerated file doesn't match source directory");
                    Assert.That(fullFileName, Does.Not.Contain(@"\"),
                        "Unexpected separator presence in " + fullFileName);
                }
                Assert.That(fileNames, Has.Member("/var/opt/mssql/log/errorlog"), "Unexpected folder contents");
            });
        }

        /// <summary>
        /// Regression test for SQL injection bug 9473340
        /// </summary>
        [TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]        
        public void When_Path_has_quote_query_is_properly_escaped()
        {
            ExecuteTest(() =>
            {
                var urn = new Urn(@"Server/File[@Path='C:\path with unescaped '' would raise synax error']");
                var request = new Request(urn, new[] {"Path", "IsFile"});
                Assert.DoesNotThrow(() =>
                {
                    Enumerator.GetData(this.ServerContext.ConnectionContext, request);
                });

            });
        }

        /// <summary>
        /// Test SQL MI not supporting VolumeFreeSpace
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void When_Edition_is_Managed_InstanceVolumeFreeSpace_is_unsupported()
        {
            ExecuteWithDbDrop(database =>
            {
                FileGroup fileGroup = new FileGroup(database, "test_fg");
                fileGroup.Create();
                DataFile dataFile = new DataFile(fileGroup, "Data" + Guid.NewGuid());
                Assert.That(dataFile.IsSupportedProperty(nameof(dataFile.VolumeFreeSpace)), Is.False, "IsSupportedProperty(VolumeFreeSpace)");
            });
        }
    }
}
