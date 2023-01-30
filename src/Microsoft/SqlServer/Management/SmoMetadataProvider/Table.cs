// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    sealed class Table : TableViewBase<Smo.Table>, ITable
    {
        private readonly Utils.ConstraintCollectionHelper constraintCollection;
        private readonly Utils.IndexCollectionHelper indexCollection;
        private readonly Utils.StatisticsCollectionHelper statisticsCollection;
        private readonly Utils.DmlTriggerCollectionHelper triggerCollection;

        public Table(Smo.Table smoMetadataObject, Schema parent)
            : base(smoMetadataObject, parent)
        {
            this.constraintCollection = new Utils.ConstraintCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject);
            this.indexCollection = new Utils.IndexCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject.Indexes);
            this.statisticsCollection = new Utils.StatisticsCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject.Statistics);
            this.triggerCollection = new Utils.DmlTriggerCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject.Triggers);
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return this.m_smoMetadataObject.IsSystemObject; }
        }

        public override TabularType TabularType
        {
            get { return TabularType.Table; }
        }

        public override bool IsQuotedIdentifierOn
        {
            get { return this.m_smoMetadataObject.QuotedIdentifierStatus; }
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

        protected override Utils.StatisticsCollectionHelper StatisticsCollection
        {
            get { return this.statisticsCollection; }
        }

        protected override Utils.DmlTriggerCollectionHelper TriggerCollection
        {
            get { return this.triggerCollection; }
        }
    }
}
