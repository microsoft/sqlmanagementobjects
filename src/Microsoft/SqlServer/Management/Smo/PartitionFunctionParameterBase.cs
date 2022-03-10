// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class PartitionFunctionParameter : ScriptNameObjectBase
    {
        internal PartitionFunctionParameter(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public PartitionFunctionParameter() : base() { }

        public PartitionFunctionParameter(PartitionFunction partitionFunction) : base()
        {
            this.Parent = partitionFunction;
        }

        public PartitionFunctionParameter(PartitionFunction partitionFunction, DataType dataType)
        {
            string paramType = dataType.GetSqlName(dataType.sqlDataType);

            ValidateName(paramType);
            this.key = new SimpleObjectKey(paramType);
            this.Parent = partitionFunction;

            if (UserDefinedDataType.TypeAllowsLength(dataType.Name, partitionFunction.StringComparer))
            {
                this.Length = dataType.MaximumLength;
            }
            else if (UserDefinedDataType.TypeAllowsPrecisionScale(dataType.Name, partitionFunction.StringComparer))
            {
                this.NumericPrecision = dataType.NumericPrecision;
                this.NumericScale = dataType.NumericScale;
            }
            else if (UserDefinedDataType.TypeAllowsScale(dataType.Name, partitionFunction.StringComparer))
            {   
                this.NumericScale = dataType.NumericScale;
            }
            else if (DataType.IsTypeFloatStateCreating(dataType.Name, partitionFunction))
            {
                this.NumericPrecision = dataType.NumericPrecision;
            }
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public PartitionFunction Parent
        {
            get
            {
                CheckObjectState();
                return base.ParentColl.ParentInstance as PartitionFunction;
            }
            set
            {
                SetParentImpl(value);
            }
        }


        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "PartitionFunctionParameter";
            }
        }

        /// <summary>
        /// Name of PartitionFunctionParameter
        /// </summary>
        [SfcKey(0)]
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
            return new string[]{
                                    "Length",
                                    "NumericPrecision",
                                    "NumericScale"
                                };
        }
    }
}



