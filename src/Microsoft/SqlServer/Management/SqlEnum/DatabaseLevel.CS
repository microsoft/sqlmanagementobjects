// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

    [ComVisible(false)]
    internal class DatabaseLevel : SqlObject, ISupportInitDatabaseEngineData
    {
        string m_PropertyNameForDatabase;
        string m_XmlAssembly;
        bool m_bLastDatabaseLevel;
        bool m_bForChildren;

        public DatabaseLevel()
        {
            m_PropertyNameForDatabase = "Name";
            m_bLastDatabaseLevel = false;
            m_bForChildren = false;
            m_XmlAssembly = null;
        }

        public string NameProperty
        {
            get
            {
                return m_PropertyNameForDatabase;
            }
        }

        public override Request RetrieveParentRequest()
        {
            //if it is in intermediar postion enable optimize 
            if( (ResultType.Reserved2 == this.Request.ResultType ||
                ResultType.Reserved1 == this.Request.ResultType) )
            {
                m_bForChildren = true;
            }
            return base.RetrieveParentRequest();
        }

        public override EnumResult GetData(EnumResult res)
        {
            if( ResultType.Reserved2 == this.Request.ResultType )
            {
                return res;
            }
            //if the request is for actuall data
            //i'm the same as any sql object
            if( ResultType.Reserved1 != this.Request.ResultType )
            {
                return base.GetData(res);
            }

            //for DatabaseOption
            if( false == this.SqlRequest.ResolveDatabases )
            {
                //so that sysdatabases is added
                m_bForChildren = false;
                
                //get data regulary
                return base.GetData(res);
            }

            //here starts the difference compared to other sql objects 
            SqlEnumResult ser = (SqlEnumResult)res;
            
            //filling the .Database field in SqlEnumREsult
            ser.Databases = GetRequestedDatabases(ser);
            ser.NameProperties.Add(this.NameProperty);

            // if properties are not requested from this level 
            // ( as parent properties request or link fields ) 
            // or only database name and derivatives of database name 
            // are requested, clear the filter
            CleanupFilter();

            //we don't need filtering if all it wanted was to select a database.
            SqlObjectProperty prop = GetSqlProperty(this.NameProperty, ObjectPropertyUsages.Request | ObjectPropertyUsages.Reserved1);
            string sValue = prop.GetValue(this);
            StringCollection sc = null;
            int level = ser.Level - 1;

            // we need to make the substitution of the database name property
            if( !m_bLastDatabaseLevel )  //we know if this is the last database level ( is set in config.xml )
            {
                prop.Value = "{" + (2 * level).ToString(CultureInfo.InvariantCulture) + "}";
            }
            else
            {
                //optimization when we know this is the last database level
                prop.Value = "db_name()";
                
                //inform the statement builder that we made this optimization
                ser.LastDbLevelSet = true;
            }

            // if we already have gone trough a database switch
            // must transform all table names in three part names
            if( ser.Level > 1 )
            {
                sc = new StringCollection();
                for(int i = 0; i < this.PropertyLinkList.Count; i++)
                {
                    SqlPropertyLink spl = (SqlPropertyLink)this.PropertyLinkList[i];
                    sc.Add(spl.Table);
                    spl.Table = "[{" + (2 * level - 1).ToString(CultureInfo.InvariantCulture) + "}]." + spl.Table;
                }
            }

            try
            {
                SqlEnumResult resFin = (SqlEnumResult)base.GetData(ser);
                
                //clear fail condition it was already evaluated when the database list was obtained
                resFin.StatementBuilder.ClearFailCondition();
                
                // if we added any tables, add in filter to pinpoint in this database
                int i = 0;
//				for(int i = 0; i < this.PropertyLinkList.Count; i++)
//				{
                    if( this.PropertyLinkList[i].IsUsed )
                    {
                        resFin.StatementBuilder.AddWhere(sValue + "=" + prop.Value);
                    }
//					break;
//				}
                return resFin;
            }
            finally
            {
                //retore Name property value
                prop.Value = sValue;

                //restore table names to two part name
                if( null != sc )
                {
                    for(int i = 0; i < this.PropertyLinkList.Count; i++)
                    {
                        SqlPropertyLink spl = (SqlPropertyLink)this.PropertyLinkList[i];
                        spl.Table = sc[i];
                    }
                }

                m_bForChildren = false;
            }
        }

        protected override String AddLinkProperty(String name)
        {
            if( m_bForChildren && IsDatabaseNameOrDerivate(name) )
            {
                AddConditionalsJustPropDependencies(name);
                SqlObjectProperty p = (SqlObjectProperty)GetProperty(name, ObjectPropertyUsages.Reserved1);
                return p.GetValue(this);
            }
            //calling the regular AddLinkProperty will add joins
            return base.AddLinkProperty(name);
        }

        internal DataTable GetRequestedDatabases(SqlEnumResult serParent)
        {
            Debug.Assert( true == m_bForChildren, "should only be called when this is an intermediate level");

            // if database name is available in the filter use it directly
            string database = this.GetFixedStringProperty(this.NameProperty, removeEscape:true);
            if( null != database )
            {
                DataTable dt = new DataTable();
                dt.Locale = CultureInfo.InvariantCulture;
                DataColumn dc = dt.Columns.Add("Name", System.Type.GetType("System.String"));
                DataRow dr = dt.NewRow();
                dr[0] = database;
                dt.Rows.Add(dr);
                return dt;
            }

            //else query for the list of databases to go into
            Request request = new Request();
            request.RequestFieldsTypes = RequestFieldsTypes.Request;
            request.ResultType = ResultType.DataTable;
            request.Urn = this.Urn;
            request.Fields = new String[] { this.NameProperty };
            int count = serParent.NameProperties.Count;
            if( count > 0 )
            {
                request.ParentPropertiesRequests = new PropertiesRequest[count];
                for(int i = 0; i < count; i++)
                {
                    request.ParentPropertiesRequests[i] = new PropertiesRequest();
                    request.ParentPropertiesRequests[i].Fields = new String [] { serParent.NameProperties[count - i - 1] };
                }
            }
            return new Enumerator().Process(this.ConnectionInfo, request);
        }

        //i expect an xml node with 3 things:
        // 1. db_name: PropertyNameForDatabaseName
        // 2. assemly: xml implementation file assembly optional ( default current assembly )
        // 3. xml: xml file name
        // 4. last: bool, optimization hint, informs if this is the last level requiring 
        //			a database switch
        public new void LoadInitData(String init_data, ServerVersion ver,DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
        {
            XmlDocument x = new XmlDocument();
            x.LoadXml("<d " + init_data + " />");
            XmlElement root = x.DocumentElement;

            XmlAttribute a = root.Attributes["db_name"];
            if( null != a )
            {
                m_PropertyNameForDatabase = a.Value;
            }

            a = root.Attributes["xml"];
            string xmlFile = null;
            if( null != a )
            {
                xmlFile = a.Value;
            }

            a = root.Attributes["assemly"];
            if( null != a )
            {
                m_XmlAssembly = a.Value;
            }

            a = root.Attributes["last"];
            if( null != a )
            {
                m_bLastDatabaseLevel = true;
            }

            base.LoadInitData(xmlFile, ver, databaseEngineType, databaseEngineEdition);
        }

        ///<summary>
        /// returns true if the field is 
        /// the database name or derived from it
        ///</summary>
        private bool IsDatabaseNameOrDerivate(string fieldName)
        {
            Debug.Assert( true == m_bForChildren, "should only be called when this is an intermediate level");
                        
            //check if it is the database name
            if( fieldName == this.NameProperty )
            {
                return true;
            }

            //
            // if it is multiple_link it should only use DatabaseName
            // if it is not link multiple than it is probably a column
            // , not a composite of DatabaseName
            //
            
            SqlObjectProperty prop = GetSqlProperty(fieldName, ObjectPropertyUsages.Reserved1);
            if( null == prop.LinkFields ) 
            {
                return false;
            }

            foreach(LinkField lf in prop.LinkFields)
            {
                if( LinkFieldType.Local == lf.Type && 
                    this.NameProperty != lf.Field )
                {
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// cleans database filter to avoid a join if only DatabaseName
        /// and Urn which we assume is composed from DatabaseName are requested
        ///</summary>
        private void CleanupFilter()
        {
            Debug.Assert( true == m_bForChildren, "should only be called when this is an intermediate level");
            
            if( ( null == this.SqlRequest.Fields || 0 == this.SqlRequest.Fields.Length || 2 > this.SqlRequest.Fields.Length )
                && ( null == this.SqlRequest.LinkFields || 0 == this.SqlRequest.LinkFields.Count || 2 > this.SqlRequest.LinkFields.Count ) )
            {
                this.Filter = null;
                return;
            }
            
            if( null != this.SqlRequest.Fields )
            {
                foreach(string s in this.SqlRequest.Fields)
                {
                    if( !IsDatabaseNameOrDerivate(s) )
                    {
                        goto debug_info;
                    }
                }
            }
            else if( null != this.SqlRequest.LinkFields )
            {
                foreach(LinkField f in this.SqlRequest.LinkFields)
                {
                    if( !IsDatabaseNameOrDerivate(f.Field) )
                    {
                        goto debug_info;
                    }
                }
            }
            this.Filter = null;
            return;
            
debug_info:;
#if DEBUG
            //report why the filter was not cleaned
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
            if( null != this.SqlRequest.LinkFields )
            {
                sLinkFields = this.SqlRequest.LinkFields.Count.ToString();
                sLinkFields += ": ";
                foreach(LinkField f in this.SqlRequest.LinkFields)
                { 
                    sLinkFields += f.Field;
                    sLinkFields += " ";
                }
            }

            Enumerator.TraceInfo(
                "DATABASELEVEL: filter not cleaned. LevelName = {0}, Fields.Length = <{1}>, LinkFields.Count = <{2}>",
                this.Name, sFields, sLinkFields);
#endif
        }

        public override Assembly ResourceAssembly
        {
            get
            {
                if( null == m_XmlAssembly )
                {
                    return base.ResourceAssembly;
                }
                return Util.LoadAssembly(m_XmlAssembly);
            }
        }

        public override void PostProcess(EnumResult erChildren)
        {
            m_bForChildren = false;

            base.PostProcess(erChildren);
        }
    }
}
