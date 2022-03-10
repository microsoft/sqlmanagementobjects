// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
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
    public class AuditSmoTests : SqlTestBase
    {
        /// <summary>
        /// Server audit with SECURITY_LOG destination must be successfully created.
        /// Empty or Unknown destination must throw error upon creating server audit.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, MaxMajor = 15)]
        public void ServerAudit_VerifySecurityLogAndInvalidDestinations()
        {
            var auditNameSecurityLog = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTestSecurityLog");
            var auditNameInvalid = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTestInvalid");

            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);

                Audit serverAudit = new Audit(server, auditNameSecurityLog)
                {
                    DestinationType = AuditDestinationType.SecurityLog
                };

                serverAudit.Create();
                serverAudit.Drop();

                var exception = Assert.Throws<FailedOperationException>(() =>
                {
                    Audit serverAuditInvalid = new Audit(server, auditNameInvalid);

                    serverAuditInvalid.Create();
                });

                Assert.IsInstanceOf<_SMO.PropertyNotSetException>(exception.InnerException, "Unexpected inner exception");
                Assert.That(exception.InnerException.Message, Is.EqualTo(ExceptionTemplates.PropertyNotSetExceptionText("DestinationType")), "Unexpected exception message");

                exception = Assert.Throws<FailedOperationException>(() =>
                {
                    Audit serverAuditInvalid = new Audit(server, auditNameInvalid)
                    {
                        DestinationType = AuditDestinationType.Unknown
                    };

                    serverAuditInvalid.Create();
                });

                Assert.IsInstanceOf<ArgumentException>(exception.InnerException, "Unexpected inner exception");
                Assert.That(exception.InnerException.Message, Is.EqualTo(ExceptionTemplates.UnknownEnumeration("DestinationType")), "Unexpected exception message");
            });
        }

        /// <summary>
        /// Audit object is not supported in SQL azure, this test verify the audit SMO object is not available in azure.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void ServerAudit_VerifyAuditObjectIsNotSupportedInAzure()
        {
            var auditName = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTest");

            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);

                var exception = Assert.Throws<FailedOperationException>(() =>
                {
                    Audit serverAudit = new Audit(server, auditName);
                });
                Assert.IsInstanceOf<TargetInvocationException>(exception.InnerException, "Unexpected inner exception");
                Assert.IsInstanceOf<UnsupportedVersionException>(exception.InnerException.InnerException, "Unexpected inner exception");

                string errorMsg = server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand ? ExceptionTemplates.NotSupportedOnOnDemandWithDetails(typeof(Audit).Name) :
                    ExceptionTemplates.NotSupportedOnCloudWithDetails(typeof(Audit).Name);

                Assert.That(exception.InnerException.InnerException.Message, Is.EqualTo(errorMsg), "Unexpected exception message");
            });
        }

        /// <summary>
        /// Audit object with URL target is only available for managed instance servers, this test verify URL target is not supported 
        /// in non managed instance servers.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void ServerAudit_TestUrlTargetOnlyAvailableInManagedInstance()
        {
            const string auditName = "dummyAuditName";
            const string blobPath = "https://dummystorage.blob.core.windows.net/sqldbauditlog/";

            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);

                var exception = Assert.Throws<FailedOperationException>(() =>
                {
                    Audit serverAudit = new Audit(server, auditName)
                    {
                        DestinationType = AuditDestinationType.Url,
                        FilePath = blobPath
                    };

                    serverAudit.Create();
                });

                if (server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand || server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    Assert.IsInstanceOf<TargetInvocationException>(exception.InnerException, "Unexpected inner exception for SqlOnDemand");

                    Exception sqlOnDemandError = exception.InnerException.InnerException;
                    Assert.IsInstanceOf<UnsupportedVersionException>(sqlOnDemandError, "Unexpected error for SqlOnDemand");

                    string errorMsg = server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand ? ExceptionTemplates.NotSupportedOnOnDemandWithDetails(typeof(Audit).Name) :
                        ExceptionTemplates.NotSupportedOnCloudWithDetails(typeof(Audit).Name);

                    Assert.That(sqlOnDemandError.Message, Is.EqualTo(errorMsg), "Unexpected exception message");
                }
                else
                {
                    Assert.IsInstanceOf<UnsupportedEngineEditionException>(exception.InnerException, "Unexpected inner exception");
                    Assert.That(exception.InnerException.Message,
                        Is.EqualTo(ExceptionTemplates.InvalidPropertyValueForVersion(typeof(Audit).Name,
                            "DestinationType", "Url", server.GetSqlServerVersionName())),
                        "Unexpected exception message");
                }
            });
        }

        /// <summary>
        /// Audit object with EXTERNAL_MONITOR target is only available for managed instance servers, this test verify EXTERNAL_MONITOR target is not supported 
        /// in non managed instance servers.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void ServerAudit_TestExternalMonitorTargetOnlyAvailableInManagedInstance()
        {
            const string auditName = "dummyAuditName";

            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);

                var exception = Assert.Throws<FailedOperationException>(() =>
                {
                    Audit serverAudit = new Audit(server, auditName)
                    {
                        DestinationType = AuditDestinationType.ExternalMonitor
                    };

                    serverAudit.Create();
                });

                if (server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand || server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    Assert.IsInstanceOf<TargetInvocationException>(exception.InnerException, "Unexpected inner exception for SqlOnDemand");

                    Exception sqlOnDemandError = exception.InnerException.InnerException;
                    Assert.IsInstanceOf<UnsupportedVersionException>(sqlOnDemandError, "Unexpected error for SqlOnDemand");

                    string errorMsg = server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand ? ExceptionTemplates.NotSupportedOnOnDemandWithDetails(typeof(Audit).Name) :
                        ExceptionTemplates.NotSupportedOnCloudWithDetails(typeof(Audit).Name);

                    Assert.That(sqlOnDemandError.Message, Is.EqualTo(errorMsg), "Unexpected exception message");
                }
                else
                {
                    Assert.IsInstanceOf<UnsupportedEngineEditionException>(exception.InnerException, "Unexpected inner exception");
                    Assert.That(exception.InnerException.Message,
                        Is.EqualTo(ExceptionTemplates.InvalidPropertyValueForVersion(typeof(Audit).Name,
                            "DestinationType", "ExternalMonitor", server.GetSqlServerVersionName())),
                        "Unexpected exception message");
                }
            });
        }

        /// <summary>
        /// Verifies audit object with EXTERNAL_MONITOR destination type can be successfully created
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void ServerAudit_TestExternalMonitorTargetForManagedInstance()
        {
            var auditName1 = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTest");
            var auditName2 = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTest");
            var auditName3 = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTest");

            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);

                Audit serverAudit1 = new Audit(server, auditName1)
                {
                    DestinationType = AuditDestinationType.ExternalMonitor,
                    OnFailure = OnFailureAction.Continue,
                    IsOperator = true
                };

                Audit serverAudit2 = new Audit(server, auditName2)
                {
                    DestinationType = AuditDestinationType.ExternalMonitor,
                    IsOperator = false
                };

                Audit serverAudit3 = new Audit(server, auditName3)
                {
                    DestinationType = AuditDestinationType.ExternalMonitor
                };

                try
                {
                    serverAudit1.Create();
                    serverAudit2.Create();
                    serverAudit3.Create();

                    serverAudit1.Refresh();
                    serverAudit2.Refresh();
                    serverAudit3.Refresh();

                    Assert.Multiple(() =>
                    {
                        Assert.That(serverAudit1.DestinationType, Is.EqualTo(AuditDestinationType.ExternalMonitor), $"Destinationtype of {serverAudit1.Name}");
                        Assert.That(serverAudit1.OnFailure, Is.EqualTo(OnFailureAction.Continue), $"OnFailure {serverAudit1.Name}");
                        Assert.That(serverAudit1.IsOperator, Is.True, $"IsOperator {serverAudit1.Name}");
                        Assert.That(server.Audits, Has.Member(serverAudit1), $"Server's audit collection should contain created audit {serverAudit1.Name}");
                    });

                    Assert.Multiple(() =>
                    {
                        Assert.That(serverAudit2.DestinationType, Is.EqualTo(AuditDestinationType.ExternalMonitor), $"Destinationtype {serverAudit2.Name}");
                        Assert.That(serverAudit2.IsOperator, Is.False, $"IsOperator {serverAudit2.Name}");
                        Assert.That(server.Audits, Has.Member(serverAudit2), $"Server's audit collection should contain created audit {serverAudit2.Name}");
                    });

                    Assert.Multiple(() =>
                    {
                        Assert.That(serverAudit3.DestinationType, Is.EqualTo(AuditDestinationType.ExternalMonitor), $"Destinationtype {serverAudit3.Name}");
                        Assert.That(serverAudit3.IsOperator, Is.False, $"IsOperator {serverAudit3.Name}");
                        Assert.That(server.Audits, Has.Member(serverAudit3), $"Server's audit collection should contain created audit {serverAudit3.Name}");
                    });

                    Assert.Multiple(() =>
                    {
                        serverAudit3.IsOperator = true;

                        serverAudit3.Alter();
                        serverAudit3.Refresh();

                        Assert.That(serverAudit3.IsOperator, Is.True, $"Alter IsOperator to true {serverAudit3.Name}");

                        serverAudit3.IsOperator = false;

                        serverAudit3.Alter();
                        serverAudit3.Refresh();

                        Assert.That(serverAudit3.IsOperator, Is.False, $"Alter IsOperator to false {serverAudit3.Name}");
                    });
                }
                finally
                {
                    while (server.Audits.Count > 0)
                    {
                        server.Audits[0].DropIfExists();
                    }
                }
            });
        }

        /// <summary>
        /// Batch Auditing is only Supported in SQLv150 and managed instance
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11, MaxMajor = 14)]
        public void ServerAuditspecifications_ExceptionIsThrownForUnspportedAuditGroup()
        {
            var auditName = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTest");

            ExecuteTest(() =>
            {

                var server = new _SMO.Server(this.ServerContext.ConnectionContext);

                var serverAudit = new Audit(server, auditName)
                {
                    DestinationType = AuditDestinationType.ApplicationLog
                };
                serverAudit.Create();

                ServerAuditSpecification serverAuditSpec = new ServerAuditSpecification(server, "Test Audit Specification");
                try
                {
                    serverAuditSpec.AuditName = auditName;
                    serverAuditSpec.AddAuditSpecificationDetail(new AuditSpecificationDetail(AuditActionType.BatchStartedGroup));
                    serverAuditSpec.AddAuditSpecificationDetail(new AuditSpecificationDetail(AuditActionType.BatchCompletedGroup));


                    var exception = Assert.Throws<FailedOperationException>(() =>
                    {
                        serverAuditSpec.Create();
                    });
                }
                finally
                {
                    serverAudit.Drop();
                }
            });
        }

        /// <summary>
        /// Batch Auditing is only Supported in SQLv150 and managed instance
        /// </summary>
        [TestMethod]
        //[SupportedTargetServerFriendlyName("Sqlv150")]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void ServerAuditspecifications_VerifySupportForBatchAuditingActionType()
        {
            var auditName = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTest");
            var auditSpecificationName = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditSpecificationTest");

            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);

                var serverAudit = new Audit(server, auditName)
                {
                    DestinationType = AuditDestinationType.ApplicationLog
                };
                serverAudit.Create();

                ServerAuditSpecification serverAuditSpec = new ServerAuditSpecification(server, auditSpecificationName);
                try
                {
                    serverAuditSpec.AuditName = auditName;
                    serverAuditSpec.AddAuditSpecificationDetail(new AuditSpecificationDetail(AuditActionType.BatchStartedGroup));
                    serverAuditSpec.AddAuditSpecificationDetail(new AuditSpecificationDetail(AuditActionType.BatchCompletedGroup));

                    Assert.DoesNotThrow(() =>
                    {
                        serverAuditSpec.Create();
                        serverAuditSpec.Drop();
                    }, "Batch Auditing should be supported on SQL 2019");
                }
                finally
                {
                    try { serverAudit.Drop(); } catch { }
                }
            });
        }

        /// <summary>
        /// SERVER_PERMISSION_CHANGE_GROUP Auditing should work in SMO
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void ServerAuditspecifications_I_Should_Be_Able_To_Create_And_Script_A_ServerAuditSpecification_For_SERVER_PERMISSION_CHANGE_GROUP()
        {
            var auditName = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditTest");
            var auditSpecificationName = SmoObjectHelpers.GenerateUniqueObjectName("SmoAuditSpecificationTest");

            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);

                var serverAudit = new Audit(server, auditName) { DestinationType = AuditDestinationType.ApplicationLog };

                var serverAuditSpec = new ServerAuditSpecification(server, auditSpecificationName) { AuditName = auditName };

                try
                {
                    serverAudit.Create();

                    serverAuditSpec.AddAuditSpecificationDetail(new AuditSpecificationDetail(AuditActionType.ServerPermissionChangeGroup));

                    Assert.DoesNotThrow(() =>
                    {
                        // If this one throws, then something regressed...
                        serverAuditSpec.Create();

                    }, "Unexpected exception while creating audit specification 'SERVER_PERMISSION_CHANGE_GROUP'");

                    Assert.DoesNotThrow(() =>
                    {
                        // Likewise, this should not throw ,and the one and only string in the script should have this T-SQL fragment
                        Assert.That(serverAuditSpec.Script()[0], Contains.Substring("ADD (SERVER_PERMISSION_CHANGE_GROUP)"));
                    }, "Unexpected exception while scripting audit specification 'SERVER_PERMISSION_CHANGE_GROUP'");

                }
                finally
                {
                    // It is benign to fail to clean-up
                    try { serverAuditSpec.Drop(); } catch { }
                    try { serverAudit.Drop(); } catch { }
                }
            });
        }

        /// <summary>
        /// AuditActionType enum should contain all the values present in the database
        /// </summary>
        [TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void ServerAuditspecifications_Audit_Action_Types_In_DB_Should_Match_Enum_Values()
        {
            ExecuteTest(() =>
            {
                var enumAttributeNames = GetTsqlSyntaxStringAttributeNames();
                var query = @"Select DISTINCT name FROM master.sys.dm_audit_actions WHERE name like '%GROUP%'
                    UNION ALL SELECT name FROM (VALUES ('SELECT'),('UPDATE'),('INSERT'),('DELETE'),('EXECUTE'),('RECEIVE'),('REFERENCES')) actions(name)";

                var dbAuditList = ServerContext.ConnectionContext.ExecuteWithResults(query).Tables[0].Rows.Cast<DataRow>().Select(row => row["name"].ToString());

                // enumAttributeNames is a superset of dbAuditList.
                Assert.That(enumAttributeNames, Is.SupersetOf(dbAuditList), @"Some types are missing in AuditActionType enum.
                    Please, update enum AuditActionType in /src/Microsoft/SqlServer/Management/SqlEnum/enumstructs.cs");
            });
        }

        private static IEnumerable<string> GetTsqlSyntaxStringAttributeNames()
        {
            var enumType = typeof(AuditActionType);
            return from name in Enum.GetNames(enumType)
                   let valueAttributes = enumType.GetMember(name).First(m => m.DeclaringType == enumType).GetCustomAttributes(typeof(TsqlSyntaxStringAttribute), false)
                   select ((TsqlSyntaxStringAttribute)valueAttributes[0]).DisplayName;
        }
    }
}
