// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public class ParameterBase : ScriptNameObjectBase, IExtendedProperties, IMarkForDrop
    {
        // this is the long under score character which engine allows in a stored proc param name
        const char longUderscoreChar = '\xff3f';
        internal ParameterBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            Init();
        }

        internal ParameterBase() :
            base()
        {
            Init();
        }

        private void Init()
        {
            bUseOutput = true;
            bUseDefault = true;
            bIsReadOnly = true;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Param";
            }
        }

        private bool bUseOutput;
        internal bool UseOutput
        {
            get { return bUseOutput; }
            set { bUseOutput = value; }
        }

        private bool bUseDefault;
        internal bool UseDefault
        {
            get { return bUseDefault; }
            set { bUseDefault = value; }
        }

        private bool bIsReadOnly;
        internal bool UseIsReadOnly
        {
            get { return bIsReadOnly; }
            set { bIsReadOnly = value; }
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

		private DataType dataType = null;
        [CLSCompliant(false)]
        [SfcReference(typeof(UserDefinedType), typeof(UserDefinedTypeResolver), "Resolve")]
        [SfcReference(typeof(UserDefinedDataType), typeof(UserDefinedDataTypeResolver), "Resolve")]
        [SfcReference(typeof(UserDefinedTableType), typeof(UserDefinedTableTypeResolver), "Resolve")]
        [SfcProperty(SfcPropertyFlags.Design | SfcPropertyFlags.Standalone)]
		public virtual DataType DataType 
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

        internal override void ValidateName(string name)
        {
            base.ValidateName(name);
            CheckParamName(name);
        }

		/// <summary>
		/// Check parameter name for validaton
		/// </summary>
		/// <param name="paramName">parameter name</param>
		/// <remarks>First character should be @ sign and the rest characters should follow the regular identifier rule.</remarks>
		protected void CheckParamName(string paramName)
		{
			bool bFirst = true;
            foreach (char c in paramName.ToCharArray())
			{
				if (bFirst)
				{
					//first character is always at sign(@) for parameter name
					if (c != '@')					
					{
						throw new WrongPropertyValueException(ExceptionTemplates.WrongPropertyValueException("Name", paramName));
					}
					bFirst = false;
				}
				else
				{
					//can be letter, digit, underscore, at sign, number sign, dollar sign or the unicode long underscore unless it's first char.
					if (!(char.IsLetterOrDigit(c) || c == '_' || c == '@' || c == '#' || c == '$' || c == longUderscoreChar))
					{
						throw new WrongPropertyValueException(ExceptionTemplates.WrongPropertyValueException("Name", paramName));
					}
				}
			}
		}

        internal override void ScriptDdl(StringCollection queries, ScriptingPreferences sp)
        {
            CheckObjectState();
            InitializeKeepDirtyValues();
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            String name = this.ScriptName != null && this.ScriptName.Length > 0 ? this.ScriptName : this.Name;
            CheckParamName(name);

            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} ", name);
            UserDefinedDataType.AppendScriptTypeDefinition(sb, sp, this, DataType.SqlDataType);

            if (bUseDefault)
            {
                String sDefaultValue = (string)this.GetPropValueOptional("DefaultValue");               
                if (sDefaultValue != null)
                {
                    // CLR object's parameter's default values or default value through SMO need to be surrounded quotes if required
                    if ((this.Properties["DefaultValue"].Dirty ||(isParentClrImplemented() &&  this.GetPropValueOptional<bool>("HasDefaultValue",false))))                       
                    {
                        // null value should not be surrounded with quotes
                        if (sDefaultValue.Equals("null", StringComparison.OrdinalIgnoreCase))
                        {
                             sb.AppendFormat(SmoApplication.DefaultCulture, " = {0}", sDefaultValue);
                        }
                        else
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, " = {0}", MakeSqlStringIfRequired(sDefaultValue));
                        }
                    }
                    else if(sDefaultValue.Length > 0)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " = {0}", sDefaultValue);
                    }
                }
            }

            if (bUseOutput)
            {
                if (null != Properties.Get("IsOutputParameter").Value && true == (bool)Properties.Get("IsOutputParameter").Value)
                {
                    sb.Append(" OUTPUT");
                }
            }

            if (bIsReadOnly && !(this is NumberedStoredProcedureParameter) && ServerVersion.Major >= 10)
            {
                if (null != Properties.Get("IsReadOnly").Value && true == (bool)Properties.Get("IsReadOnly").Value)
                {
                    sb.Append(" READONLY");
                }
            }

            queries.Add(sb.ToString());
        }

        /// <summary>
        /// method to convert default values of types which can contain space
        /// like datetime and char types to sqlstring
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string MakeSqlStringIfRequired(string defaultValue)
        {
            switch (this.DataType.SqlDataType)
            {
                case SqlDataType.DateTime:
                case SqlDataType.NChar:
                case SqlDataType.NText:
                case SqlDataType.NVarChar:
                case SqlDataType.NVarCharMax:
                case SqlDataType.SmallDateTime:
                case SqlDataType.Text:
                case SqlDataType.VarChar:
                case SqlDataType.VarCharMax:
                case SqlDataType.SysName:
                case SqlDataType.Date:
                case SqlDataType.Time:
                case SqlDataType.DateTimeOffset:
                case SqlDataType.DateTime2:
                case SqlDataType.Json:
                case SqlDataType.Vector:
                    return MakeSqlString(defaultValue);
                default:
                    return defaultValue;
            }           
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (this is NumberedStoredProcedureParameter)
            {
                return base.GetPropagateInfo(action);
            }
            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 || this.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        public override void Refresh()
        {
            base.Refresh();
            this.dataType = null;
        }

        #region TextModeImpl
        internal override string ScriptName
        {
            get { return base.ScriptName; }
            set { ((ScriptSchemaObjectBase)ParentColl.ParentInstance).CheckTextModeAccess("ScriptName"); base.ScriptName = value; }
        }
        #endregion

        /// <summary>
        /// Check if it is for direct execution i.e. create/alter/drop or parent is CLR type
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        protected virtual bool isParentClrImplemented()
        {
            return false;
        }
    }


    [SfcElementType("Param")]
    public partial class StoredProcedureParameter : Parameter
    {
        internal StoredProcedureParameter(AbstractCollectionBase parent, ObjectKeyBase key, SqlSmoState state)
            :
                base(parent, key, state)
        {
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "DefaultValue" || prop.Name == "IsOutputParameter")
            {
                Validate_set_ChildTextObjectDDLProperty(prop, value);
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
                                    DatabaseEngineType databaseEngineType,
                                    DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            if (!defaultTextMode)
            {
                string[] fields = {   
                                            "NumericPrecision",
                                            "NumericScale",
                                            "Length",
                                            "DataType",
                                            "DataTypeSchema",
                                            "SystemType",
                                            "IsOutputParameter",
                                            "DefaultValue",
                                            "HasDefaultValue",
                                            "XmlSchemaNamespace",
                                            "XmlSchemaNamespaceSchema",
                                            "XmlDocumentConstraint",
                                            "IsReadOnly"};
                List<string> list = GetSupportedScriptFields(typeof(StoredProcedureParameter.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                return list.ToArray();
            }
            else
            {
                // if the default text mode is true this means we will script the 
                // object's header directly from the text, so there is no need to bring 
                // any other properties. But we will still prefetch the parameters because
                // they might have extended properties.
                return new string[] { };
            }
        }

        protected override bool isParentClrImplemented()
        {
            return ((this.ServerVersion.Major > 8) && (this.Parent.GetPropValueOptional<ImplementationType>("ImplementationType") == ImplementationType.SqlClr));
        }
    }


    [SfcElementType("Param")]
    public partial class NumberedStoredProcedureParameter : Parameter
    {

        internal NumberedStoredProcedureParameter(AbstractCollectionBase parent, ObjectKeyBase key, SqlSmoState state)
            :
                base(parent, key, state)
        {
            UseIsReadOnly = false;
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "DefaultValue" || prop.Name == "IsOutputParameter")
            {
                Validate_set_ChildTextObjectDDLProperty(prop, value);
            }
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone)]
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
    }


    [SfcElementType("Param")]
    public partial class UserDefinedFunctionParameter : Parameter
    {
        internal UserDefinedFunctionParameter(AbstractCollectionBase parent, ObjectKeyBase key, SqlSmoState state)
            :
                base(parent, key, state)
        {
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "DefaultValue")
            {
                Validate_set_ChildTextObjectDDLProperty(prop, value);
            }
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    DatabaseEngineType databaseEngineType,
                                    DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            if (!defaultTextMode)
            {
                string[] fields = {   
                                                "NumericPrecision",
                                                "NumericScale",
                                                "Length",
                                                "DataType",
                                                "DataTypeSchema",
                                                "SystemType",
                                                "DefaultValue",
                                                "HasDefaultValue",
                                                "XmlSchemaNamespace",
                                                "XmlSchemaNamespaceSchema",
                                                "XmlDocumentConstraint",
                                                "IsReadOnly"};
                List<string> list = GetSupportedScriptFields(typeof(UserDefinedFunctionParameter.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                return list.ToArray();
            }
            else
            {
                // if the default text mode is true this means we will script the 
                // object's header directly from the text, so there is no need to bring 
                // any other properties. But we will still prefetch the parameters because
                // they might have extended properties.
                return new string[] { };
            }
        }

        protected override bool isParentClrImplemented()
        {
            return ((this.ServerVersion.Major > 8) && (this.Parent.GetPropValueOptional<ImplementationType>("ImplementationType") == ImplementationType.SqlClr));
        }
    }


}


