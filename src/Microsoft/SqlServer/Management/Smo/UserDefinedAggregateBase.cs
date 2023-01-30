// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class UserDefinedAggregate : ScriptSchemaObjectBase,
        Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists,
        IExtendedProperties, IScriptable, Cmn.IAlterable
    {
        private UserDefinedAggregateParameterCollection parameters;
        // state variable for Parameters property.

        private void init()
        {
            this.parameters = null;
        }

        internal UserDefinedAggregate(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            init();
        }

        public void ChangeSchema(string newSchema)
        {
            CheckObjectState();
            ChangeSchema(newSchema, true);
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "UserDefinedAggregate";
            }
        }

        /// <summary>
        /// Schema of UserDefinedAggregate
        /// </summary>
        [SfcKey(0)]
        [SfcReference(typeof(Schema), typeof(SchemaCustomResolver), "Resolve")]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        [CLSCompliant(false)]
        public override string Schema
        {
            get
            {
                return base.Schema;
            }
            set
            {
                base.Schema = value;
            }
        }

        /// <summary>
        /// Name of UserDefinedAggregate
        /// </summary>
        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
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
                if (null == this.m_ExtendedProperties)
                {
                    this.m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return this.m_ExtendedProperties;
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(UserDefinedAggregateParameter))]
        public UserDefinedAggregateParameterCollection Parameters
        {
            get
            {
                CheckObjectState();
                if (null == this.parameters)
                {
                    this.parameters = new UserDefinedAggregateParameterCollection(this);
                }
                return this.parameters;
            }
        }

        private void AddParam(StringBuilder sb, ScriptingPreferences sp, UserDefinedAggregateParameter spp)
        {
            StringCollection param_strings = new StringCollection();

            spp.UseOutput = false;
            spp.UseDefault = false;
            spp.ScriptDdlInternal(param_strings, sp);

            sb.Append(param_strings[0]);
            param_strings.Clear();
        }

        /// <summary>
        /// Alter Schema for User Defined Aggregate
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptChangeOwner(alterQuery, sp);
            }
        }

        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string fullName = FormatFullNameForScripting(sp);
            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            StringBuilder sbSpExec = new StringBuilder(1024);
            // CREATE AGGREGATE
            sbSpExec.AppendFormat("CREATE AGGREGATE {0}", fullName);
            sbSpExec.Append(sp.NewLine);

            // Validate that we have at least one parameter
            if (this.Parameters.Count < 1)
            {
                throw new SmoException(ExceptionTemplates.MustSpecifyOneParameter);
            }

            sbSpExec.Append(Globals.LParen);
            // The aggregate has at least one parameter
            AddParam(sbSpExec, sp, parameters[0]);
            for (int i = 1; i < Parameters.Count; i++)
            {
                sbSpExec.Append(Globals.comma);
                sbSpExec.Append(Globals.space);
                AddParam(sbSpExec, sp, parameters[i]);
            }
            sbSpExec.Append(Globals.RParen);
            sbSpExec.Append(sp.NewLine);

            // add return type
            sbSpExec.Append("RETURNS");
            UserDefinedDataType.AppendScriptTypeDefinition(sbSpExec, sp, this, this.DataType.SqlDataType);
            sbSpExec.Append(sp.NewLine);

            // EXTERNAL NAME
            sbSpExec.Append("EXTERNAL NAME ");

            string tempString;
            tempString = (string)this.GetPropValue("AssemblyName");
            if (string.Empty == tempString)
            {
                throw new PropertyNotSetException("AssemblyName");
            }

            sbSpExec.AppendFormat("[{0}]", SqlBraket(tempString));

            Property propClassName = Properties.Get("ClassName");
            if (propClassName.Value != null && ((string)propClassName.Value).Length > 0)
            {
                sbSpExec.AppendFormat(".[{0}]", SqlBraket((string)propClassName.Value));
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_UDA, "NOT", SqlString(fullName));
                sb.Append(sp.NewLine);
                sb.Append("EXEC dbo.sp_executesql @statement =");
                sb.Append(sp.NewLine);
                sb.Append("N'");
                sb.Append(SqlString(sbSpExec.ToString()));
                sb.Append(sp.NewLine);
                sb.Append("'");
            }
            else
            {
                sb.Append(sbSpExec.ToString());
            }

            queries.Add(sb.ToString());

            if (sp.IncludeScripts.Owner)
            {
                ScriptOwner(queries, sp);
            }
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

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            // DROP TYPE
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string fullName = FormatFullNameForScripting(sp);
            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_UDA, "", SqlString(fullName));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat("DROP AGGREGATE {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty,
                fullName);
            sb.Append(sp.NewLine);

            dropQuery.Add(sb.ToString());
        }

        private DataType dataType = null;
        /// <summary>
        /// Exposes return type information.
        /// </summary>
        [CLSCompliant(false)]
        [SfcReference(typeof(UserDefinedType), typeof(UserDefinedTypeResolver), "Resolve")]
        [SfcReference(typeof(UserDefinedDataType), typeof(UserDefinedDataTypeResolver), "Resolve")]
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public DataType DataType
        {
            get
            {
                return GetDataType(ref dataType);
            }
            set
            {
                SetDataType(ref dataType, value);
            }
        }

        /// <summary>
        /// Refresh the object.
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.dataType = null;
        }

        /// <summary>
        /// Generate object script.
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Generate object script with specific scripting options.
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
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
            string[] fields = {
                        "DataTypeSchema",
                        "SystemType",
                        "Length",
                        "NumericPrecision",
                        "NumericScale",
                        "XmlSchemaNamespace",
                        "XmlSchemaNamespaceSchema",
                        "XmlDocumentConstraint",
                        "AssemblyName",
                        "ClassName",
                        "ID",
                        "Owner",
                        "IsSchemaOwned"};
            List<string> list = GetSupportedScriptFields(typeof(UserDefinedAggregate.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            list.Add("DataType");
            return list.ToArray();
        }
    }
}

