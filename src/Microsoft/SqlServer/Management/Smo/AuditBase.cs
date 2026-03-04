// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Audit : ScriptNameObjectBase, ICreatable, IAlterable, IDroppable, IDropIfExists, IRenamable, IScriptable
    {
        internal Audit(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state) { }

        public static string UrnSuffix
        {
            get
            {
                return "Audit";
            }
        }


        /// <summary>
        /// Name of Audit
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

        /// <summary>
        /// Create an Audit
        /// </summary>
        public void Create()
        {
            this.ThrowIfNotSupported(typeof(Audit));
            base.CreateImpl();
        }

        /// <summary>
        /// Alter an existing Audit
        /// </summary>
        public void Alter()
        {
            this.ThrowIfNotSupported(typeof(Audit));
            base.AlterImpl();
        }

        /// <summary>
        /// Drop an existing Audit with force drop Audit Specifications
        /// </summary>
        public void Drop()
        {
            this.ThrowIfNotSupported(typeof(Audit));
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            this.ThrowIfNotSupported(typeof(Audit));
            base.DropImpl(true);
        }

        /// <summary>
        /// Rename an existing Audit
        /// </summary>
        /// <param name="newname"></param>
        public void Rename(string newname)
        {
            this.ThrowIfNotSupported(typeof(Audit));
            base.RenameImpl(newname);
        }

        /// <summary>
        /// Script an Audit
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            this.ThrowIfNotSupported(typeof(Audit));
            return base.ScriptImpl();
        }

        /// <summary>
        /// Script audit with specific scripting optiions
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            this.ThrowIfNotSupported(typeof(Audit));
            return base.ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Enable an Audit
        /// </summary>
        public void Enable()
        {
            this.EnableDisable(true);
        }

        /// <summary>
        /// Disable an Audit
        /// </summary>
        public void Disable()
        {
            this.EnableDisable(false);
        }

        /// <summary>
        /// Enable or Disable a Server Audit
        /// </summary>
        /// <param name="enable">true enables the audit specification</param>
        private void EnableDisable(bool enable)
        {
            this.ThrowIfNotSupported(typeof(Audit));
            CheckObjectState();
            try
            {
                StringCollection query = new StringCollection();
                AddDatabaseContext(query);

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER SERVER AUDIT {0}", FullQualifiedName);
                sb.AppendLine();

                if (enable)
                {
                    sb.Append("WITH (STATE = ON)");
                }
                else
                {
                    sb.Append("WITH (STATE = OFF)");
                }

                query.Add(sb.ToString());


                if (!this.IsDesignMode)
                {
                    ExecutionManager.ExecuteNonQuery(query);
                }

                Property p = this.Properties.Get("Enabled");
                p.SetValue(enable);
                p.SetRetrieved(true);

                if (!this.ExecutionManager.Recording)
                {
                    if (!SmoApplication.eventsSingleton.IsNullObjectAltered())
                    {
                        SmoApplication.eventsSingleton.CallObjectAltered(GetServerObject(), new ObjectAlteredEventArgs(this.Urn, this));
                    }
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                if (enable)
                {
                    throw new FailedOperationException(ExceptionTemplates.Enable, this, e);
                }
                else
                {
                    throw new FailedOperationException(ExceptionTemplates.Disable, this, e);
                }
            }
        }

        public string EnumServerAuditSpecification()
        {
            this.ThrowIfNotSupported(typeof(Audit));
            CheckObjectState();
            Urn urn = new Urn("Server/ServerAuditSpecification[@Guid='" + this.Guid.ToString() + "']");
            string[] fields = new string[] { "Name" };
            Request request = new Request(urn, fields);
            DataTable dt = this.ExecutionManager.GetEnumeratorData(request);
            Debug.Assert(dt.Rows.Count <= 1, "There can be max one ServerAuditSpecification per Audit");
            if (dt.Rows.Count == 1)
            {
                return dt.Rows[0]["Name"].ToString();
            }
            return string.Empty;
        }

        public DataTable EnumDatabaseAuditSpecification()
        {
            this.ThrowIfNotSupported(typeof(Audit));
            CheckObjectState();
            Urn urn = new Urn("Server/Database/DatabaseAuditSpecification[@Guid='" + this.Guid.ToString() + "']");
            string[] fields = new string[] { "DatabaseName", "Name" };
            Request request = new Request(urn, fields);
            return this.ExecutionManager.GetEnumeratorData(request);
        }

        /// <summary>
        /// Generate the create server audit script
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            ScriptAudit(query, sp, true);
        }

        /// <summary>
        /// Generate the alter server audit script
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            ScriptAudit(query, sp, false);
        }

        /// <summary>
        /// Generate the alter server audit script to rename the audit
        /// </summary>
        /// <param name="renameQuery"></param>
        /// <param name="so"></param>
        /// <param name="newName"></param>
        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER SERVER AUDIT {0} MODIFY NAME = {1}", FormatFullNameForScripting(sp), MakeSqlBraket(newName));
            renameQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Generate the drop server audit script with force drop audit specifications
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void ScriptDrop(StringCollection query, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AUDIT, "", FormatFullNameForScripting(sp, false));
                sb.AppendLine();
            }
            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP SERVER AUDIT {0}", FormatFullNameForScripting(sp));

            query.Add(sb.ToString());

        }

        private void ValidateManagedInstanceProperty(ScriptingPreferences sp, Property property)
        {
            if (sp.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance && sp.TargetDatabaseEngineEdition != DatabaseEngineEdition.SqlOnDemand)
            {
                throw new UnsupportedEngineEditionException(ExceptionTemplates.InvalidPropertyValueForVersion(
                                                this.GetType().Name,
                                                property.Name,
                                                property.Value.ToString(),
                                                GetSqlServerVersionName()));
            }
        }

        //
        // CREATE/ALTER SERVER AUDIT audit_name
        //     TO { [FILE (<file_options> [,..n]) ] | APPLICATION_LOG | SECURITY_LOG | EXTERNAL_MONITOR | URL (<url_options> [,..n]) }
        //     [ WITH ( <audit_options> [,..n] ) ]
        //
        // <file_options> ::=
        // { FILEPATH = 'filepath'
        // [, MAXSIZE = { int {MB | GB | TB } | UNLIMITED} ]
        // [, MAX_ROLLOVER_FILES = int ]
        // [, RESERVE_DISK_SPACE = {ON|OFF ] }
        //
        // <url_options ::=
        // { PATH = 'blobpath'}
        // [, RETENTION_DAYS = int] }
        //
        // <audit_options> ::=
        // {
        // [ QUEUE_DELAY = int ]
        // [, ON_FAILURE = CONTINUE | SHUTDOWN ]
        // [, AUDIT_GUID = uniqueidentifier ]
        // [, OPERATOR_AUDIT = ON | OFF ]
        // }
        //
        private void ScriptAudit(StringCollection query, ScriptingPreferences sp, bool create)
        {
            ThrowIfBelowVersion100(sp.TargetServerVersion);
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (create && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AUDIT, "NOT", FormatFullNameForScripting(sp, false));
                sb.AppendLine();
            }
            sb.Append(create ? "CREATE SERVER AUDIT " : "ALTER SERVER AUDIT ");
            sb.Append(FormatFullNameForScripting(sp));
            sb.AppendLine();

            Property destinationType = this.Properties.Get("DestinationType");
            if (create || destinationType.Dirty || NeedToScriptTO())
            {
                sb.Append("TO ");
                if (destinationType.IsNull)
                {
                    throw new PropertyNotSetException("DestinationType");
                }

                TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(AuditDestinationType));
                string destination = typeConverter.ConvertToInvariantString(destinationType.Value);

                switch ((AuditDestinationType)destinationType.Value)
                {
                    case AuditDestinationType.File:
                        sb.Append(destination).Append(' ');
                        sb.Append(ScriptFileOptions(create, destinationType.Dirty, sp));
                        break;
                    case AuditDestinationType.ApplicationLog:
                    case AuditDestinationType.SecurityLog:
                        sb.Append(destination);
                        break;
                    case AuditDestinationType.Url:
                        ValidateManagedInstanceProperty(sp, destinationType);

                        sb.Append(destination).Append(' ');
                        sb.Append(ScriptUrlOptions(create));
                        break;
                    case AuditDestinationType.ExternalMonitor:
                        ValidateManagedInstanceProperty(sp, destinationType);

                        sb.Append(destination);
                        break;
                    default:
                        throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("DestinationType"));
                }
            }

            sb.Append(ScriptAuditOptions(create, sp));

            if (this.IsSupportedProperty("Filter"))
            {
                Property filter = this.GetPropertyOptional("Filter");
                string strFilter = filter.Value as string;

                if (sp.TargetServerVersion >= SqlServerVersion.Version110
                    && sp.TargetDatabaseEngineType == DatabaseEngineType.Standalone)
                {
                    if ((create || filter.Dirty) && !string.IsNullOrEmpty(strFilter))
                    {
                        sb.AppendLine();
                        sb.Append("WHERE ");
                        sb.Append(strFilter);
                    }
                    else if (!create && filter.Dirty)
                    {
                        sb.AppendLine();
                        sb.Append("REMOVE WHERE");
                    }
                }
                else if (!string.IsNullOrEmpty(strFilter))
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.PropertySupportedOnlyOn110("Filter")).SetHelpContext("SupportedOnlyOn110");
                }
            }

            if (create)
            {
                Property enabled = this.Properties.Get("Enabled");
                if (!enabled.IsNull)
                {
                    sb.AppendLine();
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER SERVER AUDIT {0} WITH (STATE = {1})", this.FullQualifiedName, (bool)enabled.Value ? "ON" : "OFF");
                }

                query.Add(sb.ToString());
            }
            else if (this.Properties.Dirty || NeedToScriptTO())
            {
                query.Add(sb.ToString());
            }

        }

        private bool NeedToScriptTO()
        {
            Property filePath = this.Properties.Get("FilePath");
            Property maxRolloverFiles = this.Properties.Get("MaximumRolloverFiles");
            Property reserveDiskSpace = this.Properties.Get("ReserveDiskSpace");
            Property maxFileSizeInAcceptedRange = this.Properties.Get("MaximumFileSize");
            Property maxFileSizeUnit = this.Properties.Get("MaximumFileSizeUnit");

            bool result = filePath.Dirty || maxRolloverFiles.Dirty || reserveDiskSpace.Dirty || maxFileSizeInAcceptedRange.Dirty || maxFileSizeUnit.Dirty;

            if (this.IsSupportedProperty("MaximumFiles"))
            {
                Property maxFiles = this.GetPropertyOptional("MaximumFiles");
                result = result || maxFiles.Dirty;
            }

            if (this.IsSupportedProperty("RetentionDays"))
            {
                Property retentionDays = this.properties.Get("RetentionDays");
                result = result || retentionDays.Dirty;
            }

            return result;
        }

        /// <summary>
        /// Scripts single audit option within "WITH" clause
        /// </summary>
        /// <param name="create">Whether create script is requested</param>
        /// <param name="sp">Scripting preferences</param>
        /// <param name="propertyName">Property name to script</param>
        /// <param name="optionName">Option to script</param>
        /// <param name="format">Option format</param>
        /// <param name="valueResolver">Callback to resolve the actual value from the property value</param>
        /// <param name="ssb">Buffer to aggregate the WITH clause</param>
        private void ScriptAuditOption(bool create, ScriptingPreferences sp, 
            string propertyName, string optionName, ParameterValueFormat format,
            Func<object, string> valueResolver, ref ScriptStringBuilder ssb)
        {
            if (!IsSupportedProperty(propertyName, sp))
            {
                return;
            }

            Property property = Properties.Get(propertyName);

            if (property != null && (property.Dirty || create))
            {
                // Can't use property.IsNull to test whether property value is null or not 
                // as property might not be fetched yet because Audit is a server-level object
                //
                object propertyValue = GetPropValueOptional(propertyName);

                if (propertyValue == null)
                {
                    return;
                }

                string value = valueResolver == null ? propertyValue.ToString() : valueResolver(propertyValue);

                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                if (ssb == null)
                {
                    ssb = new ScriptStringBuilder(" WITH");
                }                

                if (format == ParameterValueFormat.CharString)
                {
                    value = Util.EscapeString(value, '\'');
                }                

                ssb.SetParameter(optionName, value, format);
            }
        }

        private string ScriptAuditOptions(bool create, ScriptingPreferences sp)
        {
            ScriptStringBuilder ssb = null;

            ScriptAuditOption(create, sp, nameof(QueueDelay), "QUEUE_DELAY", ParameterValueFormat.NotString, null, ref ssb);
            ScriptAuditOption(create, sp, nameof(OnFailure), "ON_FAILURE", ParameterValueFormat.NotString,
                propertyValue =>
                {
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(OnFailureAction));
                    string failureAction = typeConverter.ConvertToInvariantString(propertyValue);

                    switch ((OnFailureAction)propertyValue)
                    {
                        case OnFailureAction.Continue:
                        case OnFailureAction.Shutdown:
                            return failureAction;

                        case OnFailureAction.FailOperation:
                            if (sp.TargetServerVersion >= SqlServerVersion.Version110 && sp.TargetDatabaseEngineType == DatabaseEngineType.Standalone)
                            {
                                return failureAction;
                            }

                            throw new UnsupportedVersionException(ExceptionTemplates.PropertyValueSupportedOnlyOn110(nameof(OnFailure), "FailOperation")).SetHelpContext("PropertyValueSupportedOnlyOn110");

                        default:
                            throw new ArgumentException(ExceptionTemplates.UnknownEnumeration(nameof(OnFailure)));
                    }
                }, 
                ref ssb);
            ScriptAuditOption(create, sp, nameof(Guid), "AUDIT_GUID", ParameterValueFormat.CharString, null, ref ssb);
            ScriptAuditOption(create, sp, nameof(IsOperator), "OPERATOR_AUDIT", ParameterValueFormat.NotString,
                propertyValue => bool.Parse(propertyValue.ToString()) ? "ON" : "OFF",
                ref ssb);

            return ssb == null ? string.Empty : ssb.ToString(scriptSemiColon: false);
        }

        private string ScriptFileOptions(bool create, bool mustHaveFilePath, ScriptingPreferences sp)
        {
            bool needComma = false;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append(Globals.LParen);
            sb.Append(Globals.tab);
            Property filePath = this.Properties.Get("FilePath");
            if ((create || mustHaveFilePath) && filePath.IsNull)
            {
                throw new PropertyNotSetException("FilePath");
            }

            if (filePath.Dirty || create || mustHaveFilePath)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "FILEPATH = {0}", MakeSqlString(filePath.Value.ToString()));
                sb.AppendLine();
                needComma = true;
            }

            Property maxFileSizeInAcceptedRange = this.Properties.Get("MaximumFileSize");
            Property maxFileSizeUnit = this.Properties.Get("MaximumFileSizeUnit");
            if (!maxFileSizeInAcceptedRange.IsNull
                && (create
                    || maxFileSizeInAcceptedRange.Dirty
                    || (!maxFileSizeUnit.IsNull && maxFileSizeUnit.Dirty)
                    )
                )
            {
                this.AppendCommaOption(sb, "MAXSIZE = {0} ", maxFileSizeInAcceptedRange.Value.ToString(), false, ref needComma);

                if (maxFileSizeUnit.IsNull)
                {
                    sb.Append(AuditFileSizeUnit.Mb.ToString().ToUpperInvariant()); //default
                }
                else
                {
                    if (!Enum.IsDefined(typeof(AuditFileSizeUnit), maxFileSizeUnit.Value))
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("AuditFileSizeUnit"));
                    }

                    sb.Append(maxFileSizeUnit.Value.ToString().ToUpperInvariant());
                }

                sb.AppendLine();
            }

            Property maxRolloverFiles = this.Properties.Get("MaximumRolloverFiles");
            bool maxFilesScripted = false;

            //script only if we are on Denali or target Denali and later
            if (this.IsSupportedProperty("MaximumFiles"))
            {
                Property maxFiles = this.GetPropertyOptional("MaximumFiles");

                if (maxRolloverFiles.Dirty && maxFiles.Dirty && (System.Int64)maxRolloverFiles.Value != int.MaxValue && (int)maxFiles.Value != 0)
                {
                    throw new WrongPropertyValueException(
                        ExceptionTemplates.MutuallyExclusiveProperties("MaximumRolloverFiles",
                        "MaximumFiles"));
                }

                if ((maxFiles.Dirty || (create && !maxFiles.IsNull)) && ((int)maxFiles.Value != 0 || (!maxRolloverFiles.Dirty && !create)))
                {
                    if (sp.TargetServerVersion >= SqlServerVersion.Version110
                        && sp.TargetDatabaseEngineType == DatabaseEngineType.Standalone)
                    {
                        //script max files
                        this.AppendCommaOption(sb, "MAX_FILES = {0}", maxFiles.Value.ToString(), true, ref needComma);
                        maxFilesScripted = true;
                    }
                    else
                    {
                        throw new UnsupportedVersionException(ExceptionTemplates.PropertySupportedOnlyOn110("MaximumFiles")).SetHelpContext("SupportedOnlyOn110");
                    }
                }
            }

            if ((maxRolloverFiles.Dirty || (create && !maxRolloverFiles.IsNull)) && !maxFilesScripted)
            {
                this.AppendCommaOption(sb, "MAX_ROLLOVER_FILES = {0}", maxRolloverFiles.Value.ToString(), true, ref needComma);
            }

            Property reserveDiskSpace = this.Properties.Get("ReserveDiskSpace");
            if (reserveDiskSpace.Dirty || (create && !reserveDiskSpace.IsNull))
            {
                this.AppendCommaOption(sb, "RESERVE_DISK_SPACE = {0}", (bool)reserveDiskSpace.Value ? "ON" : "OFF", true, ref needComma);
            }
            sb.Append(Globals.RParen);
            return sb.ToString();
        }

        private string ScriptUrlOptions(bool create)
        {
            ScriptStringBuilder ssb = new ScriptStringBuilder(string.Empty);

            Property filePath = this.Properties.Get("FilePath");
            if (create && filePath.IsNull)
            {
                throw new PropertyNotSetException("FilePath");
            }

            // Add Blob path
            if (filePath.Dirty || create)
            {
                ssb.SetParameter("PATH", MakeSqlString(filePath.Value.ToString()), ParameterValueFormat.NotString);
            }

            // Add retention days
            if (this.IsSupportedProperty("RetentionDays"))
            {
                Property retentionDays = this.properties.Get("RetentionDays");
                if ((int)retentionDays.Value < 0)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.WrongPropertyValueException("RetentionDays", retentionDays.Value.ToString()));
                }
                if ((retentionDays.Dirty && !retentionDays.IsNull))
                {
                    ssb.SetParameter("RETENTION_DAYS", retentionDays.Value.ToString(), ParameterValueFormat.NotString);
                }
            }

            return ssb.ToString(scriptSemiColon: false);
        }

        private void AppendCommaOption(StringBuilder sb, string optionName, string optionValue, bool appendLine, ref bool needComma)
        {
            sb.Append(Globals.tab);
            if (needComma)
            {
                sb.Append(Globals.comma);
            }
            else
            {
                needComma = true;
            }
            sb.AppendFormat(SmoApplication.DefaultCulture, optionName, optionValue);

            if (appendLine)
            {
                sb.AppendLine();
            }
        }
    }
}
