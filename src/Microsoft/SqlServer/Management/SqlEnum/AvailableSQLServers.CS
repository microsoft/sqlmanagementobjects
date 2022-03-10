// Copyright (c) Microsoft.
// Licensed under the MIT license.

#if !NETSTANDARD2_0 && !NETCOREAPP
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.SqlEnum;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Data.Sql;
using System.Runtime.InteropServices;

namespace Microsoft.SqlServer.Management.Smo
{
    [Obsolete]
    [ComVisible(false)]
    internal class AvailableSQLServers : EnumObject
    {
        AvailableSQLServers()
        {
            ObjectProperty op = new ObjectProperty();
            op.Name = "Name";
            op.Type = "System.String";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Request | ObjectPropertyUsages.Filter;
            AddProperty(op);

            op = new ObjectProperty();
            op.Name = "Server";
            op.Type = "System.String";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Request;
            AddProperty(op);

            op = new ObjectProperty();
            op.Name = "Instance";
            op.Type = "System.String";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Request;
            AddProperty(op);

            op = new ObjectProperty();
            op.Name = "IsClustered";
            op.Type = "System.Boolean";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Request;
            AddProperty(op);

            op = new ObjectProperty();
            op.Name = "Version";
            op.Type = "System.String";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Request;
            AddProperty(op);

            op = new ObjectProperty();
            op.Name = "IsLocal";
            op.Type = "System.Boolean";
            op.ReadOnly = true;
            op.Usage = ObjectPropertyUsages.Request | ObjectPropertyUsages.Filter;
            AddProperty(op);
        }

        /// <summary>
        /// return what types of results does this object support
        /// </summary>
        public override ResultType[] ResultTypes
        {
            get { return new ResultType [] { ResultType.DataTable }; }
        }

        public override EnumResult GetData(EnumResult res)
        {
            //get list of servers
            DataTable dt = SqlDataSourceEnumerator.Instance.GetDataSources();


            //build result DataTable
            DataTable dtResult = new DataTable();
            dt.Locale = System.Globalization.CultureInfo.InvariantCulture;
            foreach(string s in this.Request.Fields)
            {
                ObjectProperty prop = GetProperty(s, ObjectPropertyUsages.Request);
                DataColumn col = dtResult.Columns.Add(s, Type.GetType(prop.Type));
            }

            //retrieve filters
            String nameFilter = this.GetFixedStringProperty("Name", true);
            FilterNodeFunction fnf = this.FixedProperties["IsLocal"] as FilterNodeFunction;
            bool isLocalSet = false;
            bool isLocalFilter = false;
            if( null != fnf )
            {
                if( fnf.FunctionType == FilterNodeFunction.Type.True )
                {
                    isLocalFilter = true;
                    isLocalSet = true;
                }
                else if( fnf.FunctionType == FilterNodeFunction.Type.False )
                {
                    isLocalSet = true;
                }
            }

            //start filling the result set
            foreach(DataRow row in dt.Rows)
            {
                string instance = row["InstanceName"].ToString();
                if( null != instance && (instance.Length <= 0 || "MSSQLSERVER" == instance) )
                {
                    instance = null;
                }
                string name = row["ServerName"].ToString() + ( null != instance ? "\\" + instance : "" );
                //check filters
                if (null != nameFilter && 0 != String.Compare(name, nameFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                bool isLocal = 0 == String.Compare(row["ServerName"].ToString(), System.Environment.MachineName, StringComparison.OrdinalIgnoreCase);
                if (isLocalSet && isLocalFilter != isLocal)
                {
                    continue;
                }
                DataRow rowResult = dtResult.NewRow();
                foreach(string s in this.Request.Fields)
                {
                    switch(s)
                    {
                        case "Name":rowResult[s] = name;; break;
                        case "IsLocal":rowResult[s] = isLocal; break;
                        case "Server":rowResult[s] = row["ServerName"]; break;
                        case "Instance":rowResult[s] = instance; break;
                        case "IsClustered":rowResult[s] = "No" == row["IsClustered"].ToString() ? false : true; break;
                        case "Version":rowResult[s] = row["Version"]; break;
                    }
                }
                dtResult.Rows.Add(rowResult);
            }

            //get the result to the user
            return new EnumResult(dtResult, ResultType.DataTable);
        }
    }
}
#endif