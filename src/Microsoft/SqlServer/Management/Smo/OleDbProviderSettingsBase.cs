// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("OleDbProviderSetting")]
    [SfcBrowsable(false)]
    public partial class OleDbProviderSettings : NamedSmoObject, Cmn.IAlterable, IScriptable
    {
        internal OleDbProviderSettings(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }


        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "OleDbProviderSetting";
            }
        }

        /// <summary>
        /// Name of OleDbProviderSetting
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

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            ScriptProperties(query, sp);
        }

        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            ScriptProperties(query, sp);
        }

        private void ScriptProperties(StringCollection query, ScriptingPreferences sp)
        {
            StringBuilder sbStatement = new StringBuilder();

            InitializeKeepDirtyValues();

            //registry properties
            string[][] REG_PROPS = new string[][] { 
														new string[]{"AllowInProcess", "AllowInProcess", "REG_DWORD"},
														new string[] {"DisallowAdHocAccess", "DisallowAdHocAccess", "REG_DWORD"},
														new string[]{"DynamicParameters", "DynamicParameters", "REG_DWORD"},
														new string[]{"IndexAsAccessPath", "IndexAsAccessPath", "REG_DWORD"},
														new string[]{"LevelZeroOnly", "LevelZeroOnly", "REG_DWORD"},
														new string[]{"NestedQueries", "NestedQueries", "REG_DWORD"},
														new string[]{"NonTransactedUpdates", "NonTransactedUpdates", "REG_DWORD"},
														new string[]{"SqlServerLike", "SqlServerLIKE", "REG_DWORD"},
														new string[]{"", "", ""} };


            Object o = null;
            for (int i = 0; REG_PROPS[i][0].Length > 0; i++)
            {
                Property prop = Properties.Get(REG_PROPS[i][0]);
                if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
                {
                    o = prop.Value;

                    int val = true == (bool)o ? 1 : 0;
                    if (sp.TargetServerVersion >= SqlServerVersion.Version90)
                    {
                        query.Add(string.Format(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_MSset_oledb_prop {0}, {1}, {2}",
                                    MakeSqlString(this.Name), MakeSqlString(REG_PROPS[i][1]), val));
                    }
                    else
                    {
                        if (false == (bool)o)
                        {
                            ScriptDeleteRegSetting(query, REG_PROPS[i]);
                        }
                        else
                        {
                            ScriptRegSetting(query, REG_PROPS[i], val);
                        }
                    }
                }
            }

        }

        void ScriptRegSetting(StringCollection query, string[] prop, Object oValue)
        {
            String sRegWrite = null;
            if (ServerVersion.Major <= 7)
            {
                sRegWrite = "EXEC xp_regwrite N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\Providers\\" +
                    SqlSmoObject.SqlString(this.Name) + "', N'{0}', {1}, {2}";
            }
            else
            {
                sRegWrite = "EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\Providers\\" +
                    SqlSmoObject.SqlString(this.Name) + "', N'{0}', {1}, {2}";
            }

            if ("REG_SZ" == prop[2])
            {
                query.Add(string.Format(SmoApplication.DefaultCulture, sRegWrite, prop[1], prop[2], "N'" + SqlString(oValue.ToString())) + "'");
            }
            else
            {
                query.Add(string.Format(SmoApplication.DefaultCulture, sRegWrite, prop[1], prop[2], oValue.ToString()));
            }
        }

        void ScriptDeleteRegSetting(StringCollection query, string[] prop)
        {
            String sRegDelete = null;
            if (ServerVersion.Major <= 7)
            {
                sRegDelete = "EXEC xp_regdeletevalue N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\Providers\\" +
                    SqlSmoObject.SqlString(this.Name) + "', N'{0}'";
            }
            else
            {
                sRegDelete = "EXEC xp_instance_regdeletevalue N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\Providers\\" +
                    SqlSmoObject.SqlString(this.Name) + "', N'{0}'";
            }

            query.Add(string.Format(SmoApplication.DefaultCulture, sRegDelete, prop[1]));
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

    }
}

