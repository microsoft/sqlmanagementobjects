// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

using Cmn = Microsoft.SqlServer.Management.Common;
using Diagnostics = Microsoft.SqlServer.Management.Diagnostics;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ExtendedProperty : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IMarkForDrop, IScriptable, Cmn.ICreateOrAlterable
    {
        internal ExtendedProperty(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public ExtendedProperty(SqlSmoObject parent, string name, object propertyValue)
            : base()
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = parent;
            this.Value = propertyValue;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "ExtendedProperty";
            }
        }

        //Overrides the GetDBName to avoid the acess in the ParentCollection of the Default Constraint
        //which is (null)
        internal protected override string GetDBName()
        {
            return this.Parent.GetDBName();
        }

        String GetParams(SqlSmoObject objParent, ScriptingPreferences sp)
        {
            ScriptingParameters parameters = new ScriptingParameters(objParent, sp);
            return parameters.GetDDLParam();
        }

        //
        // ExtendedProperty.ScriptingParameters
        // Given extended property parent, work out type/name paramters that uniquely identify it.
        // Methods GetDDLParam and GetIfNotExistParam return parameter lists formatted for sp_addextendedproperty
        // and fn_listextendedproperty correspondingly.
        //
        internal class ScriptingParameters
        {
            private struct TypeNamePair
            {
                internal string type;
                internal string name;
            }

            private TypeNamePair[] typeNames_;

            // Fills in the slot of typeNames_ pair array
            private void SetTypeNamePair(int level, string type, string name)
            {
                Diagnostics.TraceHelper.Assert(level >= 0 && level <= 2);

                // Trying to fill higher slots when lower are not filled indicates a bug
#if DEBUG
                for (int i = 0; i < level; ++i)
                {
                    Diagnostics.TraceHelper.Assert(typeNames_[i].type != null);
                    Diagnostics.TraceHelper.Assert(typeNames_[i].name != null);
                }
#endif

                typeNames_[level].type = type;
                typeNames_[level].name = SqlString(name);
            }

            // Sets level 0 type/name pair to ("SCHEMA", <object name>)
            private void SetSchema(SqlSmoObject objParent, ScriptingPreferences sp)
            {
                ScriptSchemaObjectBase o = objParent as ScriptSchemaObjectBase;
                if (null == o)
                {
                    throw new SmoException(ExceptionTemplates.CannotCreateExtendedPropertyWithoutSchema);
                }
                string type = sp.TargetServerVersion >= SqlServerVersion.Version90 ? "SCHEMA" : "USER";
                string name = o.Schema;

                SetTypeNamePair(0, type, name);
            }

            // Constructor does the figuring out of the set of type/name pairs
            internal ScriptingParameters(SqlSmoObject objParent, ScriptingPreferences sp)
            {
                typeNames_ = new TypeNamePair[3];
                for (int i = 0; i < typeNames_.Length; ++i)
                {
                    typeNames_[i].type = null; typeNames_[i].name = null;
                }

                string parentType = objParent.GetType().ToString();
                string typePrefix = "Microsoft.SqlServer.Management.Smo.";

                if (!parentType.StartsWith(typePrefix, StringComparison.Ordinal))
                {
                    return;
                }

                parentType = parentType.Remove(0, typePrefix.Length);

                switch (parentType)
                {
                    case "Database":
                        break;
                    case "Schema":
                        SetTypeNamePair(0, "SCHEMA", objParent.InternalName);
                        break;
                    case "DatabaseRole":
                    case "ApplicationRole":
                    case "User":
                        SetTypeNamePair(0, "USER", objParent.InternalName);
                        break;
                    case "UserDefinedDataType":
                        ScriptSchemaObjectBase o = objParent as ScriptSchemaObjectBase;
                        if (null != o)
                        {
                            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
                            {
                                SetTypeNamePair(0, "SCHEMA", o.Schema);
                                SetTypeNamePair(1, "TYPE", objParent.InternalName);
                            }
                            else
                            {
                                SetTypeNamePair(0, "TYPE", objParent.InternalName);
                            }
                        }
                        else
                        {
                            throw new SmoException(ExceptionTemplates.CannotCreateExtendedPropertyWithoutSchema);
                        }
                        break;
                    case "UserDefinedTableType":
                        ScriptSchemaObjectBase obj = objParent as ScriptSchemaObjectBase;
                        if (null != obj)
                        {
                            SetTypeNamePair(0, "SCHEMA", obj.Schema);
                            SetTypeNamePair(1, "TYPE", objParent.InternalName);
                        }
                        else
                        {
                            throw new SmoException(ExceptionTemplates.CannotCreateExtendedPropertyWithoutSchema);
                        }
                        break;
                    case "DdlTrigger":
                    case "DatabaseDdlTrigger":
                    case "ServerDdlTrigger":
                        SetTypeNamePair(0, "TRIGGER", objParent.InternalName);
                        break;
                    case "PlanGuide":
                        SetTypeNamePair(0, "PLAN GUIDE", objParent.InternalName);
                        break;
                    case "Table":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "TABLE", objParent.InternalName);
                        break;
                    case "View":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "VIEW", objParent.InternalName);
                        break;
                    case "ExtendedStoredProcedure":
                    case "StoredProcedure":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "PROCEDURE", objParent.InternalName);
                        break;
                    case "Synonym":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "SYNONYM", objParent.InternalName);
                        break;
                    case "Sequence":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "SEQUENCE", objParent.InternalName);
                        break;
                    case "Rule":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "RULE", objParent.InternalName);
                        break;
                    case "Default":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "DEFAULT", objParent.InternalName);
                        break;
                    case "UserDefinedFunction":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "FUNCTION", objParent.InternalName);
                        break;
                    case "Column":
                        CopyFrom(new ScriptingParameters(objParent.ParentColl.ParentInstance, sp));
                        SetTypeNamePair(2, "COLUMN", objParent.InternalName);
                        break;
                    case "Index":
                        Index idx = (Index)objParent;
                        string level2type;
                        CopyFrom(new ScriptingParameters(objParent.ParentColl.ParentInstance, sp));
                        if (IndexKeyType.None == idx.IndexKeyType)
                        {
                            level2type = "INDEX";
                        }
                        else
                        {
                            level2type = "CONSTRAINT";
                        }
                        SetTypeNamePair(2, level2type, objParent.InternalName);
                        break;
                    case "Trigger":
                        CopyFrom(new ScriptingParameters(objParent.ParentColl.ParentInstance, sp));
                        SetTypeNamePair(2, "TRIGGER", objParent.InternalName);
                        break;
                    case "UserDefinedFunctionParameter":
                    case "UserDefinedAggregateParameter":
                    case "StoredProcedureParameter":
                        CopyFrom(new ScriptingParameters(objParent.ParentColl.ParentInstance, sp));
                        SetTypeNamePair(2, "PARAMETER", objParent.InternalName);
                        break;
                    case "Check":
                        CopyFrom(new ScriptingParameters(objParent.ParentColl.ParentInstance, sp));
                        SetTypeNamePair(2, "CONSTRAINT", objParent.InternalName);
                        break;
                    case "ForeignKey":
                        CopyFrom(new ScriptingParameters(objParent.ParentColl.ParentInstance, sp));
                        SetTypeNamePair(2, "CONSTRAINT", objParent.InternalName);
                        break;
                    case "DefaultConstraint":
                        //step over column , go to the table level
                        CopyFrom(new ScriptingParameters(((DefaultConstraint)objParent).Parent.Parent, sp));
                        SetTypeNamePair(2, "CONSTRAINT", objParent.InternalName);
                        break;
                    case "XmlSchemaCollection":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "XML SCHEMA COLLECTION", objParent.InternalName);
                        break;
                    case "PartitionFunction":
                        SetTypeNamePair(0, "PARTITION FUNCTION", objParent.InternalName);
                        break;
                    case "PartitionScheme":
                        SetTypeNamePair(0, "PARTITION SCHEME", objParent.InternalName);
                        break;
                    case "SqlAssembly":
                        SetTypeNamePair(0, "ASSEMBLY", objParent.InternalName);
                        break;
                    case nameof(ExternalLanguage):
                        SetTypeNamePair(0, "EXTERNAL LANGUAGE", objParent.InternalName);
                        break;
                    case "ExternalLibrary":
                        SetTypeNamePair(0, "EXTERNAL LIBRARY", objParent.InternalName);
                        break;
                    case "UserDefinedType":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "TYPE", objParent.InternalName);
                        break;
                    case "UserDefinedAggregate":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "AGGREGATE", objParent.InternalName);
                        break;
                    case "Broker.MessageType":
                        SetTypeNamePair(0, "MESSAGE TYPE", objParent.InternalName);
                        break;
                    case "Broker.ServiceContract":
                        SetTypeNamePair(0, "CONTRACT", objParent.InternalName);
                        break;
                    case "Broker.BrokerService":
                        SetTypeNamePair(0, "SERVICE", objParent.InternalName);
                        break;
                    case "Broker.RemoteServiceBinding":
                        SetTypeNamePair(0, "REMOTE SERVICE BINDING", objParent.InternalName);
                        break;
                    case "Broker.ServiceRoute":
                        SetTypeNamePair(0, "ROUTE", objParent.InternalName);
                        break;
                    case "Broker.ServiceQueue":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "QUEUE", objParent.InternalName);
                        break;
                    case "SecurityPolicy":
                        SetSchema(objParent, sp);
                        SetTypeNamePair(1, "SECURITY POLICY", objParent.InternalName);
                        break;
                }
            }

            // Copy data from another ScriptingParameters object
            private void CopyFrom(ScriptingParameters other)
            {
                for (int i = 0; i < typeNames_.Length; ++i)
                {
                    typeNames_[i].type = other.typeNames_[i].type;
                    typeNames_[i].name = other.typeNames_[i].name;
                }
            }

            //
            // Returns parameter list for Creating/Dropping the extended property, as required by sp_addextendedproperty
            //
            internal string GetDDLParam()
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < typeNames_.Length; ++i)
                {
                    if (typeNames_[i].type != null)
                    {
                        sb.AppendFormat(", @level{0}type=N'{1}',@level{0}name=N'{2}'", i, typeNames_[i].type, typeNames_[i].name);
                    }
                }

                return sb.ToString();
            }

            //
            // Return parameter list for fn_listextendedproperty used in "IF NOT EXIST" check
            //
            internal string GetIfNotExistParam()
            {
                StringBuilder sb = new StringBuilder();

                foreach (TypeNamePair tn in typeNames_)
                {
                    if (tn.type != null)
                    {
                        sb.AppendFormat(", N'{0}',N'{1}'", tn.type, tn.name);
                    }
                    else
                    {
                        sb.AppendFormat(", NULL,NULL");
                    }
                }

                return sb.ToString();
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        private string GetPrefix(ScriptingPreferences sp)
        {
            Database db = this.Parent as Database;
            string prefix = string.Empty;

            if (sp.TargetServerVersion < SqlServerVersion.Version90)
            {
                prefix = "dbo";
            }
            else
            {
                prefix = "sys";
            }
            if(sp.IncludeScripts.DatabaseContext && db != null)
            {
                prefix = db.FormatFullNameForScripting(sp) + "." + prefix;
            }
            return prefix;
        }

        // Return "IF [NOT] EXIST" string for an extended property, given its name and list of parameters
        private string GetIfNotExistString(bool bCreate, string name, string param)
        {
            return String.Format(SmoApplication.DefaultCulture, "IF {0} EXISTS (SELECT * FROM sys.fn_listextendedproperty({1} {2}))", bCreate ? "NOT" : "", name, param);
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            object value = GetPropValueOptionalAllowNull("Value");

            bool isDateTime = (value is DateTime || value is System.Data.SqlTypes.SqlDateTime);
            ScriptingParameters parameters = new ScriptingParameters(this.ParentColl.ParentInstance, sp);

            GetScriptCreate(sb, sp, parameters, value, isDateTime);

            queries.Add(sb.ToString());
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

            ScriptingParameters parameters = new ScriptingParameters(this.ParentColl.ParentInstance, sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, GetIfNotExistString( /*bCreate =*/ false, FormatFullNameForScripting(sp, false), parameters.GetIfNotExistParam()));
                sb.AppendLine();
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC {2}.sp_dropextendedproperty @name={0} {1}",
                            FormatFullNameForScripting(sp, false),
                            parameters.GetDDLParam(),
                            GetPrefix(sp));
            sb.Append(sp.NewLine);
            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (!this.IsObjectDirty())
            {
                return;
            }
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            object value = GetPropValueOptional("Value");
            bool isDateTime = (value is DateTime || value is System.Data.SqlTypes.SqlDateTime);

            GetScriptAlter(sb, sp, value, isDateTime);

            sb.Append(sp.NewLine);
            alterQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Script for Create Or Alter 
        /// </summary>
        /// <param name="queries">container for lines that make up the scripted object</param>
        /// <param name="sp">Defines preferences for scripting</param>
        internal override void ScriptCreateOrAlter(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptingParameters parameters = new ScriptingParameters(this.ParentColl.ParentInstance, sp);
            object value = GetPropValueOptional("Value");
            bool isDateTime = (value is DateTime || value is System.Data.SqlTypes.SqlDateTime);

            //We are storing the old values of the scripting parameters
            bool DdlHeaderOnly = sp.OldOptions.DdlHeaderOnly;
            bool DdlBodyOnly = sp.OldOptions.DdlBodyOnly;
            bool ExistenceCheck = sp.IncludeScripts.ExistenceCheck;

            //We are using the below preferences to return "IF [NOT] EXIST" string to add an extended property, given its name and list of parameters.
            sp.OldOptions.DdlHeaderOnly = false;
            sp.OldOptions.DdlBodyOnly = false;
            sp.IncludeScripts.ExistenceCheck = true;

            try
            {
                //we are returning "IF [NOT] EXIST" string to add an extended property, given its name and list of parameters and "ELSE" string to update the existing extended property.
                //So, we are reusing the StringBuilder sb parameter.
                GetScriptCreate(sb, sp, parameters, value, isDateTime);

                sb.Append(sp.NewLine);
                sb.AppendFormat(String.Format(SmoApplication.DefaultCulture, "ELSE"));
                sb.AppendLine();
                sb.AppendLine(Scripts.BEGIN);

                sb.Append(Globals.tab);
                GetScriptAlter(sb, sp, value, isDateTime);

                sb.AppendLine();
                sb.AppendLine(Scripts.END);

                queries.Add(sb.ToString());
            }
            finally
            {
                //we are restoring the values back to old value.
                sp.OldOptions.DdlHeaderOnly = DdlHeaderOnly;
                sp.OldOptions.DdlBodyOnly = DdlBodyOnly;
                sp.IncludeScripts.ExistenceCheck = ExistenceCheck;
            }
            
        }

        private void GetScriptCreate(StringBuilder sb, ScriptingPreferences sp, ScriptingParameters parameters, object value, bool isDateTime)
        {
            bool nNeedIfNotExistCheck = !sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly && sp.IncludeScripts.ExistenceCheck;
            if (nNeedIfNotExistCheck)
            {
                sb.Append(GetIfNotExistString(bCreate: true, name: FormatFullNameForScripting(sp, false), param: parameters.GetIfNotExistParam()));
                sb.AppendLine();
                sb.Append(Globals.tab);
            }

            if (isDateTime)
            {
                if (nNeedIfNotExistCheck)
                {
                    sb.AppendLine(Scripts.BEGIN);
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, "declare @datetime_val datetime \n set @datetime_val=cast({0} as datetime) \n EXEC {1}.sp_addextendedproperty @name={2}, @value=@datetime_val {3}",
                                FormatSqlVariant(value),
                                GetPrefix(sp),
                                FormatFullNameForScripting(sp, false),
                                parameters.GetDDLParam());

                if (nNeedIfNotExistCheck)
                {
                    sb.AppendLine();
                    sb.AppendLine(Scripts.END);
                }
            }
            else
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC {0}.sp_addextendedproperty @name={1}, @value={2} {3}",
                                GetPrefix(sp),
                                FormatFullNameForScripting(sp, false),
                                FormatSqlVariant(value),
                                parameters.GetDDLParam());
            }
        }

        private void GetScriptAlter(StringBuilder sb, ScriptingPreferences sp, object value, bool isDateTime)
        {
            string tsql = string.Empty;
            if (isDateTime)
            {
                tsql = String.Format(SmoApplication.DefaultCulture,
                                     "declare @datetime_val datetime \n set @datetime_val=cast({0} as datetime) \n EXEC {1}.sp_updateextendedproperty @name={2}, @value=@datetime_val {3}",
                                     FormatSqlVariant(value),
                                     GetPrefix(sp),
                                     FormatFullNameForScripting(sp, false),
                                     GetParams(this.ParentColl.ParentInstance, sp));
            }
            else
            {
                tsql = String.Format(SmoApplication.DefaultCulture,
                                     "EXEC {0}.sp_updateextendedproperty @name={1}, @value={2} {3}",
                                     GetPrefix(sp),
                                     FormatFullNameForScripting(sp, false),
                                     FormatSqlVariant(value),
                                     GetParams(this.ParentColl.ParentInstance, sp));
            }

            sb.Append(tsql);
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
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

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Object Value
        {
            get
            {
                return this.Properties.GetValueWithNullReplacement("Value", false, false);
            }

            set
            {
                //allows null to be set
                this.Properties.SetValueWithConsistencyCheck("Value", value, true);
            }
        }

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
            return new string[] {
                "Value"
            };
        }

        /// <summary>
        /// ExtendedProperty does not support "CREATE OR ALTER" syntax. 
        /// Its implementation of CreateOrAlter uses "if not exists" combined with "sp_addextendedproperty" or "sp_updateextendedproperty" as appropriate.
        /// </summary>
        public void CreateOrAlter()
        {
            base.CreateOrAlterImpl();
        }
    }
}


