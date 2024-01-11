// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Specialized;
    using System.IO;
    using System.Data;
    using Microsoft.SqlServer.Management.Common;
    using System.Runtime.InteropServices;
#if !NETSTANDARD2_0 && !NETCOREAPP
    using Microsoft.SqlServer.Smo.UnSafeInternals;
#endif
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;
    using System.Diagnostics;
    using System.Linq;


    /// <summary>
    ///assignes arbitrary numbers to level names ( db object types )
    ///the coresponding xpath from database level down</summary>
    internal class SqlTypeConvert
    {
        String m_typeName;
        int m_typeNo;
        String m_path;

        /// <summary>
        ///constructor: name of level, assigned number path from database level</summary>
        public SqlTypeConvert(String name, int no, String path)
        {
            Name = name;
            No = no;
            Path = path;
        }

        /// <summary>
        ///name of level</summary>
        public String Name
        {
            get { return m_typeName; }
            set { m_typeName = value; }
        }

        /// <summary>
        ///assigned number</summary>
        public int No
        {
            get { return m_typeNo; }
            set { m_typeNo = value; }
        }

        /// <summary>
        ///path from database level</summary>
        public String Path
        {
            get { return m_path; }
            set { m_path = value; }
        }
    }

    /// <summary>
    ///used to identify an object in the dependency list
    ///pair of object id and object type</summary>
    internal class IDKey : IComparable
    {
        int m_id;
        int m_type;

        /// <summary>
        ///constructor with object id and object type</summary>
        internal IDKey(int id, int type)
        {
            m_id = id;
            m_type = type;
        }

        /// <summary>
        ///object id</summary>
        public int id
        {
            get { return m_id; }
            set { m_id = value; }
        }

        /// <summary>
        ///object type</summary>
        public int type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        /// <summary>
        ///compare to another IDKey</summary>
        public int CompareTo(object o)
        {
            IDKey idk = (IDKey)o;
            if (type > idk.type)
            {
                return 1;
            }
            if (type < idk.type)
            {
                return -1;
            }
            if (id == idk.id)
            {
                return 0;
            }
            return id > idk.id ? 1 : -1;
        }
    }

    internal class SqlEnumDependenciesSingleton
    {
        internal SortedList m_typeConvertTable;
    }

    /// <summary>
    /// Used to identify an object in the dependency list
    /// Contains information about the server, database, schema and name of object along with its type
    /// </summary>
    internal class ServerDbSchemaName : IComparable
    {
        string serverName;
        string dbName;
        string schemaName;
        string name;
        int id;
        int type;
        StringComparer svrComparer;
        StringComparer dbComparer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="dbName"></param>
        /// <param name="schemaName"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="type"></param>
        internal ServerDbSchemaName(string serverName, string dbName, string schemaName, string name, int id, int type)
        {
            this.serverName = serverName;
            this.dbName = dbName;
            this.schemaName = schemaName;
            this.name = name;
            this.id = id;
            this.type = type;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="dbName"></param>
        /// <param name="schemaName"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <param name="dbComparer"></param>
        /// <param name="svrComparer"></param>
        internal ServerDbSchemaName(string serverName, string dbName, string schemaName, string name, int id, int type, StringComparer svrComparer, StringComparer dbComparer)
        {
            this.serverName = serverName;
            this.dbName = dbName;
            this.schemaName = schemaName;
            this.name = name;
            this.id = id;
            this.type = type;
            this.svrComparer = svrComparer;
            this.dbComparer = dbComparer;
        }


        /// <summary>
        /// Server Name of the object
        /// </summary>
        public string ServerName
        {
            get { return this.serverName; }
            set { this.serverName = value; }
        }

        /// <summary>
        /// Database name of the object
        /// </summary>
        public string DatabaseName
        {
            get { return this.dbName; }
            set { this.dbName = value; }
        }

        /// <summary>
        /// Schema name of the object
        /// </summary>
        public string SchemaName
        {
            get { return this.schemaName; }
            set { this.schemaName = value; }
        }

        /// <summary>
        /// Name of the object
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        /// <summary>
        /// Id of the object
        /// </summary>
        public int Id
        {
            get { return this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Type of the object
        /// </summary>
        public int Type
        {
            get { return this.type; }
            set { this.type = value; }
        }


        /// <summary>
        /// Compare one ServerDbSchemaName with another
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            ServerDbSchemaName key = obj as ServerDbSchemaName;
            int diff;
            if (key == null)
            {
                throw new InvalidArgumentException();
            }
            if (this.svrComparer == null)
            {
                diff = string.Compare(key.serverName, this.serverName, StringComparison.Ordinal);
            }
            else
            {
                diff = this.svrComparer.Compare(key.serverName, this.serverName);
            }
            if (diff != 0)
            {
                return diff;
            }

            if (this.dbComparer == null)
            {
                diff = string.Compare(key.dbName, this.dbName, StringComparison.Ordinal);
            }
            else
            {
                diff = this.dbComparer.Compare(key.dbName, this.dbName);
            }
            if (diff != 0)
            {
                return diff;
            }

            if (key.type < this.type)
            {
                return -1;
            }

            if (key.type > this.type)
            {
                return 1;
            }

            if (key.id == this.id && this.id != 0) // the id should be initialized
            {
                return 0;
            }

            if (this.dbComparer == null)
            {
                diff = string.Compare(key.schemaName, this.schemaName, StringComparison.Ordinal);
                if (diff != 0)
                {
                    return diff;
                }

                diff = string.Compare(key.name, this.name, StringComparison.Ordinal);
                if (diff != 0)
                {
                    return diff;
                }
            }
            else
            {
                diff = this.dbComparer.Compare(key.schemaName, this.schemaName);
                if (diff != 0)
                {
                    return diff;
                }

                diff = this.dbComparer.Compare(key.name, this.name);
                if (diff != 0)
                {
                    return diff;
                }
            }
            return 0;
        }
    }

    /// <summary>
    ///class that encapsulates the dependency discovery mechanism</summary>
    [ComVisible(false)]
    internal class SqlEnumDependencies : IEnumDependencies
    {
        Object m_ci;
        String m_server;
        String m_database;
        SortedList m_tempDependencies;
        static readonly SqlEnumDependenciesSingleton sqlEnumDependenciesSingleton =
            new SqlEnumDependenciesSingleton();
        ServerVersion m_targetVersion;
        StringComparer svrComparer;
        StringComparer dbComparer;

        /// <summary>
        ///default constructor</summary>
        public SqlEnumDependencies()
        {
            m_tempDependencies = new SortedList();
        }

        bool isCloud = false;
        private bool IsDbCloud
        {
            get
            {
                return isCloud;
            }
            set
            {
                isCloud = value;
            }
        }
        /// <summary>
        ///intializes o sorted list of the triplets: Level name, assigned number, path from database level
        ///the key is the assigned number</summary>
        static SortedList TypeConvertTable
        {
            get
            {
                if (null == sqlEnumDependenciesSingleton.m_typeConvertTable)
                {

                    sqlEnumDependenciesSingleton.m_typeConvertTable = new SortedList();
                    sqlEnumDependenciesSingleton.m_typeConvertTable[3] = new SqlTypeConvert("Table", 3, "Table");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[0] = new SqlTypeConvert("UserDefinedFunction", 0, "UserDefinedFunction");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[2] = new SqlTypeConvert("View", 2, "View");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[4] = new SqlTypeConvert("StoredProcedure", 4, "StoredProcedure");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[6] = new SqlTypeConvert("Default", 6, "Default");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[7] = new SqlTypeConvert("Rule", 7, "Rule");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[8] = new SqlTypeConvert("Trigger", 8, "Table/Trigger");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[11] = new SqlTypeConvert("UserDefinedAggregate", 11, "UserDefinedAggregate");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[12] = new SqlTypeConvert("Synonym", 12, "Synonym");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[101] = new SqlTypeConvert("UserDefinedDataType", 101, "UserDefinedDataType");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[102] = new SqlTypeConvert("XmlSchemaCollection", 102, "XmlSchemaCollection");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[103] = new SqlTypeConvert("UserDefinedType", 103, "UserDefinedType");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[1000] = new SqlTypeConvert("SqlAssembly", 1000, "SqlAssembly");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[201] = new SqlTypeConvert("PartitionScheme", 201, "PartitionScheme");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[202] = new SqlTypeConvert("PartitionFunction", 202, "PartitionFunction");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[104] = new SqlTypeConvert("UserDefinedTableType", 104, "UserDefinedTableType");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[1001] = new SqlTypeConvert("UnresolvedEntity", 1001, "UnresolvedEntity");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[203] = new SqlTypeConvert("DdlTrigger", 203, "DdlTrigger");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[204] = new SqlTypeConvert("PlanGuide", 204, "PlanGuide");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[13] = new SqlTypeConvert("Sequence", 13, "Sequence");
                    sqlEnumDependenciesSingleton.m_typeConvertTable[19] = new SqlTypeConvert("SecurityPolicy", 19, "SecurityPolicy");
                    

                }
                return sqlEnumDependenciesSingleton.m_typeConvertTable;
            }
        }

        /// <summary>
        ///converts from a level name to the assigned number</summary>
        static int TypeToNo(String type)
        {
            switch (type)
            {
                case "Table": return 3;
                case "UserDefinedFunction": return 0;
                case "View": return 2;
                case "StoredProcedure": return 4;
                case "Default": return 6;
                case "Rule": return 7;
                case "Trigger": return 8;
                case "UserDefinedAggregate": return 11;
                case "Synonym": return 12;
                case "UserDefinedDataType": return 101;
                case "XmlSchemaCollection": return 102;
                case "UserDefinedType": return 103;
                case "SqlAssembly": return 1000;
                case "PartitionScheme": return 201;
                case "PartitionFunction": return 202;
                case "UserDefinedTableType": return 104;
                case "UnresolvedEntity": return 1001;
                case "DdlTrigger": return 203;
                case "PlanGuide": return 204;
                case "Sequence": return 13;
                case "SecurityPolicy": return 19;
                
            }
            StringBuilder typelist = new StringBuilder();
            int count = 0;
            foreach (SqlTypeConvert s in SqlEnumDependencies.TypeConvertTable.Values)
            {
                if (count++ > 0)
                {
                    typelist.Append(", ");
                }

                typelist.Append(s.Name);
            }

            throw new InternalEnumeratorException(StringSqlEnumerator.UnsupportedTypeDepDiscovery(type, typelist.ToString()));
        }

        /// <summary>
        /// Resolves a dependency request for the given connection to a DependencyChainCollection
        /// </summary>
        /// <param name="ci">connection object</param>
        /// <param name="rd">request</param>
        public DependencyChainCollection EnumDependencies(Object ci, DependencyRequest rd)
        {
            if (Microsoft.SqlServer.Server.SqlContext.IsAvailable)
            {
                throw new Exception(StringSqlEnumerator.SmoSQLCLRUnAvailable);
            }
            m_ci = ci;
            m_targetVersion = ExecuteSql.GetServerVersion(m_ci);
            IsDbCloud = (ExecuteSql.GetDatabaseEngineType(ci) == DatabaseEngineType.SqlAzureDatabase);
            SqlRequest sr = new SqlRequest();

            sr.ResultType = ResultType.Reserved1;

            Enumerator en = new Enumerator();
#if DEBUG
            Enumerator.TraceInfo("DEPINIT number of urns :\n{0}\n", rd.Urns.Length);

            foreach (Urn urn in rd.Urns)
            {
                Enumerator.TraceInfo(urn.ToString());
            }
#endif

            m_server = rd.Urns[0].GetNameForType("Server");
            m_database = rd.Urns[0].GetNameForType("Database");

            if (null == m_database)
            {
                //fail with message that the type is not supported
                TypeToNo(rd.Urns[0].Type);
                //if the type is supported but the database has not been specified
                //throw exception that database name must be present in the urn.
                throw new InternalEnumeratorException(StringSqlEnumerator.DatabaseNameMustBeSpecifiedinTheUrn(rd.Urns[0]));
            }
            //Will pass string comparer to ServerDbSchemaName so that we can compare two objects while adding them in dependency list

            this.dbComparer = GetStringCulture(m_database);
            this.svrComparer = GetStringCulture("MASTER");


            StringCollection queries = new StringCollection();
            if (!IsDbCloud)
            {
                queries.Add(GetUseString(m_database));
            }

            if (m_targetVersion.Major >= 10)        //only for >= Katmai
            {
                queries.Add("CREATE TABLE #tempdep (objid int NOT NULL, objname sysname NOT NULL, objschema sysname NULL, objdb sysname NOT NULL, objtype smallint NOT NULL)\n");
            }
            else
            {
                queries.Add("CREATE TABLE #tempdep (objid int NOT NULL, objtype smallint NOT NULL)\n");
            }

            // inserts need to be done in a transaction for perf reasons
            queries.Add("BEGIN TRANSACTION");

            // counts how many queries we'll execute in this batch
            Int64 queryCount = 0;
            StringBuilder strb = new StringBuilder();
            Int32 BatchSize = 1000;
            foreach (Urn urn in rd.Urns)
            {
                sr.Urn = urn;

                string type = urn.Type;
                if (type == "Default" && urn.Parent.Type == "Column")
                {
                    type = "DefaultConstraint";
                }
                int typeNo = TypeToNo(type);

                if (m_targetVersion.Major >= 10)      // only for >= Katmai
                {
                    if (typeNo == TypeToNo("Trigger") || typeNo == TypeToNo("PartitionScheme") || typeNo == TypeToNo("PartitionFunction") || typeNo == TypeToNo("SqlAssembly") || typeNo == TypeToNo("DdlTrigger") || typeNo == TypeToNo("PlanGuide"))
                    {
                        // entities without schema
                        sr.Fields = new String[] { "ID", "Name" };
                    }
                    else
                    {
                        sr.Fields = new String[] { "ID", "Name", "Schema" };
                    }

                    // generate a query that inserts the ID, name, schema & dbname into the temporary table 
                    SqlEnumResult ser = (SqlEnumResult)en.Process(ci, sr);
                    ser.StatementBuilder.AddStoredProperties();
                    if (typeNo == TypeToNo("Trigger") || typeNo == TypeToNo("PartitionScheme") || typeNo == TypeToNo("PartitionFunction") || typeNo == TypeToNo("SqlAssembly") || typeNo == TypeToNo("DdlTrigger") || typeNo == TypeToNo("PlanGuide"))
                    {
                        ser.StatementBuilder.AddFields("null");
                    }
                    ser.StatementBuilder.AddFields("db_name()");
                    ser.StatementBuilder.AddFields(typeNo.ToString(CultureInfo.InvariantCulture));
                    ser.StatementBuilder.AddPrefix("INSERT INTO #tempdep ");
                    try
                    {
                        strb.Append(ser.GetSingleDatabaseSql());
                    }
                    catch (InternalEnumeratorException e)
                    {
                        throw new InternalEnumeratorException(StringSqlEnumerator.InvalidUrnForDepends(urn), e);
                    }
                }
                else
                {
                    // see if ID has been passed in the Urn
                    string id = urn.GetAttribute("ID", urn.Type);
                    if (null != id && id.Length > 0)
                    {
                        // if the Urn contains ID we don't need to query for it
                        // so we will generate a faster INSERT statement
                        strb.Append("INSERT INTO #tempdep(objid,objtype) VALUES(");
                        // do a check to make sure ID is a string representation of an integer
                        Int32.Parse(id, CultureInfo.InvariantCulture);
                        strb.Append(id);
                        strb.Append(",");
                        strb.Append(typeNo.ToString());
                        strb.Append(")");
                        strb.AppendLine();
                    }
                    else
                    {
                        sr.Fields = new String[] { "ID" };
                        // if we don't have the ID but only the key fields we need to 
                        // generate a query that inserts the ID into the temporary table 
                        SqlEnumResult ser = (SqlEnumResult)en.Process(ci, sr);
                        ser.StatementBuilder.AddStoredProperties();
                        ser.StatementBuilder.AddFields(typeNo.ToString(CultureInfo.InvariantCulture));
                        ser.StatementBuilder.AddPrefix("INSERT INTO #tempdep ");
                        try
                        {
                            strb.Append(ser.GetSingleDatabaseSql());
                        }
                        catch (InternalEnumeratorException e)
                        {
                            throw new InternalEnumeratorException(StringSqlEnumerator.InvalidUrnForDepends(urn), e);
                        }
                    }
                }

                // add the queries if the batch size is reached
                queryCount++;
                if (queryCount > 0 && 0 == queryCount % BatchSize)
                {
                    queries.Add(strb.ToString());
                    strb.Length = 0;
                }
            }

            // add the leftover queries
            if (queryCount > 0 && 0 != queryCount % BatchSize)
            {
                queries.Add(strb.ToString());
            }
            strb = null;

            queries.Add("COMMIT TRANSACTION");

            string resourceFileName = string.Empty;
            if (IsDbCloud)
            {
                resourceFileName = "CloudDependency.sql";
            }
            else
            {
                if (m_targetVersion.Major <= 8)
                {
                    resourceFileName = "ShilohDependency.sql";
                }
                else if (m_targetVersion.Major == 9)
                {
                    resourceFileName = "YukonDependency.sql";
                }
                else if (m_targetVersion.Major == 10)
                {
                    resourceFileName = "KatmaiDependency.sql";
                }
                else if (m_targetVersion.Major < 13)
                {
                    resourceFileName = "SQL11Dependency.sql";
                }
                else
                {
                    resourceFileName = "SQL13Dependency.sql";
                }
            }
            String sp = String.Format(CultureInfo.InvariantCulture, 
                "declare @find_referencing_objects int\nset @find_referencing_objects = {0}\n",
                rd.ParentDependencies ? 0 : 1);
#if !NETSTANDARD2_0 && !NETCOREAPP
            StreamReader tr = new StreamReader(ManagementUtil.LoadResourceFromAssembly(Assembly.GetExecutingAssembly(), resourceFileName));
#else
            Assembly sqlEnumAssembly = SqlEnumNetCoreExtension.GetAssembly(typeof(SqlTypeConvert));
            StreamReader tr = new StreamReader(sqlEnumAssembly.GetManifestResourceStream(sqlEnumAssembly.GetName().Name + "." + resourceFileName) ?? sqlEnumAssembly.GetManifestResourceStream(resourceFileName));
#endif

            sp += tr.ReadToEnd();
            tr.Dispose();
            queries.Add(sp);
            DataTable dt;
#if DEBUG
            var q = queries.Cast<string>().Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine(s)).ToString();
            Trace.TraceInformation("Final dep query:\r\n" + q);
#endif
            if (this.IsDbCloud)
            {
                dt = ExecuteSql.ExecuteWithResults(queries, ci, m_database);
            }
            else
            {
                dt = ExecuteSql.ExecuteWithResults(queries, ci);
            }



#if DEBUG
            Enumerator.TraceInfo("DEPRES number of rows :\n{0}\n", dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                Enumerator.TraceInfo(DumpRow(row));
            }
#endif

            DependencyChainCollection resdep = BuildResult(dt);

            //Change the Context back to the original database
            if (!this.IsDbCloud)
            {
                ExecuteSql.ExecuteImmediate("USE [master]", ci);
            }

            return resdep;
        }

        private string MakeSqlString(String s)
        {

            return String.Format(CultureInfo.InvariantCulture, "N'{0}'", EscapeString(s, '\''));

        }
        private String EscapeString(String s, char cEsc)
        {
            if (null == s)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                sb.Append(c);
                if (cEsc == c)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private StringComparer GetStringCulture(String database)
        {
            StringComparer comparer = System.StringComparer.InvariantCultureIgnoreCase;
            StringBuilder sb = new StringBuilder();


            if (m_targetVersion.Major > 8)
            {
                sb.AppendFormat("SELECT collation_name FROM sys.databases where name={0}", MakeSqlString(database));
            }
            else
            {
                sb.AppendFormat("SELECT CAST(DATABASEPROPERTYEX(dtb.name, 'Collation') AS sysname) AS [collation_name] FROM master.dbo.sysdatabases AS dtb WHERE(dtb.name={0})", MakeSqlString(database));
            }

            DataTable dt = ExecuteSql.ExecuteWithResults(sb.ToString(), m_ci);
            if (dt.Rows.Count != 0)
            {
                string collation = (string)dt.Rows[0][0];
                if (collation.Contains("_CS_"))
                {
                    comparer = System.StringComparer.InvariantCulture;
                }
            }
            else
            {
                    comparer = System.StringComparer.InvariantCulture;
            }

            return comparer;
        }

        /// <summary>
        ///find a SqlTypeConvert based on the assigned number</summary>
        static SqlTypeConvert FindByNo(int no)
        {
            return (SqlTypeConvert)SqlEnumDependencies.TypeConvertTable[no];
        }

        /// <summary>
        ///build a DependencyChainCollection based on the result from the dependency discovery tsql</summary>
        DependencyChainCollection BuildResult(DataTable dt)
        {
            DependencyChainCollection deferredLink = new DependencyChainCollection();
            DependencyChainCollection deps = new DependencyChainCollection();
            Type dbnull = typeof(System.DBNull);

            if (m_targetVersion.Major >= 10)      // only for >= Katmai
            {
                foreach (DataRow row in dt.Rows)
                {
                    ServerDbSchemaName objectKey = BuildKey(row, true);

                    Dependency dep = (Dependency)m_tempDependencies[objectKey];

                    if (null == dep)
                    {
                        //have to build the urn
                        String surn = null;
                        surn = BuildUrn(row, true);

                        dep = new Dependency();
                        dep.Urn = surn;
                        m_tempDependencies[objectKey] = dep;
                        deps.Add(dep);
                    }
                    // Dependency of object might already exist with the IsSchemaBound value not set
                    // Always set the IsSchemaBound for the object with the parent
                    dep.IsSchemaBound = (bool)row["schema_bound"];
                    ServerDbSchemaName relativeKey = BuildKey(row, false);
                    if (objectKey.CompareTo(relativeKey) != 0)
                    {
                        Dependency d = (Dependency)m_tempDependencies[relativeKey];
                        if (null != d)
                        {
                            d.Links.Add(dep);
                        }
                        else
                        {
                            d = new Dependency();
                            d.Urn = BuildUrn(row, false);
                            m_tempDependencies[relativeKey] = d;
                            deps.Add(d);
                            d.Links.Add(dep);
                        }
                    }
                }
                deps.Reverse();
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    IDKey idk = BuildIDKey(row, false);

                    Dependency dep = (Dependency)m_tempDependencies[idk];

                    if (null == dep)
                    {
                        //have to build the urn
                        String surn = null;
                        surn = BuildUrn(idk.type, row);

                        dep = new Dependency();
                        dep.Urn = surn;
                        m_tempDependencies[idk] = dep;
                        deps.Add(dep);
                    }
                    if (dbnull != row["relative_id"].GetType())
                    {
                        idk = BuildIDKey(row, true);
                        Dependency d = (Dependency)m_tempDependencies[idk];
                        if (null != d)
                        {
                            dep.Links.Add(d);
                        }
                        else
                        {
                            //object not yet added. Defer resolution of key until all objects are added
                            dep.Links.Add(idk);
                            deferredLink.Add(dep);
                        }
                    }
                }
                ResolveDeferredLinks(deferredLink);
            }
            return deps;
        }

        /// <summary>
        ///gets the IDKey for dependency the row
        ///it get get the id for either the object or the objects parent</summary>
        IDKey BuildIDKey(DataRow row, bool forParent)
        {
            try
            {
                int id = (int)(forParent ? row["relative_id"] : row["object_id"]);
                short type = (short)(forParent ? row["relative_type"] : row["object_type"]);
                return new IDKey(id, type);
            }
            catch (InvalidCastException e)
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.CouldNotGetInfoFromDependencyRow(DumpRow(row)), e);
            }
        }

        /// <summary>
        /// Gets the ServerDbSchemaName for a datarow
        /// Gets the value for either the object or the parent
        /// </summary>
        /// <param name="row"></param>
        /// <param name="forParent"></param>
        /// <returns></returns>
        ServerDbSchemaName BuildKey(DataRow row, bool forParent)
        {
            try
            {
                string serverName = (string)(forParent ? row["object_svr"] : row["relative_svr"]);
                string dbName = (string)(forParent ? row["object_db"] : row["relative_db"]);
                string schemaName = (string)(forParent ? row["object_schema"] : row["relative_schema"]);
                string name = (string)(forParent ? row["object_name"] : row["relative_name"]);
                int id = (int)(forParent ? row["object_id"] : row["relative_id"]);
                short type = (short)(forParent ? row["object_type"] : row["relative_type"]);
                return new ServerDbSchemaName(serverName, dbName, schemaName, name, id, type, this.svrComparer, this.dbComparer);
            }
            catch (InvalidCastException e)
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.CouldNotGetInfoFromDependencyRow(DumpRow(row)), e);
            }
        }

        /// <summary>
        ///function used only in debug to trace a dependency row</summary>
        static string DumpRow(DataRow row)
        {
            StringBuilder sb = new StringBuilder();
            foreach (DataColumn col in row.Table.Columns)
            {
                sb.Append(col.Caption);
                sb.Append("(");
                sb.Append(row[col.Caption].GetType());
                sb.Append(")");
                sb.Append(" ");
                sb.Append(row[col.Caption]);
                sb.Append(".");
            }
            return sb.ToString();
        }

        /// <summary>
        ///resolve links which were defered</summary>
        void ResolveDeferredLinks(DependencyChainCollection deferredLink)
        {
            TraceHelper.Assert(m_targetVersion.Major < 10, "ResolvedDeferredLinks should not be called for version >= Katmai");

            foreach (Dependency dep in deferredLink)
            {
                for (int i = 0; i < dep.Links.Count; i++)
                {
                    IDKey idk = ((ArrayList)dep.Links)[i] as IDKey;
                    if (null != idk)
                    {
                        ((ArrayList)dep.Links)[i] = m_tempDependencies[idk];
                    }
                }
            }
        }

        /// <summary>
        ///given an idkey get the urn for that object: 
        ///expensive, makes query to resolve the urn from id
        ///no longer used, replaced by the BuildUrn function</summary>
        Urn GetUrnByQuery(IDKey idk)
        {
            SqlTypeConvert stc = FindByNo(idk.type);

            Request req = new Request();
            req.Fields = new String[1] { "Urn" };
            req.ResultType = ResultType.DataTable;
            if (null != m_server)
            {
                req.Urn = String.Format(CultureInfo.InvariantCulture, "Server[@Name='{0}']/Database[@Name='{1}']/{2}[@ID={3}]", Urn.EscapeString(m_server), Urn.EscapeString(m_database), stc.Path, idk.id);
            }
            else
            {
                req.Urn = String.Format(CultureInfo.InvariantCulture, "Server/Database[@Name='{0}']/{1}[@ID={2}]", Urn.EscapeString(m_database), stc.Path, idk.id);
            }

            Enumerator en = new Enumerator();
            DataTable dt = en.Process(m_ci, req);
            return (String)dt.Rows[0][0];
        }

        /// <summary>
        ///builds urn based on the data row returned from dependency discovery</summary>
        Urn BuildUrn(int type, DataRow row)
        {
            TraceHelper.Assert(m_targetVersion.Major < 10, "BuildUrn should never be called by server version >= Katmai");

            SqlTypeConvert stc = FindByNo(type);
            string surn = null;

            if (null != m_server)
            {
                surn = String.Format(CultureInfo.InvariantCulture, "Server[@Name='{0}']/Database[@Name='{1}']/", Urn.EscapeString(m_server), Urn.EscapeString(m_database));
            }
            else
            {
                surn = String.Format(CultureInfo.InvariantCulture, "Server/Database[@Name='{0}']/", Urn.EscapeString(m_database));
            }
            try
            {

                if ("Trigger" == stc.Name)
                {
                    surn += $"{FindByNo(((short)(row["relative_type"]))).Name}[@Name='{Urn.EscapeString((string)row["relative_name"])}' and @Schema='{Urn.EscapeString((string)row["relative_schema"])}']/{stc.Name}[@Name='{Urn.EscapeString((string)row["object_name"])}']";
                    return surn;
                }
                if (typeof(System.DBNull) == row["object_schema"].GetType())
                {
                    surn += String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", stc.Name, Urn.EscapeString((string)row["object_name"]));
                    return surn;
                }
                surn += String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}' and @Schema='{2}']",
                    stc.Name, Urn.EscapeString((string)row["object_name"]), Urn.EscapeString((string)row["object_schema"]));
            }
            catch (InvalidCastException e)
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.CouldNotGetInfoFromDependencyRow(DumpRow(row)), e);
            }
            return surn;
        }


        Urn BuildUrn(DataRow row, bool forParent)
        {
            TraceHelper.Assert(m_targetVersion.Major >= 10, "BuildUrn should be called by server version >= Katmai only");

            string surn = null;
            string serverName = forParent ? (string)row["object_svr"] : (string)row["relative_svr"];
            string dbName = forParent ? (string)row["object_db"] : (string)row["relative_db"];
            string schemaName = forParent ? (string)row["object_schema"] : (string)row["relative_schema"];
            string name = forParent ? (string)row["object_name"] : (string)row["relative_name"];
            short type = forParent ? (short)row["object_type"] : (short)row["relative_type"];

            SqlTypeConvert stc = FindByNo(type);
            Debug.Assert(stc != null, "No SqlType found for " + type.ToString());
            if (string.Empty != serverName)
            {
                surn = String.Format(CultureInfo.InvariantCulture, "Server[@Name='{0}']/", Urn.EscapeString(serverName));
            }
            else if (null != m_server)
            {
                surn = String.Format(CultureInfo.InvariantCulture, "Server[@Name='{0}']/", Urn.EscapeString(m_server));
            }
            else
            {
                surn = String.Format(CultureInfo.InvariantCulture, "Server/");
            }

            if (string.Empty != dbName)
            {
                surn += String.Format(CultureInfo.InvariantCulture, "Database[@Name='{0}']/", Urn.EscapeString(dbName));
            }
            else
            {
                surn += String.Format(CultureInfo.InvariantCulture, "Database[@Name='{0}']/", Urn.EscapeString(m_database));
            }

            try
            {
                if ((row["pname"] is string pname) && pname != "") 
                // For some types we need to have the parent's name to build the URN.
                {
                    string pschema = (string)row["pschema"];
                    int ptype = (int)row["ptype"];

                    // none of these values can be null or empty

                    surn += $"{FindByNo(ptype).Name}[@Name='{Urn.EscapeString(pname)}' and @Schema='{Urn.EscapeString(pschema)}']/{stc.Name}[@Name='{Urn.EscapeString(name)}']";
                    return surn;
                }
                if (string.Empty == schemaName)
                {
                    surn += String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", stc.Name, Urn.EscapeString(name));
                    return surn;
                }
                surn += String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}' and @Schema='{2}']",
                    stc.Name, Urn.EscapeString(name), Urn.EscapeString(schemaName));
            }
            catch (InvalidCastException e)
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.CouldNotGetInfoFromDependencyRow(DumpRow(row)), e);
            }
            return surn;
        }

        /// <summary>
        ///get use statement for the specified database</summary>
        String GetUseString(String name)
        {
            return String.Format(CultureInfo.InvariantCulture, "use [{0}]\n", Util.EscapeString(name, ']'));
        }
    }
}
