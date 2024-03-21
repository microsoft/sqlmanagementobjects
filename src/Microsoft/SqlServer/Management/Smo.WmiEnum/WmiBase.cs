namespace Microsoft.SqlServer.Management.Smo.Wmi
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Management;
    using System.Data;
    using Microsoft.SqlServer.Management.Common;
    #if STRACE
    using Microsoft.SqlServer.Management.Diagnostics;
    #endif
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Diagnostics = Microsoft.SqlServer.Management.Diagnostics;

    internal class WmiBase : EnumObject, ISqlFilterDecoderCallback
    {
        string m_ClassName;
        StatementBuilder m_sb;

        public WmiBase(string className)
        {
            m_ClassName = className;
        }

        protected StatementBuilder StatementBuilder
        {

            get { return m_sb; }
        }

        public override ResultType[] ResultTypes
        {
            get	{return new ResultType[2] { ResultType.DataSet, ResultType.DataTable };	}
        }

        private WmiProperty GetPropertyForRequest(string propName)
        {
            WmiProperty wp = null;
            if( UseReservedFields )
            {
                wp = (WmiProperty)GetProperty(propName, ObjectPropertyUsages.Reserved1);
            }
            else
            {
                //test property available for filter
                wp = (WmiProperty)GetProperty(propName, ObjectPropertyUsages.Request);
            }
            return wp;
        }

        private string GetPhysicalNameForRequest(string propName)
        {
            return GetPropertyForRequest(propName).PhysicalName;
        }

        private void AddPropertyForRequest(string propName)
        {
            m_sb.AddFields(GetPhysicalNameForRequest(propName));
        }

        /// <summary>	
        /// FilterDecoder reports that the property name is used in filter
        /// and requests its physical name</summary>
        String ISqlFilterDecoderCallback.AddPropertyForFilter(String propName)
        {
            WmiProperty wp = null;
            if( UseReservedFields )
            {
                wp = (WmiProperty)GetProperty(propName, ObjectPropertyUsages.Reserved1);
            }
            else
            {
                //test property available for filter
                wp = (WmiProperty)GetProperty(propName, ObjectPropertyUsages.Filter);
            }

            return wp.PhysicalName;
            
        }

        /// <summary>	
        /// FilterDecoder reports that a constant is used in filter
        /// gives client a chance to modify it</summary>
        String ISqlFilterDecoderCallback.AddConstantForFilter(String constantValue)
        {
            //WMI wants us to escape the '\'
            // replace all occurences of '\' with '\\'
            return constantValue.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        bool ISqlFilterDecoderCallback.SupportsParameterization
        {
            get
            {
                return false;
            }
        }

        protected string GetXpathFilter()
        {
            FilterDecoder fd = new FilterDecoder((ISqlFilterDecoderCallback)this);
            fd.StringPrefix = string.Empty;
            return fd.GetSql(Filter);
        }
        

        //add the filter requested in XPATH
        void AddXpathFilter()
        {
            String strXPathFilter = GetXpathFilter();
            if( 0 != strXPathFilter.Length  )			// add filter from the filter expression in xpath
                m_sb.AddWhere(strXPathFilter);
        }

        protected virtual void BuildStatementBuilder()
        {
            foreach(string propName in this.Request.Fields)
            {
                AddPropertyForRequest(propName);
            }

            m_sb.AddFrom(m_ClassName);

            AddXpathFilter();
        }

        private bool UseReservedFields
        {
            get
            {
                WmiRequest w = this.Request as WmiRequest;
                if( null != w && null != w.Fields )
                {
                    return true;
                }
                return false;
            }
        }

        public override EnumResult GetData(EnumResult erParent)
        {
            m_sb = new StatementBuilder();

            BuildStatementBuilder();

            ManagementObjectSearcher searcher =	new ManagementObjectSearcher();

            WmiMgmtScopeConnection wmiconn = this.ConnectionInfo as WmiMgmtScopeConnection;
            if( null != wmiconn && null != wmiconn.ManagementScope )
                searcher.Scope = wmiconn.ManagementScope;
            else
            {
                searcher.Scope = this.ConnectionInfo as ManagementScope;
            }

            if( null == searcher.Scope )
            {
                searcher.Scope = new ManagementScope(((WmiEnumResult)erParent).Scope);
            }

#if STRACE
            STrace.Trace("w", Diagnostics.SQLToolsCommonTraceLvl.Always, String.Format(CultureInfo.InvariantCulture, "query:\n{0}\n", m_sb.SqlStatement));			
#endif
            searcher.Query = new WqlObjectQuery(m_sb.SqlStatement);

            return BuildResult(searcher.Get());
        }

        protected virtual object GetTranslatedValue(ManagementObject mo, string propPhysName)
        {
            return mo[propPhysName];
        }

        protected DataTable BuildDataTable(ManagementObjectCollection listManagementObject)
        {
            DataTable dt = new DataTable();
            dt.Locale = CultureInfo.InvariantCulture;

            foreach(string propName in this.Request.Fields)
            {
                dt.Columns.Add(new DataColumn(propName, System.Type.GetType(GetPropertyForRequest(propName).BaseType)));
            }
            foreach (ManagementObject mo in listManagementObject) 
            {
                DataRow row = dt.NewRow();
                foreach(string propName in this.Request.Fields)
                {
                    object obj = GetTranslatedValue(mo,GetPhysicalNameForRequest(propName)) ;
                    row[propName] = obj != null ? obj : DBNull.Value;
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        private EnumResult BuildResult(ManagementObjectCollection listManagementObject)
        {
            ResultType res = Request.ResultType;
            if( ResultType.Default == res )
                res = ResultType.DataSet;

            DataTable dt = BuildDataTable(listManagementObject);

            if( ResultType.DataSet == res )
            {
                DataSet ds = new DataSet();
                ds.Locale = CultureInfo.InvariantCulture;
                ds.Tables.Add(dt);
                return new EnumResult(ds, res);
            }
            else if( ResultType.DataTable == res )
            {
                return new EnumResult(dt, res);
            }
            throw new ResultTypeNotSupportedEnumeratorException(res);
        }

        public override Request RetrieveParentRequest()
        {
            return new WmiRequest();
        }
    }
}
