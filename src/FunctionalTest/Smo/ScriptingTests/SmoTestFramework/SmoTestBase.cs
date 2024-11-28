// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;

// We can run our tests in parallel, scoped at the class level.
// We set Workers to 4 because a higher value led to issues such as exceeding the DTU limit and being throttled on our Azure test server
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.Parallelize(Scope = Microsoft.VisualStudio.TestTools.UnitTesting.ExecutionScope.ClassLevel, Workers = 4)]

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Base test class for SMO Scripting Tests
    /// </summary>
    public abstract class SmoTestBase : SqlTestBase
    {
        /// <summary>
        /// Mapping of all test servers to the scripting options we should use when targeting that server version
        /// </summary>
        /// <remarks>We create new objects with each call to this so that any modifications made
        /// to the ScriptingOptions object don't affect other tests, just the one that called it</remarks>
        /// For the Server versions Sqlv150, AzureSterlingV12, AzureSterlingV12_DW, the OptimizerData parameter
        /// value is set to true and without this the server will generate its own data instead of reading the stream
        /// as this property is optional
        public static IEnumerable<Tuple<string, ScriptingOptions>> TestServerScriptingOptions
        {
            get
            {
                yield return new Tuple<string, ScriptingOptions>(
                    "Sql2008",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version100,
                        IncludeScriptingParametersHeader = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "Sql2008R2",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version105,
                        IncludeScriptingParametersHeader = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "Sql2012",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version110,
                        IncludeScriptingParametersHeader = true,
                        // existence check added to increase code coverage
                        IncludeIfNotExists = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "Sql2014",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version120,
                        IncludeScriptingParametersHeader = true,
                        Permissions = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "Sql2016",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version130,
                        IncludeScriptingParametersHeader = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "Sql2017",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version140,
                        IncludeScriptingParametersHeader = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "Sqlv150",
                    new ScriptingOptions()
                    {
                        ExtendedProperties = true,
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version150,
                        IncludeScriptingParametersHeader = true,
                        OptimizerData = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "Sqlv160",
                    new ScriptingOptions()
                    {
                        ExtendedProperties = true,
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version160,
                        IncludeScriptingParametersHeader = true,
                        OptimizerData = true,
                        Permissions = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "AzureSterlingV12",
                    new ScriptingOptions()
                    {
                        ExtendedProperties = true,
                        TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlDatabase,
                        TargetDatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
                        TargetServerVersion = SqlServerVersion.Version130,
                        IncludeScriptingParametersHeader = true,
                        OptimizerData = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "AzureSterlingV12_SqlDW",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlDataWarehouse,
                        TargetDatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
                        TargetServerVersion = SqlServerVersion.Version130,
                        IncludeScriptingParametersHeader = true,
                        OptimizerData = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "SqlDatabaseEdge",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlDatabaseEdge,
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                        TargetServerVersion = SqlServerVersion.Version150,
                        IncludeScriptingParametersHeader = true,
                        OptimizerData = true
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "SqlManagedInstance",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlManagedInstance,
                        TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                    });
                yield return new Tuple<string, ScriptingOptions>(
                    "AzureSterlingV12_SqlOnDemand",
                    new ScriptingOptions()
                    {
                        TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlOnDemand,
                        TargetDatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
                        TargetServerVersion = SqlServerVersion.Version120,
                        IncludeScriptingParametersHeader = true,
                        OptimizerData = true
                    });
            }
        }

        // This enum is used to distinguish between scripting during transfer
        // and scripting during baseline generation.
        //
        public enum TestScriptingContext
        {
            Transfer,
            Baseline
        }

        // This method is used to ensure all condition are met for a test server to be the target
        // of another test server.
        //
        public static IEnumerable<Tuple<string, ScriptingOptions>> TestSupportedServerScriptingOptions(Database db, TestScriptingContext testScriptingContext)
        {
            var serverOptions = TestServerScriptingOptions.ToList();
            if (db.ServerVersion.Major < 15)
            {
                // Edge only supports scripting options for servers that are greater than version 15
                //
                serverOptions.RemoveAll((server) => server.Item1.Contains("SqlDatabaseEdge"));
            }

            if (testScriptingContext == TestScriptingContext.Transfer)
            {
                // Transfer scripting is not supported for SqlOnDemand and Managed Instance
                //
                serverOptions.RemoveAll((server) => server.Item1.Contains("AzureSterlingV12_SqlOnDemand") || server.Item1.Contains("SqlManagedInstance"));
            }

            if (testScriptingContext == TestScriptingContext.Baseline)
            {
                if (db.DatabaseEngineEdition != DatabaseEngineEdition.SqlOnDemand)
                {
                    // Only generate baseline script options when database engine edition is on demand
                    //
                    serverOptions.RemoveAll((server) => server.Item1.Contains("AzureSterlingV12_SqlOnDemand"));
                }

                // Cover only MI->MI scenarios for baseline tests
                //
                if (db.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
                {
                    serverOptions.RemoveAll((server) => server.Item1.Contains("SqlManagedInstance"));
                }

                if (db.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                {
                    return serverOptions.Where((server) => server.Item1.Contains("SqlManagedInstance"));
                }
            }
            // DW is only supported for scripting to DW
            if (db.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
            {
                return serverOptions.Where(s => s.Item2.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse);
            }
            else
            {
                return serverOptions.Where(s => s.Item2.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse);
            }
        }

        /// <summary>
        /// Scripts out a SMO object and returns the full script as a single string
        /// </summary>
        /// <param name="smoObject">The object to script</param>
        /// <param name="options">Optional : ScriptingOptions to pass to the Script call</param>
        /// <returns>A single string containing the full script</returns>
        protected string ScriptSmoObject(IScriptable smoObject, ScriptingOptions options = null)
        {
            var sb = new StringBuilder();
            foreach (string line in options == null ? smoObject.Script() : smoObject.Script(options))
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Verifies the URN list produces the expected strings when the 'ScriptMaker' class is used to script
        /// the URNs.
        /// </summary>
        /// <param name="database">Database to operate in.</param>
        /// <param name="urnList">The list of URNs to script.</param>
        /// <param name="expectedResults">The expected strings.</param>
        /// <param name="preferences">If null, defaults to ScriptBehavior.Create and ExistenceCheck == false</param>
        /// <param name="doContainComparison">
        /// If true, checks if the actual script contains the expected script. Otherwise, they should be exactly equal.
        /// </param>
        internal static void ValidateUrnScripting(Database database, Management.Sdk.Sfc.Urn[] urnList, string[] expectedResults,
            ScriptingPreferences preferences = null, bool doContainComparison = false)
        {
            preferences = preferences ?? new ScriptingPreferences(database)
            {
                Behavior = ScriptBehavior.Create,
                ContinueOnScriptingError = false,
                DependentObjects = false,
                SuppressDirtyCheck = true,
                Data = { ChangeTracking = false },
                IgnoreDependencyError = true,
                IncludeScripts =
                {
                    AnsiPadding = false,
                    Associations = false,
                    Data = false,
                    DatabaseContext = false,
                    ExistenceCheck = false,
                    ExtendedProperties = false
                },
            };

            var server = database.GetServerObject();

            var m = new ScriptMaker(server)
            {
                Preferences = preferences
            };

            var dependencyDiscoverer = new SmoDependencyDiscoverer(server)
            {
                Preferences = preferences,
            };

            m.discoverer = dependencyDiscoverer;

            //Strip out comments, the script generated has a header with scripting info that
            //we don't care about validating (and contains server-specific information) so
            //strip it out for the comparison
            var strings = new StringCollection();
            strings.AddCollection(m.Script(urnList).Cast<string>().Select(s => ScriptTokenizer.RemoveMultiLineComments(s)));

            if (doContainComparison)
            {
                Func<string, string, bool> containsComparision = (actual, expected) => actual.Contains(expected.FixNewLines());
                NUnit.Framework.Assert.That(strings,
                    NUnit.Framework.Is.EquivalentTo(expectedResults).Using(containsComparision),
                    "Generated scripts are different than expected");
            }
            else
            {
                NUnit.Framework.Assert.That(strings, NUnit.Framework.Is.EquivalentTo(expectedResults.Select(s => s.FixNewLines())),
                    "Generated scripts are different than expected");
            }
        }

        /// <summary>
        /// Validates the SqlSmoObject list produces the expected strings when the 'Scripter' class is used to script
        /// the objects.
        /// </summary>
        /// <param name="database">Database to operate in.</param>
        /// <param name="objects">The list of objects to script.</param>
        /// <param name="expectedResults">The expected strings.</param>
        /// <param name="doContainComparison">
        /// If true, checks if the actual script contains the expected script. Otherwise, they should be exactly equal.
        /// </param>
        internal static void ValidateObjectScripting(Database database, SqlSmoObject[] objects, string[] expectedResults,
            bool doContainComparison = false)
        {
            Scripter scripter = new Scripter(database.Parent);
            scripter.Options.ScriptData = false;
            scripter.Options.ScriptDrops = false;
            scripter.Options.WithDependencies = false;
            scripter.Options.ScriptSchema = true;
            scripter.Options.Statistics = true;
            scripter.Options.OptimizerData = true;
            scripter.Options.Indexes = true;
            scripter.Options.NonClusteredIndexes = true;
            scripter.Options.ScriptBatchTerminator = false;

            //Strip out comments, the script generated has a header with scripting info that
            //we don't care about validating (and contains server-specific information) so
            //strip it out for the comparison
            StringCollection strings = new StringCollection();
            strings.AddCollection(scripter.Script(objects).Cast<string>().Select(ScriptTokenizer.RemoveMultiLineComments));

            if (doContainComparison)
            {
                Func<string, string, bool> containsComparision = (actual, expected) => actual.Contains(expected.FixNewLines());
                NUnit.Framework.Assert.That(strings,
                    NUnit.Framework.Is.EquivalentTo(expectedResults).Using(containsComparision),
                    "Generated scripts are different than expected");
            }
            else
            {
                NUnit.Framework.Assert.That(strings, NUnit.Framework.Is.EquivalentTo(expectedResults.Select(s => s.FixNewLines())),
                    "Generated scripts are different than expected");
            }
        }
    }
}