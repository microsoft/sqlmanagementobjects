// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo.Agent;
using Cmn = Microsoft.SqlServer.Management.Common;


// History
// Script trigger order correctly (bug 411947)
// IfNotExist not supported (bug 403891)

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet(PhysicalFacetOptions.ReadOnly)]
    public partial class Trigger : ScriptNameObjectBase, Cmn.ICreatable, Cmn.ICreateOrAlterable, Cmn.IAlterable,
        Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IMarkForDrop,
        IExtendedProperties, IScriptable, ITextObject
    {
        internal Trigger(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState eState) :
            base(parentColl, key, eState)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Trigger";
            }
        }

        internal override string[] GetNonAlterableProperties()
        {
            if (this.ParentColl.ParentInstance is Table)
            {
                return new string[] { };
            }
            else
            {
                return new string[] { "InsteadOf" };
            }
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
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

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptDdl(queries, sp);
        }

        internal override void ScriptDdl(StringCollection queries, ScriptingPreferences sp)
        {
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

            GetInternalDDL(queries, sp, ScriptHeaderType.ScriptHeaderForCreateOrAlter);
            GetExternalDDL(queries, sp, ScriptHeaderType.ScriptHeaderForCreateOrAlter);
        }

        private string GetIfNotExistString(bool forCreate, ScriptingPreferences sp)
        {
            // perform check for existing object
            string checkString;
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                checkString = Scripts.INCLUDE_EXISTS_TRIGGER90;
            }
            else
            {
                checkString = Scripts.INCLUDE_EXISTS_TRIGGER80;
            }

            return String.Format(SmoApplication.DefaultCulture, checkString, forCreate ? "NOT" : "", SqlString(FormatFullNameForScripting(sp)));
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

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string sFullScriptingName = FormatFullNameForScripting(sp);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.AppendLine(GetIfNotExistString( /* forCreate = */ false, sp));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP TRIGGER {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty,
                sFullScriptingName);
            queries.Add(sb.ToString());
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

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        internal override string FormatFullNameForScripting(ScriptingPreferences sp)
        {
            CheckObjectState();
            // format full object name for scripting
            string sFullNameForScripting = String.Empty;
            if (sp.IncludeScripts.SchemaQualify) // pre-qualify object name with an owner name
            {
                string schema = ((TableViewBase)ParentColl.ParentInstance).GetSchema(sp);

                if (schema.Length > 0)
                {
                    sFullNameForScripting = MakeSqlBraket(schema);
                    sFullNameForScripting += Globals.Dot;
                }
            }
            sFullNameForScripting += base.FormatFullNameForScripting(sp);

            return sFullNameForScripting;
        }

        bool ShouldScriptBodyAtAlter()
        {
            if( GetIsTextDirty() )
            {
                return true;
            }
            StringCollection sc = new StringCollection();
            sc.Add("AnsiNullsStatus");
            sc.Add("QuotedIdentifierStatus");
            sc.Add("InsteadOf");
            sc.Add("IsEncrypted");
            sc.Add("Insert");
            sc.Add("Delete");
            sc.Add("Update");
            sc.Add("NotForReplication");
            sc.Add("ImplementationType");

            if( this.ServerVersion.Major >= 9 )
            {
                sc.Add("ExecutionContext");
                sc.Add("ExecutionContextPrincipal");
                if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                {
                    sc.Add("AssemblyName");
                    sc.Add("ClassName");
                    sc.Add("MethodName");
                }
            }

            if (this.Properties.ArePropertiesDirty(sc))
            {
                return true;
            }
            return false;
        }

        private bool GetInsteafOfValue(ScriptingPreferences sp)
        {
            object objInsteadOf = null;
            if (State == SqlSmoState.Creating || this.IsDesignMode)
            {
                objInsteadOf = Properties.Get("InsteadOf").Value;
            }
            else if (SqlServerVersionInternal.Version70 != sp.TargetServerVersionInternal) // Sphinx server does not support this property
            {
                objInsteadOf = Properties["InsteadOf"].Value;
            }

            bool bInsteadOf;
            if (null == objInsteadOf)
            {
                bInsteadOf = false;
            }
            else
            {
                bInsteadOf = (bool)objInsteadOf;
            }
            return bInsteadOf;
        }

        private void ScriptTrigger(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            if (IsCreate(scriptHeaderType) || ShouldScriptBodyAtAlter())
            {
                GetInternalDDL(queries, sp, scriptHeaderType);
            }
            GetExternalDDL(queries, sp, scriptHeaderType);
        }


        private void GetInternalDDL(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            bool bCreate = IsCreate(scriptHeaderType);

            bool bInsteadOf = GetInsteafOfValue(sp);

            //INSTEAD OF triggers are not supported on 7.0
            if (SqlServerVersionInternal.Version70 == sp.TargetServerVersionInternal && true == bInsteadOf)
            {
                throw new SmoException(ExceptionTemplates.TriggerNotSupported(sp.TargetServerVersionInternal.ToString()));
            }

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            bool fAnsiNullsExists = false;
            bool fQuotedIdentifierExists = false;
            bool fServerAnsiNulls = false;
            bool fServerQuotedIdentifier = false;

            string sTriggerName = FormatFullNameForScripting(sp);

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                if (sp.IncludeScripts.Header) // need to generate commentary headers
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, sTriggerName, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                fAnsiNullsExists = (null != Properties.Get("AnsiNullsStatus").Value);
                fQuotedIdentifierExists = (null != Properties.Get("QuotedIdentifierStatus").Value);

                if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                {
                    // save server settings first
                    Server svr = (Server)ParentColl.ParentInstance.ParentColl.ParentInstance.ParentColl.ParentInstance;
                    fServerAnsiNulls = (bool)svr.UserOptions.AnsiNulls;
                    fServerQuotedIdentifier = (bool)svr.UserOptions.QuotedIdentifier;
                }

                if (fAnsiNullsExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_ANSI_NULLS, (bool)Properties["AnsiNullsStatus"].Value ? Globals.On : Globals.Off);
                    queries.Add(sb.ToString());
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
                // if we are calling sp_executesql we need to escape
                // strings as well as identifiers
                bool escapeString = (bCreate && sp.IncludeScripts.ExistenceCheck);
                StringBuilder sbTmp = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                if (!sp.OldOptions.DdlBodyOnly)
                {
                    if (escapeString)
                    {
                        sb.AppendLine(GetIfNotExistString( /* forCreate = */ true, sp));
                        sb.AppendLine("EXECUTE dbo.sp_executesql N'");
                    }

                    switch (scriptHeaderType)
                    {
                        case ScriptHeaderType.ScriptHeaderForCreate:
                            sbTmp.AppendFormat(SmoApplication.DefaultCulture, "{0} TRIGGER {1}", Scripts.CREATE, sTriggerName);
                            break;
                        case ScriptHeaderType.ScriptHeaderForAlter:
                            sbTmp.AppendFormat(SmoApplication.DefaultCulture, "{0} TRIGGER {1}", Scripts.ALTER, sTriggerName);
                            break;
                        case ScriptHeaderType.ScriptHeaderForCreateOrAlter:
                            ThrowIfCreateOrAlterUnsupported(sp.TargetServerVersionInternal,
                                ExceptionTemplates.CreateOrAlterDownlevel(
                                    "Trigger",
                                    GetSqlServerName(sp)));

                            sbTmp.AppendFormat(SmoApplication.DefaultCulture, "{0} TRIGGER {1}", Scripts.CREATE_OR_ALTER, sTriggerName);
                            break;
                        default:
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(scriptHeaderType.ToString()));
                    }

                    sbTmp.AppendFormat(SmoApplication.DefaultCulture, " ON {0} ", ((TableViewBase)ParentColl.ParentInstance).FormatFullNameForScripting(sp));

                    bool bNeedsComma = false;
                    if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType)
                        && bTransactSql)
                    {
                        if (ServerVersion.Major >= 13 && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130)
                        {
                            // script Hekaton properties
                            if (IsSupportedProperty("IsNativelyCompiled", sp))
                            {
                                AppendWithOption(sbTmp, "IsNativelyCompiled", Scripts.NATIVELY_COMPILED, ref bNeedsComma);
                            }

                            if (IsSupportedProperty("IsSchemaBound", sp))
                            {
                                AppendWithOption(sbTmp, "IsSchemaBound", Scripts.SP_SCHEMABINDING, ref bNeedsComma);
                            }
                        }
                        AppendWithOption(sbTmp, "IsEncrypted", "ENCRYPTION", ref bNeedsComma);
                    }

                    if (ServerVersion.Major >= 9 && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
                    {
                        AddScriptExecuteAs(sbTmp, sp, this.Properties, ref bNeedsComma);
                    }

                    if (true == bInsteadOf)
                    {
                        sbTmp.Append(" INSTEAD OF ");
                    }
                    else
                    {
                        if (SqlServerVersionInternal.Version70 == sp.TargetServerVersionInternal)
                        {
                            // use 7.0 syntax
                            sbTmp.Append(" FOR ");
                        }
                        else
                        {
                            sbTmp.Append(" AFTER ");
                        }
                    }

                    int nCount = 0;
                    if (GetPropValueOptional("Insert", false))
                    {
                        sbTmp.Append(" INSERT");
                        ++nCount;
                    }

                    if (GetPropValueOptional("Delete", false))
                    {
                        if (nCount++ > 0)
                        {
                            sbTmp.Append(Globals.comma);
                        }

                        sbTmp.Append(" DELETE");
                    }

                    if (GetPropValueOptional("Update", false))
                    {
                        if (nCount++ > 0)
                        {
                            sbTmp.Append(Globals.comma);
                        }

                        sbTmp.Append(" UPDATE");
                    }

                    if (0 == nCount)
                    {
                        throw new PropertyNotSetException("Insert or Update or Delete");
                    }

                    if (IsSupportedProperty("NotForReplication", sp))
                    {
                        object objNotRepl = Properties.Get("NotForReplication").Value;
                        if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType) && null != objNotRepl && true == (bool)objNotRepl)
                        {
                            sbTmp.Append(" NOT FOR REPLICATION ");
                        }
                    }

                    sbTmp.Append(" AS ");
                    sbTmp.Append(sp.NewLine);
                }

                if (!sp.OldOptions.DdlHeaderOnly)
                {
                    if (bTransactSql)
                    {
                        sbTmp.Append(GetTextBody(true));
                    }
                    else
                    {
                        if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
                        {

                        sbTmp.Append("EXTERNAL NAME ");

                        string tempString = (string)this.GetPropValue("AssemblyName");
                        if (string.Empty == tempString)
                            {
                                throw new PropertyNotSetException("AssemblyName");
                            }

                            sbTmp.AppendFormat("[{0}]", SqlBraket(tempString));

                        tempString = (string)this.GetPropValue("ClassName");
                        if (string.Empty == tempString)
                            {
                                throw new PropertyNotSetException("ClassName");
                            }

                            sbTmp.AppendFormat(".[{0}]", SqlBraket(tempString));

                        tempString = (string)this.GetPropValue("MethodName");
                        if (string.Empty == tempString)
                            {
                                throw new PropertyNotSetException(tempString);
                            }

                            sbTmp.AppendFormat(".[{0}]", SqlBraket(tempString));
                        }
                    }
                 }

                if (escapeString)
                {
                    sb.Append(SqlString(sbTmp.ToString()));
                    if (!sp.OldOptions.DdlBodyOnly)
                    {
                        sb.Append(sp.NewLine);
                        sb.Append("'");
                    }
                }
                else
                {
                    sb.Append(sbTmp.ToString());
                }
            }
            else
            {
                if (this.State == SqlSmoState.Existing && IsSupportedProperty("NotForReplication", sp))
                {
                    object objNotRepl = GetPropValueOptional("NotForReplication");
                    if (Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType
                        && null != objNotRepl && true == (bool)objNotRepl)
                    {
                        throw new WrongPropertyValueException(string.Format(CultureInfo.CurrentCulture,
                            ExceptionTemplates.ReplicationOptionNotSupportedForCloud, "NOT FOR REPLICATION"));
                    }

                }

                // we switch on the forceCheckName to true for stored procs. This is because engine doesn't store
                // the definition for stored procs properly if sp_rename is used to rename the stored proc.
                // Our ssms uses sp_rename for stored procs which should not be used see vsts:204338.
                // But even if the user renamed the procs manually using the script the server stored definition
                // becomes un-trustable. We force the options which would force replace the server's definition script
                // name with the SMO name if required.-anchals
                string body = GetTextForScript(sp, new String[] { "trigger" }, forceCheckNameAndManipulateIfRequired: true, scriptHeaderType: scriptHeaderType);
                if (bCreate && sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendLine(GetIfNotExistString( /* forCreate = */ true, sp));
                    sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_executesql @statement = {0} ", MakeSqlString(body));
                }
                else
                {
                    sb.Append(body);
                }
            }

            queries.Add(sb.ToString());
            sb.Length = 0;

        }

        private void GetExternalDDL(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            bool bInsteadOf = GetInsteafOfValue(sp);

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                Property propEnable = this.GetPropertyOptional("IsEnabled");
                if (null != propEnable.Value && (propEnable.Dirty || sp.ScriptForCreateDrop || IsCreate(scriptHeaderType)))
                {
                    // Due to the Alter() property filter, we should get here
                    // only when altering Table.Enabled
                    if (ParentColl.ParentInstance is Table)
                    {
                        queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} {1} TRIGGER [{2}]",
                                                ((ScriptSchemaObjectBase)ParentColl.ParentInstance).FullQualifiedName,
                                                ((bool)propEnable.Value) ? "ENABLE" : "DISABLE",
                                                SqlBraket(this.Name)));
                    }
                    else if (sp.ScriptForCreateDrop)
                    {
                        throw new SmoException(ExceptionTemplates.CannotEnableViewTrigger);
                    }
                }

                // add ordering, only if the trigger is not INSTEAD OF
                if (!bInsteadOf)
                {
                    // use foreach over array of prop names to avoid duplicated code
                    foreach (string orderStr in new string[] { "Delete", "Insert", "Update" })
                    {
                        if (GetPropValueOptional(orderStr, false))
                        {
                            Property propOrder = Properties.Get(orderStr + "Order");
                            if (null != propOrder.Value)
                            {
                                ActivationOrder order = (ActivationOrder)propOrder.Value;
                                if (order == ActivationOrder.None && !propOrder.Dirty)
                                {
                                    // this is not an interesting string. We don't want to emit "EXEC sp_settriggerorder ... @order=N'None'"
                                    // unless the user specifically wanted to set it to None, in which case it would be dirty
                                }
                                else
                                {
                                    queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC sp_settriggerorder @triggername=N'{0}', @order=N'{1}', @stmttype=N'{2}'",
                                                            SqlString(FormatFullNameForScripting(sp)),
                                                            order,
                                                            orderStr.ToUpper(SmoApplication.DefaultCulture)));
                                }
                            }
                        }
                    }
                }
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (Cmn.DatabaseEngineType.SqlAzureDatabase == this.DatabaseEngineType)
            {
                return null;
            }
            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        public void ReCompileReferences()
        {
            ReCompile(this.Name, String.Empty);
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

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone | SfcPropertyFlags.Design | SfcPropertyFlags.SqlAzureDatabase)]
        public string TextBody
        {
            get { CheckObjectState(); return GetTextBody(); }
            set { CheckObjectState(); SetTextBody(value); }
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public string TextHeader
        {
            get { CheckObjectState(); return GetTextHeader(false); }
            set { CheckObjectState(); SetTextHeader(value); }
        }

        [SfcProperty(SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public bool TextMode
        {
            get { CheckObjectState(); return GetTextMode(); }
            set { CheckObjectState(); SetTextMode(value, null); }
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            switch (prop.Name)
            {
                case "InsteadOf":
                    Validate_set_TextObjectDDLProperty(prop, value);
                    break;
                case "IsEncrypted": goto case "InsteadOf";
                case "NotForReplication": goto case "InsteadOf";
                case "Insert": goto case "InsteadOf";
                case "InsertOrder": goto case "InsteadOf";
                case "Update": goto case "InsteadOf";
                case "UpdateOrder": goto case "InsteadOf";
                case "Delete": goto case "InsteadOf";
                case "DeleteOrder": goto case "InsteadOf";

                default:
                    // other properties are not validated
                    break;
            }
        }


        // after object creation we do not support text mode = true
        protected override void PostCreate()
        {
            if (true == this.TextMode && false == CheckTextModeSupport())
            {
                //simulate an user reset
                this.TextMode = false;
            }
        }
        #endregion

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server.
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
                if (defaultTextMode)
                {
                    string[] fields = {
                                            "InsteadOf",
                                            "Insert",
                                            "Delete",
                                            "Update",
                                            "ImplementationType",
                                            "AnsiNullsStatus",
                                            "QuotedIdentifierStatus",
                                            "IsSystemObject",
                                            "DeleteOrder",
                                            "InsertOrder",
                                            "UpdateOrder"};
                    List<string> list = GetSupportedScriptFields(typeof(Trigger.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                    list.Add("Text");
                    return list.ToArray();
                  }
                else
                {
                    string[] fields = {
                                            "AssemblyName",
                                            "ClassName",
                                            "MethodName",
                                            "InsteadOf",
                                            "Insert",
                                            "Delete",
                                            "Update",
                                            "ImplementationType",
                                            "IsEncrypted",
                                            "NotForReplication",
                                            "DeleteOrder",
                                            "InsertOrder",
                                            "UpdateOrder",
                                            "AnsiNullsStatus",
                                            "QuotedIdentifierStatus",
                                            "IsSystemObject"};
                    List<string> list = GetSupportedScriptFields(typeof(Trigger.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                    list.Add("Text");
                    return list.ToArray();

                }

        }
    }
}


