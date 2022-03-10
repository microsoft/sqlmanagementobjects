// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Reflection;

namespace Microsoft.SqlServer.Management.SqlScriptPublish
{
    /// <summary>
    /// The type of output to create. Only one type is allowed, this enumeration exists for backward compatibility.
    /// </summary>
    public enum OutputType
    {
        /// <summary>
        /// Generate script as text whose destination can be a file, a set of files, or the clipboard
        /// </summary>
        GenerateScript = 0,
    }

    /// <summary>
    /// Generate script output destination
    /// </summary>
    public enum ScriptDestination
    {
        /// <summary>
        /// Put all scripts in one file
        /// </summary>
        ToSingleFile = 0,
        /// <summary>
        /// Put all scripts on the clipboard as one string
        /// </summary>
        ToClipboard,
        /// <summary>
        /// Open the scripts in an editor window
        /// </summary>
        ToEditor,
        /// <summary>
        /// Create one file per database object
        /// </summary>
        ToFilePerObject,
        /// <summary>
        /// Create a Jupyter Notebook with one code cell per object
        /// </summary>
        ToNotebook,
        /// <summary>
        /// Hands persistence responsibility to a custom ISmoScriptWriter implementation provided by the caller
        /// </summary>
        ToCustomWriter
    }

    /// <summary>
    /// Generate script unicode, ansi, and explicit utf8 type
    /// </summary>
    public enum ScriptFileType
    {
        /// <summary>
        /// Encode in UTF-16
        /// </summary>
        Unicode = 0,
        /// <summary>
        /// Encode in ANSI using the default code page
        /// </summary>
        Ansi,
        /// <summary>
        /// Encode in UTF-8
        /// </summary>
        Utf8,
    }

    /// <summary>
    /// Generate script file overwrite option
    /// </summary>
    public enum ScriptFileMode
    {
        /// <summary>
        /// Overwrite existing files
        /// </summary>
        Overwrite = 0,
        /// <summary>
        /// Append contents to existing files
        /// </summary>
        Append
    }    

    /// <summary>
    /// Enum for database object types
    /// To support localized description, it uses LocalizedEnumConverter.
    /// </summary>
    [TypeConverter(typeof(LocalizedEnumConverter))]

    public enum DatabaseObjectType
    {
        Table,
        View,
        StoredProcedure,
        UserDefinedFunction,
        UserDefinedDataType,
        User,
        Default,
        Rule,
        DatabaseRole,
        ApplicationRole,
        SqlAssembly,
        DdlTrigger,
        Synonym,
        XmlSchemaCollection,
        Schema,
        SecurityPolicy,
        PlanGuide,
        UserDefinedType,
        UserDefinedAggregate,
        FullTextCatalog,
        UserDefinedTableType
    }

    /// <summary>
    /// Result enum type
    /// To support localized description, it uses LocalizedEnumConverter.
    /// </summary>
    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum ResultType
    {
        None,
        InProgress,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Convert enum string to localized display string
    /// </summary>
    internal class LocalizedEnumConverter : EnumConverter
    {
        private const string srTypeFullName = "Microsoft.SqlServer.Management.SqlScriptPublish.SR";

        public LocalizedEnumConverter(Type type)
            : base(type)
        {
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (value != null && destinationType == typeof(string))
            {
                Type srType = Type.GetType(srTypeFullName);
                PropertyInfo propInfo = srType.GetProperty(value.ToString());
                object locStr = propInfo.GetValue(null, null);
                return locStr;
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
