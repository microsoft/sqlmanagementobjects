// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class DdlTriggerBase : ScriptNameObjectBase, Cmn.ICreatable, Cmn.ICreateOrAlterable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable, ITextObject
    {
        internal DdlTriggerBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        internal protected DdlTriggerBase() : base() { }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptTrigger(queries, sp, ScriptHeaderType.ScriptHeaderForCreate);
        }

        /// <summary>
        /// Create OR ALTER the object. First perform an internal check for existence. If object exists, take CREATE path; Otherwise take ALTER path.
        /// return without exception.
        /// </summary>
        public void CreateOrAlter()
        {
            base.CreateOrAlterImpl();
        }

        internal override void ScriptCreateOrAlter(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptTrigger(queries, sp, ScriptHeaderType.ScriptHeaderForCreateOrAlter);
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

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            if (IsObjectDirty())
            {
                ScriptTrigger(alterQuery, sp, ScriptHeaderType.ScriptHeaderForAlter);
            }
        }

        bool ShouldScriptBodyAtAlter()
        {
            if (IsEventSetDirty())
            {
                return true;
            }

            if (GetIsTextDirty())
            {
                return true;
            }
            StringCollection sc = new StringCollection();
            sc.Add("AnsiNullsStatus");
            sc.Add("QuotedIdentifierStatus");
            sc.Add("IsEncrypted");
            sc.Add("ExecutionContext");

            if (this is DatabaseDdlTrigger)
            {
                sc.Add("ExecutionContextUser");
            }
            else if (this is ServerDdlTrigger)
            {
                sc.Add("ExecutionContextLogin");
            }

            sc.Add("ImplementationType");
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                sc.Add("AssemblyName");
                sc.Add("ClassName");
                sc.Add("MethodName");
            }

            if (this.Properties.ArePropertiesDirty(sc))
            {
                return true;
            }
            return false;
        }

        protected virtual bool IsEventSetDirty()
        {
            return false;
        }

        private bool GetInsteafOfValue(ScriptingPreferences sp)
        {
            return false;
        }

        private void ScriptTrigger(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersion,
                ExceptionTemplates.DdlTriggerDownlevel(
                    FormatFullNameForScripting(sp, true),
                    GetSqlServerName(sp)));

            if (IsCreate(scriptHeaderType) || ShouldScriptBodyAtAlter())
            {
                GetInternalDDL(queries, sp, scriptHeaderType);
            }

            if (!sp.IncludeScripts.CreateDdlTriggerDisabled)
            {
                // ENABLE/DISABLE the trigger
                Property isEnabledProp = null;
        if (this.State == SqlSmoState.Creating || this.IsDesignMode)
                {
                    isEnabledProp = Properties.Get("IsEnabled");
                }
                else
                {
                    isEnabledProp = Properties["IsEnabled"];
                }

                if (isEnabledProp.Value != null && (isEnabledProp.Dirty || !sp.ScriptForAlter))
                {
                    queries.Add(ScriptEnableDisableCommand((bool)isEnabledProp.Value, sp));
                }
            }
            else
            {
                queries.Add(ScriptEnableDisableCommand(false, sp));
            }
        }

        internal string ScriptEnableDisableCommand(bool isEnabled, ScriptingPreferences sp)
        {
            return string.Format(SmoApplication.DefaultCulture, "{0} TRIGGER {1} ON {2}",
                            isEnabled ? "ENABLE" : "DISABLE",
                            FormatFullNameForScripting(sp),
                            (this is DatabaseDdlTrigger) ? "DATABASE" : "ALL SERVER");
        }

        abstract internal string GetIfNotExistStatement(ScriptingPreferences sp, string prefix);

        protected override bool CheckObjectDirty()
        {
            if (this is DatabaseDdlTrigger && this.IsObjectDirty())
            {
                foreach (Property p in this.Properties)
                {
                    if (p.Name != "IsEnabled" && p.Dirty)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember")]
        private void GetInternalDDL(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            bool bCreate = IsCreate(scriptHeaderType);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            bool fAnsiNullsExists = false;
            bool fQuotedIdentifierExists = false;

            string sTriggerName = FormatFullNameForScripting(sp);

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                if (sp.IncludeScripts.Header) // need to generate commentary headers
                {
                    string urnsuffix = this.GetType().InvokeMember("UrnSuffix",
                        UrnSuffixBindingFlags,
                        null, null, new object[] { }, SmoApplication.DefaultCulture) as string;

                    sb.Append(ExceptionTemplates.IncludeHeader(
                        urnsuffix, sTriggerName, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                fAnsiNullsExists = (null != Properties.Get("AnsiNullsStatus").Value);
                fQuotedIdentifierExists = (null != Properties.Get("QuotedIdentifierStatus").Value);

                if (fAnsiNullsExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_ANSI_NULLS,(bool)Properties["AnsiNullsStatus"].Value?Globals.On:Globals.Off);
                    queries.Add( sb.ToString() );
                    sb.Length = 0;
                }

                if (fQuotedIdentifierExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_QUOTED_IDENTIFIER, (bool)Properties["QuotedIdentifierStatus"].Value ? Globals.On : Globals.Off);
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                }
            }

            Property property;
            bool bTransactSql = true;
            if (null != (property = this.Properties.Get("ImplementationType")).Value)
            {
                if (ImplementationType.SqlClr == (ImplementationType)property.Value)
                {
                    // CLR triggers are not supported on versions prior to 9.0
                    if (ServerVersion.Major < 9)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.ClrNotSupported("ImplementationType", ServerVersion.ToString()));
                    }

                    bTransactSql = false;

                    if (this.Properties.Get("Text").Dirty)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.NoPropertyChangeForDotNet("TextBody"));
                    }
                }
            }

            if (false == this.TextMode)
            {
                StringBuilder sbSpExec = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                if (!sp.OldOptions.DdlBodyOnly)
                {
                    bool bNeedsComma = false;
                    if (bCreate && sp.IncludeScripts.ExistenceCheck)
                    {
                        sb.AppendLine(GetIfNotExistStatement(sp, "NOT"));
                        sb.AppendLine("EXECUTE dbo.sp_executesql N'");
                    }

                    switch (scriptHeaderType)
                    {
                        case ScriptHeaderType.ScriptHeaderForCreate:
                            sbSpExec.AppendFormat(SmoApplication.DefaultCulture, "{0} TRIGGER {1}", Scripts.CREATE, sTriggerName);
                            break;
                        case ScriptHeaderType.ScriptHeaderForAlter:
                            sbSpExec.AppendFormat(SmoApplication.DefaultCulture, "{0} TRIGGER {1}", Scripts.ALTER, sTriggerName);
                            break;
                        case ScriptHeaderType.ScriptHeaderForCreateOrAlter:
                            ThrowIfCreateOrAlterUnsupported(sp.TargetServerVersion,
                                ExceptionTemplates.CreateOrAlterDownlevel(
                                    "DDL Trigger",
                                    GetSqlServerName(sp)));

                            sbSpExec.AppendFormat(SmoApplication.DefaultCulture, "{0} TRIGGER {1}", Scripts.CREATE_OR_ALTER, sTriggerName);
                            break;
                        default:
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(scriptHeaderType.ToString()));
                    }

                    sbSpExec.AppendFormat(SmoApplication.DefaultCulture, " ON {0}", (this is DatabaseDdlTrigger) ? "DATABASE" : "ALL SERVER");

                    if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType)
                        && bTransactSql)
                    {
                        object objEncrypted = Properties.Get("IsEncrypted").Value;
                        if( null != objEncrypted && true == (bool)objEncrypted )
                        {
                            sbSpExec.Append(" WITH ENCRYPTION ");
                            bNeedsComma = true;
                        }
                    }

                    if (ServerVersion.Major >= 9 && sp.TargetServerVersion >= SqlServerVersion.Version90)
                    {
                        sbSpExec.Append(" ");
                        if (this is ServerDdlTrigger)
                        {
                            AddScriptServerDdlExecuteAs(sbSpExec, sp, this.Properties, ref bNeedsComma);
                        }
                        else if (this is DatabaseDdlTrigger)
                        {
                            AddScriptDatabaseDdlExecuteAs(sbSpExec, sp, this.Properties, ref bNeedsComma);
                        }
                    }

                    sbSpExec.Append(Globals.newline);
                    sbSpExec.Append(" FOR ");

                    AddDdlTriggerEvents(sbSpExec, sp);

                    sbSpExec.Append(" AS ");
                    sbSpExec.Append(sp.NewLine);
                }

                if (!sp.OldOptions.DdlHeaderOnly)
                {
                    string tempString;

                    if (bTransactSql)
                    {
                        if (bCreate && sp.IncludeScripts.ExistenceCheck)
                        {
                            sbSpExec.Append(GetTextBody(true));
                            sbSpExec.Append(sp.NewLine);
                            sb.Append(SqlString(sbSpExec.ToString()));
                            sb.Append("'");
                        }
                        else
                        {
                            sb.Append(sbSpExec.ToString());
                            sb.Append(GetTextBody(true));
                        }
                    }
                    else
                    {
                        if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
                        {
                            sbSpExec.Append(" EXTERNAL NAME ");

                            tempString = (string)this.GetPropValue("AssemblyName");
                            if (string.Empty == tempString)
                            {
                                throw new PropertyNotSetException("AssemblyName");
                            }

                            sbSpExec.AppendFormat("[{0}]", SqlBraket(tempString));

                            tempString = (string)this.GetPropValue("ClassName");
                            if (string.Empty == tempString)
                            {
                                throw new PropertyNotSetException("ClassName");
                            }

                            sbSpExec.AppendFormat(".[{0}]", SqlBraket(tempString));

                            tempString = (string)this.GetPropValue("MethodName");
                            if (string.Empty == tempString)
                            {
                                throw new PropertyNotSetException(tempString);
                            }

                            sbSpExec.AppendFormat(".[{0}]", SqlBraket(tempString));
                        }


                        if (bCreate && sp.IncludeScripts.ExistenceCheck)
                        {
                            sb.Append(SqlString(sbSpExec.ToString()));
                            sb.Append("'");
                        }
                        else
                        {
                            sb.Append(sbSpExec.ToString());
                        }
                    }
                    sb.Append(sp.NewLine);
                }
            }
            else
            {

                string body = GetTextForScript(
                                    sp,
                                    new String[] { "trigger" },
                                    forceCheckNameAndManipulateIfRequired: false,
                                    scriptHeaderType: scriptHeaderType);

                if (bCreate && sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendLine(GetIfNotExistStatement(sp, "NOT"));
                    sb.AppendLine("EXECUTE dbo.sp_executesql N'");
                    sb.Append(SqlString(body));
                    sb.Append("'");
                }
                else
                {
                    sb.Append(body);
                }
            }

            queries.Add(sb.ToString());
            sb.Length = 0;
        }

        internal virtual void AddDdlTriggerEvents(StringBuilder sb, ScriptingPreferences sp)
        {
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

        #region TextModeImpl

        public string ScriptHeader(bool forAlter)
        {
            CheckObjectState();
            return GetTextHeader(forAlter);
        }

        public string ScriptHeader(ScriptHeaderType scriptHeaderType)
        {
            CheckObjectState();
            return GetTextHeader(scriptHeaderType);
        }


        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone)]
        public virtual string TextBody
        {
            get { CheckObjectState(); return GetTextBody(); }
            set { CheckObjectState(); SetTextBody(value); }
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone)]
        public virtual string TextHeader
        {
            get { CheckObjectState(); return GetTextHeader(false); }
            set { CheckObjectState(); SetTextHeader(value); }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public virtual bool TextMode
        {
            get { CheckObjectState(); return GetTextMode(); }
            set { CheckObjectState(); SetTextMode(value, null); }
        }

        #endregion
    }
}

