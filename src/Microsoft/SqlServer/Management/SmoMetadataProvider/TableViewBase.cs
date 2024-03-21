// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class TableViewBase<S> : SchemaOwnedObject<S>, ITableViewBase
        where S : Smo.TableViewBase
    {
        private readonly Utils.ColumnCollectionHelper columnCollection;

        protected TableViewBase(S smoMetadataObject, Schema parent)
            : base(smoMetadataObject, parent)
        {
            this.columnCollection = new Utils.ColumnCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject.Columns);
        }

        #region ITabular Members
        abstract public TabularType TabularType { get;}

        public IMetadataOrderedCollection<IColumn> Columns
        {
            get { return this.columnCollection.MetadataCollection; }
        }

        public ITabular Unaliased
        {
            get { return this; }
        }
        #endregion

        #region ITableViewBase Members
        public IMetadataCollection<IDmlTrigger> Triggers
        {
            get { return this.TriggerCollection.MetadataCollection; }
        }

        public abstract bool IsQuotedIdentifierOn { get; }
        
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
            get { return this.StatisticsCollection.MetadataCollection; }
        }
        #endregion

        protected abstract Utils.ConstraintCollectionHelper ConstraintCollection { get; }

        protected abstract Utils.IndexCollectionHelper IndexCollection { get; }

        protected abstract Utils.StatisticsCollectionHelper StatisticsCollection  { get; }

        protected abstract Utils.DmlTriggerCollectionHelper TriggerCollection  { get; }
    }
}
