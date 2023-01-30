// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing AvailabilityGroup properties and scripting
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDatabaseEdge)]
    public class AvailabilityGroup_SmoTestSuite : SmoObjectTestBase
    {
#region Property Tests

        /// <summary>
        /// Test ClusterType getter/setter on unsupported server versions.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11, MaxMajor = 13)]
        public void Property_ClusterType_Unsupported()
        {
            this.ExecuteTest(server =>
            {
                var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);
                TraceHelper.TraceInformation("Invoking the getter for ClusterType property, UnknownPropertyException should be thrown.");

                Assert.AreEqual(
                    "ClusterType",
                    Assert.Throws<_SMO.UnknownPropertyException>(() => { var _ = ag.ClusterType; }).PropertyName);

                TraceHelper.TraceInformation("Invoking the setter for ClusterType property, UnknownPropertyException should be thrown.");
                Assert.AreEqual(
                    "ClusterType",
                    Assert.Throws<_SMO.UnknownPropertyException>(() => ag.ClusterType = _SMO.AvailabilityGroupClusterType.Wsfc).PropertyName);
                Assert.AreEqual(
                    "ClusterType",
                    Assert.Throws<_SMO.UnknownPropertyException>(() => ag.ClusterType = _SMO.AvailabilityGroupClusterType.None).PropertyName);
            });
        }

        /// <summary>
        /// Test ClusterType getter/setter on unsupported server versions.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11, MaxMajor = 13)]
        public void Property_RequiredSyncSecondariesToCommit_Unsupported()
        {
            this.ExecuteTest(server =>
            {
                var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                TraceHelper.TraceInformation("Invoking the getter for RequiredSynchronizedSecondariesToCommit property, UnknownPropertyException should be thrown.");
                Assert.AreEqual(
                    "RequiredSynchronizedSecondariesToCommit",
                    Assert.Throws<_SMO.UnknownPropertyException>(() => { var _ = ag.RequiredSynchronizedSecondariesToCommit; }).PropertyName);

                TraceHelper.TraceInformation("Invoking the setter for RequiredSynchronizedSecondariesToCommit property, UnknownPropertyException should be thrown.");
                Assert.AreEqual(
                    "RequiredSynchronizedSecondariesToCommit",
                    Assert.Throws<_SMO.UnknownPropertyException>(() => ag.RequiredSynchronizedSecondariesToCommit = 1).PropertyName);
            });
        }

#endregion

#region Scripting Tests

        /// <summary>
        /// Test creating an AG of various ClusterTypes on supported server versions.
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        public void AvailabilityGroup_ScriptCreate_ClusterType_Supported()
        {
            this.ExecuteTest(server =>
            {
                {
                    var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                    TraceHelper.TraceInformation("Default cluster type should be WSFC, so DDL should not contain CLUSTER_TYPE clause.");
                    Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create), 
                        Does.Not.Contain("CLUSTER_TYPE").IgnoreCase, 
                        "Script should not contain CLUSTER_TYPE clause");
                }

                {
                    var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                    TraceHelper.TraceInformation("Setting cluster type to WSFC should work.");
                    ag.ClusterType = _SMO.AvailabilityGroupClusterType.Wsfc;
                    Assert.That(ag.ClusterType,
                        Is.EqualTo(_SMO.AvailabilityGroupClusterType.Wsfc),
                        "Incorrect ClusterType");

                    TraceHelper.TraceInformation("If cluster type is WSFC, DDL should not contain CLUSTER_TYPE clause.");
                    Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create),
                        Does.Not.Contain("CLUSTER_TYPE").IgnoreCase,
                        "Script should not contain CLUSTER_TYPE clause");
                }

                {
                    var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                    TraceHelper.TraceInformation("Setting cluster type to NONE should work.");
                    ag.ClusterType = _SMO.AvailabilityGroupClusterType.None;
                    Assert.That(ag.ClusterType,
                        Is.EqualTo(_SMO.AvailabilityGroupClusterType.None),
                        "Incorrect ClusterType");

                    TraceHelper.TraceInformation("If cluster type is None, DDL should contain CLUSTER_TYPE = NONE clause.");
                    Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create),
                        Does.Contain("CLUSTER_TYPE = NONE"),
                        "Script should contain 'CLUSTER_TYPE = NONE'");
                }

                {
                    var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                    TraceHelper.TraceInformation("Setting cluster type to EXTERNAL should work.");
                    ag.ClusterType = _SMO.AvailabilityGroupClusterType.External;
                    Assert.That(ag.ClusterType,
                        Is.EqualTo(_SMO.AvailabilityGroupClusterType.External),
                        "Incorrect ClusterType");

                    TraceHelper.TraceInformation("If cluster type is External, DDL should contain CLUSTER_TYPE = EXTERNAL clause.");
                    Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create),
                        Does.Contain("CLUSTER_TYPE = EXTERNAL"),
                        "Script should contain 'CLUSTER_TYPE = EXTERNAL'");
                }
            });
        }

        /// <summary>
        /// Test joining an AG of various ClusterTypes on unsupported server versions.
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11, MaxMajor = 13)]
        public void AvailabilityGroup_ScriptJoin_ClusterType_Unsupported()
        {
            this.ExecuteTest(server =>
            {
                TraceHelper.TraceInformation("DDL for joining AG without specifying cluster type should assume WSFC AG, so should not contain CLUSTER_TYPE clause.");
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, () => server.JoinAvailabilityGroup("ag1")),
                    Does.Not.Contain("CLUSTER_TYPE").IgnoreCase,
                    "Script should not contain CLUSTER_TYPE clause.");

                TraceHelper.TraceInformation("DDL for joining WSFC AG DDL should not contain CLUSTER_TYPE clause.");
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, () => server.JoinAvailabilityGroup("ag1", _SMO.AvailabilityGroupClusterType.Wsfc)),
                    Does.Not.Contain("CLUSTER_TYPE").IgnoreCase,
                    "Script should not contain CLUSTER_TYPE clause.");

                TraceHelper.TraceInformation("Joining AG with cluster type None or External should fail.");
                var exception = Assert.Throws<_SMO.FailedOperationException>(() => TSqlScriptingHelper.GenerateScriptForAction(server, () => server.JoinAvailabilityGroup("ag1", _SMO.AvailabilityGroupClusterType.None)));
                Assert.IsInstanceOf<_SMO.UnsupportedVersionException>(exception.InnerException);

                exception = Assert.Throws<_SMO.FailedOperationException>(() => TSqlScriptingHelper.GenerateScriptForAction(server, () => server.JoinAvailabilityGroup("ag1", _SMO.AvailabilityGroupClusterType.External)));
                Assert.IsInstanceOf<_SMO.UnsupportedVersionException>(exception.InnerException);
            });
        }

        /// <summary>
        /// Test joining an AG of various ClusterTypes on supported server versions.
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        public void AvailabilityGroup_ScriptJoin_ClusterType_Supported()
        {
            this.ExecuteTest(server =>
            {
                TraceHelper.TraceInformation("DDL for joining AG without specifying cluster type should assume WSFC AG, so should not contain CLUSTER_TYPE clause.");
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, () => server.JoinAvailabilityGroup("ag1")),
                    Does.Not.Contain("CLUSTER_TYPE").IgnoreCase,
                    "Script should not contain CLUSTER_TYPE clause");

                TraceHelper.TraceInformation("DDL for joining WSFC AG DDL should not contain CLUSTER_TYPE clause.");
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, () => server.JoinAvailabilityGroup("ag1", _SMO.AvailabilityGroupClusterType.Wsfc)),
                    Does.Not.Contain("CLUSTER_TYPE").IgnoreCase,
                    "Script should not contain CLUSTER_TYPE clause");

                TraceHelper.TraceInformation("DDL for joining AG with cluster type None should contain WITH (CLUSTER_TYPE = NONE) clause.");
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, () => server.JoinAvailabilityGroup("ag1", _SMO.AvailabilityGroupClusterType.None)),
                    Does.Contain("WITH (CLUSTER_TYPE = NONE)"),
                    "Script should contain CLUSTER_TYPE clause");

                TraceHelper.TraceInformation("DDL for joining AG with cluster type External should contain WITH (CLUSTER_TYPE = EXTERNAL) clause.");
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, () => server.JoinAvailabilityGroup("ag1", _SMO.AvailabilityGroupClusterType.External)),
                    Does.Contain("WITH (CLUSTER_TYPE = EXTERNAL)"),
                    "Script should contain CLUSTER_TYPE clause");
            });
        }

        /// <summary>
        /// Test that creating an AG on a version that doesn't support RequiredSynchronizedSecondariesToCommit does not contain the REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT clause.
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11, MaxMajor = 13)]
        public void AvailabilityGroup_ScriptCreate_RequiredSyncSecondariesToCommit_Unsupported()
        {
            this.ExecuteTest(server =>
            {
                {
                    var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                    Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create),
                        Does.Not.Contain("REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT").IgnoreCase,
                        "Script should not contain REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT clause");
                }
            });
        }

        /// <summary>
        /// Test creating an AG with various values of RequiredSynchronizedSecondariesToCommit on supported server versions.
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        public void AvailabilityGroup_ScriptCreate_RequiredSyncSecondariesToCommit_Supported()
        {
            this.ExecuteTest(server =>
            {
                {
                    var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                    TraceHelper.TraceInformation("RequiredSynchronizedSecondariesToCommit wasn't set, so DDL shouldn't contain it either.");
                    Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create),
                        Does.Not.Contain("REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT").IgnoreCase,
                        "Script should not contain REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT clause");
                }

                {
                    var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                    ag.RequiredSynchronizedSecondariesToCommit = 1;
                    Assert.That(ag.RequiredSynchronizedSecondariesToCommit, Is.EqualTo(1), "Incorrect RequiredSynchronizedSecondariesToCommit");
                    
                    TraceHelper.TraceInformation("RequiredSynchronizedSecondariesToCommit was set, so DDL should contain it.");
                    Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create), Does.Contain("REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT = 1"),
                        "Script should contain REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT clause");
                }
            });
        }

        /// <summary>
        /// Test altering an AG with various values of RequiredSynchronizedSecondariesToCommit on supported server versions.
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        public void AvailabilityGroup_ScriptAlter_RequiredSyncSecondariesToCommit_Supported()
        {
            this.ExecuteTest(server =>
            {
                var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                ag.BasicAvailabilityGroup = false;
                ag.RequiredSynchronizedSecondariesToCommit = 1;

                TraceHelper.TraceInformation("RequiredSynchronizedSecondariesToCommit was set, so DDL should contain it.");
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.Alter),
                    Does.Contain("REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT = 1"),
                    "Script should contain REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT clause");
            });
        }

        /// <summary>
        /// Test creating distributed availability group
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void AvailabilityGroup_CreateDistributedAvailabilityGroup()
        {
            this.ExecuteTest(server =>
            {
                string firstAGName = "firstAG";
                string secondAGName = "secondAG";
                string distributedGroupName = "DA G";

                _SMO.AvailabilityGroup dag = new _SMO.AvailabilityGroup(server, distributedGroupName) { IsDistributedAvailabilityGroup = true };

                dag.AvailabilityReplicas.Add(new _SMO.AvailabilityReplica(dag, server.NetNameWithInstance())
                {
                    EndpointUrl = "tcp://" + server.NetName + ":5022",
                    FailoverMode = _SMO.AvailabilityReplicaFailoverMode.Manual,
                    AvailabilityMode = _SMO.AvailabilityReplicaAvailabilityMode.AsynchronousCommit,
                    SeedingMode = _SMO.AvailabilityReplicaSeedingMode.Automatic,
                    Name = firstAGName
                });

                dag.AvailabilityReplicas.Add(new _SMO.AvailabilityReplica(dag, secondAGName)
                {
                    EndpointUrl = "tcp://MyOtherServer:1234",
                    FailoverMode = _SMO.AvailabilityReplicaFailoverMode.Manual,
                    AvailabilityMode = _SMO.AvailabilityReplicaAvailabilityMode.AsynchronousCommit,
                    SeedingMode = _SMO.AvailabilityReplicaSeedingMode.Automatic,
                    Name = secondAGName
                });

                string generatedScript = TSqlScriptingHelper.GenerateScriptForAction(server, dag.Create);

                Assert.That(generatedScript,
                     Does.Contain("WITH (DISTRIBUTED)"),
                     "Script should contain WITH (DISTRIBUTED) clause");

                Assert.That(generatedScript,
                    Does.Contain("AVAILABILITY GROUP ON"),
                    "Script should contain AVAILABILITY GROUP ON clause");

                Assert.That(generatedScript,
                    Does.Contain($"N'{secondAGName}' WITH (LISTENER_URL = N'tcp://MyOtherServer:1234', AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT, FAILOVER_MODE = MANUAL, SEEDING_MODE = AUTOMATIC)"),
                    "Script should contain second AG specs");

                Assert.That(generatedScript,
                   Does.Contain($"N'{firstAGName}' WITH (LISTENER_URL = N'tcp://{server.NetName}:5022', AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT, FAILOVER_MODE = MANUAL, SEEDING_MODE = AUTOMATIC)"),
                   "Script should contain first AG specs");

                Assert.That(generatedScript,
                  Does.Contain($"[DA G]"),
                  "Script should escape special characters.");
            });
        }

        /// <summary>
        /// Test if distributed availability group that doesn't have "Server=" in endpoint URL of any replica isn't managed instance link
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, HostPlatform = HostPlatformNames.Windows)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlDatabaseEdge)]
        public void AvailabilityGroup_IsNotManagedInstanceLink_WhenReplicaDoesNotContainServerInEndpointUrl()
        {
            this.ExecuteTest(server =>
            {
                if (server.IsHadrEnabled)
                {
                    var dbName = "db" + Guid.NewGuid();
                    var db = new _SMO.Database(server, dbName);
                    var agName = "ag" + Guid.NewGuid();
                    var ag = new _SMO.AvailabilityGroup(server, agName)
                    {
                        ClusterType = _SMO.AvailabilityGroupClusterType.None
                    };
                    var dagName = "dag" + Guid.NewGuid();
                    var dag = new _SMO.AvailabilityGroup(server, dagName)
                    {
                        IsDistributedAvailabilityGroup = true
                    };
                    try
                    {
                        AlwaysOnTestHelper.CreateDatabaseWithBackup(server, db);
                        AlwaysOnTestHelper.CreateAvailabilityGroupForDatabase(server, ag, dbName);
                        AlwaysOnTestHelper.CreateDistributedAvailabilityGroup(server, dag, agName, "tcp://1.2.3.4:5022");

                        Assert.That(ag.IsManagedInstanceLink, Is.False, "Availability Group that isn't distributed shoudn't be Managed Instance Link");
                        Assert.That(dag.IsManagedInstanceLink, Is.False, "Availability Group that doesn't have replica that has 'Server=' in endpoint url shoudn't be Managed Instance Link");
                    }
                    finally
                    {
                        ag.DropIfExists();
                        db.DropIfExists();
                        dag.DropIfExists();
                    }
                }
            });
        }

        /// <summary>
        /// Test creating valid managed instance link
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, HostPlatform = HostPlatformNames.Windows)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlDatabaseEdge)]
        public void AvailabilityGroup_AvailabilityGroupIsManagedInstanceLink()
        {
            this.ExecuteTest(server =>
            {
                if (server.IsHadrEnabled)
                {
                    var dbName = "db" + Guid.NewGuid();
                    var db = new _SMO.Database(server, dbName);
                    var agName = "ag" + Guid.NewGuid();
                    var ag = new _SMO.AvailabilityGroup(server, agName)
                    {
                        ClusterType = _SMO.AvailabilityGroupClusterType.None
                    };
                    var dagName = "dag" + Guid.NewGuid();
                    var dag = new _SMO.AvailabilityGroup(server, dagName)
                    {
                        IsDistributedAvailabilityGroup = true
                    };
                    try
                    {
                        AlwaysOnTestHelper.CreateDatabaseWithBackup(server, db);
                        AlwaysOnTestHelper.CreateAvailabilityGroupForDatabase(server, ag, dbName);
                        AlwaysOnTestHelper.CreateDistributedAvailabilityGroup(server, dag, agName, "tcp://chimera-prod-gp-04eu.public.7a059cce123c.database.windows.net:5022;Server=[chimera-prod-gp-04eu]");

                        Assert.That(ag.IsManagedInstanceLink, Is.False, "Availability Group that isn't distributed shoudn't be Managed Instance Link");
                        Assert.That(dag.IsManagedInstanceLink, Is.True, "Availability Group that is distributed, has two replicas and one of the replicas has 'Server=' in endpoint url should be Managed Instance Link");
                    }
                    finally
                    {
                        ag.DropIfExists();
                        db.DropIfExists();
                        dag.DropIfExists();
                    }
                }
            });
        }

        /// <summary>
        /// Test granting an AG permission to create any database
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void AvailabilityGroup_ScriptGrantAvailabilityGroupCreateDatabasePermission()
        {
            this.ExecuteTest(server =>
            {
                string agName = "ag1";
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, () => { server.GrantAvailabilityGroupCreateDatabasePrivilege(agName); }),
                    Does.Contain(string.Format("ALTER AVAILABILITY GROUP [{0}] GRANT CREATE ANY DATABASE", agName)),
                    "Script for granting AG permission was not generated correctly");
            });
        }

        /// <summary>
        /// Test granting an AG permission to create any database
        ///
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void AvailabilityGroup_ScriptDemoteAvailabilityGroup()
        {
            this.ExecuteTest(server =>
            {
                var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);
                Assert.That(TSqlScriptingHelper.GenerateScriptForAction(server, ag.DemoteAsSecondary),
                    Does.Contain(string.Format("ALTER AVAILABILITY GROUP [{0}] SET (ROLE = SECONDARY)", ag.Name)),
                    "Script for demoting AG was not generated correctly");
            });
        }

        #endregion Scripting Tests

        #region Contained AG Tests
        // DEVNOTE(MatteoT): 4/17/2022 I am intentionally tagging these tests with
        //   [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        // for 2 reasons:
        //  1) we don't have a nice standalone box, so this is always trouble to run locally (at least for me)
        //  2) I am not sure there's a whole lot of value running the test for both Linux and Windows: they are equivalent in this case.

        /// <summary>
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        /// <remarks>Contained AG is new in SQL 2022</remarks>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11, MaxMajor = 15, Edition = DatabaseEngineEdition.Enterprise)]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        public void AvailabilityGroup_Neither_Contained_Nor_Reuse_System_Databases_Are_Scripted_For_Servers_That_Dont_Support_Contained_AGs()
        {
            this.ExecuteTest(server =>
            {
                {
                    foreach (var reusesdbs in new[] { (bool?)null, true, false })
                    {
                        var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                        if (reusesdbs.HasValue)
                        {
                            ag.ReuseSystemDatabases = reusesdbs.Value;
                        }

                        var createScript = TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create);

                        Assert.That(createScript,
                            Does.Not.Contain("CONTAINED").IgnoreCase,
                            $"CONTAINED should not appear in script for legacy versions of SQL Server (ReuseSystemDatabases={reusesdbs}");

                        Assert.That(createScript,
                            Does.Not.Contain("REUSE_SYSTEM_DATABASES").IgnoreCase,
                            $"REUSE_SYSTEM_DATABASES should not appear in script for legacy versions of SQL Server (ReuseSystemDatabases={reusesdbs}");
                    }
                }
            });
        }

        /// <summary>
        /// Only captures the T-SQL scripts, doesn't execute them.
        /// </summary>
        /// <remarks>Contained AG is new in SQL 2022</remarks>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16, Edition = DatabaseEngineEdition.Enterprise)]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        public void AvailabilityGroup_Contained_And_Reuse_System_Databases_Are_Scripted_Currectly_For_Servers_That_Support_Contained_AGs()
        {
            this.ExecuteTest(server =>
            {
                {
                    foreach (var contained in new[] { (bool?)null, true, false })
                    {
                        foreach (var reusesdbs in new[] { (bool?)null, true, false })
                        {
                            var ag = AlwaysOnTestHelper.CreateDefaultAGObject(server);

                            if (contained.HasValue)
                            {
                                ag.IsContained = contained.Value;
                            }

                            if (reusesdbs.HasValue)
                            {
                                ag.ReuseSystemDatabases = reusesdbs.Value;
                            }

                            var createScript = TSqlScriptingHelper.GenerateScriptForAction(server, ag.Create);

                            if (!contained.GetValueOrDefault())
                            {
                                Assert.That(createScript,
                                    Does.Not.Contain("CONTAINED").IgnoreCase,
                                    $"CONTAINED should not be scriped (IsContained={contained}, ReuseSystemDatabases={reusesdbs}");

                                // Regardless of the value we set it to, the propery is ignored because the AG is not contained
                                Assert.That(createScript,
                                    Does.Not.Contain("REUSE_SYSTEM_DATABASES").IgnoreCase,
                                    $"REUSE_SYSTEM_DATABASES should not be scripted (IsContained={contained}, ReuseSystemDatabases={reusesdbs}");
                            }
                            else
                            {
                                Assert.That(createScript,
                                    Does.Contain("CONTAINED").IgnoreCase,
                                    $"CONTAINED should appear in script for SQL 2022 or higher (IsContained={contained}, ReuseSystemDatabases={reusesdbs})");

                                var scripted_or_not = reusesdbs.GetValueOrDefault() ? "scripted " : "not scripted ";

                                Assert.That(createScript,
                                    !reusesdbs.GetValueOrDefault()
                                    ? Does.Not.Contain("REUSE_SYSTEM_DATABASES").IgnoreCase
                                    : Does.Contain("REUSE_SYSTEM_DATABASES").IgnoreCase,
                                    $"REUSE_SYSTEM_DATABASES was {scripted_or_not} (IsContained={contained}, ReuseSystemDatabases={reusesdbs}");
                            }
                        }
                    }
                }
            });
        }

        #endregion Contained AG Test


        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            Assert.Fail();
        }
    }

#if false //Commented out temporarily for moving to SSMS_Main as this will take significant rework to be usable in the new branch
    /// <summary>
    /// Test suite for testing AvailabilityGroup properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Full, FeatureCoverage.Manageability)]
    [TestRequirementNumberOfMachines(2, 3)]
    [TestRequirementBuildDynamicCluster(true)]
    public class AvailabilityGroup_SmoTestSuite : SmoObjectTestBase
    {
#region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.AvailabilityGroup ag = (_SMO.AvailabilityGroup)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.AvailabilityGroups.Refresh();
            Assert.IsNull(server.AvailabilityGroups[ag.Name],
                            "Current availability group not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an availability group with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod,Ignore]
        public void SmoDropIfExists_AvailabilityGroup_Sql16AndAfterOnPrem()
        {
            string serverPrimName = Context.TestEnvironment.SqlProcessEnvironments[0].ServerName;
            string epPrimName = GenerateUniqueSmoObjectName("epPrim");
            const int epPrimPort = 5022;
            _SMO.Server serverPrim = new _SMO.Server(serverPrimName);
            _SMO.AvailabilityGroup ag = new _SMO.AvailabilityGroup(serverPrim,
                GenerateUniqueSmoObjectName("ag"));
            _SMO.AvailabilityReplica arPrim = new _SMO.AvailabilityReplica(ag,
                serverPrim.ConnectionContext.TrueName);
            _SMO.Endpoint[] epPrimList = serverPrim.Endpoints.EnumEndpoints(_SMO.EndpointType.DatabaseMirroring);
            _SMO.Endpoint epPrim = new _SMO.Endpoint(serverPrim, epPrimName);

            if (serverPrim.VersionMajor < 13)
                throw new RequirementsNotMetException("DropIfExists method is available for SQL2016+ (v13+)");

            try
            {
                // Server can have only one database mirroring endpoint. If endpoint doesn't exists,
                // it will be created.
                //
                if (epPrimList.Length != 0)
                {
                    epPrim = epPrimList[0];
                }
                else
                {
                    epPrim.ProtocolType = _SMO.ProtocolType.Tcp;
                    epPrim.EndpointType = _SMO.EndpointType.DatabaseMirroring;
                    epPrim.Protocol.Tcp.ListenerPort = epPrimPort;
                    epPrim.Payload.DatabaseMirroring.ServerMirroringRole = _SMO.ServerMirroringRole.All;
                    epPrim.Payload.DatabaseMirroring.EndpointEncryption = _SMO.EndpointEncryption.Required;
                    epPrim.Payload.DatabaseMirroring.EndpointEncryptionAlgorithm = _SMO.EndpointEncryptionAlgorithm.Aes;

                    epPrim.Create();
                }

                arPrim.EndpointUrl = string.Format("TCP://{0}:{1}", serverPrim.NetName, epPrim.Protocol.Tcp.ListenerPort);
                arPrim.FailoverMode = _SMO.AvailabilityReplicaFailoverMode.Automatic;
                arPrim.AvailabilityMode = _SMO.AvailabilityReplicaAvailabilityMode.SynchronousCommit;

                ag.AvailabilityReplicas.Add(arPrim);
                serverPrim.AvailabilityGroups.Add(ag);

                VerifySmoObjectDropIfExists(ag, serverPrim);
            }
            catch (Exception)
            {
                if (serverPrim.AvailabilityGroups[ag.Name] != null &&
                    ag.State == _SMO.SqlSmoState.Existing)
                {
                    ag.Drop();
                }
                throw;
            }
            finally
            {
                if (serverPrim.Endpoints[epPrimName] != null)
                {
                    epPrim.DropIfExists();
                }
            }
        }

#endregion // Scripting Tests
    }
#endif
}
