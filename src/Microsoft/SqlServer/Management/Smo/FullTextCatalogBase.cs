// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class FullTextCatalog : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
    {
        public FullTextCatalog() : base() { }

        public FullTextCatalog(Database database, string name)
            : base()
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            if (database != null)
            {
                if (database.IsExpressSku() && !database.Parent.IsFullTextInstalled)
                {
                    throw new UnsupportedFeatureException(ExceptionTemplates.UnsupportedFeatureFullText);
                }
            }

            this.Parent = database;
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Database Parent
        {
            get
            {
                CheckObjectState();
                return base.ParentColl.ParentInstance as Database;
            }

            set
            {
                SetParentImpl(value);
            }
        }

        internal FullTextCatalog(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "FullTextCatalog";
            }
        }

        /// <summary>
        /// Name of FullTextCatalog
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
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

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptCreateCatalog(queries, sp);
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            if (IsObjectDirty())
            {
                ScriptAlterCatalog(alterQuery, sp);
            }
        }

        private void ScriptCreateCatalog(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // full quoted scripting name
            string fullName = FormatFullNameForScripting(sp);

            // unquoted name of the object
            string name = GetName(sp);

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_CATALOG, "NOT", SqlString(name));
                sb.Append(sp.NewLine);
            }

            Property property;

            // Target version >= 9
            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                // CREATE FULLTEXT CATALOG <catalog_name>
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE FULLTEXT CATALOG {0} ", fullName);

                // ON FILEGROUP <filegroup>
                if (sp.TargetServerVersion == SqlServerVersion.Version90)
                {
                    if (ServerVersion.Major >= 9 &&
                    sp.Storage.FileGroup)
                    {
                        if ((null != (property = this.GetPropertyOptional("FileGroup")).Value) && (property.Value.ToString().Length > 0))
                        {
                            if (0 != StringComparer.Compare(property.Value.ToString(), "PRIMARY"))
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, "ON FILEGROUP [{0}]", SqlBraket(property.Value.ToString()));
                            }
                        }
                    }
                    sb.Append(sp.NewLine);
                    // IN PATH 'rootpath'
                    if (sp.OldOptions.IncludeFullTextCatalogRootPath &&
                    (null != (property = this.Properties.Get("RootPath")).Value) && (property.Value.ToString().Length > 0))
                    {
                        string path = property.Value.ToString();

                        // Strip trailing '\<catlog_name>' from path before using
                        if (path.EndsWith("\\" + this.Name, StringComparison.Ordinal) || path.EndsWith("/" + this.Name, StringComparison.Ordinal))
                        {
                            path = path.Substring(0, path.Length - this.Name.Length - 1);
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture, "IN PATH N'{0}'", SqlString(path));
                        sb.Append(sp.NewLine);
                    }
                }

                if (ServerVersion.Major >= 9)
                {
                    // WITH ACCENT_SENSITIVITY {ON | OFF}
                    if (null != (property = this.Properties.Get("IsAccentSensitive")).Value)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "WITH ACCENT_SENSITIVITY = {0}", ((bool)property.Value) ? "ON" : "OFF");
                        sb.Append(sp.NewLine);
                    }

                    // AS DEFAULT
                    if ((null != (property = this.Properties.Get("IsDefault")).Value) && (true == (bool)property.Value))
                    {
                        sb.Append("AS DEFAULT");
                        sb.Append(sp.NewLine);
                    }

                    // AUTHORIZATION <owner_name>
                    if (sp.IncludeScripts.Owner && (null != (property = this.Properties.Get("Owner")).Value) && (property.Value.ToString().Length > 0))
                    {
                        sb.AppendFormat("AUTHORIZATION [{0}]", SqlBraket(property.Value.ToString()));
                        sb.Append(sp.NewLine);
                    }
                }
            }

            // Target version < 9
            else
            {
                Database db = (Database)ParentColl.ParentInstance;
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_catalog @ftcat=N'{0}', @action=N'create'", SqlString(name));

                // @path='<root_directory>'
                if (sp.OldOptions.IncludeFullTextCatalogRootPath &&
                    (null != (property = this.Properties.Get("RootPath")).Value) && (property.Value.ToString().Length > 0))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", @path=N'{0}'",
                                    SqlString(property.Value.ToString()));
                }
                sb.Append(sp.NewLine);
            }

            queries.Add(sb.ToString());
        }

        private void ScriptAlterCatalog(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            Property property;
            bool altered = false;

            // Target version >= 9
            if (sp.TargetServerVersion >= SqlServerVersion.Version90 && ServerVersion.Major >= 9)
            {
                // For now, only one property, Owner, is supported by this method.
                // If adding more properties, put them in front of Owner and
                // add a new line.
                // ALTER AUTHORIZATION should be a separate statement


                // ALTER AUTHORIZATION ON FULLTEXT CATALOG::<entity_name> TO <owner>
                if ((null != (property = this.Properties.Get("Owner")).Value) && property.Dirty)
                {
                    sb.AppendFormat("ALTER AUTHORIZATION ON FULLTEXT CATALOG::{0} TO {1}",
                        FullQualifiedName, MakeSqlBraket(property.Value.ToString()));
                    altered = true;
                }

                sb.Append(sp.NewLine);
            }

            if (altered)
            {
                queries.Add(sb.ToString());
            }
        }

        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        /// <summary>
        /// Scripts permissions for this object. returns without scripting anything if source version is less
        /// than 9 since permissions on FullTextCatalogs were not supported in 8.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // return if source version is less than 9 because permissions on ApplicationRoles were
            // not supported prior to that
            if (Parent.Parent.Information.Version.Major < 9)
            {
                return;
            }

            base.AddScriptPermission(query, sp);
        }


        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // full scripting name
            string fullName = FormatFullNameForScripting(sp);

            // unquoted name of the object
            string name = GetName(sp);

            if (sp.IncludeScripts.Header)
            {
                dropQuery.Add(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
            }

            // Target version < 9
            if (sp.TargetServerVersion < SqlServerVersion.Version90) // pre-Yukon server
            {
                sb.Length = 0; // Reset the content

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_CATALOG, "", SqlString(name));
                    sb.Append(sp.NewLine);
                }

                // Stop population of any indexes in this catalog.
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_catalog @ftcat=N'{0}', @action=N'stop'", SqlString(name));

                dropQuery.Add(sb.ToString());
            }

            sb.Length = 0; // Reset the content

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FT_CATALOG, "", SqlString(name));
                sb.Append(sp.NewLine);
            }

            // Target version >= 9
            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "DROP FULLTEXT CATALOG {0}", fullName);
            }

            // Target version < 9
            else
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_catalog @ftcat=N'{0}', @action=N'drop'", SqlString(name));
            }

            dropQuery.Add(sb.ToString());
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting options
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        public void Rebuild()
        {
            StringCollection statements = new StringCollection();

            Database db = (Database)ParentColl.ParentInstance;
            statements.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(db.Name)));

            if (ServerVersion.Major >= 9)
            {
                statements.Add(string.Format(SmoApplication.DefaultCulture,
                    "ALTER FULLTEXT CATALOG [{0}] REBUILD", SqlBraket(this.Name)));
            }
            else
            {
                statements.Add(string.Format(SmoApplication.DefaultCulture,
                            "EXEC dbo.sp_fulltext_catalog @ftcat=N'{0}', @action=N'rebuild'",
                            SqlString(this.Name)));
            }
            this.ExecutionManager.ExecuteNonQuery(statements);
        }

        public void Rebuild(bool accentSensitive)
        {
            if (ServerVersion.Major >= 9)
            {
                StringCollection statements = new StringCollection();
                Database db = (Database)ParentColl.ParentInstance;
                statements.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(db.Name)));
                statements.Add(string.Format(SmoApplication.DefaultCulture,
                    "ALTER FULLTEXT CATALOG [{0}] REBUILD WITH ACCENT_SENSITIVITY = {1}",
                    SqlBraket(this.Name), accentSensitive ? "ON" : "OFF"));
                this.ExecutionManager.ExecuteNonQuery(statements);
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
            }
        }

        public void Reorganize()
        {
            if (ServerVersion.Major >= 9)
            {
                StringCollection statements = new StringCollection();
                Database db = (Database)ParentColl.ParentInstance;
                statements.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(db.Name)));
                statements.Add(string.Format(SmoApplication.DefaultCulture,
                    "ALTER FULLTEXT CATALOG [{0}] REORGANIZE", SqlBraket(this.Name)));
                this.ExecutionManager.ExecuteNonQuery(statements);
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
            }
        }

        private enum CatalogPopulationActionEx
        {
            Full = CatalogPopulationAction.Full,
            Incremental = CatalogPopulationAction.Incremental,
            Stop
        }

        private void StartOrStopPopulation(CatalogPopulationActionEx action)
        {
            Database db = (Database)ParentColl.ParentInstance;

            // sp_fulltext_catalog has been deprecated in Yukon, so we need to iterate over FT indexes
            // in the catalog and generate ALTER FULLTEXT INDEX
            if (ServerVersion.Major >= 9) // Yukon or later server
            {
                DataTable dt = EnumTables();
                foreach (DataRow dr in dt.Rows)
                {
                    Table table = db.Tables[(string)dr["Table_Name"], (string)dr["Table_Schema"]];

                    Debug.Assert(table != null);
                    Debug.Assert(table.FullTextIndex != null);

                    if (action == CatalogPopulationActionEx.Stop)
                    {
                        table.FullTextIndex.StopPopulation();
                    }
                    else
                    {
                        IndexPopulationAction indexAction = (action == CatalogPopulationActionEx.Incremental ? IndexPopulationAction.Incremental : IndexPopulationAction.Full);
                        table.FullTextIndex.StartPopulation(indexAction);
                    }
                }
            }
            else
            {
                StringCollection statements = new StringCollection();
                string action_stmt;
                if (action == CatalogPopulationActionEx.Stop)
                {
                    action_stmt = "stop";
                }
                else
                {
                    action_stmt = (action == CatalogPopulationActionEx.Incremental ? "start_incremental" : "start_full");
                }
                statements.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(db.Name)));
                statements.Add(string.Format(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_fulltext_catalog @ftcat=N'{0}', @action=N'{1}'",
                    SqlString(this.Name),
                    action_stmt));
                db.ExecutionManager.ExecuteNonQuery(statements);
            }
        }

        public void StartPopulation(CatalogPopulationAction action)
        {
            StartOrStopPopulation((CatalogPopulationActionEx)action);
        }

        public void StopPopulation()
        {
            StartOrStopPopulation(CatalogPopulationActionEx.Stop);
        }

        private DataTable EnumTables()
        {
            StringCollection statements = new StringCollection();
            Database db = (Database)ParentColl.ParentInstance;

            Request tablesRequest = new Request(new Urn(string.Format(SmoApplication.DefaultCulture, db.Urn + "/Table/FullTextIndex[@CatalogName='{0}']",
                                                Urn.EscapeString(this.Name))));
            tablesRequest.Fields = new String[] { "Name" };
            tablesRequest.ParentPropertiesRequests = new PropertiesRequest[1];
            PropertiesRequest parentProps = new PropertiesRequest();
            parentProps.Fields = new String[] { "Schema", "Name" };
            parentProps.OrderByList = new OrderBy[] { 	new OrderBy("Schema", OrderBy.Direction.Asc),
                                    new OrderBy("Name", OrderBy.Direction.Asc) };
            tablesRequest.ParentPropertiesRequests[0] = parentProps;

            return this.ExecutionManager.GetEnumeratorData(tablesRequest);
        }

        internal void Validate_set_IsDefault(Property prop, object newValue)
        {
            if (this.State != SqlSmoState.Creating)
            {
                throw new PropertyReadOnlyException(prop.Name);
            }

            if ((bool)newValue)
            {
                SmoInternalStorage cats = ((FullTextCatalogCollection)ParentColl).InternalStorage;
                foreach (FullTextCatalog cat in cats)
                {
                    Property pDef = cat.Properties.Get("IsDefault");
                    if (this != cat &&
                        (bool)pDef.Value &&
                        cat.State == SqlSmoState.Creating)
                    {
                        pDef.SetValue(false);
                    }
                }
            }
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (this.ServerVersion.Major >= 9 && prop.Name == "IsDefault")
            {
                Validate_set_IsDefault(prop, value);
            }
        }

        protected override void PostCreate()
        {
            if (this.ServerVersion.Major >= 9)
            {
                Property pDef = this.Properties.Get("IsDefault");

                if (null != pDef.Value && (bool)pDef.Value)
                {
                    Property parentDef = this.Parent.Properties.Get("DefaultFullTextCatalog");
                    parentDef.SetValue(this.Name);
                    parentDef.SetRetrieved(true);
                }
            }
        }


        /// <summary>
        /// Returns a list of error logs.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumErrorLogs()
        {
            this.ThrowIfBelowVersion90();
            return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/ErrorLog"));
        }

        /// <summary>
        /// Reads the current error log.
        /// </summary>
        /// <returns></returns>
        public DataTable ReadErrorLog()
        {
            return this.ReadErrorLog(0);
        }

        /// <summary>
        /// Reads the log specified by logNumber.
        /// </summary>
        /// <param name="logNumber"></param>
        /// <returns></returns>
        public DataTable ReadErrorLog(System.Int32 logNumber)
        {
            this.ThrowIfBelowVersion90();
            if (logNumber < 0) // 0 or negative means the current error log
            {
                logNumber = 0;
            }
            return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/ErrorLog[@ArchiveNo=" + logNumber + "]/LogEntry"));
        }
    }
}

