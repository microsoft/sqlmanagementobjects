// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class View : TableViewBase<Smo.View>, IView
    {
        private readonly Utils.ConstraintCollectionHelper constraintCollection;
        private readonly Utils.IndexCollectionHelper indexCollection;
        private readonly Utils.StatisticsCollectionHelper statisticsCollection;
        private readonly Utils.DmlTriggerCollectionHelper triggerCollection;

        private bool viewInfoRetrieved;
        private IDictionary<string, object> viewInfo;

        public View(Smo.View smoMetadataObject, Schema parent)
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
            get { return TabularType.View; }
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

        private IDictionary<string, object> GetViewInfo()
        {
            if (!this.viewInfoRetrieved)
            {
                string definitionText = Utils.Module.GetDefinitionTest(this.m_smoMetadataObject);

                if (!string.IsNullOrEmpty(definitionText))
                {
                    this.viewInfo = ParseUtils.RetrieveViewDefinition(definitionText, new ParseOptions(string.Empty, this.IsQuotedIdentifierOn));
                }
                
                this.viewInfoRetrieved = true;
            }

            return this.viewInfo;
        }

        #region IView Members

        public bool HasCheckOption
        {
            get 
            {
                IDictionary<string, object> viewInfo = this.GetViewInfo();

                Debug.Assert(viewInfo == null || viewInfo.ContainsKey(PropertyKeys.HasCheckOption), "Parser Assert", "viewInfo == null || viewInfo.ContainsKey(PropertyKeys.HasCheckOption)");
                return (viewInfo != null) && (bool)viewInfo[PropertyKeys.HasCheckOption];
            }
        }

        public bool IsEncrypted
        {
            get { return this.m_smoMetadataObject.IsEncrypted; }
        }

        public bool IsSchemaBound
        {
            get { return this.m_smoMetadataObject.IsSchemaBound; }
        }

        public string QueryText
        {
            get 
            {
                IDictionary<string, object> viewInfo = this.GetViewInfo();

                Debug.Assert(viewInfo == null || viewInfo.ContainsKey(PropertyKeys.QueryDefinition), "Parser Assert", "viewInfo == null || viewInfo.ContainsKey(PropertyKeys.QueryDefinition)");
                return (viewInfo != null) ? (string)viewInfo[PropertyKeys.QueryDefinition] : null;
            }
        }

        public bool ReturnsViewMetadata
        {
            get
            {
                bool? returnsViewMetadata;

                Utils.TryGetPropertyValue<bool>(this.m_smoMetadataObject, "ReturnsViewMetadata", out returnsViewMetadata);
                return returnsViewMetadata.GetValueOrDefault();
            }
        }

        public override bool IsQuotedIdentifierOn
        {
            get
            {
                bool? isQuotedIdentifierOn;

                Utils.TryGetPropertyValue<bool>(this.m_smoMetadataObject, "QuotedIdentifierStatus", out isQuotedIdentifierOn);
                return isQuotedIdentifierOn.GetValueOrDefault();
            }
        }

        public bool HasColumnSpecification
        {
            get { return this.m_smoMetadataObject.HasColumnSpecification; }
        }

        #endregion
    }
}
