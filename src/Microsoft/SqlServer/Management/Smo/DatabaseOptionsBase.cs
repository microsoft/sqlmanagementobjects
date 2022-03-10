// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

// History
//
// Fix for bug 406507: removed property MirroringRedoQueueMaxSize, which maps to a syntax that is no longer supported

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Option")]
    public partial class DatabaseOptions : SqlSmoObject, Cmn.IAlterable
    {
        OptionTerminationStatement m_OptionTerminationStatement;

        internal void SetOptionTerminationStatement(OptionTerminationStatement optionTerminationStatement)
        {
            m_OptionTerminationStatement = optionTerminationStatement;
        }

        internal DatabaseOptions(Database parentdb, ObjectKeyBase key, SqlSmoState state)
            : base(key, state)
        {
            // even though we called with the parent collection of the column, we will 
            // place the DefaultConstraint under the right collection
            singletonParent = parentdb as Database;
            
            // WATCH OUT! we are setting the m_server value here, because DefaultConstraint does
            // not live in a collection, but directly under the Column
            SetServerObject(parentdb.GetServerObject());
        }

        internal DatabaseOptions() : base () {}
            
        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Database Parent
        {
            get
            {
                return singletonParent as Database;
            }
        }

        public new SqlPropertyCollection Properties
        {
            get
            {
                return this.Parent.Properties;
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            this.Parent.Refresh();
        }

        internal protected override string GetDBName()
        {
            return this.Parent.InternalName;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Option";
            }
        }

        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            this.Parent.ScriptAlterInternal(query, sp);
        }

        /// <summary>
        /// Enables or disables snapshot isolation for the current database
        /// </summary>
        /// <param name="enabled"></param>
        public void SetSnapshotIsolation(bool enabled)
        {
            try
            {
                CheckObjectState();
                if (State == SqlSmoState.Creating)
                {
                    throw new InvalidSmoOperationException("SetSnapshotIsolation", State);
                }

                if (ServerVersion.Major < 9)
                {
                    throw new SmoException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
                }

                this.ExecutionManager.ExecuteNonQuery(
                    string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] SET ALLOW_SNAPSHOT_ISOLATION {1}",
                    SqlBraket(this.Parent.Name), enabled ? "ON" : "OFF"));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetSnapshotIsolation, (Database)singletonParent, e);
            }
        }

        public void Alter()
        {
            this.Parent.Alter();
        }

        public void Alter(TerminationClause terminationClause)
        {
            BuildOptionTerminationStatement(terminationClause);
            Alter();
        }

        // emits ROLLBACK AFTER integer [SECONDS]
        public void Alter(TimeSpan transactionTerminationTime)
        {
            BuildOptionTerminationStatement(transactionTerminationTime);
            Alter();
        }

        internal void BuildOptionTerminationStatement(TerminationClause terminationClause)
        {
            m_OptionTerminationStatement = new OptionTerminationStatement(terminationClause);
        }

        internal void BuildOptionTerminationStatement(TimeSpan transactionTerminationTime)
        {
            if (transactionTerminationTime.Seconds < 0)
            {
                throw new FailedOperationException(ExceptionTemplates.Alter, this, null,
                                                    ExceptionTemplates.TimeoutMustBePositive);
            }

            m_OptionTerminationStatement = new OptionTerminationStatement(transactionTerminationTime);
        }

        // this property is a little special, because its accessibility depends on the 
        // SP version, not only on the server version
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean DatabaseOwnershipChaining
        {
            get
            {
                ThrowIfBelowVersion80SP3();
                return (System.Boolean)(Parent.Properties["DatabaseOwnershipChaining"].Value);
            }

            set
            {
                ThrowIfBelowVersion80SP3();
                Parent.Properties.Get("DatabaseOwnershipChaining").Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AnsiNullDefault
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AnsiNullDefault");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AnsiNullDefault", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AnsiNullsEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AnsiNullsEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AnsiNullsEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AnsiPaddingEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AnsiPaddingEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AnsiPaddingEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AnsiWarningsEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AnsiWarningsEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AnsiWarningsEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean ArithmeticAbortEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("ArithmeticAbortEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("ArithmeticAbortEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AutoClose
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AutoClose");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AutoClose", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AutoCreateStatistics
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AutoCreateStatisticsEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AutoCreateStatisticsEnabled", value);
            }
        }
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public System.Boolean AutoCreateStatisticsIncremental
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AutoCreateIncrementalStatisticsEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AutoCreateIncrementalStatisticsEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AutoShrink
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AutoShrink");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AutoShrink", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AutoUpdateStatistics
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AutoUpdateStatisticsEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AutoUpdateStatisticsEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean AutoUpdateStatisticsAsync
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("AutoUpdateStatisticsAsync");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("AutoUpdateStatisticsAsync", value);
            }
        }
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean BrokerEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("BrokerEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("BrokerEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean CloseCursorsOnCommitEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("CloseCursorsOnCommitEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("CloseCursorsOnCommitEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean ConcatenateNullYieldsNull
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("ConcatenateNullYieldsNull");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("ConcatenateNullYieldsNull", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean DateCorrelationOptimization
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("DateCorrelationOptimization");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("DateCorrelationOptimization", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean IsParameterizationForced
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("IsParameterizationForced");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("IsParameterizationForced", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean LocalCursorsDefault
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("LocalCursorsDefault");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("LocalCursorsDefault", value);
            }
        }
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Int32 MirroringRedoQueueMaxSize
        {
            get
            {
                return (System.Int32)this.Parent.Properties.GetValueWithNullReplacement("MirroringRedoQueueMaxSize");
            }
        }
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Int32 MirroringTimeout
        {
            get
            {
                return (System.Int32)this.Parent.Properties.GetValueWithNullReplacement("MirroringTimeout");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("MirroringTimeout", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean NumericRoundAbortEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("NumericRoundAbortEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("NumericRoundAbortEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Microsoft.SqlServer.Management.Smo.PageVerify PageVerify
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.PageVerify)this.Parent.Properties.GetValueWithNullReplacement("PageVerify");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("PageVerify", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean QuotedIdentifiersEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("QuotedIdentifiersEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("QuotedIdentifiersEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean ReadOnly
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("ReadOnly");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("ReadOnly", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Microsoft.SqlServer.Management.Smo.RecoveryModel RecoveryModel
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.RecoveryModel)this.Parent.Properties.GetValueWithNullReplacement("RecoveryModel");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("RecoveryModel", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean RecursiveTriggersEnabled
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("RecursiveTriggersEnabled");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("RecursiveTriggersEnabled", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Microsoft.SqlServer.Management.Smo.SnapshotIsolationState SnapshotIsolationState
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.SnapshotIsolationState)this.Parent.Properties.GetValueWithNullReplacement("SnapshotIsolationState");
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Boolean Trustworthy
        {
            get
            {
                return (System.Boolean)this.Parent.Properties.GetValueWithNullReplacement("Trustworthy");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("Trustworthy", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Microsoft.SqlServer.Management.Smo.DatabaseUserAccess UserAccess
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.DatabaseUserAccess)this.Parent.Properties.GetValueWithNullReplacement("UserAccess");
            }
            set
            {
                Parent.Properties.SetValueWithConsistencyCheck("UserAccess", value);
            }
        }

        internal class OptionTerminationStatement
        {
            TimeSpan m_time;
            TerminationClause m_clause;

            internal OptionTerminationStatement(TimeSpan time)
            {
                m_time = time;
            }

            internal OptionTerminationStatement(TerminationClause clause)
            {
                m_time = TimeSpan.Zero;
                m_clause = clause;
            }

            internal string GetTerminationScript()
            {
                if (TimeSpan.Zero != m_time)
                {
                    return string.Format(SmoApplication.DefaultCulture, "WITH ROLLBACK AFTER {0} SECONDS", m_time.Seconds);
                }
                if (m_clause == TerminationClause.FailOnOpenTransactions)
                {
                    return "WITH NO_WAIT";
                }
                return "WITH ROLLBACK IMMEDIATE"; //TerminationClause.RollbackTransactionsImmediately
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

    }

    public enum TerminationClause
    {
        FailOnOpenTransactions, // NO_WAIT - make sure specified in doc
        RollbackTransactionsImmediately // ROLLBACK IMMEDIATE
    }
}
