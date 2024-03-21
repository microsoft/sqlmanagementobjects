// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// The SqlDataType specifies the type of the DataType object.
    /// </summary>
    public enum SqlDataType
    {
        None    = 0, // Not set.
        BigInt  = 1, //A 64-bit signed integer.
        Binary  = 2, //A fixed-length stream of binary data ranging between 1 and 8,000 bytes.
        Bit = 3, //An unsigned numeric value that can be 0, 1, or a null reference.
        Char = 4, //A fixed-length stream of non-Unicode characters ranging between 1 and 8,000 characters.
        DateTime = 6,// Date and time data ranging in value from January 1, 1753 to December 31, 9999 to an accuracy of 3.33 milliseconds.
        Decimal = 7, //A fixed precision and scale numeric value between -10^38 -1 and 10^38 -1.
        Float = 8, //A floating point number within the range of -1.79E +308 through 1.79E +308.
        Image = 9, //A variable-length stream of binary data ranging from 0 to 231 -1 (or 2,147,483,647) bytes.
        Int = 10, //A 32-bit signed integer.
        Money   = 11, //A currency value ranging from -263 (or -922,337,203,685,477.5808) to 2 63 -1 (or +922,337,203,685,477.5807) with an accuracy to a ten-thousandth of a currency unit.
        NChar   = 12, //A fixed-length stream of Unicode characters ranging between 1 and 4,000 characters.
        NText   = 13, //A variable-length stream of Unicode data with a maximum length of 230 - 1 (or 1,073,741,823) characters.
        NVarChar    = 14, //A variable-length stream of Unicode characters ranging between 1 and 2^63 characters.
        NVarCharMax = 15, //NVARCHAR(MAX) type.
        Real = 16, //A floating point number within the range of -3.40E +38 through 3.40E +38.
        SmallDateTime = 17, //Date and time data ranging in value from January 1, 1900 to June 6, 2079 to an accuracy of one minute.
        SmallInt     = 18, //A 16-bit signed integer.
        SmallMoney  = 19, //A currency value ranging from -214,748.3648 to +214,748.3647 with an accuracy to a ten-thousandth of a currency unit.
        Text = 20, //A variable-length stream of non-Unicode data with a maximum length of 231 -1 (or 2,147,483,647) characters.
        Timestamp   = 21, //Automatically generated binary numbers, which are guaranteed to be unique within a database.
        TinyInt = 22, //An 8-bit unsigned integer.
        UniqueIdentifier    = 23, //A globally unique identifier (or GUID).
        UserDefinedDataType = 24, //User defined data type.
        UserDefinedType = 25, //SQL CLR User Defined Type.
        VarBinary   = 28, //A variable-length stream of binary data ranging between 1 and 2^64 bytes.
        VarBinaryMax    = 29, //VARBINARY(MAX) type.
        VarChar = 30, //A variable-length stream of non-Unicode characters ranging between 1 and 2^64 characters.
        VarCharMax  = 31, //VARCHAR(MAX) type.
        Variant = 32, //A special data type that can contain numeric, string, binary, or date data as well as the SQL Server values Empty and Null, which is assumed if no other type is declared.
        Xml = 33, //XML data type.
        SysName = 34, //XML data type.
        Numeric = 35, //A fixed precision and scale numeric value between -10^38 -1 and 10^38 -1, functionally identical to Decimal.
        Date = 36, //Date data ranging from January 1, 0001 to December, 31 9999.
        Time = 37, //Time data based on a 24 hour clock with 8 positions of fractional seconds
        DateTimeOffset = 38, //Date and time data ranging from January 1, 0001 to December, 31 9999 and 24-hour time with 8 fractional seconds with timezone information.
        DateTime2 = 39, //Date and time data ranging from January 1, 0001 to December, 31 9999 with 24-hour clock time with 19 positions of fractional seconds.
        UserDefinedTableType = 40, //User defined table type
        HierarchyId = 41, //system clr type
        Geometry = 42, // A datatype used for planar 2-dimensional geometries.
        Geography = 43, // A geodetic datatype.
        Json = 44 // A json datatype.

        // !!IMPORTANT!! If updating this with new types make sure to update IsDataTypeSupportedOnTargetVersion and/or IsSystemDataType with the new type!
        // You should also update the AllSqlDataTypeValues_SupportedOnAllApplicableVersions unit test with the new type and minimum version
        //
    }

    /// <summary>
    /// The DataType object allows users to define a SQL Server data type.
    /// </summary>
    public class DataType : IXmlSerializable
    {
#region Constructors
        /// <summary>
        /// Creates a new DataType object.
        /// </summary>
        public DataType()
        {
        }

        /// <summary>
        /// Creates a new DataType object. The sqlDataType specifies the SQL Server data type.
        /// </summary>
        /// <param name="sqlDataType"></param>
        public DataType(SqlDataType sqlDataType)
        {
            switch(sqlDataType)
            {
                case SqlDataType.BigInt:
                case SqlDataType.Bit:
                case SqlDataType.Char:
                case SqlDataType.DateTime:
                case SqlDataType.HierarchyId:
                case SqlDataType.Geography:
                case SqlDataType.Geometry:
                case SqlDataType.Image:
                case SqlDataType.Int:
                case SqlDataType.Money:
                case SqlDataType.NChar:
                case SqlDataType.NText:
                case SqlDataType.NVarChar:
                case SqlDataType.NVarCharMax:
                case SqlDataType.Real:
                case SqlDataType.SmallDateTime:
                case SqlDataType.SmallInt:
                case SqlDataType.SmallMoney:
                case SqlDataType.Text:
                case SqlDataType.Timestamp:
                case SqlDataType.TinyInt:
                case SqlDataType.UniqueIdentifier:
                case SqlDataType.Binary:
                case SqlDataType.VarBinary:
                case SqlDataType.VarBinaryMax:
                case SqlDataType.VarChar:
                case SqlDataType.VarCharMax:
                case SqlDataType.Variant:
                case SqlDataType.Xml:
                case SqlDataType.SysName:
                case SqlDataType.Date:
                case SqlDataType.Json:
                    this.sqlDataType = sqlDataType;
                    this.name = GetSqlName(sqlDataType);
                    break;
                case SqlDataType.Numeric:
                case SqlDataType.Decimal:
                    // set the default Precision and Scale values when not mentioned anything
                    this.sqlDataType = sqlDataType;
                    this.name = GetSqlName(sqlDataType);
                    this.NumericPrecision = 18;
                    this.NumericScale = 0;
                    break;
                case SqlDataType.Float:
                    // set the default Precision value when not mentioned anything
                    this.sqlDataType = sqlDataType;
                    this.name = GetSqlName(sqlDataType);
                    this.NumericPrecision = 53;
                    break;
                case SqlDataType.Time:
                case SqlDataType.DateTime2:
                case SqlDataType.DateTimeOffset:
                    // set the default Precision value when not mentioned anything
                    this.sqlDataType = sqlDataType;
                    this.name = GetSqlName(sqlDataType);
                    this.NumericScale = 7;
                    break;
                default:
                    throw new SmoException(ExceptionTemplates.DataTypeUnsupported(sqlDataType.ToString()));
            }
        }

        /// <summary>
        /// Creates a new DataType object. The sqlDataType specifies the SQL Server data type.
        /// MaxLength specifies the maximum length of the type. In case of Decimal or Numeric,
        /// maxLength specifies the precision.
        /// </summary>
        /// <param name="sqlDataType"></param>
        /// <param name="precisionOrMaxLengthOrScale"></param>
        public DataType(SqlDataType sqlDataType, Int32 precisionOrMaxLengthOrScale)
        {
            switch(sqlDataType)
            {
                case SqlDataType.Binary:
                case SqlDataType.Char:
                case SqlDataType.NChar:
                case SqlDataType.NVarChar:
                case SqlDataType.VarBinary:
                case SqlDataType.VarChar:
                case SqlDataType.Image:
                case SqlDataType.NText:
                case SqlDataType.Text:
                    this.sqlDataType = sqlDataType;
                    this.MaximumLength = precisionOrMaxLengthOrScale;
                    this.name = GetSqlName(sqlDataType);
                    break;
                case SqlDataType.Decimal:
                case SqlDataType.Numeric:
                    this.sqlDataType = sqlDataType;
                    this.NumericPrecision = precisionOrMaxLengthOrScale;
                    this.NumericScale = 0;
                    this.name = GetSqlName(sqlDataType);
                    break;
                case SqlDataType.Float:
                    this.sqlDataType = sqlDataType;
                    this.NumericPrecision = precisionOrMaxLengthOrScale;
                    this.name = GetSqlName(sqlDataType);
                    break;
                case SqlDataType.Time:
                case SqlDataType.DateTimeOffset:
                case SqlDataType.DateTime2:
                    this.sqlDataType = sqlDataType;
                    this.NumericScale = precisionOrMaxLengthOrScale;
                    this.name = GetSqlName(sqlDataType);
                    break;
                default:
                    throw new SmoException(ExceptionTemplates.DataTypeUnsupported(sqlDataType.ToString()));
            }
        }

        /// <summary>
        /// Creates a new DataType object. The sqlDataType specifies the SQL Server data type.
        /// Precision and scale define the numeric precision and scale of Decimal and Numeric types.
        /// The scale may be 0.
        /// </summary>
        /// <param name="sqlDataType"></param>
        /// <param name="precision"></param>
        /// <param name="scale"></param>
        public DataType(SqlDataType sqlDataType, Int32 precision, Int32 scale)
        {
            switch(sqlDataType)
            {
                case SqlDataType.Decimal:
                case SqlDataType.Numeric:
                    this.sqlDataType = sqlDataType;
                    this.NumericPrecision = precision;
                    this.NumericScale = scale;
                    this.name = GetSqlName(sqlDataType);
                    break;
                default:
                    throw new SmoException(ExceptionTemplates.DataTypeUnsupported(sqlDataType.ToString()));
            }

        }

        /// <summary>
        /// Creates a new DataType object. The sqlDataType specifies the SQL Server data type.
        /// The type parameter specifies a SQL Server type string, i.e. "varchar(10)",
        /// an Xml schema collection, a user defined type, or a user defined type.
        /// </summary>
        /// <param name="sqlDataType"></param>
        /// <param name="type"></param>
        public DataType(SqlDataType sqlDataType, string type)
        {
            switch(sqlDataType)
            {
                case SqlDataType.Xml:
                case SqlDataType.UserDefinedDataType:
                case SqlDataType.UserDefinedType:
                case SqlDataType.UserDefinedTableType:
                    this.sqlDataType = sqlDataType;
                    this.Name = type;
                    break;
                default:
                    throw new SmoException(ExceptionTemplates.DataTypeUnsupported(sqlDataType.ToString()));
            }
        }

        /// <summary>
        /// Creates a new DataType object. The sqlDataType specifies the SQL Server data type.
        /// The type parameter specifies a SQL Server type string, i.e. "varchar(10)",
        /// an Xml schema collection, a user defined type, or a user defined type.
        /// </summary>
        /// <param name="sqlDataType"></param>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        public DataType(SqlDataType sqlDataType, string type, string schema)
        {
            switch(sqlDataType)
            {
                case SqlDataType.Xml:
                case SqlDataType.UserDefinedDataType:
                case SqlDataType.UserDefinedType:
                case SqlDataType.UserDefinedTableType:
                    this.sqlDataType = sqlDataType;
                    this.Name = type;
                    this.Schema = schema;
                    break;
                default:
                    throw new SmoException(ExceptionTemplates.DataTypeUnsupported(sqlDataType.ToString()));
            }
        }

        /// <summary>
        /// Allows a DataType object to be created based on a XmlSchemaCollection instance.
        /// </summary>
        /// <param name="xmlSchemaCollection"></param>
        public DataType(XmlSchemaCollection xmlSchemaCollection)
        {
            CheckInputObject(xmlSchemaCollection);

            this.sqlDataType = SqlDataType.Xml;
            this.name = xmlSchemaCollection.Name;
            this.schema = xmlSchemaCollection.Schema;
        }

        /// <summary>
        /// Allows a DataType object to be created based on a UserDefinedDataType instance.
        /// </summary>
        /// <param name="userDefinedDataType"></param>
        public DataType(UserDefinedDataType userDefinedDataType)
        {
            CheckInputObject(userDefinedDataType);
            this.sqlDataType = SqlDataType.UserDefinedDataType;
            this.name = userDefinedDataType.Name;
            this.schema = userDefinedDataType.Schema;
            this.MaximumLength = userDefinedDataType.Length;
            this.NumericPrecision = userDefinedDataType.NumericPrecision;
            this.NumericScale = userDefinedDataType.NumericScale;
        }

        /// <summary>
        /// Allows a DataType object to be created based on a UserDefinedTableType instance.
        /// </summary>
        /// <param name="userDefinedTableType"></param>
        public DataType(UserDefinedTableType userDefinedTableType)
        {
            CheckInputObject(userDefinedTableType);
            this.sqlDataType = SqlDataType.UserDefinedTableType;
            this.name = userDefinedTableType.Name;
            this.schema = userDefinedTableType.Schema;
        }

        /// <summary>
        /// Allows a DataType object to be created based on a UserDefinedType instance.
        /// </summary>
        /// <param name="userDefinedType"></param>
        public DataType(UserDefinedType userDefinedType)
        {
            CheckInputObject(userDefinedType);
            this.sqlDataType = SqlDataType.UserDefinedType;
            this.name = userDefinedType.Name;
            this.schema = userDefinedType.Schema;
        }

#endregion

#region Equal Overriding

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to DataType return false.
            DataType dt = obj as DataType;

            // Return true if the sqlDataType filed match
            return IsEqualInAllAspects(dt);
        }

        public bool Equals(DataType dt)
        {
            // Return true if the sqlDataType filed match
            return IsEqualInAllAspects(dt);
        }

        public override int GetHashCode()
        {
            return (int)this.sqlDataType;
        }

        private bool IsEqualInAllAspects(DataType dt)
        {
            if (dt == null)
            {
                return false;
            }

            if (this.sqlDataType != dt.sqlDataType)
            {
                return false;
            }

            if (this.name != dt.name)
            {
                return false;
            }

            if (this.schema != dt.schema)
            {
                return false;
            }

            if (this.maximumLength != dt.maximumLength)
            {
                return false;
            }

            if (this.numericPrecision != dt.numericPrecision)
            {
                return false;
            }

            if (this.numericScale != dt.numericScale)
            {
                return false;
            }

            return true;
        }
#endregion

#region Static Helpers
        /// <summary>
        /// Creates a DataType of type SqlDataType.BigInt
        /// </summary>
        public static DataType BigInt
        {
            get { return new DataType(SqlDataType.BigInt); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.HierarchyId
        /// </summary>
        public static DataType HierarchyId
        {
            get { return new DataType(SqlDataType.HierarchyId); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Binary
        /// </summary>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static DataType Binary(Int32 maxLength)
        {
            return new DataType(SqlDataType.Binary, maxLength);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Bit
        /// </summary>
        public static DataType Bit
        {
            get { return new DataType(SqlDataType.Bit); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Char
        /// </summary>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static DataType Char(Int32 maxLength)
        {
            return new DataType(SqlDataType.Char, maxLength);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.DateTime
        /// </summary>
        public static DataType DateTime
        {
            get { return new DataType(SqlDataType.DateTime); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Decimal
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static DataType Decimal(Int32 scale, Int32 precision)
        {
            return new DataType(SqlDataType.Decimal, precision, scale);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Numeric
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static DataType Numeric(Int32 scale, Int32 precision)
        {
            return new DataType(SqlDataType.Numeric, precision, scale);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Float
        /// </summary>
        public static DataType Float
        {
            get { return new DataType(SqlDataType.Float); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Geography
        /// </summary>
        public static DataType Geography
        {
            get { return new DataType(SqlDataType.Geography); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Geometry
        /// </summary>
        public static DataType Geometry
        {
            get { return new DataType(SqlDataType.Geometry); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Image
        /// </summary>
        public static DataType Image
        {
            get { return new DataType(SqlDataType.Image); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Int
        /// </summary>
        public static DataType Int
        {
            get { return new DataType(SqlDataType.Int); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Money
        /// </summary>
        public static DataType Money
        {
            get { return new DataType(SqlDataType.Money); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.NChar
        /// </summary>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static DataType NChar(Int32 maxLength)
        {
            return new DataType(SqlDataType.NChar, maxLength);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.NText
        /// </summary>
        public static DataType NText
        {
            get { return new DataType(SqlDataType.NText); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.NVarChar
        /// </summary>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static DataType NVarChar(Int32 maxLength)
        {
            return new DataType(SqlDataType.NVarChar, maxLength);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.NVarCharMax
        /// </summary>
        public static DataType NVarCharMax
        {
            get { return new DataType(SqlDataType.NVarCharMax); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Real
        /// </summary>
        public static DataType Real
        {
            get { return new DataType(SqlDataType.Real); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.SmallDateTime
        /// </summary>
        public static DataType SmallDateTime
        {
            get { return new DataType(SqlDataType.SmallDateTime); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.SmallInt
        /// </summary>
        public static DataType SmallInt
        {
            get { return new DataType(SqlDataType.SmallInt); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.SmallMoney
        /// </summary>
        public static DataType SmallMoney
        {
            get { return new DataType(SqlDataType.SmallMoney); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Text
        /// </summary>
        public static DataType Text
        {
            get { return new DataType(SqlDataType.Text); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Timestamp
        /// </summary>
        public static DataType Timestamp
        {
            get { return new DataType(SqlDataType.Timestamp); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.TinyInt
        /// </summary>
        public static DataType TinyInt
        {
            get { return new DataType(SqlDataType.TinyInt); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.UniqueIdentifier
        /// </summary>
        public static DataType UniqueIdentifier
        {
            get { return new DataType(SqlDataType.UniqueIdentifier); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.UserDefinedDataType
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static DataType UserDefinedDataType(string type, string schema)
        {
            return new DataType(SqlDataType.UserDefinedDataType, type, schema);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.UserDefinedDataType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DataType UserDefinedDataType(string type)
        {
            return new DataType(SqlDataType.UserDefinedDataType, type, string.Empty);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.UserDefinedTableType
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static DataType UserDefinedTableType(string type, string schema)
        {
            return new DataType(SqlDataType.UserDefinedTableType, type, schema);
        }

        /// <summary>
        /// Creates a DataType of SqlDataType.UserDefinedTableType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DataType UserDefinedTableType(string type)
        {
            return new DataType(SqlDataType.UserDefinedTableType, type, string.Empty);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.UserDefinedType
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static DataType UserDefinedType(string type, string schema)
        {
            return new DataType(SqlDataType.UserDefinedType, type, schema);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.UserDefinedType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DataType UserDefinedType(string type)
        {
            return new DataType(SqlDataType.UserDefinedType, type, string.Empty);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.VarBinary
        /// </summary>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static DataType VarBinary(Int32 maxLength)
        {
            return new DataType(SqlDataType.VarBinary, maxLength);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.VarBinaryMax
        /// </summary>
        public static DataType VarBinaryMax
        {
            get { return new DataType(SqlDataType.VarBinaryMax); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.VarChar
        /// </summary>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static DataType VarChar(Int32 maxLength)
        {
            return new DataType(SqlDataType.VarChar, maxLength);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.VarCharMax
        /// </summary>
        public static DataType VarCharMax
        {
            get { return new DataType(SqlDataType.VarCharMax); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Variant
        /// </summary>
        public static DataType Variant
        {
            get { return new DataType(SqlDataType.Variant); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Xml
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DataType Xml(string type )
        {
            return new DataType(SqlDataType.Xml, type, string.Empty);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Xml
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static DataType Xml(string type, string schema)
        {
            return new DataType(SqlDataType.Xml, type, schema);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Xml
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="xmlDocumentConstraint"></param>
        /// <returns></returns>
        public static DataType Xml(string type, string schema,
                                   XmlDocumentConstraint xmlDocumentConstraint)
        {
            DataType dt = new DataType(SqlDataType.Xml, type, schema);
            dt.XmlDocumentConstraint = xmlDocumentConstraint;
            return dt;
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.SysName
        /// </summary>
        public static DataType SysName
        {
            get { return new DataType(SqlDataType.SysName); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Date
        /// </summary>
        public static DataType Date
        {
            get { return new DataType(SqlDataType.Date); }
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Time
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static DataType Time(Int32 scale)
        {
            return new DataType(SqlDataType.Time, scale);
        }


        /// <summary>
        /// Creates a DataType of type SqlDataType.DateTimeOffset
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static DataType DateTimeOffset(Int32 scale)
        {
            return new DataType(SqlDataType.DateTimeOffset, scale);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.DateTime2
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static DataType DateTime2(Int32 scale)
        {
            return new DataType(SqlDataType.DateTime2, scale);
        }

        /// <summary>
        /// Creates a DataType of type SqlDataType.Json
        /// </summary>
        public static DataType Json
        {
            get { return new DataType(SqlDataType.Json); }
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }

        private void CheckInputObject(SqlSmoObject input)
        {
            if (input.State == SqlSmoState.Creating ||
                input.State == SqlSmoState.Dropped)
            {
                throw new SmoException(ExceptionTemplates.NoPendingObjForDataType(input.State.ToString()));
            }

            if (input.State == SqlSmoState.Creating)
            {
                throw new SmoException(ExceptionTemplates.NeedExistingObjForDataType(input.FullQualifiedName));
            }
        }

        // the parent object is the object that owns the DataType object
        // this means that its property bag should be in sync with this object
        // so any changes in this object should be reflected in its the property
        // bag of the parent object if any
        private SqlSmoObject parent = null;
        internal SqlSmoObject Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        private string name = string.Empty;
        /// <summary>
        /// Name of the type
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (null == value)
                {
                    throw new SmoException( ExceptionTemplates.InnerException, new ArgumentNullException("Name"));
                }

                if (this.sqlDataType == SqlDataType.Xml ||
                    this.sqlDataType == SqlDataType.UserDefinedDataType ||
                    this.SqlDataType == SqlDataType.UserDefinedTableType ||
                    this.sqlDataType == SqlDataType.UserDefinedType)
                {
                    name = value;
                    if (null != parent)
                    {
                        if (this.sqlDataType == SqlDataType.Xml)
                        {
                            parent.Properties.Get("XmlSchemaNamespace").Value = name;
                        }
                        else
                        {
                            parent.Properties.Get("DataType").Value = name;
                        }
                    }
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.CantSetTypeName(this.sqlDataType.ToString()));
                }
            }
        }

        internal SqlDataType sqlDataType = 0;
        /// <summary>
        /// Specifies the SQL Server data type.
        /// </summary>
        public SqlDataType SqlDataType
        {
            get
            {
                // get the DataType property from the property bag as "float" DataType changes to "real"
                // if the precision is less than 24.
                if (HasTypeChangedToReal())
                {
                    return SqlDataType.Real;
                }
                return sqlDataType;
            }
            [SuppressMessage("Microsoft.Performance", "CA1803:AvoidCostlyCallsWherePossible")]
            set
            {
                if (Enum.IsDefined(typeof(SqlDataType), value))
                {
                    sqlDataType = value;
                    name = GetSqlName(value);

                    if (null != parent)
                    {
                        if (this.sqlDataType != SqlDataType.UserDefinedDataType &&
                            this.SqlDataType != SqlDataType.UserDefinedTableType &&
                            this.sqlDataType != SqlDataType.UserDefinedType)
                        {
                            parent.Properties.Get("DataType").Value = GetSqlName(sqlDataType);
                            if (this.sqlDataType == SqlDataType.NVarCharMax ||
                                this.sqlDataType == SqlDataType.VarBinaryMax ||
                                this.sqlDataType == SqlDataType.VarCharMax)
                            {
                                parent.Properties.Get("Length").Value = -1;
                            }
                            if (parent.ServerVersion.Major >= 9)
                            {
                                schema = "sys";
                            }
                            else
                            {
                                schema = "dbo";
                            }
                        }
                        else
                        {
                            schema = string.Empty;
                        }
                    }

                }
                else
                {
                    throw new SmoException(ExceptionTemplates.UnknownSqlDataType(value.ToString()));
                }
            }
        }

        System.String schema = string.Empty;
        /// <summary>
        /// DataType schema
        /// </summary>
        public string Schema
        {
            get { return schema; }
            set
            {
                if (null == value)
                {
                    throw new SmoException( ExceptionTemplates.InnerException, new ArgumentNullException("Schema"));
                }

                if (this.sqlDataType == SqlDataType.Xml ||
                    this.sqlDataType == SqlDataType.UserDefinedDataType ||
                    this.SqlDataType == SqlDataType.UserDefinedTableType ||
                    this.sqlDataType == SqlDataType.UserDefinedType)
                {
                    schema = value;

                    if (null != parent)
                    {
                        if (this.sqlDataType == SqlDataType.Xml)
                        {
                            parent.Properties.Get("XmlSchemaNamespaceSchema").Value = schema;
                        }
                        else
                        {
                            parent.Properties.Get("DataTypeSchema").Value = schema;
                        }
                    }
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.CantSetTypeSchema(this.sqlDataType.ToString()));
                }
            }
        }

        System.Int32 maximumLength = 0;
        /// <summary>
        /// Maximum length of type in bytes.
        /// </summary>
        public Int32 MaximumLength
        {
            get { return maximumLength; }
            set
            {
                maximumLength = value;

                if (null != parent  &&
                    this.sqlDataType != SqlDataType.NVarCharMax &&
                    this.sqlDataType != SqlDataType.VarBinaryMax &&
                    this.sqlDataType != SqlDataType.VarCharMax )
                {
                        parent.Properties.Get("Length").Value = maximumLength;
                }
            }
        }

        System.Int32 numericPrecision = 0;
        /// <summary>
        /// Maximum precision of type if numeric-based, else 0.
        /// </summary>
        public Int32 NumericPrecision
        {
            get
            {
                // Precision of "float" DataType changes to 24 if it's specified between 1 and 24
                // and to 53 if specified between 25 and 53. Also, the precision of "decimal" gets
                // assigned to 18 if it's not specified.
                if (parent != null && parent.Properties.Get("NumericPrecision").Value != null)
                {
                    return (Int32)parent.Properties.Get("NumericPrecision").Value;
                }
                else
                {
                    return numericPrecision;
                }
            }
            set
            {
                numericPrecision = value;

                if (null != parent)
                {
                    parent.Properties.Get("NumericPrecision").Value = numericPrecision;
                }
            }
        }

        System.Int32 numericScale = 0;
        /// <summary>
        /// Maximum scale of type if numeric-based, else 0.
        /// </summary>
        public Int32 NumericScale
        {
            get { return numericScale; }
            set
            {
                numericScale = value;

                if (null != parent)
                {
                    parent.Properties.Get("NumericScale").Value = numericScale;
                }
            }
        }

        XmlDocumentConstraint xmlDocumentConstraint = XmlDocumentConstraint.Default;
        /// <summary>
        /// Specifies whether the XML data type is a fragment (content)
        /// or a well-formed XML 1.0 instance (document).
        /// </summary>
        public XmlDocumentConstraint XmlDocumentConstraint
        {
            get
            {
                return xmlDocumentConstraint;
            }
            set
            {
                xmlDocumentConstraint = value;

                if (null != parent)
                {
                    parent.Properties.Get("XmlDocumentConstraint").Value = xmlDocumentConstraint;
                }
            }
        }

        /// <summary>
        /// Indicates whether the type is a numeric type or not.
        /// </summary>
        public bool IsNumericType
        {
            get
            {
                bool isNumeric = false;

                switch (sqlDataType)
                {
                    case SqlDataType.BigInt:
                    case SqlDataType.Decimal:
                    case SqlDataType.Float:
                    case SqlDataType.Int:
                    case SqlDataType.Money:
                    case SqlDataType.Real:
                    case SqlDataType.SmallInt:
                    case SqlDataType.SmallMoney:
                    case SqlDataType.TinyInt:
                    case SqlDataType.Numeric:
                        isNumeric = true;
                        break;
                    case SqlDataType.UserDefinedDataType:
                        if (numericScale != 0)
                        {
                            isNumeric = true;
                        }
                        break;
                    default:
                        // All other types will enter this clause, thus returning 'false'
                        break;
                }
                return isNumeric;
            }
        }

        /// <summary>
        /// Indicates whether the type is a string type or not.
        /// </summary>
        public bool IsStringType
        {
            get
            {
                bool isString = false;

                switch (sqlDataType)
                {
                    case SqlDataType.Char:
                    case SqlDataType.NChar:
                    case SqlDataType.NText:
                    case SqlDataType.NVarChar:
                    case SqlDataType.NVarCharMax:
                    case SqlDataType.Text:
                    case SqlDataType.VarChar:
                    case SqlDataType.VarCharMax:
                        isString = true;
                        break;
                    case SqlDataType.UserDefinedDataType:           // There is no way of finding out for sure if UserDefinedDataType is a string type
                    default:
                        // All other types will enter this clause, thus returning 'false'
                        break;
                }
                return isString;
            }
        }

        internal DataType Clone()
        {
            DataType ret = new DataType();
            ret.Parent = null;
            // clone the object by just copying the fields over
            ret.sqlDataType = this.SqlDataType;
            ret.name = this.Name;
            ret.schema = this.Schema;
            ret.numericPrecision = this.NumericPrecision;
            ret.numericScale = this.NumericScale;
            ret.maximumLength = this.MaximumLength;

            return ret;
        }

        internal void ReadFromPropBag(SqlSmoObject sqlObject)
        {
            string dt = sqlObject.GetPropValueOptional("DataType", string.Empty) as string;
            string st = sqlObject.GetPropValueOptional("SystemType", string.Empty) as string;

            if (sqlObject is Parameter && !(sqlObject is NumberedStoredProcedureParameter) && parent.ServerVersion.Major >= 10)
            {
                bool isReadOnly = (bool)(sqlObject.GetPropValueOptional("IsReadOnly", false));
                if (isReadOnly) // IsReadOnly is true only for Parameters of UserDefinedTableType type
                {
                    sqlDataType = SqlDataType.UserDefinedTableType;
                    name = dt;
                    schema = sqlObject.GetPropValueOptional("DataTypeSchema", string.Empty) as string;
                    return;
                }
            }
            if (!(sqlObject is Sequence))
            {
                maximumLength = (Int32)sqlObject.GetPropValueOptional("Length", 0); //must be read before this.sqlDataType is set
            }
            numericPrecision = (Int32)sqlObject.GetPropValueOptional("NumericPrecision", 0);
            numericScale = (Int32)sqlObject.GetPropValueOptional("NumericScale", 0);

            //get the enum for system data types
            //special case for sysname, although it is a UDDT we treat it as a SDT
            if (dt == st || "sysname" == dt)
            {
                name = dt;
                // it's a system type
                sqlDataType = SqlToEnum(dt);
                if (sqlDataType == SqlDataType.NVarChar && maximumLength <= 0)
                {
                    sqlDataType = SqlDataType.NVarCharMax;
                }
                else if (sqlDataType == SqlDataType.VarBinary && maximumLength <= 0)
                {
                    sqlDataType = SqlDataType.VarBinaryMax;
                }
                else if (sqlDataType == SqlDataType.VarChar && maximumLength <= 0)
                {
                    sqlDataType = SqlDataType.VarCharMax;
                }
                else if (sqlDataType == SqlDataType.Xml)
                {
                    name = sqlObject.GetPropValueOptional("XmlSchemaNamespace", string.Empty) as string;
                    schema = sqlObject.GetPropValueOptional("XmlSchemaNamespaceSchema", string.Empty) as string;
                    xmlDocumentConstraint = (XmlDocumentConstraint)sqlObject.GetPropValueOptional("XmlDocumentConstraint", XmlDocumentConstraint.Default);
                }
            }
            else if( st.Length > 0)
            {
                // UserDefinedDataType
                sqlDataType = SqlDataType.UserDefinedDataType;
                name = dt;
                schema = sqlObject.GetPropValueOptional("DataTypeSchema", string.Empty) as string;
            }
            else
            {
                // UserDefinedType
                sqlDataType = SqlDataType.UserDefinedType;
                name = dt;
                schema = sqlObject.GetPropValueOptional("DataTypeSchema", string.Empty) as string;
            }
        }

        /// <summary>
        /// Returns the TSQL name of the given SqlDataType
        /// </summary>
        /// <param name="sqldt"></param>
        /// <returns></returns>
        public string GetSqlName(SqlDataType sqldt)
        {
            switch(sqldt)
            {
                case SqlDataType.BigInt:
                    return  "bigint";
                case SqlDataType.Binary:
                    return  "binary";
                case SqlDataType.Bit:
                    return  "bit";
                case SqlDataType.Char:
                    return  "char";
                case SqlDataType.DateTime:
                    return  "datetime";
                case SqlDataType.Decimal:
                    return  "decimal";
                case SqlDataType.Numeric:
                    return "numeric";
                case SqlDataType.Float:
                    return  "float";
                case SqlDataType.Geography:
                    return "geography";
                case SqlDataType.Geometry:
                    return "geometry";
                case SqlDataType.Image:
                    return  "image";
                case SqlDataType.Int:
                    return  "int";
                case SqlDataType.Money:
                    return  "money";
                case SqlDataType.NChar:
                    return  "nchar";
                case SqlDataType.NText:
                    return  "ntext";
                case SqlDataType.NVarChar:
                    return  "nvarchar";
                case SqlDataType.NVarCharMax:
                    return  "nvarchar";
                case SqlDataType.Real:
                    return  "real";
                case SqlDataType.SmallDateTime:
                    return  "smalldatetime";
                case SqlDataType.SmallInt:
                    return  "smallint";
                case SqlDataType.SmallMoney:
                    return  "smallmoney";
                case SqlDataType.Text:
                    return  "text";
                case SqlDataType.Timestamp:
                    return  "timestamp";
                case SqlDataType.TinyInt:
                    return  "tinyint";
                case SqlDataType.UniqueIdentifier:
                    return  "uniqueidentifier";
                case SqlDataType.UserDefinedDataType:
                    return string.Empty;
                case SqlDataType.UserDefinedTableType:
                    return string.Empty;
                case SqlDataType.UserDefinedType:
                    return string.Empty;
                case SqlDataType.VarBinary:
                    return "varbinary";
                case SqlDataType.HierarchyId:
                    return "hierarchyid";
                case SqlDataType.VarBinaryMax:
                    return "varbinary";
                case SqlDataType.VarChar:
                    return "varchar";
                case SqlDataType.VarCharMax:
                    return "varchar";
                case SqlDataType.Variant:
                    return "sql_variant";
                case SqlDataType.Xml:
                    //for sql data type the name is the xml namespace, no namespace by default
                    return "";
                case SqlDataType.SysName:
                    return "sysname";
                case SqlDataType.Date:
                    return "date";
                case SqlDataType.Time:
                    return "time";
                case SqlDataType.DateTimeOffset:
                    return "datetimeoffset";
                case SqlDataType.DateTime2:
                    return "datetime2";
                case SqlDataType.Json:
                    return "json";
            }

            return string.Empty;
        }

        internal static SqlDataType UserDefinedDataTypeToEnum(UserDefinedDataType uddt)
        {
            SqlDataType sqlDataType = SqlToEnum(uddt.SystemType);
            if (sqlDataType == SqlDataType.NVarChar || sqlDataType == SqlDataType.VarBinary || sqlDataType == SqlDataType.VarChar)
            {
                if (uddt.MaxLength == -1)
                {
                    if (sqlDataType == SqlDataType.NVarChar)
                    {
                        sqlDataType = SqlDataType.NVarCharMax;
                    }
                    else if (sqlDataType == SqlDataType.VarBinary)
                    {
                        sqlDataType = SqlDataType.VarBinaryMax;
                    }
                    else if (sqlDataType == SqlDataType.VarChar)
                    {
                        sqlDataType = SqlDataType.VarCharMax;
                    }
                }
            }
            else if (sqlDataType == SqlDataType.UserDefinedDataType)
            {
                Diagnostics.TraceHelper.Assert(false, "UDDT cannot have another UDDT as the system type");
            }


            return sqlDataType;
        }

        /// <summary>
        /// Converts a string Sql Data Type name into the corresponding SqlDataType enum
        /// value. Returns SqlDataType.None if the name is an unknown type.
        /// </summary>
        /// <param name="sqlTypeName"></param>
        /// <returns></returns>
        public static SqlDataType SqlToEnum(string sqlTypeName)
        {
            SqlDataType sqlDataType = SqlDataType.None;

            if (string.IsNullOrEmpty(sqlTypeName))
            {
                return sqlDataType;
            }

            switch (sqlTypeName.ToLowerInvariant())
            {
                case "bigint":
                    sqlDataType = SqlDataType.BigInt;
                    break;
                case "binary":
                    sqlDataType = SqlDataType.Binary;
                    break;
                case "bit":
                    sqlDataType = SqlDataType.Bit;
                    break;
                case "char":
                    sqlDataType = SqlDataType.Char;
                    break;
                case "datetime":
                    sqlDataType = SqlDataType.DateTime;
                    break;
                case "datetime2":
                    sqlDataType = SqlDataType.DateTime2;
                    break;
                case "time":
                    sqlDataType = SqlDataType.Time;
                    break;
                case "date":
                    sqlDataType = SqlDataType.Date;
                    break;
                case "datetimeoffset":
                    sqlDataType = SqlDataType.DateTimeOffset;
                    break;
                case "hierarchyid":
                    sqlDataType = SqlDataType.HierarchyId;
                    break;
                case "geometry":
                    sqlDataType = SqlDataType.Geometry;
                    break;
                case "geography":
                    sqlDataType = SqlDataType.Geography;
                    break;
                 case "decimal":
                    sqlDataType = SqlDataType.Decimal;
                    break;
                case "float":
                    sqlDataType = SqlDataType.Float;
                    break;
                case "image":
                    sqlDataType = SqlDataType.Image;
                    break;
                case "int":
                    sqlDataType = SqlDataType.Int;
                    break;
                case "money":
                    sqlDataType = SqlDataType.Money;
                    break;
                case "nchar":
                    sqlDataType = SqlDataType.NChar;
                    break;
                case "ntext":
                    sqlDataType = SqlDataType.NText;
                    break;
                case "nvarchar":
                    sqlDataType = SqlDataType.NVarChar;
                    break;
                case "nvarcharmax":
                    sqlDataType = SqlDataType.NVarCharMax;
                    break;
                case "real":
                    sqlDataType = SqlDataType.Real;
                    break;
                case "smalldatetime":
                    sqlDataType = SqlDataType.SmallDateTime;
                    break;
                case "smallint":
                    sqlDataType = SqlDataType.SmallInt;
                    break;
                case "smallmoney":
                    sqlDataType = SqlDataType.SmallMoney;
                    break;
                case "text":
                    sqlDataType = SqlDataType.Text;
                    break;
                case "timestamp":
                    sqlDataType = SqlDataType.Timestamp;
                    break;
                case "tinyint":
                    sqlDataType = SqlDataType.TinyInt;
                    break;
                case "uniqueidentifier":
                    sqlDataType = SqlDataType.UniqueIdentifier;
                    break;
                case "userdefinedtype":
                    sqlDataType = SqlDataType.UserDefinedType;
                    break;
                case "varbinary":
                    sqlDataType = SqlDataType.VarBinary;
                    break;
                case "varbinarymax":
                    sqlDataType = SqlDataType.VarBinaryMax;
                    break;
                case "varchar":
                    sqlDataType = SqlDataType.VarChar;
                    break;
                case "varcharmax":
                    sqlDataType = SqlDataType.VarCharMax;
                    break;
                case "sql_variant":
                    sqlDataType = SqlDataType.Variant;
                    break;
                case "xml":
                    sqlDataType = SqlDataType.Xml;
                    break;
                case "sysname":
                    sqlDataType = SqlDataType.SysName;
                    break;
                case "numeric":
                    sqlDataType = SqlDataType.Numeric;
                    break;
                case "userdefineddatatype":
                    sqlDataType = SqlDataType.UserDefinedDataType;
                    break;
                case "json":
                    sqlDataType = SqlDataType.Json;
                    break;

                default:
                    /*Removing Strace as in case of computed columns , there might
                     * be a case when we donot provide DataTypeName  BUG 151436*/
                    break;
            }
            return sqlDataType;
        }

        private static bool IsSystemDataType80(SqlDataType dataType)
        {
            switch (dataType)
            {
                case SqlDataType.BigInt:
                case SqlDataType.Binary:
                case SqlDataType.Bit:
                case SqlDataType.Char:
                case SqlDataType.DateTime:
                case SqlDataType.Decimal:
                case SqlDataType.Float:
                case SqlDataType.Image:
                case SqlDataType.Int:
                case SqlDataType.Money:
                case SqlDataType.NChar:
                case SqlDataType.NText:
                case SqlDataType.Numeric:
                case SqlDataType.NVarChar:
                case SqlDataType.Real:
                case SqlDataType.SmallDateTime:
                case SqlDataType.SmallInt:
                case SqlDataType.SmallMoney:
                case SqlDataType.SysName:
                case SqlDataType.Text:
                case SqlDataType.Timestamp:
                case SqlDataType.TinyInt:
                case SqlDataType.UniqueIdentifier:
                case SqlDataType.VarBinary:
                case SqlDataType.VarChar:
                case SqlDataType.Variant:
                     return true;
            }
            return false;
        }

        private static bool IsSystemDataType90(SqlDataType dataType)
        {
            if (IsSystemDataType80(dataType))
            {
                return true;
            }

            switch (dataType)
            {
                case SqlDataType.NVarCharMax:
                case SqlDataType.VarCharMax:
                case SqlDataType.VarBinaryMax:
                case SqlDataType.Xml:
                    return true;
            }

            return false;
        }

        private static bool IsSystemDataType100(SqlDataType dataType)
        {
            if (IsSystemDataType90(dataType))
            {
                return true;
            }

            switch (dataType)
            {
                case SqlDataType.Date:
                case SqlDataType.DateTime2:
                case SqlDataType.DateTimeOffset:
                case SqlDataType.Geography:
                case SqlDataType.Geometry:
                case SqlDataType.HierarchyId:
                case SqlDataType.Time:
                    return true;
            }
            return false;
        }

        private static bool IsSystemDataType160(SqlDataType dataType)
        {
            if (IsSystemDataType100(dataType))
            {
                return true;
            }

            switch (dataType)
            {
                case SqlDataType.Json:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// This function verify whether given data type is system type or not for given version, engine type/edition.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="targetVersion"></param>
        /// <param name="engineType"></param>
        /// <param name="engineEdition"></param>
        /// <returns></returns>
        internal static bool IsSystemDataType(SqlDataType dataType, SqlServerVersion targetVersion, DatabaseEngineType engineType, DatabaseEngineEdition engineEdition)
        {
            // SQL DB doesn't care about server version
            //
            if (engineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return IsSystemDataTypeOnAzure(dataType, engineEdition);
            }

            // We don't want to check the SQL version for MI (its engine type is Standalone)
            // because versionless MI and versioned MI may have different versions.
            // Since both of them are supposed to support all system data types, we simply
            // treat them as the latest SQL version.
            //
            if (engineEdition == DatabaseEngineEdition.SqlManagedInstance || targetVersion >= SqlServerVersion.Version160)
            {
                // If a new type is added later on then a new IsSystemDataType method should be added
                // with those new types - and that one set as the default. Also update IsSystemDataTypeOnAzure
                // with the new IsSystemDataType method because Azure uses the latest types.
                //
                return IsSystemDataType160(dataType);
            }
            else if (targetVersion <= SqlServerVersion.Version80)
            {
                return IsSystemDataType80(dataType);
            }
            else if (targetVersion <= SqlServerVersion.Version90)
            {
                return IsSystemDataType90(dataType);
            }
            else
            {
                return IsSystemDataType100(dataType);
            }
        }

        /// <summary>
        /// Determines if the data type is supported on the target version
        /// </summary>
        /// <param name="dataType">The <see cref="SqlDataType"/> to check</param>
        /// <param name="targetVersion">The <see cref="SqlServerVersion"/> to check</param>
        /// <param name="engineType">The <see cref="SqlDataType"/> to check</param>
        /// <param name="engineEdition">The <see cref="DatabaseEngineEdition"/> to check</param>
        /// <returns></returns>
        internal static bool IsDataTypeSupportedOnTargetVersion(SqlDataType dataType, SqlServerVersion targetVersion, DatabaseEngineType engineType, DatabaseEngineEdition engineEdition)
        {
            //Check if it's a known system type and if so whether it's supported
            if (IsSystemDataType(dataType, targetVersion, engineType, engineEdition))
            {
                return true;
            }

            //UDTT were added in SQL 2008 (Version 100)
            if (targetVersion >= SqlServerVersion.Version100 && dataType == SqlDataType.UserDefinedTableType)
            {
                return true;
            }

            //UDT's and UDDT's are supported on all known versions (2000+)
            if (dataType == SqlDataType.UserDefinedType ||
                dataType == SqlDataType.UserDefinedDataType)
            {
                return true;
            }

            return false;
        }

        // This method should not be used in future code as now Cloud does support UDT. It is not removed because existing code may be dependent on it and the impact
        // of removing it is hard to evaluate.
        // For the new code, if we want to check the supportability of a data type in Azure, it is suggested to use IsSystemDataTypeOnAzure.
        //
        internal static bool IsDataTypeSupportedOnCloud(SqlDataType dataType)
        {
            //Only UserDefinedType is not supported on Cloud.
            return dataType != SqlDataType.UserDefinedType;
        }

        internal static bool IsSystemDataTypeOnAzure(SqlDataType dataType, DatabaseEngineEdition engineEdition)
        {
            bool isSupported = true;
            switch (engineEdition)
            {
                // By default, Azure engine type doesn't have restrictions on data types. However,
                // Some engine edition may have its own supportablitiy of data types.
                // Add such editions as cases here.
                //
                case DatabaseEngineEdition.SqlDataWarehouse:
                    isSupported = IsDataTypeSupportedOnSqlDw(dataType);
                    break;
                default:
                    break;
            }

            // If the data type is supported, check if it is a system data type
            //
            return isSupported ? IsSystemDataType160(dataType) : false;
        }

        internal static bool IsDataTypeSupportedOnSqlDw(SqlDataType dataType)
        {
            // JSON data type is not supported on SQL DW.
            //
            return dataType != SqlDataType.Json;
        }

        internal static void CheckColumnTypeSupportability(string parentName, string columnName, SqlDataType dataType, ScriptingPreferences sp)
        {
            if (!IsDataTypeSupportedOnTargetVersion(dataType, sp.TargetServerVersion, sp.TargetDatabaseEngineType, sp.TargetDatabaseEngineEdition))
            {
                throw new SmoException(ExceptionTemplates.UnsupportedColumnType(
                    parentName,
                    columnName,
                    dataType.ToString(),
                    sp.TargetServerVersion.ToString(),
                    sp.TargetDatabaseEngineType.ToString(),
                    sp.TargetDatabaseEngineEdition.ToString()));
            }
        }

        /// <summary>
        /// Special function for taking care of scripting of float type columns.
        /// This makes sense only before a column of float type is getting created.
        /// </summary>
        /// <param name="sqlType"></param>
        /// <param name="sObj"></param>
        /// <returns></returns>
        static internal bool IsTypeFloatStateCreating(string sqlType, SqlSmoObject sObj)
        {
            if (0 == sObj.StringComparer.Compare(sqlType, "float") && sObj.State == SqlSmoState.Creating)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Special function for taking care of scripting the correct DataType if it's "float"
        /// </summary>
        /// <returns></returns>
        private bool HasTypeChangedToReal()
        {
            if (this.sqlDataType == SqlDataType.Float && parent != null && parent.Properties.Get("DataType").Value != null && ((string)parent.Properties.Get("DataType").Value).Equals("real", StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }

        #region IXmlSerializable Members

        //at first look it may seem that we need not implement IXmlSerializable, since we are only serializing all
        //public instance properties, so XmlSerializer should automatically discover it. However, for DataType,
        //order of properties in serialization matters, hence we need this.

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null; // this is per recommendation in .net documentation for IXmlSerializable
        }

        void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
        {
            //read past the start element for this whole object, since XmlSerializer does not automatically do it.
            reader.ReadStartElement();

            //note that we are setting the private state, not invoking setter on the properties themselves
            //since setter imposes restrictions (for the public user) on what can and cannot be set.
            this.sqlDataType = (SqlDataType)Enum.Parse(typeof(SqlDataType), reader.ReadElementContentAsString("SqlDataType", string.Empty));
            this.name = reader.ReadElementContentAsString("Name", string.Empty);
            this.schema = reader.ReadElementContentAsString("Schema", string.Empty);
            this.maximumLength = reader.ReadElementContentAsInt("MaximumLength", string.Empty);
            this.numericPrecision = reader.ReadElementContentAsInt("NumericPrecision", string.Empty);
            this.numericScale = reader.ReadElementContentAsInt("NumericScale", string.Empty);
            this.xmlDocumentConstraint = (XmlDocumentConstraint)Enum.Parse(typeof(XmlDocumentConstraint), reader.ReadElementContentAsString("XmlDocumentConstraint", string.Empty));

            reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
        {
            //XmlSerializer will write both start and end element for this whole object, so just serializing individual properties.
            //note SqlDataType has to come first, since other properties depend on it (needed for deserialization).
            IFormatProvider iFP = SmoApplication.DefaultCulture;
            writer.WriteElementString("SqlDataType", Enum.GetName(typeof(SqlDataType), this.SqlDataType));
            writer.WriteElementString("Name", this.Name);
            writer.WriteElementString("Schema", this.Schema);
            writer.WriteElementString("MaximumLength", this.MaximumLength.ToString(iFP));
            writer.WriteElementString("NumericPrecision", this.NumericPrecision.ToString(iFP));
            writer.WriteElementString("NumericScale", this.NumericScale.ToString(iFP));
            writer.WriteElementString("XmlDocumentConstraint", Enum.GetName(typeof(XmlDocumentConstraint), this.XmlDocumentConstraint));
        }

        #endregion
    }
}
