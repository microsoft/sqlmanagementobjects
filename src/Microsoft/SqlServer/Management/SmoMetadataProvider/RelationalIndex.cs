// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class RelationalIndex : Index, IRelationalIndex
    {
        private readonly Utils.IndexedColumnCollectionHelper columnCollection;
        private readonly Utils.OrderedColumnCollectionHelper orderedColumnsCollection;
        private IUniqueConstraintBase m_indexKey;

        public RelationalIndex(Database database, IDatabaseTable parent, Smo.Index smoIndex)
            : base(parent, smoIndex)
        {
            Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
            Debug.Assert(!Utils.IsSpatialIndex(smoIndex), 
                "SmoMetadataProvider Assert", "SMO index should not be spatial!");

            Debug.Assert(!Utils.IsXmlIndex(smoIndex), "SmoMetadataProvider Assert", "SMO index should not be XML!");

            this.columnCollection = new Utils.IndexedColumnCollectionHelper(database, parent, this.m_smoIndex.IndexedColumns);
            this.orderedColumnsCollection = new Utils.OrderedColumnCollectionHelper(database, parent, this.m_smoIndex.IndexedColumns);
        }

        public override T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region IRelationalIndex Members
        public bool CompactLargeObjects
        {
            get
            {
                bool? compactLargeObjects;
                Utils.TryGetPropertyValue(this.m_smoIndex, "CompactLargeObjects", out compactLargeObjects);

                return compactLargeObjects.GetValueOrDefault();
            }
        }

        public IFileGroup FileGroup
        {
            get
            {
                // ISSUE-TODO-sboshra-2009/02/18 Need to support this property
                return null;
            }
        }

        public IFileGroup FileStreamFileGroup
        {
            get
            {
                // ISSUE-TODO-sboshra-2009/02/18 Need to support this property
                return null;
            }
        }

        public IPartitionScheme FileStreamPartitionScheme
        {
            get
            {
                // ISSUE-TODO-sboshra-2009/02/18 Need to support this property
                return null;
            }
        }

        public string FilterDefinition
        {
            get
            {
                string value;
                Utils.TryGetPropertyObject<String>(this.m_smoIndex, "FilterDefinition", out value);

                return value;
            }
        }

        public IMetadataOrderedCollection<IIndexedColumn> IndexedColumns
        {
            get { return this.columnCollection.MetadataCollection; }
        }

        public IMetadataOrderedCollection<IOrderedColumn> OrderedColumns
        {
            get
            {
                return this.orderedColumnsCollection.MetadataCollection;
            }
        }

        public IUniqueConstraintBase IndexKey
        {
            get
            {
                if (this.m_indexKey == null)
                {
                    switch (this.m_smoIndex.IndexKeyType)
                    {
                        case Smo.IndexKeyType.DriPrimaryKey:
                            this.m_indexKey = new PrimaryKeyConstraint(this.m_parent, this);
                            break;
                        case Smo.IndexKeyType.DriUniqueKey:
                            this.m_indexKey = new UniqueConstraint(this.m_parent, this);
                            break;
                        default:
                            break;
                    }
                }

                return this.m_indexKey;
            }
        }

        public bool IsClustered
        {
            get { return this.m_smoIndex.IsClustered; }
        }

        public bool IsSystemNamed
        {
            get 
            {
                return Utils.GetPropertyValue<bool>(this.m_smoIndex, "IsSystemNamed").
                    GetValueOrDefault();
            }
        }

        public bool IsUnique
        {
            get { return this.m_smoIndex.IsUnique; }
        }

        public bool NoAutomaticRecomputation
        {
            get { return this.m_smoIndex.NoAutomaticRecomputation; }
        }

        public bool OnlineIndexOperation
        {
            get { return this.m_smoIndex.OnlineIndexOperation; }
        }

        public IPartitionScheme PartitionScheme
        {
            get
            {
                // ISSUE-TODO-sboshra-2009/02/18 Need to support this property
                return null;
            }
        }
        #endregion
    }
}
