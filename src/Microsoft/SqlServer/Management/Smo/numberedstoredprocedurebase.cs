// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Numbered")]
    public partial class NumberedStoredProcedure : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IAlterable, ITextObject
    {
        internal NumberedStoredProcedure() : base()
        {
        }

        internal NumberedStoredProcedure(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public NumberedStoredProcedure(StoredProcedure storedProcedure, short number)
            : base()
        {
            SetParentImpl(storedProcedure);
            this.key = new NumberedObjectKey(number);
            SetState(SqlSmoState.Creating);
            this.Properties.SetValue(this.Properties.LookupID("Name", PropertyAccessPurpose.Write),
                this.Parent.Name + ";" + number);
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public StoredProcedure Parent
        {
            get
            {
                CheckObjectState();
                return base.ParentColl.ParentInstance as StoredProcedure;
            }
            internal set { SetParentImpl(value); }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Int16 Number
        {
            get { return ((NumberedObjectKey) key).Number; }
        }

        private NumberedStoredProcedureParameterCollection m_Params = null;

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny,
            typeof (NumberedStoredProcedureParameter))]
        public NumberedStoredProcedureParameterCollection Parameters
        {
            get
            {
                CheckObjectState();
                if (null == m_Params)
                {
                    m_Params = new NumberedStoredProcedureParameterCollection(this);
                    SetCollectionTextMode(this.TextMode, m_Params);
                }
                return m_Params;
            }
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get { return "Numbered"; }
        }

        private void ScriptSP(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            ScriptInternal(queries, sp, scriptHeaderType);
        }

        internal override string FormatFullNameForScripting(ScriptingPreferences sp)
        {
            return this.Parent.FormatFullNameForScripting(sp) + ";" + this.Number;
        }

        private void ScriptSPHeaderInternal(StringBuilder sb, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            switch (scriptHeaderType)
            {
                case ScriptHeaderType.ScriptHeaderForCreate:
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0} PROCEDURE {1}", Scripts.CREATE,
                        FormatFullNameForScripting(sp));
                    break;
                case ScriptHeaderType.ScriptHeaderForAlter:
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0} PROCEDURE {1}", Scripts.ALTER,
                        FormatFullNameForScripting(sp));
                    break;
                case ScriptHeaderType.ScriptHeaderForCreateOrAlter:
                    throw new SmoException(ExceptionTemplates.ScriptHeaderTypeNotSupported(
                        scriptHeaderType.ToString(),
                        this.GetType().Name,
                        this.Name));
                default:
                    throw new SmoException(ExceptionTemplates.UnknownEnumeration(scriptHeaderType.ToString()));
            }

            sb.Append(sp.NewLine);

            bool bFirst = true;
            StringCollection param_strings = new StringCollection();
            ScriptingPreferences soParam = sp;
            soParam.DataType.XmlNamespaces = true; //numbered stored procedures do not support this
            foreach (NumberedStoredProcedureParameter spp in Parameters)
            {
                if (spp.State == SqlSmoState.ToBeDropped)
                {
                    continue;
                }
                if (String.Empty == spp.Name)
                {
                    continue;
                }

                if (!bFirst)
                {
                    sb.Append(",");
                    sb.Append(sp.NewLine);
                }
                bFirst = false;
                spp.ScriptDdlInternal(param_strings, soParam);
                sb.AppendFormat(SmoApplication.DefaultCulture, "\t");
                sb.Append(param_strings[0]);
                param_strings.Clear();
            }
            if (!bFirst)
            {
                sb.Append(sp.NewLine);
            }

            bool bNeedsComma = false;
            AppendWithOption(sb, "IsEncrypted", "ENCRYPTION", ref bNeedsComma);
            if (bNeedsComma) //if options were added then go to next line
            {
                sb.Append(sp.NewLine);
            }

            sb.Append("AS");
        }

        private void ScriptSPBodyInternal(StringBuilder sb)
        {
            sb.Append(GetTextBody(true));
        }

        private void ScriptInternal(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // retrieve full scripting name
            string sFullScriptingName = FormatFullNameForScripting(sp);

            ScriptInternalCreateDdl(queries, sp, scriptHeaderType);
            sb.Length = 0;
        }

        public override string ToString()
        {
            return FormatFullNameForScripting(new ScriptingPreferences());
        }

        internal void ScriptInternalCreateDdl(StringCollection queries, ScriptingPreferences sp,
            ScriptHeaderType scriptHeaderType)
        {
            string parentScriptingName = SqlString(Parent.FormatFullNameForScripting(sp));
            StringBuilder sb = new StringBuilder();
            ScriptInformativeHeaders(sp, sb);

            object ansi, qi;
            ScriptAnsiQI(this.Parent, sp, queries, sb, out ansi, out qi);

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    if (sp.TargetServerVersion <= SqlServerVersion.Version80)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                            Scripts.INCLUDE_EXISTS_NUMBERED_PROCEDURE80, "NOT",
                            parentScriptingName, this.Number);
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                            Scripts.INCLUDE_EXISTS_NUMBERED_PROCEDURE90, "NOT",
                            parentScriptingName, this.Number);
                    }
                    sb.Append(Globals.newline);
                    sb.Append("BEGIN");
                    sb.Append(Globals.newline);
                }
            }

            StringBuilder spBody = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (false == this.TextMode ||
                (true == sp.OldOptions.EnforceScriptingPreferences && true == sp.DataType.UserDefinedDataTypesToBaseType))
            {
                if (!sp.OldOptions.DdlBodyOnly)
                {
                    ScriptSPHeaderInternal(spBody, sp, scriptHeaderType);
                    spBody.Append(sp.NewLine);
                }

                if (!sp.OldOptions.DdlHeaderOnly)
                {
                    ScriptSPBodyInternal(spBody);
                }
            }
            else
            {
                spBody.Append(GetTextForScript(
                    sp,
                    new String[] {"procedure", "proc"},
                    forceCheckNameAndManipulateIfRequired: false,
                    scriptHeaderType: scriptHeaderType));
            }

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "execute sp_executesql @statement = {0} ",
                    MakeSqlString(spBody.ToString()));
                sb.Append(Globals.newline);
                sb.Append("END");
            }
            else
            {
                sb.Append(spBody.ToString());
            }

            queries.Add(sb.ToString());

        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptSP(queries, sp, ScriptHeaderType.ScriptHeaderForCreate);
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
                InitializeKeepDirtyValues();
                ScriptSP(alterQuery, sp, ScriptHeaderType.ScriptHeaderForAlter);
            }
        }

        protected override bool IsObjectDirty()
        {
            return base.IsObjectDirty() || IsCollectionDirty(Parameters);
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[]
            {
                new PropagateInfo(Parameters, false)
            };
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get { return this.Parent.Name + ";" + this.Number; }
            set { throw new FailedOperationException(ExceptionTemplates.SetName, this, null); }
        }

        #region TextModeImpl

        public string ScriptHeader(bool forAlter)
        {
            CheckObjectState();
            return GetTextHeader(forAlter);
        }

        public string ScriptHeader(ScriptHeaderType scriptHeaderType)
        {
            if (ScriptHeaderType.ScriptHeaderForCreateOrAlter == scriptHeaderType)
            {
                throw new NotSupportedException(
                    ExceptionTemplates.CreateOrAlterNotSupported(this.GetType().Name));
            }
            else
            {
                return GetTextHeader(scriptHeaderType);
            }
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone)]
        public string TextBody
        {
            get
            {
                CheckObjectState();
                return GetTextBody();
            }
            set
            {
                CheckObjectState();
                SetTextBody(value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone)]
        public string TextHeader
        {
            get
            {
                CheckObjectState();
                return GetTextHeader(false);
            }
            set
            {
                CheckObjectState();
                SetTextHeader(value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool TextMode
        {
            get
            {
                CheckObjectState();
                return GetTextMode();
            }
            set
            {
                CheckObjectState();
                SetTextMode(value, new SmoCollectionBase[] {Parameters});
            }
        }


        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "IsEncrypted")
            {
                Validate_set_TextObjectDDLProperty(prop, value);
            }
        }

        protected override string GetBraketNameForText()
        {
            return MakeSqlBraket(this.Parent.Name) + ";" + this.Number;
        }

        #endregion

        internal static string[] GetScriptFields(Type parentType,
            Cmn.ServerVersion version,
            Cmn.DatabaseEngineType databaseEngineType,
            Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            return new string[]
            {
                "Number",
            };
        }
    }
}
