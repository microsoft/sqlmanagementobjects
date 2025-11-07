// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.SqlServer.Management.Diagnostics;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    public class ScriptNameObjectBase : NamedSmoObject
    {
        internal ScriptNameObjectBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState eState) :
            base(parentColl, key, eState)
        {
        }

        internal ScriptNameObjectBase(ObjectKeyBase key, SqlSmoState eState) :
            base(key, eState)
        {
        }

        internal protected ScriptNameObjectBase() : base() { }

        private string m_sScriptName = string.Empty;

        internal virtual string ScriptName
        {
            get
            {
                CheckObjectState();
                return m_sScriptName;
            }
            set
            {
                CheckObjectState();
                if (null == value)
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("ScriptName"));
                }

                if (this is Microsoft.SqlServer.Management.Smo.Table)
                {
                    Table.CheckTableName(value);
                }
                m_sScriptName = value;
            }
        }


        protected void AutoGenerateName()
        {
            this.Name = Guid.NewGuid().ToString();
            SetIsSystemNamed(true);
        }

        protected void SetIsSystemNamed(bool flag)
        {
            int isSystemNamed = this.Properties.LookupID("IsSystemNamed", PropertyAccessPurpose.Write);
            this.Properties.SetValue(isSystemNamed, flag);
            this.Properties.SetRetrieved(isSystemNamed, true);
        }

        protected bool GetIsSystemNamed()
        {
            try
            {
                return (bool)this.Properties.GetValueWithNullReplacement("IsSystemNamed");
            }
            catch (Exception) //if property not yet set
            {
                return key.IsNull;
            }
        }

        internal override string GetName(ScriptingPreferences sp)
        {
            bool useScriptName = sp != null && (!sp.ForDirectExecution && ScriptName.Length > 0);

            // use script name only if we are strictly in scripting mode and script name has some value
            if (useScriptName)
            {
                return ScriptName;
            }
            // use owner name only if it's available
            else if (null != this.Name)
            {
                return this.Name;
            }
            return string.Empty;
        }

        internal void AddConstraintName(StringBuilder sb, ScriptingPreferences sp)
        {
            if (ScriptConstraintWithName(sp))
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, " CONSTRAINT {0} ", FormatFullNameForScripting(sp));
            }
        }

        internal bool ScriptConstraintWithName(ScriptingPreferences sp)
        {
            bool bIsSystemNamed = false;

            if (!sp.Table.SystemNamesForConstraints)
            {
                object oIsSystemNamed = GetPropValueOptional("IsSystemNamed");
                if (null != oIsSystemNamed)
                {
                    bIsSystemNamed = (bool)oIsSystemNamed;
                }
                else if(IsDesignMode)//this case is going to be true in case of object is in memory only and user had not set the name property
                {
                    bIsSystemNamed = GetIsSystemNamed();
                }
            }
            return !bIsSystemNamed;
        }


        internal virtual string GetScriptIncludeExists(ScriptingPreferences sp, string tableName, bool forCreate)
        {
            return string.Empty;
        }

        internal void ConstraintScriptCreate(string scriptBody, StringCollection createQuery, ScriptingPreferences sp)
        {
            if (scriptBody.Length == 0)
            {
                return;
            }
            TableViewBase table = (TableViewBase)ParentColl.ParentInstance;
            string sTableName = table.FormatFullNameForScripting(sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(GetScriptIncludeExists(sp, sTableName, true));
                sb.Append(Globals.newline);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ", sTableName);

            bool bCheckValue = true;  //default

            //sp.Table.ConstraintsWithNoCheck takes precedence
            if (sp.Table.ConstraintsWithNoCheck)
            {
                sb.Append(" WITH NOCHECK");
                bCheckValue = false;
            }
            else
            {
                object pChecked = GetPropValueOptional("IsChecked");
                if (null != pChecked)
                {
                    bool checkFK = (bool)pChecked;
                    bCheckValue = checkFK;
                    sb.Append(checkFK ? " WITH CHECK" : " WITH NOCHECK");
                }
            }

            sb.Append(" ADD ");
            sb.Append(scriptBody);
            createQuery.Add(sb.ToString());

            // 1. We can't really enable\disable constraint if it is scripted
            // un-named.
            if (ScriptConstraintWithName(sp))
            {

                object pEnabled = GetPropValueOptional("IsEnabled");
                if (null != pEnabled)
                {
                    bool enableFK = (bool)pEnabled;
                    StringBuilder strngBldr = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    if (sp.IncludeScripts.ExistenceCheck)
                    {
                        strngBldr.Append(GetScriptIncludeExists(sp, sTableName, false));
                        strngBldr.Append(Globals.newline);
                    }
                    // In the past, we only added this statement when bCheckValue != enableFK. This is wrong,
                    // because these two properties mean differnt things: checking data already in the table (IsChecked) vs.
                    // checking newly added data (IsChecked). Refer to "ALTER TABLE" article in BOL
                    strngBldr.Append(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} {1} CONSTRAINT {2}", sTableName,
                    enableFK ? "CHECK" : "NOCHECK", FormatFullNameForScripting(sp, true)));
                    createQuery.Add(strngBldr.ToString());
                }
            }

        }

        internal void ConstraintScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            TableViewBase table = (TableViewBase)ParentColl.ParentInstance;
            Property pChecked = Properties.Get("IsEnabled");
            if (null != pChecked.Value && (pChecked.Dirty || sp.ScriptForCreateDrop))
            {
                bool enableFK = (bool)pChecked.Value;
                alterQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} {1} CONSTRAINT [{2}]", table.FullQualifiedName,
                    enableFK ? "CHECK" : "NOCHECK", SqlBraket(this.Name)));
            }
        }

        #region utility_functions
        protected void AppendWithOption(StringBuilder sb, string propName, string optionText, ref bool needsComma)
        {
            //append only if the property is set
            if (true != (bool)GetPropValueOptional(propName, false))
            {
                return;
            }

            AppendWithCommaText(sb, optionText, ref needsComma);
        }

        protected void AppendWithCommaText(StringBuilder sb, string optionText, ref bool needsComma)
        {
            AppendCommaText(sb, optionText, ref needsComma, "WITH");
        }

        protected void AppendCommaText(StringBuilder sb, string optionText, ref bool needsComma, string beginWord)
        {
            if (false == needsComma)
            {
                sb.Append(beginWord + " ");
            }
            else
            {
                sb.Append(Globals.commaspace);
            }

            sb.Append(optionText);
            needsComma = true;
        }

        internal void ScriptAnsiQI(SqlSmoObject o, ScriptingPreferences sp, StringCollection queries, StringBuilder sb, bool skipSetOptions = false)
        {
            bool fAnsiNullsExists = false;
            bool fQuotedIdentifierExists = false;
            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                fAnsiNullsExists = (null != o.Properties.Get("AnsiNullsStatus").Value);
                fQuotedIdentifierExists = (null != o.Properties.Get("QuotedIdentifierStatus").Value);
            }

            // save server settings first
            Server svr = (Server)o.ParentColl.ParentInstance.ParentColl.ParentInstance;

            if (!skipSetOptions)
            {
                if (fAnsiNullsExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_ANSI_NULLS, (bool)o.Properties["AnsiNullsStatus"].Value ? Globals.On : Globals.Off);
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                }

                if (fQuotedIdentifierExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_QUOTED_IDENTIFIER, (bool)o.Properties["QuotedIdentifierStatus"].Value ? Globals.On : Globals.Off);
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                }
            }
        }

        internal void ScriptInformativeHeaders(ScriptingPreferences sp, StringBuilder sb)
        {
            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                if (sp.IncludeScripts.Header) // need to generate commentary headers
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        this.GetType().Name, FormatFullNameForScripting(sp),
                        DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }
            }
        }

        internal bool AddScriptExecuteAs(StringBuilder sb, ScriptingPreferences sp, PropertyCollection col, ref bool needsComma)
        {
            Property prop = col.Get("ExecutionContext");
            if (!sp.Security.ExecuteAs || null == prop.Value)
            {
                return false;
            }

            ExecutionContext ec = (ExecutionContext)prop.Value;

            AppendWithCommaText(sb, "EXECUTE AS ", ref needsComma);
            switch (ec)
            {
                case ExecutionContext.Caller:
                    sb.Append("CALLER");
                    break;

                case ExecutionContext.Owner:
                    sb.Append("OWNER");
                    break;

                case ExecutionContext.Self:
                    sb.Append("SELF");
                    break;

                case ExecutionContext.ExecuteAsUser:
                    // Verify that an ExecuteAsUser property is set and not empty.
                    string executeAsUser = (string)this.GetPropValue("ExecutionContextPrincipal");
                    if (string.Empty == executeAsUser)
                    {
                        throw new PropertyNotSetException("ExecutionContextPrincipal");
                    }

                    sb.AppendFormat("N'{0}'", SqlString(executeAsUser));
                    break;
            }
            return true;
        }

        internal bool AddScriptServerDdlExecuteAs(StringBuilder sb, ScriptingPreferences sp, PropertyCollection col, ref bool needsComma)
        {
            Property prop = col.Get("ExecutionContext");
            if (!sp.Security.ExecuteAs || null == prop.Value)
            {
                return false;
            }

            ServerDdlTriggerExecutionContext ec = (ServerDdlTriggerExecutionContext)prop.Value;

            AppendWithCommaText(sb, "EXECUTE AS ", ref needsComma);
            switch (ec)
            {
                case ServerDdlTriggerExecutionContext.Caller:
                    sb.Append("CALLER");
                    break;

                case ServerDdlTriggerExecutionContext.Self:
                    sb.Append("SELF");
                    break;

                case ServerDdlTriggerExecutionContext.ExecuteAsLogin:
                    // Verify that an ExecuteAsUser property is set and not empty.
                    string executeAsLogin = (string)this.GetPropValue("ExecutionContextLogin");
                    if (string.Empty == executeAsLogin)
                    {
                        throw new PropertyNotSetException("ExecutionContextLogin");
                    }

                    sb.AppendFormat("N'{0}'", SqlString(executeAsLogin));
                    break;
            }
            return true;
        }

        internal bool AddScriptDatabaseDdlExecuteAs(StringBuilder sb, ScriptingPreferences sp, PropertyCollection col, ref bool needsComma)
        {
            Property prop = col.Get("ExecutionContext");
            if (!sp.Security.ExecuteAs || null == prop.Value)
            {
                return false;
            }

            DatabaseDdlTriggerExecutionContext ec = (DatabaseDdlTriggerExecutionContext)prop.Value;

            AppendWithCommaText(sb, "EXECUTE AS ", ref needsComma);
            switch (ec)
            {
                case DatabaseDdlTriggerExecutionContext.Caller:
                    sb.Append("CALLER");
                    break;

                case DatabaseDdlTriggerExecutionContext.Self:
                    sb.Append("SELF");
                    break;

                case DatabaseDdlTriggerExecutionContext.ExecuteAsUser:
                    // Verify that an ExecuteAsUser property is set and not empty.
                    string executeAsUser = (string)this.GetPropValue("ExecutionContextUser");
                    if (string.Empty == executeAsUser)
                    {
                        throw new PropertyNotSetException("ExecutionContextUser");
                    }

                    sb.AppendFormat("N'{0}'", SqlString(executeAsUser));
                    break;
            }
            return true;
        }

        internal bool IsCreate(ScriptHeaderType scriptHeaderType)
        {
            return scriptHeaderType == ScriptHeaderType.ScriptHeaderForCreate || scriptHeaderType == ScriptHeaderType.ScriptHeaderForCreateOrAlter;
        }

        internal bool IsOrAlterSupported(ScriptingPreferences sp)
        {
            return sp.TargetDatabaseEngineEdition == Cmn.DatabaseEngineEdition.SqlDatabase || sp.TargetServerVersion >= SqlServerVersion.Version130;
        }

        #endregion

        #region TextModeImpl

        bool m_textMode = false;
        bool m_isTextModeInitialized = false;
        protected void SetTextMode(bool textMode, IEnumerable<ILockableCollection> collList)
        {
            //if the the object is existing and clr we do not support text mode = true
            if (true == m_textMode && false == CheckTextModeSupport())
            {
                return;
            }

            m_textMode = GetTextMode();

            //do we have to do anything ?
            if (textMode != m_textMode)
            {
                SwitchTextMode(textMode, collList);
                m_textMode = textMode;
            }
        }

        protected bool GetTextMode()
        {
            if (!m_isTextModeInitialized)
            {
                m_textMode = GetServerObject().DefaultTextMode;
                //if the the object is existing and clr we do not support text mode = true
                if (true == m_textMode && false == CheckTextModeSupport())
                {
                    m_textMode = false;
                }
                m_isTextModeInitialized = true;
            }

            return m_textMode;
        }

        //if the the object is existing and clr we do not support text mode = true
        protected bool CheckTextModeSupport()
        {
            if (SqlSmoState.Existing == this.State //is the object existing ?
                && this.Properties.Contains("ImplementationType") //can it be clr ?
                && ImplementationType.SqlClr == (ImplementationType)this.GetPropValue("ImplementationType") //is it clr ?
                )
            {
                return false;
            }
            return true;
        }

        internal void CheckTextModeAccess(string propName)
        {
            if (true == GetTextMode())
            {
                throw new PropertyWriteException(propName, this.GetType().Name, this.Name,
                    ExceptionTemplates.ReasonIntextMode);
            }
        }

        private enum ScriptDDLPartialOptions
        {
            ScriptBody,
            ScriptHeaderForAlter,
            ScriptHeaderForCreate,
            ScriptHeaderForCreateOrAlter
        }

        //scripts the header
        private string ScriptDDLPartialInternal(ScriptDDLPartialOptions options)
        {
            ScriptingPreferences sp = new ScriptingPreferences();
            StringCollection queries = new StringCollection();

            sp.SuppressDirtyCheck = false;
            // pass target version
            sp.SetTargetServerInfo(this);

            //check if we script the body
            sp.OldOptions.DdlBodyOnly = ScriptDDLPartialOptions.ScriptBody == options;
            //we script either the header or the body
            sp.OldOptions.DdlHeaderOnly = !sp.OldOptions.DdlBodyOnly;

            // using this option will avoid any script name/owner substitutions
            sp.ForDirectExecution = true;

            //script create with no propagate
            sp.ScriptForCreateDrop = true;
            ScriptCreate(queries, sp);

            //
            // if people requested the Alter/CreateOrAlter script:
            // 1. they do not want what we would execute for Alter()/CreateOrAlter() as this would be
            //      optimized based on dirtyness
            // 2. they do want the full Alter/CreateOrAlter script; for text objects this is the
            //      create script with the CREATE replaced with ALTER/CREATE OR ALTER
            //
            if (ScriptDDLPartialOptions.ScriptHeaderForAlter == options)
            {
                TraceHelper.Assert(queries.Count > 0, "queries.Count > 0 failed. queries.Count=" + queries.Count);

                // modify for alter
                // header starts at index 0, there are no commentaries
                queries[0] = ModifyTextForAlter(queries[0], 0);
            }
            else if (ScriptDDLPartialOptions.ScriptHeaderForCreateOrAlter == options)
            {
                Diagnostics.TraceHelper.Assert(queries.Count > 0, "queries.Count > 0 failed. queries.Count=" + queries.Count);

                // modify for alter
                // header starts at index 0, there are no commentaries
                queries[0] = ModifyTextForCreateOrAlter(queries[0], 0);
            }

            StringBuilder strResult = new StringBuilder();
            foreach (string s in queries)
            {
                strResult.Append(s);
            }
            return strResult.ToString();
        }

        //the offset where the header ends and the body starts
        //if 'text' is the full text, than text[headerCutIndex] is the first
        //character in the body
        int m_headerCutIndex = -1;
        //accessor function, calculates at first use
        int GetHeaderCutIndex(string text)
        {
            if (m_headerCutIndex >= 0)
            {
                return m_headerCutIndex;
            }

            int idx = DdlTextParser.ParseDdlHeader(text);
            //idx = text.Length is valid.
            //you can have as stored proc with the following ddl: 'create proc p as'
            if (idx <= 0 || idx > text.Length)
            {
                throw new FailedOperationException(
                    ExceptionTemplates.SyntaxErrorInTextHeader(this.GetType().Name, this.Name));
            }
            return idx;
        }

        void ResetTextData()
        {
            m_headerCutIndex = -1;
            m_isTextDirty = false;
            m_textHeader = null;
            m_textBody = null;
        }

        protected void ThrowIfTextIsDirtyForAlter()
        {
            if (m_isTextDirty)
            {
                throw new SmoException(ExceptionTemplates.PropNotModifiable("Text", this.GetType().Name));
            }
        }

        protected override void CleanObject()
        {
            base.CleanObject();
            m_isTextDirty = false;
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();
            ResetTextData();
        }

        string m_textHeader = null;
        protected void SetTextHeader(string textHeader)
        {
            if (false == GetTextMode())
            {
                throw new PropertyWriteException("TextHeader", this.GetType().Name, this.Name,
                    ExceptionTemplates.ReasonNotIntextMode);
            }
            SetTextHeaderInternal(textHeader);
        }

        void SetTextHeaderInternal(string textHeader)
        {
            if (textHeader == null)
            {
                throw new ArgumentNullException();
            }

            m_textHeader = textHeader;
            m_isTextDirty = true;
        }

        string m_textBody = null;
        protected void SetTextBody(string textBody)
        {
            if (false == ForceTextModeOnTextBody() && false == GetTextMode())
            {
                throw new PropertyWriteException("TextBody", this.GetType().Name, this.Name,
                    ExceptionTemplates.ReasonNotIntextMode);
            }
            SetTextBodyInternal(textBody);
        }

        void SetTextBodyInternal(string textBody)
        {
            if (textBody == null)
            {
                throw new ArgumentNullException();
            }

            m_textBody = textBody;
            m_isTextDirty = true;
        }

        protected bool ForceTextModeOnTextBody()
        {
            if (this.Properties.Contains("ImplementationType"))
            {
                return ImplementationType.TransactSql == (ImplementationType)this.GetPropValueOptional
                    ("ImplementationType", ImplementationType.TransactSql);
            }
            return true;
        }

        protected string GetTextBody()
        {
            return GetTextBody(false);
        }

        protected string GetTextBody(bool forScripting)
        {
            //if not in text mode and not CLR proc
            if (false == ForceTextModeOnTextBody() && false == GetTextMode())
            {
                //generate it from ddl
                try
                {
                    return ScriptDDLPartialInternal(ScriptDDLPartialOptions.ScriptBody);
                }
                catch (PropertyCannotBeRetrievedException)
                {
                    throw new PropertyCannotBeRetrievedException("TextBody", this);
                }
            }

            //try to get it from text
            if (null == m_textBody)
            {
                string text = this.GetTextProperty("TextBody", false);
                if (null != text && text.Length > 0)
                {
                    int idx = this.GetHeaderCutIndex(text);
                    m_textBody = text.Substring(idx, text.Length - idx);
                }
                else if (forScripting)
                {
                    throw new PropertyNotSetException("TextBody");
                }
            }

            return m_textBody;
        }

        public enum ScriptHeaderType
        {
            ScriptHeaderForAlter,
            ScriptHeaderForCreate,
            ScriptHeaderForCreateOrAlter
        }

        protected string GetTextHeader(bool forAlter)
        {
            return GetTextHeader(forAlter ? ScriptHeaderType.ScriptHeaderForAlter : ScriptHeaderType.ScriptHeaderForCreate);
        }

        protected string GetTextHeader(ScriptHeaderType scriptHeaderType)
        {
            //if not in text mode
            if (false == GetTextMode())
            {
                //generate it from ddl
                try
                {
                    switch (scriptHeaderType)
                    {
                        case ScriptHeaderType.ScriptHeaderForCreate:
                            return ScriptDDLPartialInternal(ScriptDDLPartialOptions.ScriptHeaderForCreate);
                        case ScriptHeaderType.ScriptHeaderForAlter:
                            return ScriptDDLPartialInternal(ScriptDDLPartialOptions.ScriptHeaderForAlter);
                        case ScriptHeaderType.ScriptHeaderForCreateOrAlter:
                            if (!(this is Cmn.ICreateOrAlterable))
                            {
                                throw new FailedOperationException(
                                    ExceptionTemplates.ScriptHeaderTypeNotSupported(
                                        scriptHeaderType.ToString(),
                                        this.GetType().Name,
                                        this.Name));
                            }
                            return ScriptDDLPartialInternal(ScriptDDLPartialOptions.ScriptHeaderForCreateOrAlter);
                    }
                }
                catch (PropertyCannotBeRetrievedException)
                {
                    throw new PropertyCannotBeRetrievedException("TextHeader", this);
                }
            }

            //we are in TextMode = true
            //try to get it from text
            if (null == m_textHeader)
            {
                string text = this.GetTextProperty("TextHeader", false);
                if (null != text)
                {
                    int idx = this.GetHeaderCutIndex(text);
                    m_textHeader = text.Substring(0, idx);
                }
            }

            //scriptHeaderType == ScriptHeaderType.ScriptHeaderForAlter or scriptHeaderType == ScriptHeaderType.ScriptHeaderForCreateOrAlter
            //may only come from users ( ScriptHeader )
            //internaly we always call with scriptHeaderType == ScriptHeaderType.ScriptHeaderForCreate
            //and manipulate it later if necessary
            if (ShouldScriptForNonCreate(scriptHeaderType))
            {
                //we need to replace CREATE with ALTER/CREATE OR ALTER
                return CheckAndManipulateText(m_textHeader ?? string.Empty,
                                                null, new ScriptingPreferences(), scriptHeaderType);
            }

            return m_textHeader ?? string.Empty;
        }

        protected void SetCollectionTextMode(bool newTextModeValue, ILockableCollection coll) 
        {
            if (true == newTextModeValue)
            {
                coll.LockCollection(ExceptionTemplates.ReasonIntextMode);
            }
            else
            {
                coll.UnlockCollection();
            }
        }

        protected void SwitchTextMode(bool newTextModeValue, IEnumerable<ILockableCollection> collList)
        {
            if (null != collList)
            {
                foreach (var coll in collList)
                {
                    SetCollectionTextMode(newTextModeValue, coll);
                }
            }

            if (false == newTextModeValue)
            {
                m_textHeader = null;
                if (false == ForceTextModeOnTextBody())
                {
                    m_isTextDirty = false;
                    m_textBody = null;
                }
            }
            else
            {
                try
                {
                    m_textHeader = ScriptDDLPartialInternal(ScriptDDLPartialOptions.ScriptHeaderForCreate);
                }
                catch (PropertyNotSetException) //if we fail to generate DDL
                {
                    m_textHeader = string.Empty; //leave it empty
                }
                if (false == ForceTextModeOnTextBody())
                {
                    try
                    {
                        m_textBody = ScriptDDLPartialInternal(ScriptDDLPartialOptions.ScriptBody);
                    }
                    catch (PropertyNotSetException) //if we fail to generate DDL
                    {
                        m_textBody = string.Empty; //leave it empty
                    }
                }
                m_isTextDirty = true;
            }
        }

        internal static void Validate_set_TextObjectDDLProperty(Property prop, object newValue)
        {
            ((ScriptNameObjectBase)prop.Parent.m_parent).CheckTextModeAccess(prop.Name);
        }

        internal static void Validate_set_ChildTextObjectDDLProperty(Property prop, object newValue)
        {
            ((ScriptNameObjectBase)((SqlSmoObject)prop.Parent.m_parent).ParentColl.ParentInstance).CheckTextModeAccess(prop.Name);
        }

        /// <summary>
        /// returns whether it should script for non-create cases including Alter and CreateOrAlter
        /// </summary>
        /// <param name="scriptHeaderType"></param>
        /// <returns></returns>
        private bool ShouldScriptForNonCreate(ScriptHeaderType scriptHeaderType)
        {
            return ScriptHeaderType.ScriptHeaderForAlter == scriptHeaderType || ScriptHeaderType.ScriptHeaderForCreateOrAlter == scriptHeaderType;
        }

        /// <summary>
        /// replace CREATE with ALTER in a given text at a given position
        /// </summary>
        /// <param name="text"></param>
        /// <param name="indexCreate"></param>
        /// <returns>the modified text</returns>
        private string ModifyTextForAlter(string text, int indexCreate)
        {
            //callers should take actions to ensure the validity
            //and report the runtime error for the following 2 assertions
            Diagnostics.TraceHelper.Assert(indexCreate >= 0, "indexCreate >= 0 failed, indexCreate=" + indexCreate);
            Diagnostics.TraceHelper.Assert(indexCreate <= text.Length - Scripts.CREATE.Length,
                                        "The statement \"" + text + "\" is shorter than \"" + Scripts.CREATE + "\"");
            Diagnostics.TraceHelper.Assert(Scripts.CREATE == text.Substring(indexCreate, Scripts.CREATE.Length).ToUpper(SmoApplication.DefaultCulture),
                                        "\"CREATE\" == text.Substring(indexCreate, 6).ToUpper() failed. text=" + text);

            text = text.Remove(indexCreate, Scripts.CREATE.Length/*CREATE length*/);
            return text.Insert(indexCreate, Scripts.ALTER);
        }

        /// <summary>
        /// replace CREATE with CREATE OR ALTER in a given text at a given position
        /// </summary>
        /// <param name="text"></param>
        /// <param name="indexCreate"></param>
        /// <returns>the modified text</returns>
        private string ModifyTextForCreateOrAlter(string text, int indexCreate)
        {
            // invalid index specified
            if (indexCreate < 0 || indexCreate > text.Length - Scripts.CREATE.Length)
            {
                throw new SmoException(
                    ExceptionTemplates.InvalidIndexSpecifiedForModifyingTextToCreateOrAlter(indexCreate, 0, text.Length - Scripts.CREATE.Length));
            }
            // invalid text to modify to CREATE OR ALTER
            if (Scripts.CREATE != text.Substring(indexCreate, Scripts.CREATE.Length).ToUpper(SmoApplication.DefaultCulture))
            {
                throw new SmoException(ExceptionTemplates.InvalidTextForModifyingToCreateOrAlter);
            }

            if (Scripts.CREATE_OR_ALTER != text.Substring(indexCreate, Scripts.CREATE_OR_ALTER.Length).ToUpper(SmoApplication.DefaultCulture))
                {
                    text = text.Remove(indexCreate, Scripts.CREATE.Length);
                    text = text.Insert(indexCreate, Scripts.CREATE_OR_ALTER);
                }

            return text;
        }


        bool m_isTextDirty = false;
        protected bool GetIsTextDirty()
        {
            return (m_isTextDirty || this.IsTouched);
        }

        protected override bool IsObjectDirty()
        {
            return base.IsObjectDirty() || GetIsTextDirty();
        }

        //it should be only called when we script with TextMode = true;
        internal string GetTextForScript(ScriptingPreferences sp, string[] expectedObjectTypes, bool forceCheckNameAndManipulateIfRequired, ScriptHeaderType scriptHeaderType)
        {
            Diagnostics.TraceHelper.Assert(true == GetTextMode(), "true == GetTextMode() failed");

            //if for scripting and we don't enforce scripting options
            if (!sp.ForDirectExecution && !sp.OldOptions.EnforceScriptingPreferences)
            {
                //we leave it AS IS
                string ddlText = BuildText(sp);
                // forceCheckNameAndManipulateIfRequired is true then we need to
                // check the name of the object in the script and change it if required.
                // This can happen when say a stored proc is renamed using sp_rename leaving the
                // definition name to be old name in the server catalog but the actual object name
                // changed properly. This is documented for sp_rename in BOL.-anchals
                if (forceCheckNameAndManipulateIfRequired)
                {

                    DdlTextParserHeaderInfo headerInfo;
                    // PARSE and check syntax, get header info, extract text info
                    // like object type, name and schema as found in text
                    if (!DdlTextParser.CheckDdlHeader(ddlText, GetQuotedIdentifier(), IsOrAlterSupported(sp), out headerInfo))
                    {
                        throw new FailedOperationException(
                            ExceptionTemplates.SyntaxErrorInTextHeader(this.GetType().Name, this.Name));
                    }

                    CheckObjectSupportability(headerInfo, sp);

                    CheckAndManipulateName(ref ddlText, sp, ref headerInfo, false);

                }
                return ddlText;
            }

            //build expectedObjectTypes list
            if (null == expectedObjectTypes)
            {
                expectedObjectTypes = new string[] { this.GetType().Name };
            }

            // CheckAndManipulateText Would check the Name so we don't want BuildText to check the name
            return CheckAndManipulateText(BuildText(sp), expectedObjectTypes, sp, scriptHeaderType);
        }

        /// <summary>
        ///If there are some properties which are respected in special cases then use this function.
        ///currently applicable for DatabaseDdltriggers
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckObjectDirty() { return true; }

        /// <summary>
        /// Checks SMO object is supported by target scripting environment using its scripted T-SQL header. Note: very
        /// few edge cases were handled here, normally this suppotability check should be done in object itself's class.
        /// </summary>
        private void CheckObjectSupportability(DdlTextParserHeaderInfo headerInfo, ScriptingPreferences sp)
        {
            // If we create a set(count >= 1) of numbered stored procedures, like the examples below:
            //        CREATE PROCEDURE nsp;1
            //        CREATE PROCEDURE nsp;2
            //        CREATE PROCEDURE nsp;3
            //        ...
            //        GO
            // We would have a parent StoredProcedure object(the one created with ;1) which has a collection of child
            // NumberedStoredProcedure objects(those created with ;2/;3/etc...). Though the parent object is defined as
            // "StoredProcedure" type instead of "NumberedStoredProcedure" type in SMO, it is essentially still a numbered
            // stored procedure and contains typcial <numbered> properties. Right now the only way to distinguish this
            // is to check parent object's generated scripts, specifically its <headerInfo.procedureNumber>.
            //
            // While this object is not supported on Azure SQL DW database and thus generated scripts targting SQL DW
            // will contain unsupported T-SQL syntax(i.e ';1'). We should throw exception if this scenario actually happens
            // instead of returning back unsupported scripts to the user.
            if ((this is StoredProcedure && !string.IsNullOrEmpty(headerInfo.procedureNumber)) && sp.TargetEngineIsAzureSqlDw())
            {
                throw new UnsupportedEngineEditionException(ExceptionTemplates.NotSupportedForSqlDw(typeof(NumberedStoredProcedure).Name))
                           .SetHelpContext("NotSupportedForSqlDw");
            }
        }

        //we are preparing to script, "create" or "alter" or "create or alter"
        string BuildText(ScriptingPreferences sp)
        {
            Diagnostics.TraceHelper.Assert(true == GetTextMode(), "true == GetTextMode() failed");

            //if text is not dirty and it must not be cut , just return it
            if (false == GetIsTextDirty() && !(sp.OldOptions.DdlBodyOnly || sp.OldOptions.DdlHeaderOnly))
            {
                //
                if (base.IsObjectDirty() && CheckObjectDirty())
                {
                    throw new FailedOperationException(ExceptionTemplates.WrongPropertyValueExceptionText("TextMode", "true"));
                }
                return GetTextProperty("TextHeader", sp);
            }
            string textHeader = string.Empty;
            if (!sp.OldOptions.DdlBodyOnly)
            {
                textHeader =  GetTextHeader(false);
                Diagnostics.TraceHelper.Assert(null != textHeader, "null == textHeader");

                //text header cannot be empty
                if (textHeader.Length <= 0)
                {
                    throw new PropertyNotSetException("TextHeader");
                }
            }

            StringBuilder text = new StringBuilder(textHeader);

            string textBody = string.Empty;
            if (!sp.OldOptions.DdlHeaderOnly)
            {
                textBody = GetTextBody();
                Diagnostics.TraceHelper.Assert(null != textBody, "null == textBody");

                //text header should end in space
                if (textHeader.Length > 0 && !char.IsWhiteSpace(textHeader[textHeader.Length - 1]))
                {
                    //else check if the text body ends in space
                    if (textBody.Length > 0 && !char.IsWhiteSpace(textBody[0]))
                    {
                        //else we need to add a space
                        text.Append(sp.NewLine);
                    }
                }
                text.Append(textBody);
            }
            return text.ToString();
        }

        protected virtual string GetBraketNameForText()
        {
            return MakeSqlBraket(this.Name);
        }

        ///<summary>
        /// checks that the name as apears in text maches the name as specified in metadata
        /// all name/schemas are expected in brakets
        ///</summary>
        internal void CheckNameInTextCorrectness(string expectedName, string expectedSchema, string foundName,
                                                string foundSchema, string foundProcedureNumber)
        {
            Diagnostics.TraceHelper.Assert(true == GetTextMode(), "true == GetTextMode() failed");
            Diagnostics.TraceHelper.Assert(null != expectedName && expectedName.Length > 1 && expectedName[0] == '[');
            Diagnostics.TraceHelper.Assert(null != expectedSchema && (expectedSchema.Length == 0
                            || (expectedSchema.Length > 1 && expectedSchema[0] == '[')));
            Diagnostics.TraceHelper.Assert(null != foundName && foundName.Length > 1 && foundName[0] == '[');
            Diagnostics.TraceHelper.Assert(null != foundSchema && (foundSchema.Length == 0
                            || (foundSchema.Length > 1 && foundSchema[0] == '[')));
            Diagnostics.TraceHelper.Assert(null != foundProcedureNumber && (foundProcedureNumber.Length == 0
                            || (foundProcedureNumber.Length > 1 && foundProcedureNumber[0] == ';')));
            Diagnostics.TraceHelper.Assert((this is ScriptSchemaObjectBase && expectedSchema.Length > 0) ||
                            (!(this is ScriptSchemaObjectBase) && expectedSchema.Length == 0 && foundSchema.Length == 0)
                            || this.State == SqlSmoState.Creating || this is Trigger);
            //
            // check name
            //

            // adjust procedure number: ";1" == ""
            if (";1" == foundProcedureNumber)
            {
                foundProcedureNumber = "";
            }

            //proceed checking the name
            string foundBraketName = foundName + foundProcedureNumber;
            if (0 != this.StringComparer.Compare(expectedName, foundBraketName))
            {
                throw new FailedOperationException(
                    ExceptionTemplates.IncorrectTextHeader(this.GetType().Name, this.Name, "name", "Name"));
            }

            //
            // check schema
            //
            if (expectedSchema.Length > 0)
            {
                Diagnostics.TraceHelper.Assert(this is ScriptSchemaObjectBase);

                if (foundSchema.Length > 0)
                {
                    if (0 != this.StringComparer.Compare(expectedSchema, foundSchema))
                    {
                        throw new FailedOperationException(
                            ExceptionTemplates.IncorrectTextHeader(this.GetType().Name, this.Name, "schema", "Schema"));
                    }
                }
                else
                {
                    // if no schema was specified in the text, check that the default schema is what we want
                    ScriptSchemaObjectBase schemaObject = (ScriptSchemaObjectBase)this;
                    var parentCollection =  (ISchemaObjectCollection)schemaObject.ParentColl;

                    if (0 != this.StringComparer.Compare(expectedSchema, MakeSqlBraket(parentCollection.GetDefaultSchema())))
                    {
                        throw new FailedOperationException(
                            ExceptionTemplates.IncorrectTextHeader(this.GetType().Name, this.Name, "schema", "Schema"));
                    }
                }
            }
        }

        /// <summary>
        /// checks text syntax, that it manipulates the right type of object ( PROC, TRIGGER, etc )
        /// </summary>
        /// <param name="ddlText"></param>
        /// <param name="enforceCreate"></param>
        /// <param name="checkName"></param>
        /// <param name="expectedObjectTypes"></param>
        /// <param name="headerInfo"></param>
        protected void CheckTextCorrectness(string ddlText, bool enforceCreate, bool checkName,
                                string[] expectedObjectTypes, out DdlTextParserHeaderInfo headerInfo)
        {
            CheckTextCorrectness(ddlText, enforceCreate, checkName, isOrAlterSupported: false, expectedObjectTypes: expectedObjectTypes, headerInfo: out headerInfo);
        }

        /// <summary>
        /// checks text syntax, that it manipulates the right type of object ( PROC, TRIGGER, etc )
        /// </summary>
        /// <param name="ddlText"></param>
        /// <param name="enforceCreate"></param>
        /// <param name="checkName"></param>
        /// <param name="isOrAlterSupported"></param>
        /// <param name="expectedObjectTypes"></param>
        /// <param name="headerInfo"></param>
        protected void CheckTextCorrectness(string ddlText, bool enforceCreate, bool checkName, bool isOrAlterSupported,
                                string[] expectedObjectTypes, out DdlTextParserHeaderInfo headerInfo)
        {
            Diagnostics.TraceHelper.Assert(true == GetTextMode(), "true == GetTextMode() failed");

            // PARSE and check syntax, get header info, extract text info
            // like object type, name and schema as found in text
            if (!DdlTextParser.CheckDdlHeader(ddlText, GetQuotedIdentifier(), isOrAlterSupported, out headerInfo))
            {
                throw new FailedOperationException(
                    ExceptionTemplates.SyntaxErrorInTextHeader(this.GetType().Name, this.Name));
            }

            //check if the script must be for CREATE
            if (true == enforceCreate && false == headerInfo.scriptForCreate)
            {
                throw new FailedOperationException(
                    ExceptionTemplates.SyntaxErrorInTextHeader(this.GetType().Name, this.Name));
            }

            //check that the text operates on the type of object that we want
            if (null != expectedObjectTypes)
            {
                //make sequencial search; we expected 1 or 2 elements in the array
                bool bObjectTypeFound = false;
                foreach (string s in expectedObjectTypes)
                {
                    if (0 == string.Compare(s, headerInfo.objectType, StringComparison.OrdinalIgnoreCase))
                    {
                        bObjectTypeFound = true;
                        break;
                    }
                }
                if (false == bObjectTypeFound)
                {
                    throw new FailedOperationException(
                        ExceptionTemplates.SyntaxErrorInTextHeader(this.GetType().Name, this.Name));

                }
            }

            if (true == checkName)
            {
                // if the object has schema, then we'll need to do some extra validation
                ScriptSchemaObjectBase schemaObject = this as ScriptSchemaObjectBase;
                string expectedSchema = null != schemaObject ? MakeSqlBraket(schemaObject.Schema) : string.Empty;

                CheckNameInTextCorrectness(GetBraketNameForText(), expectedSchema,
                                            headerInfo.name, headerInfo.schema, headerInfo.procedureNumber);

                // if this is a trigger, also check the table name
                if (0 == string.Compare("TRIGGER", headerInfo.objectType, StringComparison.OrdinalIgnoreCase))
                {
                    Trigger tr = this as Trigger;

                    if (tr != null)
                    {
                        TableViewBase tv = (TableViewBase)tr.Parent;
                        tv.CheckNameInTextCorrectness(tv.GetBraketNameForText(), MakeSqlBraket(tv.Schema),
                                                    headerInfo.nameSecondary, headerInfo.schemaSecondary, string.Empty);
                    }
                }
            }
        }

        ///<summary>
        ///put in one place the basic text manipulations that we do
        ///</summary>
        private string CheckAndManipulateText(string ddlText, string[] expectedObjectTypes, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            //it should be only called when we script with TextMode = true;
            Diagnostics.TraceHelper.Assert(true == GetTextMode(), "true == GetTextMode() failed");

            //
            //check text corectness
            //

            DdlTextParserHeaderInfo headerInfo;

            //if the script is for create it must have the CREATE keyword
            //Note: if we want the script for alter we will never error, rather we will fix it on the fly
            bool enforceCreate = ScriptHeaderType.ScriptHeaderForCreate == scriptHeaderType || !sp.ForDirectExecution;

            //if the script is for direct execution ( create/alter ) we throw error if the name doesn't match;
            //for alter or scripting we fix it on the fly
            bool checkName = sp.ForDirectExecution;

            CheckTextCorrectness(ddlText, enforceCreate, checkName, IsOrAlterSupported(sp), expectedObjectTypes, out headerInfo);

            CheckObjectSupportability(headerInfo, sp);

            //
            //manipulate text
            //

            //if we didn't check the name, fix it
            if (false == checkName)
            {
                CheckAndManipulateName(ref ddlText, sp, ref headerInfo, true);
            }

            //modify text replacing CREATE with ALTER if neccessary
            if (ScriptHeaderType.ScriptHeaderForAlter == scriptHeaderType && headerInfo.scriptForCreate)
            {
                ddlText = ModifyTextForAlter(ddlText, headerInfo.indexCreate);
            }
            //modify text replacing CREATE with CREATE OR ALTER if neccessary
            else if (ScriptHeaderType.ScriptHeaderForCreateOrAlter == scriptHeaderType && !headerInfo.scriptContainsOrAlter)
            {
                ddlText = ModifyTextForCreateOrAlter(ddlText, headerInfo.indexCreate);
            }

            return ddlText;
        }

        // if schema name is null or empty then this method returns true
        // also passed in schemaName should be bracketed. Use MakeSqlBracket if its not.
        // This does a server/database dependent case insensitive or case sensitive name match
        // and if the object doesn't have a schema it returns false.
        private bool IsSchemaNameSame(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                return true;
            }
            ScriptSchemaObjectBase schemaObject = this as ScriptSchemaObjectBase;
            bool schemaNameSame = false;
            if (schemaObject != null)
            {
                schemaNameSame = (this.StringComparer.Compare(MakeSqlBraket(schemaObject.Schema), schemaName) == 0);
            }
            return schemaNameSame;
        }

        ///<summary>
        ///Check the script name with the Object's name and replace if necessary. ForceScripting name would force the name
        /// change even when the names are same. (This might lose original formatting but introduce standard naming convention
        /// like bracketting of identifiers.
        ///</summary>
        private void CheckAndManipulateName(ref string ddlText, ScriptingPreferences sp, ref DdlTextParserHeaderInfo headerInfo, bool forceScriptingName)
        {
            // change the name only if the old name doesn't match the current name or
            // forcScriptingName is true.-anchals
            // we compare the bracketted names because the user might have provided brackets or not. HeaderInfo.name always gives us bracket names.
            if (forceScriptingName || this.StringComparer.Compare(GetBraketNameForText(), headerInfo.name) != 0 ||
                !IsSchemaNameSame(headerInfo.schema))
            {

                // if this is a trigger, also fix the table name
                // note: fix it before the object's name so that the text indexes are not affected
                if (0 == string.Compare("TRIGGER", headerInfo.objectType, StringComparison.OrdinalIgnoreCase))
                {
                    Diagnostics.TraceHelper.Assert(headerInfo.indexNameStartSecondary > 0 && headerInfo.indexNameEndSecondary > 0
                                && headerInfo.indexNameEndSecondary > headerInfo.indexNameStartSecondary);

                    Trigger tr = this as Trigger;
                    if (null != tr)
                    {
                        TableViewBase tv = (TableViewBase)tr.Parent;

                        ddlText = ddlText.Remove(headerInfo.indexNameStartSecondary,
                                            headerInfo.indexNameEndSecondary - headerInfo.indexNameStartSecondary);

                        if (null != headerInfo.databaseSecondary && headerInfo.databaseSecondary.Length > 0)
                        {
                            Database db = tv.ParentColl.ParentInstance as Database;
                            string dbName = headerInfo.databaseSecondary;
                            if (null != db)
                            {
                                dbName = db.FormatFullNameForScripting(sp);
                            }
                            ddlText = ddlText.Insert(headerInfo.indexNameStartSecondary,
                                  dbName + "." + tv.FormatFullNameForScripting(sp));
                        }
                        else
                        {
                            ddlText = ddlText.Insert(headerInfo.indexNameStartSecondary, tv.FormatFullNameForScripting(sp));
                        }
                    }
                }
                // fix the object name
                Diagnostics.TraceHelper.Assert(headerInfo.indexNameStart > 0 && headerInfo.indexNameEnd > 0
                                && headerInfo.indexNameEnd > headerInfo.indexNameStart);

                ddlText = ddlText.Remove(headerInfo.indexNameStart, headerInfo.indexNameEnd - headerInfo.indexNameStart);
                ddlText = ddlText.Insert(headerInfo.indexNameStart, FormatFullNameForScripting(sp));

            }
        }

        bool GetQuotedIdentifier()
        {
            //if it doesn't have the property ( see Default )
            if (!this.Properties.Contains("QuotedIdentifierStatus"))
            {
                //assume false
                return false;
            }
            return this.GetPropValueOptional("QuotedIdentifierStatus", false);
        }

        /// <summary>
        /// get the text of the object, if not available, throw apropiate exception
        /// based on the State of the object
        /// </summary>
        /// <param name="requestingProperty">name of the property that should appear in the exception as failed to be
        /// retrived, useful when we have properties derived from Text</param>
        /// <returns></returns>
        protected string GetTextProperty(string requestingProperty)
        {
            return GetTextProperty(requestingProperty, true);
        }

        /// <summary>
        /// overloaded to allow for scriping options to be passed in
        /// </summary>
        /// <param name="requestingProperty"></param>
        /// <param name="sp"></param>
        /// <returns></returns>
        internal string GetTextProperty(string requestingProperty, ScriptingPreferences sp)
        {
            return GetTextProperty(requestingProperty, sp, true);
        }

        string GetTextProperty(string requestingProperty, bool bThrowIfCreating)
        {
            return GetTextProperty(requestingProperty, null, bThrowIfCreating);
        }


        /// <summary>
        /// Returns a string representation of the ServerDdlTriggerExecutionContext   enumeration
        /// </summary>
        /// <param name="ec"></param>
        /// <returns></returns>
        private string GetExecutionContextString(ServerDdlTriggerExecutionContext ec)
        {
            string executionContext = string.Empty;

            switch (ec)
            {
                case ServerDdlTriggerExecutionContext.Caller:
                    executionContext = "CALLER";
                    break;

                case ServerDdlTriggerExecutionContext.Self:
                    executionContext = "SELF";
                    break;

                case ServerDdlTriggerExecutionContext.ExecuteAsLogin:
                    // Verify that an ExecuteAsUser property is set and not empty.
                    string executeAsUser = (string)this.GetPropValue("ExecutionContextPrincipal");
                    if (string.Empty == executeAsUser)
                    {
                        executionContext = string.Format(CultureInfo.InvariantCulture, "USER ({0})", executeAsUser);
                    }
                    else
                    {
                        executionContext = "USER";
                    }
                    break;
            }

            return executionContext;

        }


        /// <summary>
        /// Returns a string representation of the DatabaseDdlTriggerExecutionContext  enumeration
        /// </summary>
        /// <param name="ec"></param>
        /// <returns></returns>
        private string GetExecutionContextString(DatabaseDdlTriggerExecutionContext ec)
        {
            string executionContext = string.Empty;

            switch (ec)
            {
                case DatabaseDdlTriggerExecutionContext.Caller:
                    executionContext = "CALLER";
                    break;

                case DatabaseDdlTriggerExecutionContext.Self:
                    executionContext = "SELF";
                    break;

                case DatabaseDdlTriggerExecutionContext.ExecuteAsUser:
                    // Verify that an ExecuteAsUser property is set and not empty.
                    string executeAsUser = (string)this.GetPropValue("ExecutionContextPrincipal");
                    if (string.Empty == executeAsUser)
                    {
                        executionContext = string.Format(CultureInfo.InvariantCulture, "USER ({0})", executeAsUser);
                    }
                    else
                    {
                        executionContext = "USER";
                    }
                    break;
            }

            return executionContext;

        }


        /// <summary>
        /// Returns a string representation of the ExecutionContext enumeration
        /// </summary>
        /// <param name="ec"></param>
        /// <returns></returns>
        private string GetExecutionContextString(ExecutionContext ec)
        {
            string executionContext = string.Empty;

            switch (ec)
            {
                case ExecutionContext.Caller:
                    executionContext = "CALLER";
                    break;

                case ExecutionContext.Owner:
                    executionContext = "OWNER";
                    break;

                case ExecutionContext.Self:
                    executionContext = "SELF";
                    break;

                case ExecutionContext.ExecuteAsUser:
                    // Verify that an ExecuteAsUser property is set and not empty.
                    string executeAsUser = (string)this.GetPropValue("ExecutionContextPrincipal");
                    if (string.Empty == executeAsUser)
                    {
                        executionContext = string.Format(CultureInfo.InvariantCulture, "USER ({0})", executeAsUser);
                    }
                    else
                    {
                        executionContext = "USER";
                    }
                    break;
            }

            return executionContext;

        }

        /// <summary>
        /// get the text of the object, if not available, throw apropiate exception
        /// based on the State of the object
        /// </summary>
        /// <param name="requestingProperty">name of the property that should appear in the exception as failed to be
        /// retrived, useful when we have properties derived from Text</param>
        /// <param name="sp"></param>
        /// <param name="bThrowIfCreating">if true throw also if the state of the object is Creating</param>
        /// <returns></returns>
        string GetTextProperty(string requestingProperty, ScriptingPreferences sp, bool bThrowIfCreating)
        {
        if (this.IsDesignMode)
            {
                return GetTextPropertyDesignMode(requestingProperty, sp, bThrowIfCreating);
            }

            if (this.ServerVersion.Major < 9 && State != SqlSmoState.Creating)
            {
                if (this.Properties.Contains("IsEncrypted") && true == (bool)GetPropValue("IsEncrypted"))
                {
                    throw new PropertyCannotBeRetrievedException(requestingProperty, this, ExceptionTemplates.ReasonTextIsEncrypted);
                }
            }

            string text = (string)this.GetPropValueOptional("Text");

            // check to see if we have this property
            if (this.Properties.Contains("ExecutionContext") && null != sp && sp.TargetServerVersion == SqlServerVersion.Version80)
            {

                Property prop = this.Properties["ExecutionContext"];

                // if we have an execution context, we need throw an exception
                if (null != prop.Value)
                {
                    string errorMessage = string.Empty;
                    bool contextExecutionIsCaller = true;
                    string sqlServerVersion = GetSqlServerName(sp);
                    string fullObjectName = FormatFullNameForScripting(sp, true);

                    switch (this.GetType().Name)
                    {
                        case "StoredProcedure":
                            // get the execution context
                            ExecutionContext ec = (ExecutionContext)prop.Value;

                            if (ec != ExecutionContext.Caller)
                            {
                                errorMessage = ExceptionTemplates.StoredProcedureDownlevelExecutionContext(
                                            fullObjectName,
                                            GetExecutionContextString(ec),
                                            sqlServerVersion);

                                contextExecutionIsCaller = false;
                            }



                            break;

                        case "UserDefinedFunction":
                            // get the execution context
                            ExecutionContext udfec = (ExecutionContext)prop.Value;

                            if (udfec != ExecutionContext.Caller)
                            {
                                errorMessage = ExceptionTemplates.UserDefinedFunctionDownlevelExecutionContext(
                                                fullObjectName,
                                                GetExecutionContextString(udfec),
                                                sqlServerVersion);

                                contextExecutionIsCaller = false;
                            }

                            break;

                        case "Trigger":
                            // get the execution context
                            ExecutionContext tec = (ExecutionContext)prop.Value;

                            if (tec != ExecutionContext.Caller)
                            {
                                errorMessage = ExceptionTemplates.TriggerDownlevelExecutionContext(
                                                fullObjectName,
                                                GetExecutionContextString(tec),
                                                sqlServerVersion);

                                contextExecutionIsCaller = false;
                            }

                            break;

                        case "DatabaseDdlTrigger":
                            // get the execution context
                            DatabaseDdlTriggerExecutionContext ddtcc = (DatabaseDdlTriggerExecutionContext)prop.Value;

                            if (ddtcc != DatabaseDdlTriggerExecutionContext.Caller)
                            {
                                errorMessage = ExceptionTemplates.TriggerDownlevelExecutionContext(
                                                fullObjectName,
                                                GetExecutionContextString(ddtcc),
                                                sqlServerVersion);

                                contextExecutionIsCaller = false;
                            }
                            break;

                        case "ServerDdlTrigger":
                            // check for execution principal to create string to match and strip
                            ServerDdlTriggerExecutionContext sdtec = (ServerDdlTriggerExecutionContext)prop.Value;

                            if (sdtec != ServerDdlTriggerExecutionContext.Caller)
                            {
                                errorMessage = ExceptionTemplates.TriggerDownlevelExecutionContext(
                                                fullObjectName,
                                                GetExecutionContextString(sdtec),
                                                sqlServerVersion);

                                contextExecutionIsCaller = false;
                            }

                            break;

                    }

                    if (!contextExecutionIsCaller)
                    {
                        throw new InvalidSmoOperationException(errorMessage);
                    }
                }
            }

        if (null == text || text.Length <= 0)
            {
                //different exceptions thrown in different contexts
                //if fail to get the text
                //user must see the exception as coming from the requested property
                if (State != SqlSmoState.Creating)
                {
                    if (this.Properties.Contains("IsEncrypted") && true == (bool)GetPropValue("IsEncrypted"))
                    {
                        throw new PropertyCannotBeRetrievedException(requestingProperty, this, ExceptionTemplates.ReasonTextIsEncrypted);
                    }
                    throw new PropertyCannotBeRetrievedException(requestingProperty, this);
                }
                else if (bThrowIfCreating)
                {
                    throw new PropertyNotSetException(requestingProperty);
                }
            }
            return text;
        }

        private string GetTextPropertyDesignMode(string requestingProperty, ScriptingPreferences sp, bool bThrowIfCreating)
        {
            if (null != sp && sp.TargetServerVersion == SqlServerVersion.Version80)
            {
                //We are currently not supporting text mode object for SQL Server 2000
                //Need to fix this once we start supporting
                string errorMessage = ExceptionTemplates.InvalidVersionSmoOperation(LocalizableResources.ServerShiloh);
                throw new UnsupportedVersionException(errorMessage);
            }

            string text = this.GetPropValueOptional("Text") as string;
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            StringBuilder sb = new StringBuilder();
            if ((this.m_textHeader != null) && (this.m_textBody != null))
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, this.m_textHeader);
                //text header should end in space
                if (this.m_textHeader.Length > 0 && !char.IsWhiteSpace(this.m_textHeader[this.m_textHeader.Length - 1]))
                {
                    //else check if the text body starts with space
                    if (this.m_textBody.Length > 0 && !char.IsWhiteSpace(this.m_textBody[0]))
                    {
                        //else we need to add a new line
                        sb.AppendFormat(SmoApplication.DefaultCulture, sp.NewLine);
                    }
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, this.m_textBody);
            }

            if (string.IsNullOrEmpty(sb.ToString()))
            {
                if (State != SqlSmoState.Creating)
                {
                    //We are not checking for encrypted properties we have to check this once we fix encryption related issues
                    throw new PropertyCannotBeRetrievedException(requestingProperty, this);
                }
                else if (bThrowIfCreating)
                {
                    throw new PropertyNotSetException(requestingProperty);
                }
            }

            return sb.ToString();
        }

        #endregion

    }

}



