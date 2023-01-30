// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.Collections;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif
    using System.Collections.Specialized;
    using Microsoft.SqlServer.Management.Common;
    using System.Runtime.InteropServices;

    ///	<summary>
    ///	main work horse for the sql enumerator. it generates the tsql for a level and 
    ///	comunicates with the other levels </summary>
    [ComVisible(false)]
    abstract public class SqlObjectBase : EnumObject, ISqlFilterDecoderCallback
    {
        StatementBuilder m_sb;					//placeholder for the curent object's sql statement
        ParentLink m_parentLink;
        ConditionedSqlList m_conditionedSqlList;
        ConditionedSqlList m_propertyLinkList;
        ConditionedSqlList m_postProcessList;
        RequestParentSelect m_RequestParentSelect;
        SortedList m_OrderByRedirect;
        SortedList m_SpecialQuery;
        ArrayList m_LinkFields;
        bool m_distinct;

        ///	<summary>
        ///	Default contructor </summary>
        public SqlObjectBase()
        {
            m_conditionedSqlList = new ConditionedSqlList();
            m_propertyLinkList = new ConditionedSqlList();
            m_postProcessList = new ConditionedSqlList();
        }

        ///	<summary>
        ///	The TSQL place holder + information that will be later transformed in TSQL</summary>
        public StatementBuilder StatementBuilder
        {
            get { return m_sb; }
            set { m_sb = value; }
        }

        ///	<summary>
        ///	how the current level links with the parent level </summary>
        internal ParentLink ParentLink
        {
            get { return m_parentLink; }
            set { m_parentLink = value; }
        }

        ///	<summary>
        ///	Storeage for a parent select ( when we need the tsql for the parent level, usually to insert it in prefix ) </summary>
        protected RequestParentSelect RequestParentSelect
        {
            get { return m_RequestParentSelect; }
            set { m_RequestParentSelect = value; }
        }

        ///	<summary>
        ///	used to translate an order by on post process fields to other, supporting fields</summary>
        protected SortedList OrderByRedirect
        {
            get 
            { 
                if( null == m_OrderByRedirect )
                {
                    m_OrderByRedirect = new SortedList(System.StringComparer.Ordinal);
                }
                return m_OrderByRedirect; 
            }
        }

        ///	<summary>
        ///	stores a query used to test a condition , for example that a table needed to resolve this level
        /// exists, if that fails then the empty set is returned rather then exception</summary>
        protected SortedList SpecialQuery
        {
            get 
            { 
                return m_SpecialQuery; 
            }
        }

        ///	<summary>
        ///	prefix, postfix etc, that must be added when certain fields are requested</summary>
        protected ConditionedSqlList ConditionedSqlList
        {
            get 
            { 
                return m_conditionedSqlList; 
            }

            set 
            { 
                m_conditionedSqlList = value; 
            }
        }

        ///	<summary>
        ///	links for properties</summary>
        protected ConditionedSqlList PropertyLinkList
        {
            get 
            { 
                return m_propertyLinkList; 
            }

            set 
            { 
                m_propertyLinkList = value; 
            }
        }

        ///	<summary>
        ///	post processes for the level</summary>
        protected ConditionedSqlList PostProcessList
        {
            get 
            { 
                return m_postProcessList; 
            }

            set 
            { 
                m_postProcessList = value; 
            }
        }

        ///	<summary>
        ///	the result must be distinct</summary>
        protected bool Distinct
        {
            get { return m_distinct; }
            set { m_distinct = value; }
        }

        ///	<summary>
        ///	set's the Urn up to this level 
        ///	</summary>
        internal void SetUrn(Urn urn)
        {
            this.Urn = urn;
        }

        ///	<summary>
        ///	the SqlRequest that this level must resolve</summary>
        protected SqlRequest SqlRequest
        {
            get
            {
                SqlRequest req;
                if( null == Request )
                {
                    req = new SqlRequest();
                }
                else if( Request is SqlRequest )
                {
                    req = (SqlRequest)Request; 
                }
                else
                {
                    req = new SqlRequest(Request);
                }
                return req;
            }
        }

        ///	<summary>
        ///	Level name</summary>
        internal string ObjectName
        {
            get { return this.Urn.Type; }
        }

        ///	<summary>
        ///	get the property field if it is legal for the usage, else throw</summary>
        public SqlObjectProperty GetSqlProperty(String field, ObjectPropertyUsages usage)
        {
            return (SqlObjectProperty)GetProperty(field, usage);
        }

        ///	<summary>
        ///	reports the ResultTypes supported by this level</summary>
        public override ResultType[] ResultTypes
        {
            get	{return new ResultType[] {ResultType.IDataReader, ResultType.DataTable, ResultType.DataSet };	}
        }

        void AddRequestProperty(SqlObjectProperty prop, bool triggered)
        {
            if( this.CalculateParentRequest )
            {
                AddLinkFields(prop.LinkFields);
                this.PostProcessList.AddHits(this, prop.Name, null);
            }
            else
            {
                prop.Alias = this.GetAliasPropertyName(prop.Name);
                prop.Add(this, triggered);
                AddLinkProperty(prop.Name);
            }
        }

        void RegisterPostProcessHits()
        {
            foreach(SqlPostProcess pp in this.PostProcessList)
            {
                if( pp.Used )
                {
                    pp.Register(this);
                }
            }
        }

        //add all the properties to the sql statement
        void AddRequestProperties()
        {
            if( null == this.Request || null == this.Request.Fields )
            {
                return;
            }

            foreach (String field in Request.Fields )
            {
                //for internal calls allow hidden fields to be used
                ObjectPropertyUsages usage = ResultType.Reserved2 == this.Request.ResultType ? 
                                        ObjectPropertyUsages.Reserved1 : ObjectPropertyUsages.Request;
                SqlObjectProperty prop = GetSqlProperty(field, usage);
                AddRequestProperty(prop, false);
            }

            AddPostProcessTriggers();
        }

        ///	<summary>
        /// the xpath scanner has detected that this property is used in filter
        ///add whatever is required to support it, return value </summary>
        public virtual String AddFilterProperty(String name)
        {
            SqlObjectProperty prop = GetSqlProperty(name, ObjectPropertyUsages.Filter);
            if( this.CalculateParentRequest )
            {
                AddLinkFields(prop.LinkFields);
            }
            else
            {
                AddLinkProperty(name);
            }
            return prop.GetValueWithCast(this);
        }

        ///	<summary>
        /// the xpath scanner has detected that this property is used in orderby
        ///add whatever is required to support it, return value	</summary>
        public virtual String AddOrderByProperty(String name)
        {
            return AddOrderByProperty(name, false);
        }

        ///	<summary>
        ///	Add the information that the TSQL is ordered by the property name
        /// if overrideFlags is tru don't check if it is legal to use this property for order by</summary>
        public virtual String AddOrderByProperty(String name, bool overrideFlags)
        {
            SqlObjectProperty prop;
            if (overrideFlags)
            {
                prop = GetSqlProperty(name, ObjectPropertyUsages.Reserved1);
            }
            else
            {
                prop = GetSqlProperty(name, ObjectPropertyUsages.OrderBy);
            }
            if( this.CalculateParentRequest )
            {
                AddLinkFields(prop.LinkFields);
            }
            else
            {
                AddLinkProperty(name);
            }
            return prop.GetValueWithCast(this);
        }

        ///	<summary>
        ///	add whatever is required to support it, return value </summary>
        protected virtual String AddLinkProperty(String name)
        {
            SqlObjectProperty prop = GetSqlProperty(name, ObjectPropertyUsages.Reserved1);
            AddConditionals(name);
            return prop.GetValueWithCast(this);
        }

        ///	<summary>
        ///	add whatever is required to support it</summary>
        protected void AddConditionalsJustPropDependencies(String name)
        {
            if( null == m_sb )
            {
                return;
            }
            SqlObjectProperty prop = GetSqlProperty(name, ObjectPropertyUsages.Reserved1);
            if( null != prop.LinkFields )
            {
                foreach(LinkField lf in prop.LinkFields )
                {
                    if( LinkFieldType.Local == lf.Type )
                    {
                        AddLinkProperty(lf.Field);
                    }
                }
            }
        }

        ///	<summary>
        ///add sql wich is necessary if the property: field, is requested </summary>
        protected virtual void AddConditionals(String field)
        {
            if( null == m_sb )
            {
                return;
            }
            this.ConditionedSqlList.AddHits(this, field, m_sb);
            this.PropertyLinkList.AddHits(this, field, m_sb);
            this.PostProcessList.AddHits(this, field, m_sb);

            AddConditionalsJustPropDependencies(field);
        }

        //looks at all multiple_links tags and gets the properties requested
        //from the parent object
        void RetrieveParentRequestLinks(SqlRequest sr)
        {
            //holder for link properties that we want from parent
            //properties which are not in the request.Field
            //but we need them to form other composed properties
            //or for join
            m_LinkFields = new ArrayList();

        
            //we look at ParentLink which defines how we link with parent
            //we add in request the parent properties requested for join
            if( null != this.ParentLink )
            {
                AddLinkFields(this.ParentLink.LinkFields);
            }

            //we look at property_link 
            //we add in request the parent properties requested for join
            foreach(ConditionedSql pl in this.PropertyLinkList)
            {
                if( null != pl.LinkMultiple )
                {
                    AddLinkFields(pl.LinkMultiple.LinkFields);
                }
            }

            //we look at prefix
            //we add in request the parent properties requested for join
            foreach (ConditionedSql pl in this.ConditionedSqlList)
            {
                if (null != pl.LinkMultiple)
                {
                    AddLinkFields(pl.LinkMultiple.LinkFields);
                }
            }

            //simulate actions in GetData but only to detect 
            //neccessary properties from parent and put them in m_LinkFields
            //this might be cause by composite properties
            AddRequestProperties();

            //check all the link fields requested by children
            //if there are any composite properties which need properties from parent
            //add the to the LinkFields list
            if( null != this.SqlRequest && null != this.SqlRequest.LinkFields )
            {

                foreach(LinkField lf in this.SqlRequest.LinkFields)
                {
                    if( LinkFieldType.Parent == lf.Type )
                    {
                        SqlObjectProperty prop = GetSqlProperty(lf.Field, ObjectPropertyUsages.Reserved1);
                        AddLinkFields(prop.LinkFields);
                    }
                }
            }
            //add link properties to the request if needed
            if( m_LinkFields.Count > 0 )
            {
#if DEBUG
                string sFields = "null";
                if( null != this.SqlRequest.Fields )
                {
                    sFields = this.SqlRequest.Fields.Length.ToString();
                    sFields += ": ";
                    foreach(string f in this.SqlRequest.Fields)
                    { 
                        sFields+= f;
                        sFields += " ";
                    }					
                }

                string sLinkFields = "null";
                if( null != m_LinkFields )
                {
                    sLinkFields = m_LinkFields.Count.ToString();
                    sLinkFields += ": ";
                    foreach(LinkField f in m_LinkFields)
                    { 
                        sLinkFields += f.Field;
                        sLinkFields += " ";
                    }					
                }

                Enumerator.TraceInfo(
                    "Adding link fields. LevelName = {0}, requested Fields = <{1}>, asking for LinkFields = <{2}>",
                    this.Name, sFields, sLinkFields);
#endif
                
                sr.SetLinkFields(m_LinkFields); 
            }

        }

        //if the request had also parent properties, propagate them
        void PropagateRequestedParentProperties(SqlRequest sr)
        {
            //propagate properties requested from parent
            if( null != Request && null != Request.ParentPropertiesRequests )
            {
                //add user requested properties from parent
                if( Request.ParentPropertiesRequests.Length > 0 )
                {
                    PropertiesRequest pr = Request.ParentPropertiesRequests[0];
                    if( null != pr )
                    {
                        if( null != pr.Fields )
                        {
                            if( null == sr.Fields )
                            {
                                sr.Fields = new string[pr.Fields.Length];
                            }
                            pr.Fields.CopyTo(sr.Fields, 0);
                            sr.PropertyAlias = pr.PropertyAlias;
                        }
                        if( null != pr.OrderByList )
                        {
                            if( null == sr.OrderByList )
                            {
                                sr.OrderByList = new OrderBy[pr.OrderByList.Length];
                            }
                            pr.OrderByList.CopyTo(sr.OrderByList, 0);
                        }
                    }
                }
                if( Request.ParentPropertiesRequests.Length > 1 )
                {
                    sr.ParentPropertiesRequests = new PropertiesRequest[Request.ParentPropertiesRequests.Length - 1];
                    Array.Copy(Request.ParentPropertiesRequests, 1, sr.ParentPropertiesRequests, 0, sr.ParentPropertiesRequests.Length);
                }
                else
                {
                    sr.ParentPropertiesRequests = null;
                }
            }
        }

        ///	<summary>
        ///	Retrive the request that will be sent to the parent level. 
        /// we must comunicate wht info we need from the parent level here</summary>
        public override Request RetrieveParentRequest()
        {
            //build parent request
            SqlRequest sr = new SqlRequest();

            sr.RequestFieldsTypes = RequestFieldsTypes.Request;
            if( ResultType.Reserved2 == this.Request.ResultType )
            {
                //used for internal calls so that they stop at database level see database.cs GetData
                sr.ResultType = ResultType.Reserved2; 
            }
            else
            {
                //internal call
                sr.ResultType = ResultType.Reserved1;
            }

            //looks at all multiple_links tags and gets the properties requested
            //from the parent object
            RetrieveParentRequestLinks(sr);

            AddXpathFilter();
            AddOrderByInDatabase();

            //we requested a select statement from parent and insert it somewhere in our statement
            //ask for prefix and postfix only to support the insertes statement
            if( null != this.RequestParentSelect )
            {
                sr.PrefixPostfixFields = this.RequestParentSelect.Fields;
            }

            //propagate properties requested from parent
            PropagateRequestedParentProperties(sr);

            //job done, using it now will only be a bug
            m_LinkFields = null;

            return sr;
        }

        ///	<summary>
        ///	Add link properties in linkFields
        /// lft indicates which side of the link pair ( local parent ) is to be added </summary>
        internal void AddLinkProperties(LinkFieldType lft, ArrayList linkFields)
        {
            if( null != linkFields )
            {
                foreach(LinkField lf in linkFields)
                {
                    if( lft == lf.Type )
                    {
                        SqlObjectProperty p = (SqlObjectProperty)GetProperty(lf.Field, ObjectPropertyUsages.Reserved1);
                        AddLinkProperties(LinkFieldType.Local, p.LinkFields);
                        lf.Value = AddLinkProperty(lf.Field);
                    }
                }
            }
        }

        void AddParentLinkProperties()
        {
            AddParentLinkPropertiesParent();
            AddParentLinkPropertiesLocal();
        }

        //the parent processes its part of the child-parent link
        void AddParentLinkPropertiesParent()
        {
            if( null != SqlRequest )
            {
                AddLinkProperties(LinkFieldType.Parent, SqlRequest.LinkFields);
            }
        }

        void AddParentLinkPropertiesLocal()
        {
            if( null != m_parentLink )
            {
                AddLinkProperties(LinkFieldType.Local, m_parentLink.LinkFields);
            }
        }

        ///	<summary>
        ///combine StatementBuilder from this level with what was received in the result from the parent level</summary>
        protected virtual void IntegrateParentResult(EnumResult erParent)
        {
            if( null != erParent )
            {
                //as an SqlObject I know I've asked for Reserved1 in Request ... that is SqlEnumResult
                SqlEnumResult ser = (SqlEnumResult)erParent;
                this.StatementBuilder.Merge(ser.StatementBuilder);
                ser.StatementBuilder = this.StatementBuilder;
            }
        }

        //add the filter requested in XPATH
        void AddXpathFilter()
        {
            FilterDecoder fd = new FilterDecoder((ISqlFilterDecoderCallback)this);
            String strXPathFilter = fd.GetSql(Filter);
            if( 0 != strXPathFilter.Length && !this.CalculateParentRequest )			// add filter from the filter expression in xpath
            {
                this.StatementBuilder.AddWhere(strXPathFilter);
            }
        }

        ///	<summary>
        ///	prepare to do main work and fill StatementBuilder with the information for this level</summary>
        internal void PrepareGetData(EnumResult erParent)
        {
            BuildStatement(erParent);
            //integrate the parent sql
            IntegrateParentResult(erParent);

            RegisterPostProcessHits();
        }

        ///	<summary>
        ///	fill StatementBuilder with the information for this level </summary>
        public override EnumResult GetData(EnumResult erParent)
        {
            PrepareGetData(erParent);

            BeforeStatementExecuted(this.ObjectName);
            
            //transform the StamentBuilder in whatever is asked in Request
            return BuildResult(erParent);
        }

        ///	<summary>
        ///	Allow subclasses to add anything to the statement
        /// </summary>
        protected virtual void BeforeStatementExecuted(string levelName)
        {
        }
        
        ///	<summary>
        /// build statement</summary>
        protected void BuildStatement(EnumResult erParent)
        {
            m_sb = new StatementBuilder();

            //add properties needed for link with parent
            AddParentLinkProperties();

            //add to StatementBuilder the requested properties
            AddRequestProperties();

            //fill prefix postfix for requested properties
            FillPrefixPostfix();

            //add the filter requested in XPATH
            AddXpathFilter();
            //should select be distinct ?
            if( true == this.Distinct )
            {
                this.StatementBuilder.Distinct = true;
            }
            //the order by clause
            if( null != erParent && ((SqlEnumResult)erParent).MultipleDatabases )
            {
                AddOrderByAcrossDatabases();
            }
            else
            {
                AddOrderByInDatabase();
            }
        }

        ///	<summary>
        /// Add the special query to the tsql</summary>
        internal void AddSpecialQuery(string database, string query)
        {
            if( null == m_SpecialQuery )
            {
                m_SpecialQuery = new SortedList(System.StringComparer.Ordinal);
            }
            m_SpecialQuery.Add(database, query);
        }

        private void AddSpecialQueryToResult(SqlEnumResult result)
        {
            if( null != this.SpecialQuery )
            {
                foreach(DictionaryEntry e in this.SpecialQuery)
                {
                    result.AddSpecialQuery((string)e.Key, (string)e.Value);
                }
            }
        }

        ///	<summary>
        ///	Based on the requested result type and on the result from the parent level 
        ///return the requested info from this level</summary>
        internal EnumResult BuildResult(EnumResult result)
        {
            if (null == result)
            {
                //We cannot ask for GetDatabaseEngineType on non-sql servers. So, we guard this with a check to
                //ask for engine type only if the type supports DatabaseEngineTypes!

                //NOTE: This is the same check as the one in Environment.GetDatabaseEngineType
                DatabaseEngineType databaseEngineType = DatabaseEngineType.Standalone;

                if(this is ISupportDatabaseEngineTypes)
                {
                    databaseEngineType = ExecuteSql.GetDatabaseEngineType(this.ConnectionInfo);
                }

                result = new SqlEnumResult(this.StatementBuilder, ResultType.Reserved1, databaseEngineType);
            }

            SqlEnumResult sqlresult = (SqlEnumResult)result;
            AddSpecialQueryToResult(sqlresult);

            if( null == Request || ResultType.Reserved1 == Request.ResultType || 
                                    ResultType.Reserved2 == Request.ResultType)
            {
                return sqlresult;
            }

            ResultType resultType = Request.ResultType;
            //TODO: SQlCE interfaces need to update to respect IDataReader instead of DataTable.
            if ((Request.Urn.ToString().StartsWith("SqlServerCe", StringComparison.OrdinalIgnoreCase)) || 
                (ResultType.Default == Request.ResultType ))
            {
                resultType = ResultType.DataTable;
            }

            // if there is post processing to be done then we return DataTable
            if (this.StatementBuilder.PostProcessList.Count > 0)
            {
                bool needsDataTable = false;
                // See if we can do the post processing directly on the data reader
                // in principle this is possible when data fields are calculated 
                // from other hidden columns
                foreach (DictionaryEntry postProcess in this.StatementBuilder.PostProcessList)
                {
#if false // CC_REMOVE_POSTPROCESS
                    if (postProcess.Value is PostProcessCreateSqlSecureString)
                    {
                        // PostProcessCreateSqlSecureString can be done directly
                        continue;
                    }
                    else if ((postProcess.Value is PostProcessBodyText) &&
                        ((ServerConnection)this.ConnectionInfo).ServerVersion.Major >= 9)
                    {
                        // PostProcessBodyText can be done directly only on 9.0
                        // on 8.0 we need to execute additional queries and paste 
                        // the syscomments fields
                        continue;
                    }
                    else
#endif
                    {
                        needsDataTable = true;
                        break;
                    }
                }

                if (needsDataTable)
                {
                    if (resultType == ResultType.IDataReader)
                    {
                        TraceHelper.Trace("w", SQLToolsCommonTraceLvl.Always,
                            "IDataReader will be returned from a DataTable because post processing is needed");
                    }

                    resultType = ResultType.DataTable;
                }
            }

            Object data = FillDataWithUseFailure(sqlresult, resultType);
            return new EnumResult(data, resultType);
        }

        ///	<summary>
        /// Get data from Sql Server. If we fail to get into a database then ignore that database</summary>
        protected Object FillDataWithUseFailure(SqlEnumResult sqlresult, ResultType resultType)
        {
            StringCollection sql = sqlresult.BuildSql();
            try
            {
                return FillData(resultType, sql, this.ConnectionInfo, sqlresult.StatementBuilder);
            }
            catch(ExecutionFailureException efe)
            {
                SqlException e = efe.InnerException as SqlException;
                //we want:
                //Server: Msg 911, Level 16, State 1, Line 1
                //Could not locate entry in sysdatabases for database 'foo'. No entry found with that name. Make sure that the name is entered correctly.
                if( null ==  e || e.Class != 16 || e.State != 1 || e.Number != 911 )
                {
                    throw;
                }
                sql.Clear();
                sql.Add(sqlresult.StatementBuilder.GetCreateTemporaryTableSqlConnect("#empty_result"));
                sql.Add("select * from #empty_result\nDROP TABLE #empty_result");
                return FillData(resultType, sql, this.ConnectionInfo, sqlresult.StatementBuilder);
            }
        }

        ///	<summary>
        ///	n queries to prepare 
        ///	and the last one to fill the data</summary>
        protected virtual Object FillData(ResultType resultType, StringCollection sql, Object connectionInfo, StatementBuilder sb)
        {
            if( resultType == ResultType.IDataReader )
            {
                return ExecuteSql.GetDataProvider(sql, connectionInfo, sb);
            }
            else
            {
                TraceHelper.Assert( resultType == ResultType.DataTable || resultType == ResultType.DataSet );
                DataTable dt = ExecuteSql.ExecuteWithResults(sql, connectionInfo, sb);
                if( resultType == ResultType.DataTable )
                {
                    return dt;
                }
                DataSet ds = new DataSet();
                ds.Locale = CultureInfo.InvariantCulture;
                ds.Tables.Add((DataTable)dt);
                return ds;
            }
        }

        ///	<summary>
        /// clear all the conditional tsql which was activated for the fields in this request</summary>
        protected void ClearHits()
        {
            this.ConditionedSqlList.ClearHits();
            this.PropertyLinkList.ClearHits();
            this.PostProcessList.ClearHits();
        }

        ///	<summary>
        ///	name property is used in filter</summary>
        public String AddPropertyForFilter(String name)
        {
            return AddFilterProperty(name);
        }

        /// <summary>	
        /// FilterDecoder reports that a constant is used in filter
        /// gives client a chance to modify it</summary>
        public String AddConstantForFilter(String constantValue)
        {
            //this is NOP for the SQL enumerator
            //call should be optimized out
            return constantValue;
        }


        void AddLinkFields(ArrayList linkfields)
        {
            if( null == linkfields )
            {
                return;
            }
            foreach(LinkField lf in linkfields)
            {
                if( LinkFieldType.Parent == lf.Type )
                {
                    m_LinkFields.Add(lf);
                }
            }
        }

        ///<summary>
        /// Returns true if we are in the process of building
        /// a parent request
        /// Returns false if we are building a result</summary>
        bool CalculateParentRequest
        {
            get { return null != m_LinkFields; }
        }

        ///	<summary>
        /// resolve all links which can be resolved at the local level</summary>
        protected void ResolveLocalLinkLinks()
        {
            //ResolveLocalParentLinkLinks
            if (null != this.ParentLink)
            {
                AddLinkProperties(LinkFieldType.Local, this.ParentLink.LinkFields);
            }

            //ResolveLocalPropertyLinkLinks
            foreach (ConditionedSql cs in this.PropertyLinkList)
            {
                AddLinkProperties(LinkFieldType.Local, cs.LinkFields);
            }

            //resolve local prefix links
            foreach (ConditionedSql cs in this.ConditionedSqlList)
            {
                AddLinkProperties(LinkFieldType.Local, cs.LinkFields);
            }

            //ResolveLocalPropertyLinks
            ObjectProperty[] props = GetProperties(ObjectPropertyUsages.Reserved1);
            foreach (SqlObjectProperty p in props)
            {
                AddLinkProperties(LinkFieldType.Local, p.LinkFields);
            }
        }

        void AddPostProcessTriggers()
        {
            foreach(SqlPostProcess p in this.PostProcessList)
            {
                if( true == p.Used )
                {
                    foreach(string field in p.TriggeredFields)
                    {
                        SqlObjectProperty prop = GetSqlProperty(field, ObjectPropertyUsages.Reserved1);
                        AddRequestProperty(prop, true);
                    }
                }
            }
        }

        ///	<summary>
        /// pre initialize the object with the static info. ( resolve local links )</summary>
        protected void StoreInitialState()
        {
            m_sb = null;
            ResolveLocalLinkLinks();
        }

        ///	<summary>
        /// get the object in a clean state remove whatever was changed to resolve the current request</summary>
        protected void RestoreInitialState()
        {
            m_sb = null;
            ClearHits();
            ResolveLocalLinkLinks();
        }

        String GetRequestedParentSelect()
        {
            Request request = new Request();
            request.RequestFieldsTypes = RequestFieldsTypes.Request;
            request.ResultType = ResultType.Reserved2;
            request.Urn = this.Urn.Parent;
            request.Fields = new String[this.RequestParentSelect.Fields.Count];
            this.RequestParentSelect.Fields.CopyTo(request.Fields, 0);

            SqlEnumResult ser = (SqlEnumResult)(new Enumerator().Process(this.ConnectionInfo, request));

            //it's database independent , so is safe
            StatementBuilder sb = ser.StatementBuilder;
            sb.AddStoredProperties();
            sb.ClearPrefixPostfix();
            return sb.SqlStatement;
        }

        void FillPrefixPostfix()
        {
            if( null == this.RequestParentSelect )
            {
                return;
            }

            foreach(String s in this.RequestParentSelect.Fields )
            {
                this.ConditionedSqlList.AddHits(this, s, m_sb);
            }
        }

        ///	<summary>
        ///	a computed field has been used. 
        ///	return its dynamically calculated value</summary>
        internal protected virtual String ResolveComputedField(string fieldName)
        {
            switch(fieldName)
            {
                case "ParentSelect":
                    return GetRequestedParentSelect();
                case "NType":
                    return this.ObjectName;
            }
            return null;
        }

        ///	<summary>
        ///	execute any PostProcess actions</summary>
        public override void PostProcess(EnumResult erChildren)
        {
            RestoreInitialState();
        }

        ///	<summary>
        ///	get the property name acording with the alias rules set in the request</summary>
        internal new string GetAliasPropertyName(string prop)
        {
            return base.GetAliasPropertyName(prop);
        }

        ///	<summary>
        /// add the order by clause when the tsql runs inside one database</summary>
        protected void AddOrderByInDatabase()
        {
            AddOrderByDatabase(false);
        }
        
        ///	<summary>
        /// add the order by clause when the tsql runs across databases</summary>
        protected void AddOrderByAcrossDatabases()
        {
            AddOrderByDatabase(true);
        }

        private void AddOrderByDatabase(bool bAcrossDatabases)
        {
            if( null == Request || null == Request.OrderByList || null == this.StatementBuilder )
            {
                return;
            }
            foreach(OrderBy ob in Request.OrderByList )
            {
                AddOrderByDatabase(ob.Field, ob.Dir, bAcrossDatabases, false);
            }
        }
        
        private void AddOrderByDatabase(string field, OrderBy.Direction dir, bool bAcrossDatabases, bool bHiddenField)
        {
            String fieldName;
            String val = AddOrderByProperty(field, bHiddenField);

            StringCollection scOrderByRedirect = (StringCollection)this.OrderByRedirect[field];
            if( null != scOrderByRedirect )
            {
                foreach(string f in scOrderByRedirect)
                {
                    AddOrderByDatabase(f, dir, bAcrossDatabases, true);
                }
                return;
            }

            if( bAcrossDatabases )
            {
                fieldName = GetAliasPropertyName(field);
                val = fieldName;
            }
            else
            {
                fieldName = field;
            }
            this.StatementBuilder.AddOrderBy(fieldName, val, dir);
        }

        ///	<summary>
        ///	get the filter value the we know for sure the is equal to a fixed constannt value</summary>
        internal string GetFixedFilterValue(string field)
        {
            FilterNodeConstant fnc = (FilterNodeConstant)this.FixedProperties[field];
            if( null == fnc )
            {
                return null;
            }
            if( fnc.ObjType == FilterNodeConstant.ObjectType.String )
            {
                return String.Format(CultureInfo.InvariantCulture, "N'{0}'", fnc.ValueAsString);
            }
            return fnc.ValueAsString;
        }

        public virtual bool SupportsParameterization
        {
            get
            {
                #if !SMOCODEGEN
                return (ServerConnection.ParameterizationMode >= QueryParameterizationMode.ForcedParameterization);
                #else
                return false;
                #endif
    }
        }

    }
}
