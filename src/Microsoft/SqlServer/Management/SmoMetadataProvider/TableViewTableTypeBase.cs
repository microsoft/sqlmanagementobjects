// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class TableViewTableTypeBase<S> : SchemaOwnedObject<S>, IDatabaseTable
        where S : Smo.TableViewTableTypeBase
    {
        private readonly Utils.ColumnCollectionHelper columnCollection;

        protected TableViewTableTypeBase(S smoMetadataObject, Schema parent)
            : base(smoMetadataObject, parent)
        {
            this.columnCollection = new Utils.ColumnCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject.Columns);
        }

        #region ITabular Members
        abstract public TabularType TabularType { get; }

        public IMetadataOrderedCollection<IColumn> Columns
        {
            get { return this.columnCollection.MetadataCollection; }
        }

        public ITabular Unaliased
        {
            get { return this; }
        }
        #endregion

        #region IDatabaseTable Members
        public IMetadataCollection<IConstraint> Constraints
        {
            get { return this.ConstraintCollection.MetadataCollection; }
        }

        public IMetadataCollection<IIndex> Indexes
        {
            get { return this.IndexCollection.MetadataCollection; }
        }

        public IMetadataCollection<IStatistics> Statistics
        {
            get
            {
                return Collection<IStatistics>.Empty;
            }
        }
        #endregion

        protected abstract Utils.ConstraintCollectionHelper ConstraintCollection { get; }
        protected abstract Utils.IndexCollectionHelper IndexCollection { get; }
    }
}
