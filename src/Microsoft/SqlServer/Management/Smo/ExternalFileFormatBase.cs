// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a SQL server External File Format object.
    ///</summary>
    public partial class ExternalFileFormat : NamedSmoObject, Common.ICreatable, Common.IDroppable, IScriptable
    {
        const string FirstRowName = "FirstRow";

        internal ExternalFileFormat(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Parameterized constructor - populates properties from parameter values.
        /// Overloaded constructor to set all external file format required properties.
        /// </summary>
        /// <param name="parent">The name of the parent database.</param>
        /// <param name="name">The name of the external file format.</param>
        /// <param name="formatType">The external file format type.</param>
        public ExternalFileFormat(Database parent, string name, ExternalFileFormatType formatType)
            : base()
        {
            this.Parent = parent;
            base.Name = name;
            this.FormatType = formatType;
        }

        #region ICreatable Members
        /// <summary>
        /// Create an external file format object.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }
        #endregion

        #region IDroppable Members
        /// <summary>
        /// Drop an external file format object.
        /// </summary>
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
        #endregion

        #region IScriptable Members
        /// <summary>
        /// Generate a script for the external file format.
        /// </summary>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Generate a script for the external file format using the
        /// specified scripting options.
        /// </summary>
        /// <param name="scriptingOptions">The scripting options.</param>
        /// <returns>A string collection representing the T-SQL script.</returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }
        #endregion

        #region InternalProperties
        /// <summary>
        /// Returns the name of the object type in the urn expression.
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "ExternalFileFormat";
            }
        }
        #endregion

        #region InternalOverrides
        /// <summary>
        /// Constructs a T-SQL string to drop an external file format object.
        /// </summary>
        /// <param name="dropQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string.</param>
        /// <param name="sp">The scripting preferences.</param>
        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(typeof(ExternalFileFormat), sp);
            
            /* DROP EXTERNAL FILE FORMAT external_file_format_name
             */
            
            string fullyFormattedName = FormatFullNameForScripting(sp);

            // check the external file format object state
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // build the T-SQL script to drop the specified external file format, if it exists
            // add a comment header to the T-SQL script
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    ExternalFileFormat.UrnSuffix, fullyFormattedName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // check if the specified external file format object exists before attempting to drop it
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture,
                   Scripts.INCLUDE_EXISTS_EXTERNAL_FILE_FORMAT, String.Empty, FormatFullNameForScripting(sp, false)));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.Append("DROP EXTERNAL FILE FORMAT " + fullyFormattedName);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            dropQuery.Add(sb.ToString());
        }

        const string FormatTypePropertyName = "FormatType";
        /// <summary>
        /// Constructs the T-SQL string to create an external file format object.
        /// </summary>
        /// <param name="createQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string.</param>
        /// <param name="sp">The scripting preferences.</param>
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(typeof(ExternalFileFormat), sp);
            
            /* CREATE EXTERNAL FILE FORMAT external_file_format_name WITH
             * (FORMAT_TYPE = { DELIMITEDTEXT | RCFILE | ORC | PARQUET | JSON }
             *  ,[SERDE_METHOD = 'Serialization/Deserialization method']
             *  ,[FORMAT_OPTIONS (<format_options> [ ,...n ] )]
             *  ,[DATA_COMPRESSION = 'data_compression_method']
             * )[;]
             * 
             * <format_options> ::= [FIELD_TERMINATOR = 'value'][,STRING_DELIMITER = 'value'] [,DATE_FORMAT = 'value'] [,USE_TYPE_DEFAULT = 'value']                    
             */


            ExternalFileFormatType externalFileFormatType = ExternalFileFormatType.DelimitedText;

            // validate required property exists
            ValidateProperty(FormatTypePropertyName, sp);

            // validate the external file format type is supported and set
            if (IsSupportedProperty(FormatTypePropertyName, sp))
            {
                externalFileFormatType = (ExternalFileFormatType)this.GetPropValue(FormatTypePropertyName);
                if (!Enum.IsDefined(typeof(ExternalFileFormatType), externalFileFormatType))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration(externalFileFormatType.ToString()));
                }
            }
            
            string fullyFormattedName = FormatFullNameForScripting(sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // build the T-SQL script to create an external file format
            // add a comment header to the T-SQL script
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    ExternalFileFormat.UrnSuffix, fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // check if the specified external file format object does not already exist before attempting to create one
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_EXTERNAL_FILE_FORMAT, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            TypeConverter typeConverter = SmoManagementUtil.GetTypeConverter(typeof(ExternalFileFormatType));

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE EXTERNAL FILE FORMAT {0} WITH ", fullyFormattedName);
            sb.Append(Globals.LParen);
            sb.AppendFormat(SmoApplication.DefaultCulture, "FORMAT_TYPE = {0}", typeConverter.ConvertToInvariantString(externalFileFormatType));

            // check for conflicting properties
            CheckConflictingProperties(sp);

            // add any optional properties if they are set
            ProcessOptionalProperties(externalFileFormatType, sb, sp);

            sb.Append(Globals.RParen);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());
        }
        #endregion

        #region PrivateMethods
        /// <summary>
        /// Adds a property to the specified T-SQL script.
        /// </summary>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="sqlString">The formated T-SQL string to insert the property value.</param>
        /// <param name="fileFormatOptions">The T-SQL script to add the formated property value.</param>
        private void AddPropertyToScript(string propertyValue, string sqlString, StringBuilder fileFormatOptions)
        {
            // if this is the first property value being added, the string builder length will be 0, so don't prepend a comma
            // for all consecutive properties, we need to prepend a comma
            if (fileFormatOptions.Length > 0)
            {
                fileFormatOptions.Append(", ");
            }

            fileFormatOptions.AppendFormat(SmoApplication.DefaultCulture, sqlString, propertyValue);
        }

        /// <summary>
        /// Checks for conflicting properties for the specified external file format type.
        /// If a conflicting configuration is detected, throws an exception.
        /// </summary>
        private void CheckConflictingProperties(ScriptingPreferences sp)
        {
            const string FormatTypePropertyName = "FormatType";
            const string SerDeMethodPropertyName = "SerDeMethod";
            const string FieldTerminatorPropertyName = "FieldTerminator";
            const string StringDelimiterPropertyName = "StringDelimiter";
            const string DateFormatPropertyName = "DateFormat";
            const string UseTypeDefaultPropertyName = "UseTypeDefault";

            // confirm that for each of the file format types,
            // correct optional properties are specified, if any
            // ensure that the format type property has a value
            if (IsSupportedProperty(FormatTypePropertyName, sp))
            {
                Property formatTypeProp = this.GetPropertyOptional(FormatTypePropertyName);
                if (!formatTypeProp.IsNull)
                {
                    Property prop = null;

                    // if the format type is either delimited text, orc, parquet, or json then the serde method property is not supported
                    if ((ExternalFileFormatType)formatTypeProp.Value == ExternalFileFormatType.DelimitedText ||
                        (ExternalFileFormatType)formatTypeProp.Value == ExternalFileFormatType.Orc ||
                        (ExternalFileFormatType)formatTypeProp.Value == ExternalFileFormatType.Parquet ||
                        (ExternalFileFormatType)formatTypeProp.Value == ExternalFileFormatType.JSON)
                    {
                        // check the serde method property
                        // if it is specified and not a default, throw an exception
                        if (IsSupportedProperty(SerDeMethodPropertyName, sp))
                        {
                            prop = this.GetPropertyOptional(SerDeMethodPropertyName);
                            if (!prop.IsNull)
                            {
                                // if the property is set to something other than its default value, throw an exception
                                if (!IsPropertyDefaultValue<string>(prop, (string)prop.Value, new List<string> { null, string.Empty }))
                                {
                                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingExternalFileFormatProperties, prop.Name, prop.Value.ToString(), formatTypeProp.Value.ToString()));
                                }
                            }
                        }
                    }

                    // if the format type is rcfile, orc, parquet or json then the format options properties are not supported
                    if ((ExternalFileFormatType)formatTypeProp.Value == ExternalFileFormatType.RcFile ||
                        (ExternalFileFormatType)formatTypeProp.Value == ExternalFileFormatType.Orc ||
                        (ExternalFileFormatType)formatTypeProp.Value == ExternalFileFormatType.Parquet ||
                        (ExternalFileFormatType)formatTypeProp.Value == ExternalFileFormatType.JSON)
                    {
                        // check the format options properties
                        // if any of them are specified, throw an exception
                        if (IsSupportedProperty(FieldTerminatorPropertyName, sp))
                        {
                            prop = this.GetPropertyOptional(FieldTerminatorPropertyName);
                            if (!prop.IsNull)
                            {
                                if (!IsPropertyDefaultValue<string>(prop, (string)prop.Value, new List<string> { null, string.Empty }))
                                {
                                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingExternalFileFormatProperties, prop.Name, prop.Value.ToString(), formatTypeProp.Value.ToString()));
                                }
                            }
                        }

                        if (IsSupportedProperty(StringDelimiterPropertyName, sp))
                        {
                            prop = this.GetPropertyOptional(StringDelimiterPropertyName);
                            if (!prop.IsNull)
                            {
                                if (!IsPropertyDefaultValue<string>(prop, (string)prop.Value, new List<string> { null, string.Empty }))
                                {
                                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingExternalFileFormatProperties, prop.Name, prop.Value.ToString(), formatTypeProp.Value.ToString()));
                                }
                            }
                        }

                        if (IsSupportedProperty(DateFormatPropertyName, sp))
                        {
                            prop = this.GetPropertyOptional(DateFormatPropertyName);
                            if (!prop.IsNull)
                            {
                                if (!IsPropertyDefaultValue<string>(prop, (string)prop.Value, new List<string> { null, string.Empty }))
                                {
                                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingExternalFileFormatProperties, prop.Name, prop.Value.ToString(), formatTypeProp.Value.ToString()));
                                }
                            }
                        }

                        if (IsSupportedProperty(UseTypeDefaultPropertyName, sp))
                        {
                            prop = this.GetPropertyOptional(UseTypeDefaultPropertyName);
                            if (!prop.IsNull)
                            {
                                if (!IsPropertyDefaultValue<bool>(prop, (bool)prop.Value, new List<bool> { false }))
                                {
                                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingExternalFileFormatProperties, prop.Name, prop.Value.ToString(), formatTypeProp.Value.ToString()));
                                }
                            }
                        }

                        if (IsSupportedProperty(FirstRowName, sp))
                        {
                            prop = this.GetPropertyOptional(FirstRowName);
                            if (!prop.IsNull)
                            {
                                if (!IsPropertyDefaultValue<int>(prop, (int)prop.Value, new List<int> { 0 }))
                                {
                                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingExternalFileFormatProperties, prop.Name, prop.Value.ToString(), formatTypeProp.Value.ToString()));
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check the specified property if it has the default value.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="prop">The property to check.</param>
        /// <param name="value">The property value to check.</param>
        /// <param name="defaultValues">The default property values.</param>
        /// <returns>True, if the property value has the default value.  False otherwise.</returns>
        private bool IsPropertyDefaultValue<T>(Property prop, T value, List<T> defaultValues)
        {
            // if the value is the default, return true
            // otherwise, return false
            if (!prop.IsNull)
            {
                foreach (T defaultValue in defaultValues)
                {
                    if (EqualityComparer<T>.Default.Equals((T)prop.Value, defaultValue))
                    {
                        return true;
                    }    
                }
            }

            return false;
        }

        /// <summary>
        /// Processes optional properties for each of the supported file format types
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="externalFileFormatType">The external file format type.</param>
        /// <param name="script">The external file format T-SQL script.</param>
        /// <param name="sp">The scripting preferences.</param>
        private void ProcessOptionalProperties(ExternalFileFormatType externalFileFormatType, StringBuilder script, ScriptingPreferences sp)
        {
            // check the optional properties are supported by the specified format type
            switch (externalFileFormatType)
            {
                case ExternalFileFormatType.DelimitedText:
                    ValidateDelimitedTextProperties(script, sp);
                    break;
                case ExternalFileFormatType.JSON:
                case ExternalFileFormatType.Orc:
                case ExternalFileFormatType.Parquet:
                    ValidateOrcOrParquetProperties(script, sp);
                    break;
                case ExternalFileFormatType.RcFile:
                    ValidateRcFileProperties(script, sp);
                    break;
                default:
                    // if the format type set to any other value throw an exception
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration(externalFileFormatType.ToString()));    
            }
        }
        
        /// <summary>
        /// Validates optional properties for the DelimtedText file format
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="script">The external file format T-SQL script.</param>
        /// <param name="sp">The scripting preferences.</param>
        private void ValidateDelimitedTextProperties(StringBuilder script, ScriptingPreferences sp)
        {
            const string UseTypeDefaultPropertyName = "UseTypeDefault";

            // check for the DelimitedText supported optional properties - FormatOptions and DataCompression
            // check for any format options optinal parameters - FieldTerminator, StringDelimiter, DateFormat and UseTypeDefault
            // if any are found, add them to the T-SQL script
            StringBuilder formatOptions = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            List<string> defaultValues = new List<string> { null, string.Empty };

            // validate and process the field terminator file format option
            ValidateOptionalProperty("FieldTerminator", "FIELD_TERMINATOR = {0}", defaultValues, formatOptions, sp);

            // validate and process the string delimiter file format option
            ValidateOptionalProperty("StringDelimiter", "STRING_DELIMITER = {0}", defaultValues, formatOptions, sp);

            // validate and process the date format file format option
            ValidateOptionalProperty("DateFormat", "DATE_FORMAT = {0}", defaultValues, formatOptions, sp);

            // validate and process the first row optional file format property
            ValidateOptionalProperty(FirstRowName, "FIRST_ROW = {0}", new List<int> { 1 }, formatOptions, sp, quotePropertyValue: false);

            // validate and process the use type default file format option
            if (IsSupportedProperty(UseTypeDefaultPropertyName, sp))
            {
                if (!this.GetPropertyOptional(UseTypeDefaultPropertyName).IsNull)
                {
                    bool externalFileFormatUseTypeDefault = (bool)this.GetPropValueOptional(UseTypeDefaultPropertyName);
                
                    if (formatOptions.Length > 0)
                    {
                        formatOptions.Append(", ");
                    }

                    formatOptions.AppendFormat(SmoApplication.DefaultCulture, "USE_TYPE_DEFAULT = {0}", externalFileFormatUseTypeDefault); 
                }
            }

            // if any format options were specified, add the FORMAT_OPTIONS and enclose them in the parenthesis
            string fileFormatOptions = formatOptions.ToString();
            if (!string.IsNullOrEmpty(fileFormatOptions))
            {
                script.AppendFormat(SmoApplication.DefaultCulture, ", FORMAT_OPTIONS ({0})", fileFormatOptions);
            }

            // validate and process the data compression optional file format property
            ValidateOptionalProperty("DataCompression", "DATA_COMPRESSION = {0}", defaultValues, script, sp);
        }

        /// <summary>
        /// Validates the specified property that it is not the default value and adds it to the T-SQL script.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="sqlString">The T-SQL script to add.</param>
        /// <param name="defaultValues">The default property values.</param>
        /// <param name="fileFormatOptions">The T-SQL script with the already added file format options.</param>
        /// <param name="sp">The scripting preferences.</param>
        /// <param name="quotePropertyValue">If true, the property value would be quoted as SQL string (like: N'') else not.</param>
        private void ValidateOptionalProperty<T>(string propertyName, string sqlString, List<T> defaultValues, StringBuilder fileFormatOptions, ScriptingPreferences sp, bool quotePropertyValue = true)
        {
            // check if the property has been modified
            // if it has been and if the value is not the default, add it to the T-SQL script
            if (IsSupportedProperty(propertyName, sp))
            {
                Property prop = this.GetPropertyOptional(propertyName);
                if (!prop.IsNull)
                {
                    if (!IsPropertyDefaultValue(prop, (T)prop.Value, defaultValues))
                    {
                        AddPropertyToScript(quotePropertyValue
                            ? Util.MakeSqlString(Convert.ToString(prop.Value, SmoApplication.DefaultCulture))
                            : prop.Value.ToString(),
                            sqlString,
                            fileFormatOptions);
                    }
                }
            }
        }

        /// <summary>
        /// Validates optional properties for the Orc or Parquet file format
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="script">The external file format T-SQL script.</param>
        /// <param name="sp">The scripting preferences.</param>
        private void ValidateOrcOrParquetProperties(StringBuilder script, ScriptingPreferences sp)
        {
            List<string> defaultValues = new List<string> { null, string.Empty };

            // check for the Orc and Parquet supported optional properties - DataCompression
            // validate and process the data compression optional file format property
            ValidateOptionalProperty("DataCompression", "DATA_COMPRESSION = {0}", defaultValues, script, sp);
        }

        /// <summary>
        /// Validates the specified property is not null.
        /// Throws exception, if the property is null.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="sp">The scripting preferences.</param>
        private void ValidateProperty(string propertyName, ScriptingPreferences sp)
        {
            if (IsSupportedProperty(propertyName, sp))
            {
                if (this.GetPropertyOptional(propertyName).IsNull)
                {
                    throw new ArgumentNullException(propertyName);
                }
            }   
        }

        /// <summary>
        /// Validates optional properties for the RcFile file format
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="script">The external file format T-SQL script.</param>
        /// <param name="sp">The scripting preferences.</param>
        private void ValidateRcFileProperties(StringBuilder script, ScriptingPreferences sp)
        {
            List<string> defaultValues = new List<string> { null, string.Empty };
            
            // check for the optional properties - SerDeMethod and DataCompression
            // the SerDeMethod property is required for the RcFile format
            // if it is not set, throw an exception
            ValidateProperty("SerDeMethod", sp);

            // check if the serde method property was set
            // if yes, add it to the script
            ValidateOptionalProperty("SerDeMethod", "SERDE_METHOD = {0}", defaultValues, script, sp);

            // validate and process the data compression optional file format property
            ValidateOptionalProperty("DataCompression", "DATA_COMPRESSION = {0}", defaultValues, script, sp);
        }
        #endregion

        internal static string[] GetScriptFields(Type parentType,
            Common.ServerVersion version,
            Common.DatabaseEngineType databaseEngineType,
            Common.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            var fields = new string[]
            {
                "DataCompression",
                "DateFormat",
                "Encoding",
                "FieldTerminator",
                "FirstRow",
                FormatTypePropertyName,
                "ID",
                "Name",
                "RowTerminator",
                "SerDeMethod",
                "StringDelimiter",
                "UseTypeDefault",
            };

            var list = GetSupportedScriptFields(typeof(DatabaseScopedConfiguration.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}
