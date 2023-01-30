// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public class ConfigurationBase
    {
        internal Server m_server;
        internal ConfigurationBase(Server server)
        {
            m_server = server;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Configuration";
            }
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                return m_server as Server;
            }
        }

        internal DataTable m_table = null;
        internal DataTable ConfigDataTable
        {
            get
            {
                return m_table;
            }
        }

        internal void PopulateDataTable()
        {
            // format the request
            Request req = new Request("Server/Configuration");

            // execute the request
            Enumerator e = new Enumerator();
            m_table = m_server.ExecutionManager.GetEnumeratorData(req);

            m_table.Columns.Add("Value", Type.GetType("System.Int32"));
            m_table.Columns.Add("Updated", Type.GetType("System.Boolean"));

            foreach (DataRow row in m_table.Rows)
            {
                row["Value"] = row["ConfigValue"];
                row["Updated"] = false;
            }
        }

        internal object GetConfigProperty(int iNumber, string sColumnName)
        {
            if (null == ConfigDataTable)
            {
                PopulateDataTable();
            }

            object objValue = null;
            foreach (DataRow row in ConfigDataTable.Rows)
            {
                if (iNumber == (int)row["Number"])
                {
                    objValue = row[sColumnName];
                    break;
                }
            }
            return objValue;
        }

        internal bool SetConfigProperty(int iNumber, int iValue)
        {
            if (null == ConfigDataTable)
            {
                PopulateDataTable();
            }

            bool fUpdated = false;
            foreach (DataRow row in ConfigDataTable.Rows)
            {
                if (iNumber == (int)row["Number"])
                {
                    row["Value"] = iValue;
                    row["Updated"] = true;
                    fUpdated = true;
                    break;
                }
            }
            return fUpdated;
        }

        bool IsRowChanged(DataRow row)
        {
            return (bool)row["Updated"];
        }

        void CleanRow(DataRow row)
        {
            row["Updated"] = false;
            row["ConfigValue"] = row["Value"];
            if (true == (bool)row["Dynamic"])
            {
                row["RunValue"] = row["Value"];
            }
        }

        bool ShowAdvancedOptionsIsSet()
        {
            return 0 != (System.Int32)GetConfigProperty(518, "RunValue");
        }

        string GetSchema()
        {
            return m_server.ServerVersion.Major < 9 ? "master.dbo" : "sys";
        }

        private void ScriptAlterWithStatistics(StringCollection configStrings,
            ref bool bHasChangedOptions,
            ref bool bHasAdvancedOptions,
            ref bool bShowAdvancedOptionsModified)
        {
            foreach (DataRow row in ConfigDataTable.Rows)
            {
                if (IsRowChanged(row))
                {
                    bHasChangedOptions = true;

                    if (true == (bool)row["Advanced"])
                    {
                        bHasAdvancedOptions = true;
                    }
                    //do we try to set 'show advanced options' ?
                    if (518 == (int)row["Number"])
                    {
                        bShowAdvancedOptionsModified = true;
                    }

                    configStrings.Add(string.Format(SmoApplication.DefaultCulture, "EXEC {2}.sp_configure N'{0}', N'{1}'",
                        SqlSmoObject.SqlString((string)row["Name"]),
                        row["Value"].ToString(),
                        GetSchema()
                        ));
                }
            }
        }

        public void Refresh()
        {
            m_table = null;
        }

        internal void DoAlter(bool overrideValueChecking)
        {
            // handle possible configuration options changes if user ever touched the object
            if (null == this.ConfigDataTable)
            {
                return;
            }

            bool bCurrentAdvancedValue = ShowAdvancedOptionsIsSet();
            bool bHasChangedOptions = false;
            bool bHasAdvancedOptions = false;
            bool bShowAdvancedOptionsModified = false;
            bool bMustRestoreShowAdvancedOptions = false;
            StringCollection configStrings = new StringCollection();

            ScriptAlterWithStatistics(configStrings, ref bHasChangedOptions,
                        ref bHasAdvancedOptions, ref bShowAdvancedOptionsModified);

            if (configStrings.Count <= 0)
            {
                return;
            }
            if (!bCurrentAdvancedValue && bHasAdvancedOptions)
            {
                //use with override in case allow updates is 1
                m_server.ExecutionManager.ExecuteNonQuery(
                    "EXEC " + GetSchema() + ".sp_configure N'show advanced options', N'1'  RECONFIGURE WITH OVERRIDE");
                bMustRestoreShowAdvancedOptions = true;
            }

            try
            {
                configStrings.Add(overrideValueChecking ? "RECONFIGURE WITH OVERRIDE" : "RECONFIGURE");
                m_server.ExecutionManager.ExecuteNonQuery(configStrings);
                if (bShowAdvancedOptionsModified)
                {
                    bMustRestoreShowAdvancedOptions = false;
                }
            }
            finally
            {
                //attempt to restore initial state
                if (bMustRestoreShowAdvancedOptions)
                {
                    //use with override in case allow updates is 1
                    m_server.ExecutionManager.ExecuteNonQuery(
                        "EXEC " + GetSchema() + ".sp_configure N'show advanced options', N'0'  RECONFIGURE WITH OVERRIDE");
                }
            }
        }

        internal void CleanObject()
        {
            if (null != this.ConfigDataTable)
            {
                foreach (DataRow row in this.ConfigDataTable.Rows)
                {
                    CleanRow(row);
                }
            }
        }

        internal void ScriptAlter(StringCollection query, ScriptingPreferences sp, bool overrideValueChecking)
        {
            DoAlter(overrideValueChecking);
        }

        public void Alter()
        {
            Alter(false);
        }

        public void Alter(bool overrideValueChecking)
        {
            try
            {
                DoAlter(overrideValueChecking);

                // update object state only if we are in execution mode
                if (!m_server.ExecutionManager.Recording)
                {
                    CleanObject();
                }
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Alter, this, e);
            }
        }
    }
}


