// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;


namespace Microsoft.SqlServer.Test.SMO.Sfc
{
    /// <summary>
    /// SFC enumerator tests
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class SfcEnumeratorTests : SqlTestBase
    {
        /// <summary>
        /// Verify that database names that contain curly braces are not problematic
        /// when trying to retrieve the 'UsedSpace' property of a file in a database
        /// when the database name contains curly braces in it.
        /// </summary>
        /// <remarks>This is the regression test for TFS#9692371
        /// </remarks>
        [TestMethod]
        [SupportedServerVersionRangeAttribute(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void Can_Query_File_UsedSpace_When_Database_Name_Contains_Curly_Braces()
        {
            foreach(var trickyDbName in new [] { "{0}", "{foo", "}foo", "f{o}o" })
            {
                ExecuteWithDbDrop(
                    dbNamePrefix: trickyDbName,
                    testMethod:
                        db =>
                        {
                            var requestUrn =
                                string.Format(
                                    "Server/Database[@Name='{0}']/FileGroup[@Name='{1}' and @IsFileStream = 0]/File",
                                    Urn.EscapeString(db.Name),
                                    Urn.EscapeString(db.FileGroups[0].Name));

                            var requestFields = new[] { "Name", "UsedSpace" };

                            var request = new Request(requestUrn, requestFields);

                            // This is not supposed to throw...
                            var result = (new Enumerator()).Process(this.ServerContext.ConnectionContext, request);

                            Assert.NotNull(result, "Process() returned an unexpected value!");

                            Assert.IsInstanceOf<DataTable>(result.Data, "The Data field is of an unexpected type!");

                            Assert.That(((DataTable)result.Data).Rows.Count, Is.EqualTo(1), "There should be exactly one row in the returned data!");
                        });
            }
        }
    }
}
