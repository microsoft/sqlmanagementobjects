// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Statistic : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IMarkForDrop, IScriptable
    {
        private StatisticsScanType m_ScanType = StatisticsScanType.Default;
        private int m_sampleValue;
        private bool m_bIsOnComputed;

        internal Statistic(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            m_StatisticColumn = null;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Statistic";
            }
        }

        public void SetScanOptions(StatisticsScanType type, int no)
        {
            m_ScanType = type;
            m_sampleValue = no;
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        private StatisticColumnCollection m_StatisticColumn;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(StatisticColumn))]
        public StatisticColumnCollection StatisticColumns
        {
            get
            {
                CheckObjectState();
                if (null == m_StatisticColumn)
                {
                    m_StatisticColumn = new StatisticColumnCollection(this);
                }
                return m_StatisticColumn;
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();
            if (null != m_StatisticColumn)
            {
                m_StatisticColumn.MarkAllDropped();
            }
        }

        public void Create()
        {
            base.CreateImpl();

            //a new index will be automaticaly created
            ((TableViewBase)ParentColl.ParentInstance).Indexes.MarkOutOfSync();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            CheckObjectState();

            if ((!sp.ForDirectExecution && this.IgnoreForScripting))
            {
                return;
            }

            bool updateStream = sp.Data.OptimizerData &&
                                sp.TargetServerVersion >= SqlServerVersion.Version90;
            // skip statistics that are created automatically for index
            // if we don't require optimizer data
            if (GetPropValueOptional("IsFromIndexCreation", false) &&
                !updateStream)
            {
                return;
            }

            // skip statistics that are created automatically by the query processor
            // if we don't require optimizer data
            if (GetPropValueOptional("IsAutoCreated", false) &&
                !updateStream)
            {
                return;
            }

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            GetDDL(sb, sp, true);
            if (sb.Length > 0)
            {
                AddSetOptionsForStats(queries);
            }
            queries.Add(sb.ToString());
        }

        private void GetDDL(StringBuilder sb, ScriptingPreferences sp, bool creating)
        {
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            // if true this property tells that the statistic has been created 
            // as a result of an index creation
            bool fromIndexCreation = GetPropValueOptional("IsFromIndexCreation", false);

            // if true it means that the statistic is auto created by the query
            // optimizer as a result of processing a query
            bool autoCreated = GetPropValueOptional("IsAutoCreated", false);

            String ifExists = (sp.Data.OptimizerData && fromIndexCreation) ? String.Empty : "not";

            m_bIsOnComputed = false;

            if (sp.IncludeScripts.ExistenceCheck)
            {
                TableViewBase statParent = (TableViewBase)Parent;
                if (sp.TargetServerVersion >= SqlServerVersion.Version90)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_STATISTIC90, ifExists,
                        FormatFullNameForScripting(sp, false),
                        SqlString(statParent.FormatFullNameForScripting(sp)));
                    sb.Append(sp.NewLine);
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_STATISTIC80, ifExists,
                        SqlString(this.Name),
                        SqlString(statParent.Name));
                    sb.Append(sp.NewLine);
                }
            }

            // UPDATE STATISTICS statements do not support filter clauses. This boolean tracks
            // what kind of statement was created, to ensure we don't script the filter clause
            // for UPDATE STATISTICS statements.
            //
            bool isUpdateStatistics = false;

            if (sp.Data.OptimizerData && fromIndexCreation)
            {
                // UPDATE STATISTICS [table_schema].[table_name]([name]) 
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "UPDATE STATISTICS {0}({1})",
                    GetTableName(sp),
                    FormatFullNameForScripting(sp));

                isUpdateStatistics = true;
            }
            else
            {
                string oldScriptName = string.Empty;
                bool isTemporary = false;

                if (IsSupportedProperty("IsTemporary"))
                {
                    isTemporary = GetPropValueOptional("IsTemporary", false);
                    if (isTemporary)
                    {
                        oldScriptName = ScriptName;
                        //Changing the name of temorary auto statistics by appending "_scripted" at end while scripting. 
                        //Since the name is reserved by engine and user won't have been able to execute with that name.
                        ScriptName = this.Name + "_scripted";
                    }
                }

                try
                {
                    // CREATE STATISTICS [name] ON [schema].[table_name]([col1], ... )
                    sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE STATISTICS {0}",
                                FormatFullNameForScripting(sp));
                }
                finally
                {
                    if (isTemporary)
                    {
                        ScriptName = oldScriptName;
                    }
                }

                sb.Append(" ON ");
                sb.Append(GetTableName(sp));
                sb.Append(Globals.LParen);
                if (!GetColumnList(sb, sp))
                {
                    return;
                }

                sb.Append(Globals.RParen);
            }

            //We are not checking for SQL injection
            //Adding the WHERE clause for FILTER, except in the case of UPDATE STATISTICS (which does not support filters).
            if (sp.TargetServerVersion >= SqlServerVersion.Version100)
            {
                string FilterDefinition = Properties.Get("FilterDefinition").Value as string;
                if (null != FilterDefinition && 0 < FilterDefinition.Length && !isUpdateStatistics)
                {
                    sb.Append(sp.NewLine);
                    sb.AppendFormat(SmoApplication.DefaultCulture, "WHERE {0}", ParseFilterDefinition(FilterDefinition));
                    sb.Append(sp.NewLine);
                }
            }


            if (sp.Data.OptimizerData &&
                sp.TargetServerVersion >= SqlServerVersion.Version90 &&
                this.ServerInfo.DatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse)
            {
                Property statStream = Properties.Get("Stream");
                object stData = null;
                object rowCount = null;
                object pageCount = null;

                // we want to know if the stream has already been retrieved
                // for instance in an earlier access to this property
                if (null != statStream.Value)
                {
                    stData = GetPropValueOptional("Stream");
                }
                else
                {
                    // if we did not have the stream before we don't want to keep it at this 
                    // point because it consumes memory. The likelihood of it being used in 
                    // a subsequent scripting operation is small.

                    // for perf reasons we don't go through the enumerator
                    // because if we don't have the property at this point 
                    // we will fault, and the enumerator solves this request with two 
                    // queries, first one being redundant and slow so we can't 
                    // afford it because we don't prefetch stat streams
                    string streamQuery = string.Format(SmoApplication.DefaultCulture,
                        "DBCC SHOW_STATISTICS(N'{0}.{1}', N'{2}') WITH STATS_STREAM",
                        // database name
                        SqlString(this.Parent.ParentColl.ParentInstance.FullQualifiedName),
                        // please note that we do not use the scripting name, we need 
                        // the stat for the existing object
                        SqlString(this.Parent.FullQualifiedName),
                        SqlString(this.Name));

                    DataSet ds = null;
                    Cmn.SqlExecutionModes em = this.ExecutionManager.ConnectionContext.SqlExecutionModes;
                    this.ExecutionManager.ConnectionContext.SqlExecutionModes = Cmn.SqlExecutionModes.ExecuteSql;
                    try
                    {
                        ds = this.ExecutionManager.ExecuteWithResults(streamQuery);
                    }
                    finally
                    {
                        this.ExecutionManager.ConnectionContext.SqlExecutionModes = em;
                    }

                    if (ds.Tables.Count > 0)
                    {
                        DataTable streamTbl = ds.Tables[0];
                        if (streamTbl.Rows.Count > 0)
                        {
                            stData = streamTbl.Rows[0]["Stats_Stream"];
                            rowCount = streamTbl.Rows[0]["Rows"];
                            pageCount = streamTbl.Rows[0]["Data Pages"];
                        }
                    }
                }

                var clauses = new StringCollection();

                // go through only if we have the stream
                if (null != stData)
                {
                    StringBuilder ss = new StringBuilder();
                    ss.Append("STATS_STREAM = 0x");

                    foreach (byte b in (byte[])stData)
                    {
                        ss.Append(b.ToString("X2", SmoApplication.DefaultCulture));
                    }
                    clauses.Add(ss.ToString());
                    // PAGECOUNT and ROWCOUNT are not supported on memory optimized tables
                    // and NORECOMPUTE is a required option for memory optimized tables.
                    // For more about statistics on memory optimized tables, refer to
                    // https://msdn.microsoft.com/en-us/library/dn232522.aspx
                    //
                    bool isMemoryOptimized = false;
                    if (this.Parent.IsSupportedProperty("IsMemoryOptimized"))
                    {
                        if (this.Parent.GetPropValueOptional("IsMemoryOptimized", false))
                        {
                            isMemoryOptimized = true;
                        }
                    }

                    if (isMemoryOptimized)
                    {
                        clauses.Add("NORECOMPUTE");
                    }
                    else
                    {
                        // add row and page count
                        if (null != rowCount && !(rowCount is DBNull))
                        {
                            clauses.Add("ROWCOUNT = " + Convert.ToInt64(rowCount, SmoApplication.DefaultCulture).ToString(SmoApplication.DefaultCulture));
                        }

                        if (null != pageCount && !(pageCount is DBNull))
                        {
                            clauses.Add("PAGECOUNT = " + Convert.ToInt64(pageCount, SmoApplication.DefaultCulture).ToString(SmoApplication.DefaultCulture));
                        }
                    }
                }
                if (IsSupportedProperty(nameof(IsAutoDropped), sp))
                {
                    clauses.Add("AUTO_DROP = " + (GetPropValueOptional(nameof(IsAutoDropped), false) ? "ON" : "OFF"));
                }

                if(clauses.Count > 0)
                {
                    var arr = new string[clauses.Count];
                    clauses.CopyTo(arr, 0);
                    sb.Append(" WITH " + String.Join(Globals.comma, arr));
                }
            }
            else
            {
                GetDDLBody(sb, sp, true);
            }
        }

        private static string replaceOR(Match m)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(SmoApplication.DefaultCulture, "([{0}] IN (", m.Groups["column"].Captures[0].Value);

            //Retreiving values 
            for (int i = 0; i < m.Groups["value"].Captures.Count; i++)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0},", m.Groups["value"].Captures[i].Value);
            }

            sb.Replace(',', ')', sb.Length - 1, 1);
            sb.AppendFormat(SmoApplication.DefaultCulture, ")");
            return sb.ToString();
        }

        private static string ParseFilterDefinition(string p)
        {
            //This function parses filter definition stored by engine which is not in accepted t-sql format and replace it with accepted t-sql
            /* For eg:- (([col1]=N'b' OR [col1]=N'a' OR [col1]='l') AND ([col1]='1' AND [col2]<>'8'))
             * should return (([col1] IN (N'b',N'a','l')) AND ([col1]='1' AND [col2]<>'8'))
             * 
             * Or (([col1]=(60) OR [col1]=(50) OR [col1]=(40)) AND ([col1]=(1) AND [col1]>(8)))
             * should return (([col1] IN (60,50,40)) AND ([col1]=(1) AND [col1]>(8)))
             * 
             */
            Regex regex = new Regex(@"\((\[(?<column>(([^\[])|((?<=')\[))*?)\](=)(\()?(?<value>([^\[])*?)(\))?)(\sOR\s(\[(?<column>(([^\[])|((?<=')\[))*?)\](=)(\()?(?<value>([^\[])*?)(\))?))+\)");
            return regex.Replace(p, new MatchEvaluator(replaceOR));
        }

        /// <summary>
        /// Appends to the script the columns that this statistic is created on.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="so"></param>
        /// <returns></returns>
        private bool GetColumnList(StringBuilder sb, ScriptingPreferences sp)
        {
            bool fFirstColumn = true;

            if (StatisticColumns.Count <= 0)
            {
                throw new SmoException(ExceptionTemplates.ColumnsMustBeSpecified);
            }

            // add the column list 
            foreach (StatisticColumn col in StatisticColumns)
            {
                // we check to see if the specified column exists,
                // first we need to know which type is the parent, table or view
                // for views, we leave it unchecked for the moment, we'll have to do it later, defect fix 500317 pending
                Column colBase = null;
                colBase = ((TableViewBase)ParentColl.ParentInstance).Columns[col.Name];
                if (null == colBase)
                {
                    // the column does not exist, so we need to abort this scripting
                    throw new SmoException(ExceptionTemplates.ObjectRefsNonexCol(UrnSuffix, Name, "[" + SqlStringBraket(ParentColl.ParentInstance.InternalName) + "].[" + SqlStringBraket(col.Name) + "]"));
                }

                // if this column is going to be ignored for scripting skip the whole index
                if (colBase.IgnoreForScripting)
                {
                    // flag this object to be ignored for scripting and return from the function
                    this.IgnoreForScripting = true;
                    return false;
                }

                if (false == m_bIsOnComputed)
                {
                    Object o = colBase.GetPropValueOptional("Computed");
                    if (null != o && true == (bool)o)
                    {
                        m_bIsOnComputed = true;
                    }
                }

                if (fFirstColumn)
                {
                    fFirstColumn = false;
                }
                else
                {
                    sb.Append(Globals.comma);
                    sb.Append(" ");
                }
                // use proper name for scripting
                if (null != colBase && colBase.ScriptName.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(colBase.ScriptName));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(col.Name));
                }
            }

            return true;
        }

        internal bool AddSetOptionsForStats(StringCollection queries)
        {
            //if the statistic is on a computed column 
            //the following set options must have the right value
            if (m_bIsOnComputed || this.Parent is View)
            {
                queries.Add("SET ARITHABORT ON");
                queries.Add("SET CONCAT_NULL_YIELDS_NULL ON");
                queries.Add("SET QUOTED_IDENTIFIER ON");
                queries.Add("SET ANSI_NULLS ON");
                queries.Add("SET ANSI_PADDING ON");
                queries.Add("SET ANSI_WARNINGS ON");
                queries.Add("SET NUMERIC_ROUNDABORT OFF");

                return true;
            }
            return false;
        }


        public void Drop()
        {
            base.DropImpl();

            //a index will be automaticaly dropped
            ((TableViewBase)ParentColl.ParentInstance).Indexes.RemoveObject(new SimpleObjectKey(this.Name));
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            CheckObjectState();

            if (GetPropValueOptional("IsFromIndexCreation", false))
            {
                return;
            }

            // skip statistics that are created automatically by the query processor
            if (GetPropValueOptional("IsAutoCreated", false))
            {
                bool isTemporary = false;
                if (IsSupportedProperty("IsTemporary"))
                {
                    isTemporary = GetPropValueOptional("IsTemporary", false);
                }

                if (!isTemporary)
                {
                    return;
                }
            }

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                TableViewBase statParent = (TableViewBase)Parent;
                if (sp.TargetServerVersion >= SqlServerVersion.Version90)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_STATISTIC90, "",
                        FormatFullNameForScripting(sp, false),
                        SqlString(statParent.FormatFullNameForScripting(sp)));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_STATISTIC80, "",
                        SqlString(this.Name),
                        SqlString(statParent.Name));
                }
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "DROP STATISTICS {0}.{1}",
                GetTableName(),
                FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        /// <summary>
        /// Updates the statistic
        /// </summary>
        public void Update()
        {
            ScriptingPreferences spnew = new ScriptingPreferences();
            this.ExecutionManager.ExecuteNonQuery(UpdateStatistics(GetDatabaseName(), GetTableName(), FormatFullNameForScripting(spnew),
                m_ScanType, StatisticsTarget.All, GetNoRecompute(), IsSupportedProperty(nameof(IsAutoDropped)), GetAutoDrop(), m_sampleValue));
        }

        /// <summary>
        /// Updates the statistic
        /// <param name="scanType">determines the type of scan (FullScan, Percent Sample, Rows Sample, Resample)</param>
        /// </summary>
        public void Update(StatisticsScanType scanType)
        {
            ScriptingPreferences spnew = new ScriptingPreferences();
            this.ExecutionManager.ExecuteNonQuery(UpdateStatistics(GetDatabaseName(), GetTableName(), FormatFullNameForScripting(spnew),
                scanType, StatisticsTarget.All, GetNoRecompute(), IsSupportedProperty(nameof(IsAutoDropped)), GetAutoDrop(), m_sampleValue));
        }

        /// <summary>
        /// Updates the statistic
        /// <param name="scanType">determines the type of scan (FullScan, Percent Sample, Rows Sample, Resample)</param>
        /// <param name="sampleValue">determines percent or rows to sample</param>
        /// </summary>
        public void Update(StatisticsScanType scanType, System.Int32 sampleValue)
        {
            ScriptingPreferences spnew = new ScriptingPreferences();
            this.ExecutionManager.ExecuteNonQuery(UpdateStatistics(GetDatabaseName(), GetTableName(), FormatFullNameForScripting(spnew),
                scanType, StatisticsTarget.All, GetNoRecompute(), IsSupportedProperty(nameof(IsAutoDropped)), GetAutoDrop(), sampleValue));
        }

        /// <summary>
        /// Updates the statistic
        /// <param name="scanType">determines the type of scan (FullScan, Percent Sample, Rows Sample, Resample)</param>
        /// <param name="sampleValue">determines percent or rows to sample</param>
        /// <param name="recompute">determines whether automatic statistics update is enabled</param>
        /// </summary>
        public void Update(StatisticsScanType scanType, System.Int32 sampleValue, System.Boolean recompute)
        {
            ScriptingPreferences spnew = new ScriptingPreferences();
            // NOTE: The input parameter is true if they WANT to recompute. The UpdateStatistics function wants to know if the opposite is true.
            this.ExecutionManager.ExecuteNonQuery(UpdateStatistics(GetDatabaseName(), GetTableName(), FormatFullNameForScripting(spnew),
                scanType, StatisticsTarget.All, !recompute, IsSupportedProperty(nameof(IsAutoDropped)), GetAutoDrop(), sampleValue));
        }

        private String GetTableName()
        {
            return GetTableName(null);
        }

        private String GetTableName(ScriptingPreferences sp)
        {
            if (null == sp)
            {
                sp = new ScriptingPreferences();
            }

            TableViewBase parent = (TableViewBase)ParentColl.ParentInstance;
            return parent.FormatFullNameForScripting(sp);
        }


        private String GetDatabaseName()
        {
            return string.Format(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(ParentColl.ParentInstance.ParentColl.ParentInstance.InternalName));
        }

        private bool GetNoRecompute()
        {
            bool bNoRecompute = false;
            if (null != Properties.Get("NoAutomaticRecomputation").Value && true == (bool)Properties.Get("NoAutomaticRecomputation").Value)
            {
                bNoRecompute = true;
            }
            return bNoRecompute;
        }

        private bool GetAutoDrop()
        {
            return GetPropValueIfSupported(nameof(IsAutoDropped), false);
        }

        private void GetDDLBody(StringBuilder sb, ScriptingPreferences sp, bool creating)
        {
            if (creating && StatisticsScanType.Resample == m_ScanType)
            {
                throw new SmoException(ExceptionTemplates.InvalidScanType(StatisticsScanType.Resample.ToString()));
            }

            UpdateStatisticsBody(sb, sp, m_ScanType, StatisticsTarget.All, GetNoRecompute(), IsSupportedProperty(nameof(IsAutoDropped), sp), GetAutoDrop(), m_sampleValue);
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Returns a string collection for either Updating or Creating a 
        /// statistic. Assumes that AUTO_DROP does not need to be updated.
        /// <param name="dbName">name of database</param>
        /// <param name="tableName">name of table</param>
        /// <param name="statisticName">name of statistic</param>
        /// <param name="scanType">determines the type of scan (FullScan, Percent Sample, Rows Sample, Resample)</param>
        /// <param name="affectType">determines which statistics to target (stats for columns, or stats for indexes)</param>
        /// <param name="bIsNorecompute">determines whether to include NORECOMPUTE in the result string</param>
        /// <param name="sampleValue">determines percent or rows to sample</param>
        /// </summary>
        internal static StringCollection UpdateStatistics(string dbName, string tableName, string statisticName
                    , StatisticsScanType scanType, StatisticsTarget affectType, bool bIsNorecompute, int sampleValue)
        {
            return UpdateStatistics(dbName, tableName, statisticName, scanType, affectType, 
                bIsNorecompute, bUpdateAutoDrop: false, bIsAutoDropped: false, sampleValue);
        }

        /// <summary>
        /// Returns a string collection for either Updating or Creating a statistic
        /// <param name="dbName">name of database</param>
        /// <param name="tableName">name of table</param>
        /// <param name="statisticName">name of statistic</param>
        /// <param name="scanType">determines the type of scan (FullScan, Percent Sample, Rows Sample, Resample)</param>
        /// <param name="affectType">determines which statistics to target (stats for columns, or stats for indexes)</param>
        /// <param name="bIsNorecompute">determines whether to include NORECOMPUTE in the result string</param>
        /// <param name="bUpdateAutoDrop">determines whether AUTO_DROP should be set</param>
        /// <param name="bIsAutoDropped">determines the value that AUTO_DROP should be set to</param>
        /// <param name="sampleValue">determines percent or rows to sample</param>
        /// </summary>
        internal static StringCollection UpdateStatistics(string dbName, string tableName, string statisticName
                , StatisticsScanType scanType, StatisticsTarget affectType, bool bIsNorecompute, bool bUpdateAutoDrop, bool bIsAutoDropped, int sampleValue)
        {
            StringCollection queries = new StringCollection();
            queries.Add(string.Format(SmoApplication.DefaultCulture, "use {0}", dbName));

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptingPreferences sonew = new ScriptingPreferences();
            sb.AppendFormat(SmoApplication.DefaultCulture, "UPDATE STATISTICS {0} {1}", tableName, statisticName);

            UpdateStatisticsBody(sb, sonew, scanType, affectType, bIsNorecompute, bUpdateAutoDrop, bIsAutoDropped, sampleValue);

            queries.Add(sb.ToString());

            return queries;
        }

        /// <summary>
        /// Builds the string for either Updating or Creating a statistic
        /// <param name="sb">string builder for results</param>
        /// <param name="sp">preferences for script formatting</param>
        /// <param name="scanType">determines the type of scan (FullScan, Percent Sample, Rows Sample, Resample)</param>
        /// <param name="affectType">determines which statistics to target (stats for columns, or stats for indexes)</param>
        /// <param name="bIsNorecompute">determines whether to include NORECOMPUTE in the result string</param>
        /// <param name="bUpdateAutoDrop">determines whether AUTO_DROP should be set</param>
        /// <param name="bIsAutoDropped">determines the value that AUTO_DROP should be set to</param>
        /// <param name="sampleValue">determines percent or rows to sample</param>
        /// </summary>
        private static void UpdateStatisticsBody(StringBuilder sb, ScriptingPreferences sp, StatisticsScanType scanType,
            StatisticsTarget affectType, bool bIsNorecompute, bool bUpdateAutoDrop, bool bIsAutoDropped, int sampleValue)
        {
            StringCollection clauses = new StringCollection();
            switch (scanType)
            {
                case StatisticsScanType.FullScan:
                    clauses.Add("FULLSCAN");
                    break;
                case StatisticsScanType.Percent:
                    clauses.Add(String.Format(SmoApplication.DefaultCulture, "SAMPLE {0} PERCENT", sampleValue));
                    break;
                case StatisticsScanType.Rows:
                    clauses.Add(String.Format(SmoApplication.DefaultCulture, "SAMPLE {0} ROWS", sampleValue));
                    break;
                case StatisticsScanType.Resample:
                    clauses.Add("RESAMPLE");
                    break;
            }

            switch (affectType)
            {
                case StatisticsTarget.Column:
                    clauses.Add("COLUMNS");
                    break;
                case StatisticsTarget.Index:
                    clauses.Add("INDEX");
                    break;
            }

            if (bIsNorecompute)
            {
                clauses.Add("NORECOMPUTE");
            }

            if (bUpdateAutoDrop)
            {
                clauses.Add("AUTO_DROP = " + (bIsAutoDropped ? "ON" : "OFF"));
            }

            if (clauses.Count > 0)
            {
                var arr = new String[clauses.Count];
                clauses.CopyTo(arr, 0);
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}WITH{1}{2}", sp.NewLine, Globals.space, String.Join(Globals.comma, arr));
            }
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {

            string[] fields = {   
                        "NoAutomaticRecomputation",
                    "FilterDefinition",
                    "IsAutoCreated",
                    nameof(IsAutoDropped),
                    "IsFromIndexCreation",
                              "IsTemporary"};
            List<string> list = GetSupportedScriptFields(typeof(Statistic.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }

        /// <summary>
        /// Returns the result of DBCC SHOW_STATISTICS
        /// </summary>
        /// <returns></returns>
        public DataSet EnumStatistics()
        {
            DataSet set = null;
            this.CheckObjectState(true);
            StringCollection query = new StringCollection();
            ScriptingPreferences sp = new ScriptingPreferences();
            sp.SetTargetDatabaseEngineType(this.ServerInfo.DatabaseEngineType);
            sp.ScriptForCreateDrop = true;
            this.AddDatabaseContext(query, sp);
            string s = ((ScriptSchemaObjectBase)base.ParentColl.ParentInstance).FormatFullNameForScripting(sp);
            query.Add(string.Format(SmoApplication.DefaultCulture, "DBCC SHOW_STATISTICS ({0}, {1})", new object[] { SqlSmoObject.MakeSqlString(s), SqlSmoObject.MakeSqlString(this.Name) }));
            set = this.ExecutionManager.ExecuteWithResults(query);
            return set;
        }


    }
}


