// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    sealed class TableValuedFunction : UserDefinedFunction, ITableValuedFunction
    {
        private IMetadataOrderedCollection<IParameter> m_parameters;

        private readonly Utils.ColumnCollectionHelper columnCollection;
        private readonly Utils.ConstraintCollectionHelper constraintCollection;
        private readonly Utils.IndexCollectionHelper indexCollection;

        public TableValuedFunction(Smo.UserDefinedFunction function, Schema schema)
            : base(function, schema)
        {
            // only table-valued functions could be used to create this object
            Debug.Assert(this.IsTableValuedFunction, "SmoMetadataProvider Assert", "this.IsTableValuedFunction");

            this.columnCollection = new Utils.ColumnCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject.Columns);
            this.constraintCollection = new Utils.ConstraintCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject);
            this.indexCollection = new Utils.IndexCollectionHelper(this.Parent.Database, this, this.m_smoMetadataObject.Indexes);
        }

        public override IMetadataOrderedCollection<IParameter> Parameters
        {
            get
            {
                if (this.m_parameters == null)
                {
                    Database database = this.Parent.Parent;
                    this.m_parameters = Utils.UserDefinedFunction.CreateParameterCollection(
                        database, this.m_smoMetadataObject.Parameters, this.GetModuleInfo());
                }

                return this.m_parameters;
            }
        }

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region ITabular Members
        public TabularType TabularType
        {
            get { return TabularType.TableValuedFunction; }
        }

        public IMetadataOrderedCollection<IColumn> Columns
        {
            get { return this.columnCollection.MetadataCollection; }
        }

        public ITabular Unaliased
        {
            get { return this; }
        }

        public IMetadataCollection<IConstraint> Constraints
        {
            get { return this.constraintCollection.MetadataCollection; }
        }

        public IMetadataCollection<IIndex> Indexes
        {
            get { return this.indexCollection.MetadataCollection; }
        }

        public IMetadataCollection<IStatistics> Statistics
        {
            get
            {
                return Collection<IStatistics>.Empty;
            }
        }
        #endregion

        #region ITableValuedFunction Members
        public bool IsInline
        {
            get
            {
                return this.m_smoMetadataObject.FunctionType == Smo.UserDefinedFunctionType.Inline;
            }
        }

        public string TableVariableName
        {
            get
            {
                return !this.IsInline ? this.m_smoMetadataObject.TableVariableName : null;
            }
        }
        #endregion
    }
}
