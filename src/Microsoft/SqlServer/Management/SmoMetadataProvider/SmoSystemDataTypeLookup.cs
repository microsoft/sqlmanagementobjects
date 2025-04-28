// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class SmoSystemDataTypeLookup : SystemDataTypeLookupBase
    {
        /// <summary>
        /// Gets singleton instance of the <see cref="SmoSystemDataTypeLookup"/> class.
        /// </summary>
        public static SmoSystemDataTypeLookup Instance
        {
            get { return Singleton.Instance; }
        }

        static private class Singleton
        {
            public static SmoSystemDataTypeLookup Instance = new SmoSystemDataTypeLookup();

            // static constructor to suppress beforefieldinit attribute
            static Singleton()
            {
            }
        }

        // Singleton Instance
        private SmoSystemDataTypeLookup()
        {
        }

        /// <summary>
        /// Retrieves SystemDataType for a given SMO DataType object.
        /// </summary>
        /// <param name="smoDataType">SMO data type object to find and return SystemDataType for.</param>
        /// <returns>DataType object if found; otherwise, null.</returns>
        public ISystemDataType Find(Smo.DataType smoDataType)
        {
            Debug.Assert(smoDataType != null, "SmoMetadataProvider Assert", "smoDataType != null");

            ISystemDataType systemDataType = null;
            DataTypeSpec typeSpec = GetDataTypeSpec(smoDataType.SqlDataType);

            if (typeSpec != null)
            {
                if (typeSpec.ArgSpec2 != null)
                {
                    systemDataType = this.Find(typeSpec, smoDataType.NumericPrecision, smoDataType.NumericScale);
                }
                else if (typeSpec.ArgSpec1 != null)
                {
                    systemDataType = this.Find(typeSpec, typeSpec.ArgIsScale ? smoDataType.NumericScale : smoDataType.MaximumLength);
                }
                else
                {
                    systemDataType = this.Find(typeSpec, false);
#if NYI_DATATYPE_XML
                    // We need to run some extra checks if the type is XML. This is 
                    // because we don't represent the XML arguments ourselves, rather,
                    // we rely on SMO data type object on representing these.
                    if (typeSpec.SqlDataType == SqlDataType.Xml)
                    {
                        // check if we can reuse the no-arg instance
                        Smo.DataType noArgSmoDataType = dataType.m_smoDataType;

                        if ((noArgSmoDataType.XmlDocumentConstraint != smoDataType.XmlDocumentConstraint) ||
                            (noArgSmoDataType.Schema != smoDataType.Schema))
                        {
                            systemDataType = new SystemDataType(smoDataType);
                        }
                    }
#endif
                }
            }

            return systemDataType;
        }

        /// <summary>
        /// Retrieves and returns the base system data type for the specified 
        /// user-defined data type (i.e. type alias).
        /// </summary>
        /// <param name="smoUserDefinedDataType">User-defined data type SMO metadata object.</param>
        /// <returns>SystemDataType object if specified UDDT is valid; otherwise null.</returns>
        public ISystemDataType RetrieveSystemDataType(Smo.UserDefinedDataType smoUserDefinedDataType)
        {
            Debug.Assert(smoUserDefinedDataType != null, "SmoMetadataProvider Assert", "smoUserDefinedDataType != null");

            DataTypeSpec typeSpec = DataTypeSpec.GetDataTypeSpec(smoUserDefinedDataType.SystemType);
            Debug.Assert(typeSpec != null, "SmoMetadataProvider Assert", "typeSpec != null");

            // IMPORTANT NOTE: 
            // If the UDDT aliases a 'max' data type (i.e. varbinary(max), varchar(max) 
            // or nvarchar(max), SMO dows not set 'SystemType' field to the corresponding
            // non-max value (e.g. varbinary instead of varbinary(max)). It seems, however, 
            // that the length field in this case is set to -1. We will rely on this behavior
            // to identify such cases. An assert is added below to ensure that this is the
            // correct assumption.

            if (smoUserDefinedDataType.Length == -1)
            {
                switch (typeSpec.SqlDataType)
                {
                    case SqlDataType.NVarChar:
                        typeSpec = DataTypeSpec.NVarCharMax;
                        break;
                    case SqlDataType.VarBinary:
                        typeSpec = DataTypeSpec.VarBinaryMax;
                        break;
                    case SqlDataType.VarChar:
                        typeSpec = DataTypeSpec.VarCharMax;
                        break;
                    default:
                        Debug.Fail("Bind Assert", "Invalid SMO data type length for '" + smoUserDefinedDataType + "'!");
                        break;
                }
            }

            ISystemDataType sysDataType;

            if (typeSpec.ArgSpec2 != null)
            {
                sysDataType = this.Find(typeSpec, smoUserDefinedDataType.NumericPrecision, smoUserDefinedDataType.NumericScale);
            }
            else if (typeSpec.ArgSpec1 != null)
            {
                int arg = typeSpec.ArgIsScale ? smoUserDefinedDataType.NumericScale : smoUserDefinedDataType.Length;
                sysDataType = this.Find(typeSpec, arg);
            }
            else
            {
                // ISSUE-TODO-sboshra-2008/12/05 Need to set handle XML type separetly
                // because we have to set its specifiec properties.

                sysDataType = this.Find(typeSpec, false);
            }

            return sysDataType;
        }

        private static DataTypeSpec GetDataTypeSpec(Smo.SqlDataType sqlDataType)
        {
            switch (sqlDataType)
            {
                case Smo.SqlDataType.BigInt:
                    return DataTypeSpec.BigInt;
                case Smo.SqlDataType.Binary:
                    return DataTypeSpec.Binary;
                case Smo.SqlDataType.Bit:
                    return DataTypeSpec.Bit;
                case Smo.SqlDataType.Char:
                    return DataTypeSpec.Char;
                case Smo.SqlDataType.Date:
                    return DataTypeSpec.Date;
                case Smo.SqlDataType.DateTime:
                    return DataTypeSpec.DateTime;
                case Smo.SqlDataType.DateTime2:
                    return DataTypeSpec.DateTime2;
                case Smo.SqlDataType.DateTimeOffset:
                    return DataTypeSpec.DateTimeOffset;
                case Smo.SqlDataType.Decimal:
                    return DataTypeSpec.Decimal;
                case Smo.SqlDataType.Float:
                    return DataTypeSpec.Float;
                case Smo.SqlDataType.Geography:
                    return DataTypeSpec.Geography;
                case Smo.SqlDataType.Geometry:
                    return DataTypeSpec.Geometry;
                case Smo.SqlDataType.HierarchyId:
                    return DataTypeSpec.HierarchyId;
                case Smo.SqlDataType.Image:
                    return DataTypeSpec.Image;
                case Smo.SqlDataType.Int:
                    return DataTypeSpec.Int;
                case Smo.SqlDataType.Money:
                    return DataTypeSpec.Money;
                case Smo.SqlDataType.NChar:
                    return DataTypeSpec.NChar;
                case Smo.SqlDataType.NText:
                    return DataTypeSpec.NText;
                case Smo.SqlDataType.Numeric:
                    return DataTypeSpec.Numeric;
                case Smo.SqlDataType.NVarChar:
                    return DataTypeSpec.NVarChar;
                case Smo.SqlDataType.NVarCharMax:
                    return DataTypeSpec.NVarCharMax;
                case Smo.SqlDataType.Real:
                    return DataTypeSpec.Real;
                case Smo.SqlDataType.SmallDateTime:
                    return DataTypeSpec.SmallDateTime;
                case Smo.SqlDataType.SmallInt:
                    return DataTypeSpec.SmallInt;
                case Smo.SqlDataType.SmallMoney:
                    return DataTypeSpec.SmallMoney;
                case Smo.SqlDataType.SysName:
                    return DataTypeSpec.SysName;
                case Smo.SqlDataType.Text:
                    return DataTypeSpec.Text;
                case Smo.SqlDataType.Time:
                    return DataTypeSpec.Time;
                case Smo.SqlDataType.Timestamp:
                    return DataTypeSpec.Timestamp;
                case Smo.SqlDataType.TinyInt:
                    return DataTypeSpec.TinyInt;
                case Smo.SqlDataType.UniqueIdentifier:
                    return DataTypeSpec.UniqueIdentifier;
                case Smo.SqlDataType.VarBinary:
                    return DataTypeSpec.VarBinary;
                case Smo.SqlDataType.VarBinaryMax:
                    return DataTypeSpec.VarBinaryMax;
                case Smo.SqlDataType.VarChar:
                    return DataTypeSpec.VarChar;
                case Smo.SqlDataType.VarCharMax:
                    return DataTypeSpec.VarCharMax;
                case Smo.SqlDataType.Variant:
                    return DataTypeSpec.Variant;
                case Smo.SqlDataType.Xml:
                    return DataTypeSpec.Xml;
                case Smo.SqlDataType.Json:
                    return DataTypeSpec.Json;
                case Smo.SqlDataType.Vector:
                    return DataTypeSpec.Vector;

                default:
                    Debug.Assert(IsSmoUserDefinedDataType(sqlDataType) || (sqlDataType == Smo.SqlDataType.None),
                                 "SmoMetadataProvider Assert", string.Concat("Unrecognized SMO SqlDataType '", sqlDataType.ToString(), "'!"));
                    return null;
            }
        }

        private static bool IsSmoUserDefinedDataType(Smo.SqlDataType sqlDataType)
        {
            return (sqlDataType == Smo.SqlDataType.UserDefinedDataType) ||
                   (sqlDataType == Smo.SqlDataType.UserDefinedTableType) ||
                   (sqlDataType == Smo.SqlDataType.UserDefinedType);
        }
    }
}
