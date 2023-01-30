// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class UserDefinedAggregateParameter : ParameterBase
    {
        internal UserDefinedAggregateParameter(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public UserDefinedAggregateParameter(UserDefinedAggregate userDefinedAggregate, System.String name, DataType dataType) : base()
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = userDefinedAggregate;
            this.DataType = dataType;
        }

        // returns the name of the type in the urn expression
        internal static new string UrnSuffix
        {
            get
            {
                return "Param";
            }
        }

        /// <summary>
        /// Name of UserDefinedAggregateParameter
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
        /// DataType of UserDefinedAggregateParameter
        /// </summary>
        [CLSCompliant(false)]
        [SfcReference(typeof(UserDefinedType), typeof(UserDefinedTypeResolver), "Resolve")]
        [SfcReference(typeof(UserDefinedDataType), typeof(UserDefinedDataTypeResolver), "Resolve")]
        [SfcReference(typeof(UserDefinedTableType), typeof(UserDefinedTableTypeResolver), "Resolve")]
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public override DataType DataType
        {
            get
            {
                return base.DataType;
            }
            set
            {
                base.DataType = value;
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
            if (!defaultTextMode)
            {
                string[] fields = {   
                                            "NumericPrecision",
                                            "NumericScale",
                                            "Length",
                                            "DataType",
                                            "DataTypeSchema",
                                            "SystemType",
                                            "IsReadOnly"};
                List<string> list = GetSupportedScriptFields(typeof(UserDefinedAggregateParameter.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
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
            return true;
        }
    }
}
