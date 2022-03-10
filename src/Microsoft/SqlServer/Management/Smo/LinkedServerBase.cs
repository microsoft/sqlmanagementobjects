// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class LinkedServer : NamedSmoObject, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IAlterable, IScriptable
    {

        internal LinkedServer(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "LinkedServer";
            }
        }

        /// <summary>
        /// Name of LinkedServer
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

        /// <summary>
        /// ProviderString property
        /// </summary>
        /// <returns></returns>
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        [DmfIgnoreProperty]
        public string ProviderString
        {
            get
            {
                SqlSecureString providerString = (SqlSecureString)this.Properties.GetValueWithNullReplacement("ProviderString");
                return providerString.ToString();
            }

            set
            {
                SqlSecureString providerString = new SqlSecureString(value);
                Properties.SetValueWithConsistencyCheck("ProviderString", providerString);
            }
        }

        // controls cascaded deletion of dependent linked server logins 
        private bool dropLogins = true;

        public void Create()
        {
            base.CreateImpl();
        }

        // generates the scripts for creating the login
        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            StringBuilder addsrvquery = new StringBuilder();

            ScriptIncludeHeaders(addsrvquery, sp, UrnSuffix);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                ScriptIncludeIfNotExists(addsrvquery, sp, "NOT");
                addsrvquery.Append(sp.NewLine);
                addsrvquery.Append("BEGIN").Append(sp.NewLine);
            }
            //add linkedserver
            addsrvquery.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_addlinkedserver @server = N'{0}'", SqlString(this.InternalName));
            int count = 1;
                          
            string productName = (string)GetPropValueOptionalAllowNull("ProductName");
            string providerName = (string)GetPropValueOptionalAllowNull("ProviderName");

            // We cannot specify a provider or any properties for product 'SQL Server'.
            if (productName == "SQL Server")
            {
                GetStringParam(addsrvquery, sp, "ProductName", "srvproduct", ref count);
            }
            else if(providerName != null)
            {
                GetStringParamCompulsory(addsrvquery, sp, "ProductName", "srvproduct", ref count);
                GetStringParamCompulsory(addsrvquery, sp, "ProviderName", "provider", ref count);
                GetStringParam(addsrvquery, sp, "DataSource", "datasrc", ref count);
                GetStringParam(addsrvquery, sp, "Location", "location", ref count);
                GetStringParam(addsrvquery, sp, "ProviderString", "provstr", ref count);
                GetStringParam(addsrvquery, sp, "Catalog", "catalog", ref count);               
            }

            addsrvquery.Append(sp.NewLine);
            //Scripting linked server logins
            if (this.LinkedServerLogins.Count > 0)
            {
                addsrvquery.Append(" /* For security reasons the linked server remote logins password is changed with ######## */" + sp.NewLine);

                string linkedServerName = SqlString(this.InternalName);
                foreach (LinkedServerLogin lsl in this.LinkedServerLogins)
                {
                    //Finding local login
                    string localLoginName = SqlSmoObject.SqlString(lsl.Name);
                    localLoginName = ((String.IsNullOrEmpty(localLoginName)) ? "NULL" : "N'" + localLoginName + "'");
                    //Finding impersonate value
                    string impersonate = null;
                    object propImp = lsl.GetPropValueOptional("Impersonate");
                    if (null != propImp && propImp.ToString().Length > 0)
                    {
                        impersonate = SqlSmoObject.SqlString(propImp.ToString());
                    }
                    impersonate = ((String.IsNullOrEmpty(impersonate)) ? "NULL" : "N'" + impersonate + "'");
                    //Finding remote login
                    string remoteLoginName = null;
                    object propRlogin = lsl.GetPropValueOptional("RemoteUser");
                    if (null != propRlogin && propRlogin.ToString().Length > 0)
                    {
                        remoteLoginName = SqlSmoObject.SqlString(propRlogin.ToString());
                    }
                    remoteLoginName = ((String.IsNullOrEmpty(remoteLoginName)) ? "NULL" : "N'" + remoteLoginName + "'");
                    //Finding the remote password
                    string remotePassword = null;
                    if (remoteLoginName == "NULL")
                    {
                        remotePassword = "NULL";
                    }
                    else
                    {
                        //it is the responsibility of system admin to change the password
                        remotePassword = "'########'";
                    }
                    string addLsLoginQry = string.Format(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_addlinkedsrvlogin @rmtsrvname=N'{0}',@useself={1},@locallogin={2},@rmtuser={3},@rmtpassword={4}",
                                                          linkedServerName, impersonate, localLoginName, remoteLoginName, remotePassword);
                    addsrvquery.Append(addLsLoginQry).Append(sp.NewLine);
                }
            }
            if (sp.IncludeScripts.ExistenceCheck)
            {
                addsrvquery.Append("END");
            }
            query.Add(addsrvquery.ToString());


            // on top of those add properties settable with sp_serveroption
            // the code is the same as Alter() scripting
            ScriptAlter(query, sp);
        }


        private void GetStringParam(StringBuilder buffer, ScriptingPreferences sp, string propName,
                                    string sqlPropName, ref int count)
        {
            object prop = GetPropValueOptional(propName);
            if (null != prop && prop.ToString().Length > 0)
            {
                if (count++ > 0)
                {
                    buffer.Append(Globals.commaspace);
                }

                buffer.AppendFormat(SmoApplication.DefaultCulture, "@{0}=N'{1}'", sqlPropName, SqlString(prop.ToString()));
            }
        }

        private void GetStringParamCompulsory(StringBuilder buffer, ScriptingPreferences sp, string propName,
                            string sqlPropName, ref int count)
        {
            object prop = GetPropValueOptional(propName);
            string propString = (prop != null ? prop.ToString() : string.Empty);
            if (count++ > 0)
            {
                buffer.Append(Globals.commaspace);
            }

            buffer.AppendFormat(SmoApplication.DefaultCulture, "@{0}=N'{1}'", sqlPropName, SqlString(propString));
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        // generates the scripts for the alter action
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            GetStringOption(query, sp, "CollationCompatible", "collation compatible");
            GetStringOption(query, sp, "DataAccess", "data access");
            GetStringOption(query, sp, "Distributor", "dist");
            GetStringOption(query, sp, "Publisher", "pub");
            GetStringOption(query, sp, "Rpc", "rpc");
            GetStringOption(query, sp, "RpcOut", "rpc out");
            GetStringOption(query, sp, "Subscriber", "sub");

            // only on 8.0 
            if (SqlServerVersionInternal.Version80 == sp.TargetServerVersionInternal)
            {
                GetStringOption(query, sp, "DistPublisher", "dpub");
            }

            // the following work only for 8.0 and beyond
            if ((int)SqlServerVersionInternal.Version80 <= (int)sp.TargetServerVersionInternal)
            {
                GetStringOption(query, sp, "ConnectTimeout", "connect timeout");
                GetStringOption(query, sp, "CollationName", "collation name");
                GetStringOption(query, sp, "LazySchemaValidation", "lazy schema validation");
                GetStringOption(query, sp, "QueryTimeout", "query timeout");
                GetStringOption(query, sp, "UseRemoteCollation", "use remote collation");
            }

            // the following work only for 10.0 and beyond
            if ((this.ServerVersion.Major >= 10) && ((int)SqlServerVersionInternal.Version100 <= (int)sp.TargetServerVersionInternal))
            {
                GetStringOption(query, sp, "IsPromotionofDistributedTransactionsForRPCEnabled", "remote proc transaction promotion");
            }
        }

        private void GetStringOption(StringCollection queries, ScriptingPreferences sp,
                                    string propName, string optionName)
        {
            Property prop = Properties.Get(propName);

            if (null != prop.Value && (prop.Dirty || !sp.ScriptForAlter))
            {
                // we will be using null instead of empty string
                string propValue = string.Empty;
                if (null != prop.Value)
                {
                    propValue = prop.Value.ToString();
                    if (prop.Value is bool)
                    {
                        propValue = propValue.ToLower(SmoApplication.DefaultCulture);
                    }
                }
                if (propValue.Length == 0)
                {
                    propValue = "null";
                }
                else
                {
                    propValue = MakeSqlString(propValue);
                }

                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                        "EXEC master.dbo.sp_serveroption @server=N'{0}', @optname=N'{1}', @optvalue={2}",
                                        SqlString(this.InternalName), optionName, propValue));
            }
        }

        public void Drop()
        {
            this.dropLogins = false;
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            this.dropLogins = false;
            base.DropImpl(true);
        }

        public void Drop(bool dropDependentLogins)
        {
            this.dropLogins = dropDependentLogins;
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        /// <param name="dropDependentLogins">Drop dependent linked server logins.</param>
        public void DropIfExists(bool dropDependentLogins)
        {
            this.dropLogins = dropDependentLogins;
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            StringBuilder query = new StringBuilder();

            ScriptIncludeHeaders(query, sp, UrnSuffix);

            ScriptIncludeIfNotExists(query, sp, string.Empty);

            query.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_dropserver @server=N'{0}'", SqlString(this.InternalName));
            if (dropLogins)
            {
                query.Append(", @droplogins='droplogins'");
            }

            dropQuery.Add(query.ToString());
        }

        protected override void PostDrop()
        {
            // if the client has dropped the logins, then clean the collection
            if (!this.ExecutionManager.Recording && this.dropLogins)
            {
                if (null != m_LinkedServerLogins)
                {
                    m_LinkedServerLogins.MarkAllDropped();
                    m_LinkedServerLogins = null;
                }
            }
        }

        private void ScriptIncludeIfNotExists(StringBuilder sb, ScriptingPreferences sp, string predicate)
        {
            if (sp.IncludeScripts.ExistenceCheck)
            {
                if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                                Scripts.INCLUDE_EXISTS_LINKED_SERVER90,
                                predicate,
                                FormatFullNameForScripting(sp, false));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                                Scripts.INCLUDE_EXISTS_LINKED_SERVER80,
                                predicate,
                                FormatFullNameForScripting(sp, false));
                }
            }
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


        LinkedServerLoginCollection m_LinkedServerLogins = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(LinkedServerLogin))]
        public LinkedServerLoginCollection LinkedServerLogins
        {
            get
            {
                if (m_LinkedServerLogins == null)
                {
                    m_LinkedServerLogins = new LinkedServerLoginCollection(this);
                }
                return m_LinkedServerLogins;
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();

            if (null != m_LinkedServerLogins)
            {
                m_LinkedServerLogins.MarkAllDropped();
            }
        }

        /// <summary>
        /// enumerates the columns of tables defined on a linked server
        /// </summary>
        /// <returns></returns>
        public DataTable EnumColumns()
        {
            return EnumColumns(null, null, null, null);
        }

        /// <summary>
        /// enumerates the columns of tables defined on a linked server
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DataTable EnumColumns(string tableName)
        {
            return EnumColumns(tableName, null, null, null);
        }

        /// <summary>
        /// enumerates the columns of tables defined on a linked server
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        public DataTable EnumColumns(string tableName, string schemaName)
        {
            return EnumColumns(tableName, schemaName, null, null);
        }

        /// <summary>
        /// enumerates the columns of tables defined on a linked server
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public DataTable EnumColumns(string tableName, string schemaName,
                                        string databaseName)
        {
            return EnumColumns(tableName, schemaName, databaseName, null);
        }

        /// <summary>
        /// enumerates the columns of tables defined on a linked server
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <param name="databaseName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public DataTable EnumColumns(string tableName, string schemaName,
                                        string databaseName, string columnName)
        {
            try
            {
                StringCollection queries = new StringCollection();
                StringBuilder query = new StringBuilder();
                query.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_columns_ex @table_server = N'{0}'", SqlString(this.InternalName));
                if (null != tableName && tableName.Length > 0)
                {
                    query.AppendFormat(SmoApplication.DefaultCulture, ", @table_name = N'{0}'", SqlString(tableName));
                }

                if (null != schemaName && schemaName.Length > 0)
                {
                    query.AppendFormat(SmoApplication.DefaultCulture, ", @table_schema = N'{0}'", SqlString(schemaName));
                }

                if (null != databaseName && databaseName.Length > 0)
                {
                    query.AppendFormat(SmoApplication.DefaultCulture, ", @table_catalog = N'{0}'", SqlString(databaseName));
                }

                if (null != columnName && columnName.Length > 0)
                {
                    query.AppendFormat(SmoApplication.DefaultCulture, ", @column_name = N'{0}'", SqlString(columnName));
                }
                // always set ODBC version to 3, the default would have been 2
                query.Append(", @ODBCVer = 3");

                queries.Add(query.ToString());
                return this.ExecutionManager.ExecuteWithResults(queries).Tables[0];
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumColumns, this, e);
            }

        }

        /// <summary>
        /// enumerates the tables of a linked server
        /// </summary>
        /// <returns></returns>
        public DataTable EnumTables()
        {
            return EnumTables(null, null, null, LinkedTableType.Default);
        }

        /// <summary>
        /// enumerates the tables of a linked server
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DataTable EnumTables(string tableName)
        {
            return EnumTables(tableName, null, null, LinkedTableType.Default);
        }

        /// <summary>
        /// enumerates the tables of a linked server
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        public DataTable EnumTables(string tableName, string schemaName)
        {
            return EnumTables(tableName, schemaName, null, LinkedTableType.Default);
        }

        /// <summary>
        /// enumerates the tables of a linked server
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public DataTable EnumTables(string tableName, string schemaName, string databaseName)
        {
            return EnumTables(tableName, schemaName, databaseName, LinkedTableType.Default);
        }

        /// <summary>
        /// enumerates the tables of a linked server
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <param name="databaseName"></param>
        /// <param name="tableType"></param>
        /// <returns></returns>
        public DataTable EnumTables(string tableName, string schemaName,
                                    string databaseName, LinkedTableType tableType)
        {
            StringCollection queries = new StringCollection();
            StringBuilder query = new StringBuilder();
            query.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_tables_ex @table_server = N'{0}'", SqlString(this.InternalName));
            if (null != tableName && tableName.Length > 0)
            {
                query.AppendFormat(SmoApplication.DefaultCulture, ", @table_name = N'{0}'", SqlString(tableName));
            }

            if (null != schemaName && schemaName.Length > 0)
            {
                query.AppendFormat(SmoApplication.DefaultCulture, ", @table_schema = N'{0}'", SqlString(schemaName));
            }

            if (null != databaseName && databaseName.Length > 0)
            {
                query.AppendFormat(SmoApplication.DefaultCulture, ", @table_catalog = N'{0}'", SqlString(databaseName));
            }

            query.Append(", @table_type = ");

            switch (tableType)
            {
                case LinkedTableType.GlobalTemporary:
                    InternalAdd(query, "GLOBAL TEMPORARY"); break;
                case LinkedTableType.LocalTemporary:
                    InternalAdd(query, "LOCAL TEMPORARY"); break;
                case LinkedTableType.Alias:
                    InternalAdd(query, "ALIAS"); break;
                case LinkedTableType.SystemTable:
                    InternalAdd(query, "SYSTEM TABLE"); break;
                case LinkedTableType.SystemView:
                    InternalAdd(query, "SYSTEM VIEW"); break;
                case LinkedTableType.Table:
                    InternalAdd(query, "TABLE"); break;
                case LinkedTableType.View:
                    InternalAdd(query, "VIEW"); break;

                case LinkedTableType.Default:
                    query.Append("NULL"); break;
            }

            queries.Add(query.ToString());
            return this.ExecutionManager.ExecuteWithResults(queries).Tables[0];
        }


        // this is how ODBC wants the table type, varchar with embedded quotes
        private void InternalAdd(StringBuilder stmt, string optname)
        {
            stmt.AppendFormat(SmoApplication.DefaultCulture, "'''{0}'''", optname);
        }
        //This method is used to test the linked server connectivity

        public void TestConnection()
        {

            int majorVersion = ServerVersion.Major;
            if (majorVersion >= 9)
            {
                string linkedServerName = SqlString(this.InternalName);
                string sqlQuery = "EXEC sp_testlinkedserver N'" + linkedServerName + "'";
                //executing the query 
                //if connection test fails this will throw an exception
                this.ExecutionManager.ExecuteNonQuery(sqlQuery);
            }
            else
            {
                throw new InvalidVersionSmoOperationException(this.ServerVersion);
            }
        }



    }

}


