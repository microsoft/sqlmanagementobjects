// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Setting")]
    public partial class Settings : SqlSmoObject, Cmn.IAlterable, IScriptable
    {
        internal Settings(Server parentsrv, ObjectKeyBase key, SqlSmoState state) :
            base(key, state)
        {
            singletonParent = parentsrv as Server;
            SetServerObject(parentsrv.GetServerObject());
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as Server;
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Setting";
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
            //We don't need to script registry entries for cloud.
            //Presently ScriptProperties only script Registry entries.
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                ScriptProperties(query, sp);
            }
        }


        private void ScriptProperties(StringCollection query, ScriptingPreferences sp)
        {
            Initialize(true);

	    StringBuilder sbStatement = new StringBuilder();

            Object o = null;


            foreach (string[] REG_PROPS in Server.RegistryProperties(this.DatabaseEngineType))
            {
                if (REG_PROPS[0].Length == 0)
                {
                    break;
                }
                if (!IsSupportedProperty(REG_PROPS[0], sp))
                {
                    continue;
                }
                Property prop = Properties.Get(REG_PROPS[0]);
                if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
                {
                    o = prop.Value;
                    if ((REG_PROPS[0] == "NumberOfLogFiles" && (int)o < 6) ||
                        ((o is string) && ((String)o).Length == 0))
                    {
                        ScriptDeleteRegSetting(query, REG_PROPS);
                        continue;
                    }

                    if (REG_PROPS[0] == "LoginMode")
                    {
                        ServerLoginMode loginMode = (ServerLoginMode)o;
                        if (loginMode != ServerLoginMode.Integrated &&
                            loginMode != ServerLoginMode.Normal &&
                            loginMode != ServerLoginMode.Mixed)
                        {
                            throw new SmoException(ExceptionTemplates.UnsupportedLoginMode(loginMode.ToString()));
                        }

                        // LoginMode is enumeration, must be converted to its integer value
                        ScriptRegSetting(query, REG_PROPS, Enum.Format(typeof(ServerLoginMode), (ServerLoginMode)o, "d"));
                        continue;
                    }

                    if (REG_PROPS[0] == "AuditLevel")
                    {
                        AuditLevel auditLevel = (AuditLevel)o;
                        if (0 > auditLevel || AuditLevel.All < auditLevel)
                        {
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(AuditLevel).Name));
                        }

                        // auditLevel is enumeration, must be converted to its integer value
                        ScriptRegSetting(query, REG_PROPS, Enum.Format(typeof(AuditLevel), auditLevel, "d"));
                        continue;
                    }


                    if (REG_PROPS[0] == "PerfMonMode")
                    {
                        // Verify that the PerfMonMode set is one of the valid values to set
                        // PerfMonMode.None is not a valid value to set
                        PerfMonMode perfMonMode = (PerfMonMode)o;

                        switch (perfMonMode)
                        {
                            case PerfMonMode.Continuous:
                            case PerfMonMode.OnDemand:
                                // PerfMonMode is enumeration, must be converted to its integer value
                                ScriptRegSetting(query, REG_PROPS, Enum.Format(typeof(PerfMonMode), (PerfMonMode)o, "d"));

                                break;
                            case PerfMonMode.None:
                                // if we get this value we will not script anything, but we need to 
                                // throw an error if this comes from the user 
                                if (sp.ForDirectExecution)
                                {
                                    goto default;
                                }
                                else
                                {
                                    break;
                                }

                            default:
                                throw new SmoException(ExceptionTemplates.UnknownEnumeration(perfMonMode.GetType().Name));
                        }

                        continue;
                    }


                    // This check is for default data/log directory and backup directory.
                    if (REG_PROPS[0] == "DefaultFile" || REG_PROPS[0] == "DefaultLog" || REG_PROPS[0] == "BackupDirectory")
                    {
                        string regpath = (string)o;
                        if (0 == regpath.Length)
                        {
                            // remove the registry key if the string is empty.
                            ScriptDeleteRegSetting(query, REG_PROPS);
                        }
                        else
                        {
                            // strip the final '\' off
                            if (regpath[regpath.Length - 1] == '\\')
                            {
                                regpath = regpath.Remove(regpath.Length - 1, 1);
                            }
                            ScriptRegSetting(query, REG_PROPS, regpath);
                        }

                        continue;
                    }

                    ScriptRegSetting(query, REG_PROPS, o);
                }
            }
        }


        void ScriptRegSetting(StringCollection query, string[] prop, Object oValue)
        {
            String sRegWrite = Scripts.REG_WRITE_WRITE_PROP;
            if (ServerVersion.Major <= 7)
            {
                sRegWrite = Scripts.REG_WRITE_WRITE_PROP70;
            }

            if ("REG_SZ" == prop[2])
            {
                query.Add(string.Format(SmoApplication.DefaultCulture, sRegWrite, prop[1], prop[2], "N'" + SqlString(oValue.ToString())) + "'");
            }
            else if (oValue is System.Boolean)
            {
                query.Add(string.Format(SmoApplication.DefaultCulture, sRegWrite, prop[1], prop[2], ((bool)oValue) ? 1 : 0));
            }
            else
            {
                query.Add(string.Format(SmoApplication.DefaultCulture, sRegWrite, prop[1], prop[2], oValue.ToString()));
            }
        }

        void ScriptDeleteRegSetting(StringCollection query, string[] prop)
        {
            String sRegDelete = Scripts.REG_DELETE;
            if (ServerVersion.Major <= 7)
            {
                sRegDelete = Scripts.REG_DELETE70;
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

        private OleDbProviderSettingsCollection m_OleDbProviderSettings;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(OleDbProviderSettings))]
        public OleDbProviderSettingsCollection OleDbProviderSettings
        {
            get
            {
                CheckObjectState();
                if (null == m_OleDbProviderSettings)
                {
                    m_OleDbProviderSettings = new OleDbProviderSettingsCollection(this);
                }
                return m_OleDbProviderSettings;
            }
        }
    }
}

