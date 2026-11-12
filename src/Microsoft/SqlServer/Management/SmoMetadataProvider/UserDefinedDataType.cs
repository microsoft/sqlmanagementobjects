// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class UserDefinedDataType : SchemaOwnedObject<Smo.UserDefinedDataType>, IUserDefinedDataType
    {
        private readonly ISystemDataType m_baseSystemDataType;

        public UserDefinedDataType(Smo.UserDefinedDataType metadataObject, Schema schema)
            : base(metadataObject, schema)
        {
            this.m_baseSystemDataType = SmoSystemDataTypeLookup.Instance.RetrieveSystemDataType(metadataObject);

            Debug.Assert(this.m_baseSystemDataType != null, "SmoMetadataProvider Assert",
                "UDDT must have a valid base system data type!");
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return false; }
        }

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region IDataType Members
        public bool IsScalar { get { return true; } }
        public bool IsTable { get { return false; } }
        public bool IsCursor { get { return false; } }
        public bool IsUnknown { get { return false; } }

        public IScalarDataType AsScalarDataType { get { return this; } }
        public ITableDataType AsTableDataType { get { return null; } }
        public IUserDefinedType AsUserDefinedType { get { return this; } }
        #endregion

        #region IScalarDataType Members
        public bool IsSystem
        {
            get { return false; }
        }

        public bool IsClr
        {
            get { return false; }
        }

        public bool IsXml
        {
            get { return false; }
        }

        public bool IsVoid
        {
            get { return false; }
        }

        public ISystemDataType BaseSystemDataType
        {
            get { return this.m_baseSystemDataType; }
        }

        public IClrDataType AsClrDataType
        {
            get { return null; }
        }
        #endregion

        #region IUserDefinedDataType Members
        public bool Nullable
        {
            get { return this.m_smoMetadataObject.Nullable; }
        }
        #endregion
    }
}
