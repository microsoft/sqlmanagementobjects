// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class UserDefinedClrType : SchemaOwnedObject<Smo.UserDefinedType>, IUserDefinedClrType
    {
        private readonly IMetadataCollection<IUdtDataMember> m_dataMembers;
        private readonly IMetadataCollection<IUdtMethod> m_methods;

        public UserDefinedClrType(Smo.UserDefinedType smoMetadataObject, Schema parent)
            : base(smoMetadataObject, parent)
        {
            // ISSUE-TODO-sboshra-2008/07/18 We have no means now to retrieve the list
            // of members of a UDT. We will return empty collection for now until the
            // engine and SMO support this.

            this.m_dataMembers = Collection<IUdtDataMember>.Empty;
            this.m_methods = Collection<IUdtMethod>.Empty;
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

        #region IClrDataType Members
        public string AssemblyName
        {
            get { return this.m_smoMetadataObject.AssemblyName; }
        }

        public string ClassName
        {
            get { return this.m_smoMetadataObject.ClassName; }
        }

        public bool IsBinaryOrdered
        {
            get { return this.m_smoMetadataObject.IsBinaryOrdered; }
        }

        public bool IsComVisible
        {
            get { return this.m_smoMetadataObject.IsComVisible; }
        }

        public bool IsNullable
        {
            get { return this.m_smoMetadataObject.IsNullable; }
        }

        public IMetadataCollection<IUdtMethod> Methods
        {
            get { return this.m_methods; }
        }

        public IMetadataCollection<IUdtDataMember> DataMembers
        {
            get { return this.m_dataMembers; }
        }
        #endregion

        #region IScalarDataType Members
        public bool IsSystem
        {
            get { return false; }
        }

        public bool IsClr
        {
            get { return true; }
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
            get { return null; }
        }

        public IClrDataType AsClrDataType
        {
            get { return this; }
        }
        #endregion
    }
}
