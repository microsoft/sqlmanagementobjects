// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Server;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// Represents an external model in SQL Server, which can be created, altered, or dropped.
    /// Provides scripting capabilities for T-SQL statements like CREATE, ALTER, and DROP.
    /// </summary>
    public partial class ExternalModel : NamedSmoObject, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable,
        Cmn.IDropIfExists, IExtendedProperties, IScriptable
    {
        private static class Scripts
        {
            public const string BEGIN = "BEGIN";
            public const string END = "END";
            public const string INCLUDE_EXISTS_EXTERNAL_MODEL =
                "IF {0} EXISTS (SELECT * FROM sys.external_models WHERE name = N{1})";
        }

        #region Constructors

        internal ExternalModel(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
        }


        #endregion

        #region Properties

        /// <summary>
        /// Gets the URN suffix for the ExternalModel object.
        /// </summary>
        public static string UrnSuffix
        {
            get { return nameof(ExternalModel); }
        }

        /// <summary>
        /// Extended properties of the model.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
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
            return new PropagateInfo[] { new PropagateInfo(this.IsSupportedObject<ExternalModel>() ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        #endregion

        #region Create Methods

        public void Create()
        {
            base.CreateImpl();
        }

        #endregion

        #region Drop Methods

        public void Drop()
        {
            base.DropImpl();
        }

        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        #endregion

        #region Alter Methods

        public void Alter()
        {
            base.AlterImpl();
        }


        #endregion

        #region InternalOverrides

        /// <summary>
        /// Generates the T-SQL script for dropping the external model.
        /// </summary>
        /// <param name="dropQuery">The collection to which the script will be added.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            string fullyFormattedName = FormatFullNameForScripting(sp);

            // check the external model object state
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // build the T-SQL script to drop the specified external model, if it exists
            // add a comment header to the T-SQL script
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    ExternalModel.UrnSuffix, fullyFormattedName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // check if the specified external model object exists before attempting to drop it
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, IncludeExistsExternalModel(exists: true, GetName(sp)));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.Append("DROP EXTERNAL MODEL " + fullyFormattedName);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            dropQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Generates the T-SQL script for creating the external model.
        /// </summary>
        /// <param name="createQuery">The collection to which the script will be added.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            /* CREATE EXTERNAL MODEL [model_name] AUTHORIZATION [owner_name]
             * WITH (
             *     LOCATION = N'https://path/to/model',
             *     API_FORMAT = N'OpenAI',
             *     MODEL_TYPE = Embeddings,
             *     MODEL = N'model-name',
             *     [PARAMETERS = '{ "valid": "json" }'],
             *     [CREDENTIAL = credential_name]
             * )
             */

            const string LocationPropertyName = nameof(Location);
            const string ApiFormatPropertyName = nameof(ApiFormat);
            const string ModelTypePropertyName = nameof(ModelType);
            const string ModelPropertyName = nameof(Model);
            const string ParametersPropertyName = nameof(Parameters);
            const string CredentialPropertyName = nameof(Credential);

            // Check SQL Server version supports external models
            this.ThrowIfNotSupported(typeof(ExternalModel), sp);

            ExternalModelType externalModelType = ExternalModelType.Embeddings;

            // Validate required properties exist
            ValidatePropertySet(LocationPropertyName, sp);
            ValidatePropertySet(ApiFormatPropertyName, sp);
            ValidatePropertySet(ModelPropertyName, sp);

            string location = string.Empty;
            string apiFormat = string.Empty;
            string model = string.Empty;

            // Get required property values
            location = (string)this.GetPropValue(LocationPropertyName);
            apiFormat = (string)this.GetPropValue(ApiFormatPropertyName);
            model = (string)this.GetPropValue(ModelPropertyName);

           externalModelType = (ExternalModelType)this.GetPropValue(ModelTypePropertyName);
            // Validate enum
            if (!Enum.IsDefined(typeof(ExternalModelType), externalModelType))
            {
                throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration(externalModelType.ToString()));
            }

            string fullyFormattedName = FormatFullNameForScripting(sp);
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            TypeConverter typeConverter = SmoManagementUtil.GetTypeConverter(typeof(ExternalModelType));

            // Add header comment if requested
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    ExternalModel.UrnSuffix, fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // Add existence check if requested
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_EXTERNAL_MODEL,
                    "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE EXTERNAL MODEL {0} ", fullyFormattedName);

            // AUTHORIZATION block
            Property property;
            if (sp.IncludeScripts.Owner && (null != (property = this.Properties.Get(nameof(Owner))).Value))
            {
                sb.AppendFormat(" AUTHORIZATION [{0}]", SqlBraket(property.Value.ToString()));
                sb.Append(sp.NewLine);
            }

            sb.Append("WITH ");
            sb.Append(Globals.LParen);

            // Required properties
            sb.AppendFormat(SmoApplication.DefaultCulture, "LOCATION = {0}", Util.MakeSqlString(location));
            sb.Append(", ");
            sb.AppendFormat(SmoApplication.DefaultCulture, "API_FORMAT = {0}", Util.MakeSqlString(apiFormat));
            sb.Append(", ");
            sb.AppendFormat(SmoApplication.DefaultCulture, "MODEL_TYPE = {0}", typeConverter.ConvertToInvariantString(externalModelType));
            sb.Append(", ");
            sb.AppendFormat(SmoApplication.DefaultCulture, "MODEL = {0}", Util.MakeSqlString(model));

            // Optional properties
            var appendComma = true;

            // Parameters (as JSON)
            AppendStringPropertyToScript(sb, ParametersPropertyName, "PARAMETERS", sp, ref appendComma);

            // Credential (optional)
            AppendNamedPropertyToScript(sb, CredentialPropertyName, "CREDENTIAL", sp, ref appendComma);

            sb.Append(Globals.RParen);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Generates the T-SQL script for altering the external model.
        /// </summary>
        /// <param name="alterQuery">The collection to which the script will be added.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            /* ALTER EXTERNAL MODEL model_name
             * SET (
             *     LOCATION = N'https://path/to/model',
             *     API_FORMAT = N'OpenAI',
             *     MODEL_TYPE = Embeddings,
             *     MODEL = N'modelname',
             *     [PARAMETERS = '{ "valid": "json" }'],
             *     [CREDENTIAL = credential_name]
             * )
             */

            const string LocationPropertyName = nameof(Location);
            const string ApiFormatPropertyName = nameof(ApiFormat);
            const string ModelTypePropertyName = nameof(ModelType);
            const string ModelPropertyName = nameof(Model);
            const string ParametersPropertyName = nameof(Parameters);
            const string CredentialPropertyName = nameof(Credential);

            // Check SQL Server version supports external models
            this.ThrowIfNotSupported(typeof(ExternalModel), sp);

            // Skip scripting ALTER if the object is in the Creating state
            if (this.State == SqlSmoState.Creating)
            {
                return;
            }

            string fullyFormattedName = FormatFullNameForScripting(sp);
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            TypeConverter typeConverter = SmoManagementUtil.GetTypeConverter(typeof(ExternalModelType));

            // Add a comment header to the T-SQL script
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    ExternalModel.UrnSuffix, fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "ALTER EXTERNAL MODEL {0} SET ", fullyFormattedName);
            sb.Append(Globals.LParen);

            var addComma = false;

            // All properties are alterable
            var location = AppendStringPropertyToScript(sb, LocationPropertyName, "LOCATION", sp, ref addComma);
            var apiFormat = AppendStringPropertyToScript(sb, ApiFormatPropertyName, "API_FORMAT", sp, ref addComma);
            var model = AppendStringPropertyToScript(sb, ModelPropertyName, "MODEL", sp, ref addComma);
            var parameters = AppendStringPropertyToScript(sb, ParametersPropertyName, "PARAMETERS", sp, ref addComma);
            var credential = AppendNamedPropertyToScript(sb, CredentialPropertyName, "CREDENTIAL", sp, ref addComma);

            if (IsSupportedProperty(ModelTypePropertyName, sp) && !GetPropertyOptional(ModelTypePropertyName).IsNull)
            {
                var externalModelType = (ExternalModelType)GetPropValueOptional(ModelTypePropertyName);
                if (addComma)
                {
                    sb.Append(", ");
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, "MODEL_TYPE = {0}", typeConverter.ConvertToInvariantString(externalModelType));
                addComma = true;
            }

            // If no properties are being altered, return without generating a script
            if (string.IsNullOrEmpty(location) &&
                string.IsNullOrEmpty(apiFormat) &&
                string.IsNullOrEmpty(model) &&
                string.IsNullOrEmpty(parameters) &&
                string.IsNullOrEmpty(credential))
            {
                // No properties to alter, this could be called as part of a larger operation
                // so we just return without throwing an error
                return;
            }

            sb.Append(Globals.RParen);

            alterQuery.Add(sb.ToString());
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Validates the specified property is not null and has a
        /// non-null value.
        /// Throws exception,
        ///     if the property is null, or
        ///     its value is null or an empty string.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="sp">The scripting preferences.</param>
        private void ValidatePropertySet(string propertyName, ScriptingPreferences sp)
        {
            if (IsSupportedProperty(propertyName, sp))
            {
                if (this.GetPropertyOptional(propertyName).IsNull)
                {
                    throw new ArgumentNullException(propertyName);
                }

                string propertyValue = this.GetPropValue(propertyName).ToString();
                if (string.IsNullOrEmpty(propertyValue))
                {
                    throw new PropertyNotSetException(propertyName);
                }
            }
        }

        /// <summary>
        /// Appends a property and its value to a sql script in the provided string builder. Property value will be wrapped in string quotes.
        /// </summary>
        /// <param name="sb">StringBuilder that contains the sql script to append to.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="sqlPropName">The T-SQL name for the property to use in the script.</param>
        /// <param name="sp">Scripting preferences to use when checking if the property is supported.</param>
        /// <param name="addComma">Whether to add a comma and space to the beginning of the appended script text.</param>
        /// <returns>A string representing the property's value.</returns>
        private string AppendStringPropertyToScript(StringBuilder sb, string propName, string sqlPropName, ScriptingPreferences sp, ref bool addComma)
            => AppendPropertyToScript(sb, propName, sqlPropName, sp, useBrackets: false, ref addComma);

        /// <summary>
        /// Appends a property and its value to a sql script in the provided string builder. Property value will be wrapped in square brackets.
        /// </summary>
        /// <param name="sb">StringBuilder that contains the sql script to append to.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="sqlPropName">The T-SQL name for the property to use in the script.</param>
        /// <param name="sp">Scripting preferences to use when checking if the property is supported.</param>
        /// <param name="addComma">Whether to add a comma and space to the beginning of the appended script text.</param>
        /// <returns>A string representing the property's value.</returns>
        private string AppendNamedPropertyToScript(StringBuilder sb, string propName, string sqlPropName, ScriptingPreferences sp, ref bool addComma)
            => AppendPropertyToScript(sb, propName, sqlPropName, sp, useBrackets: true, ref addComma);

        /// <summary>
        /// Appends a property and its value to a sql script in the provided string builder.
        /// </summary>
        /// <param name="sb">StringBuilder that contains the sql script to append to.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="sqlPropName">The T-SQL name for the property to use in the script.</param>
        /// <param name="sp">Scripting preferences to use when checking if the property is supported.</param>
        /// <param name="useBrackets">Whether to wrap the property value in string quotes or square brackets.</param>
        /// <param name="addComma">Whether to add a comma and space to the beginning of the appended script text.</param>
        /// <returns>A string representing the property's value.</returns>
        private string AppendPropertyToScript(StringBuilder sb, string propName, string sqlPropName, ScriptingPreferences sp, bool useBrackets, ref bool addComma)
        {
            string propertyValue = string.Empty;
            if (IsSupportedProperty(propName, sp)
                && !GetPropertyOptional(propName).IsNull)
            {
                propertyValue = Convert.ToString(GetPropValueOptional(propName), SmoApplication.DefaultCulture);
                if (!string.IsNullOrEmpty(propertyValue))
                {
                    if (addComma)
                    {
                        sb.Append(", ");
                    }
                    var propertyStr = useBrackets ? MakeSqlBraket(propertyValue) : Util.MakeSqlString(propertyValue);
                    sb.Append($"{sqlPropName.ToUpper()} = {propertyStr}");
                    addComma = true;
                }
            }
            return propertyValue;
        }
        #endregion

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        protected override bool IsObjectDirty()
        {
            return (base.IsObjectDirty());
        }

        protected override void MarkDropped()
        {
            base.MarkDropped();
        }

        /// <summary>
        /// Returns a script to check existence or not existence of an external model.
        /// </summary>
        /// <param name="exists">check existence or not existence</param>
        /// <param name="name">Name of the external model</param>
        private static string IncludeExistsExternalModel(bool exists, string name)
        {
            return $"IF {(exists ? "" : "NOT")} EXISTS (SELECT * from sys.external_models models WHERE models.name = '{SqlString(name)}')";
        }

        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            return new string[] {
                nameof(ApiFormat),
                nameof(Credential),
                nameof(Location),
                nameof(Model),
                nameof(ModelType),
                nameof(Parameters)
            };
        }
    }
}