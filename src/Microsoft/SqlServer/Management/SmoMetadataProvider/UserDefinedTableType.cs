// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class UserDefinedTableType : TableViewTableTypeBase<Smo.UserDefinedTableType>, IUserDefinedTableType
    {
        private readonly Utils.ConstraintCollectionHelper constraintCollection;
        private readonly Utils.IndexCollectionHelper indexCollection;

        public UserDefinedTableType(Smo.UserDefinedTableType metadataObject, Schema schema)
            : base(metadataObject, schema)
        {
            this.constraintCollection = new Utils.ConstraintCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject);
            this.indexCollection = new Utils.IndexCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject.Indexes);
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return false; }
        }

        public override TabularType TabularType
        {
            get { return TabularType.TableDataType; }
        }

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        protected override Utils.ConstraintCollectionHelper ConstraintCollection
        {
            get { return this.constraintCollection; }
        }

        protected override Utils.IndexCollectionHelper IndexCollection
        {
            get { return this.indexCollection; }
        }

        #region IDataType Members
        public bool IsScalar { get { return false; } }
        public bool IsTable { get { return true; } }
        public bool IsCursor { get { return false; } }
        public bool IsUnknown { get { return false; } }

        public IScalarDataType AsScalarDataType { get { return null; } }
        public ITableDataType AsTableDataType { get { return this; } }
        public IUserDefinedType AsUserDefinedType { get { return this; } }
        #endregion
    }
}
