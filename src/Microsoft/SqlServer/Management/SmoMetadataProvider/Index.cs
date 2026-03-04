// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class Index : IIndex, ISmoDatabaseObject
    {
        protected readonly IDatabaseTable m_parent;
        protected readonly Smo.Index m_smoIndex;

        protected Index(IDatabaseTable parent, Smo.Index smoIndex)
        {
            Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");
            Debug.Assert(smoIndex != null, "SmoMetadataProvider Assert", "smoIndex != null");

            this.m_parent = parent;
            this.m_smoIndex = smoIndex;
        }

        #region IMetadataObject Members
        public string Name
        {
            get { return this.m_smoIndex.Name; }
        }

        public abstract T Accept<T>(IMetadataObjectVisitor<T> visitor);
        #endregion

        #region ISmoDatabaseObject Members

        public Microsoft.SqlServer.Management.Smo.SqlSmoObject SmoObject
        {
            get { return this.m_smoIndex; }
        }

        #endregion

        #region IIndex Members

        public ITabular Parent
        {
            get { return this.m_parent; }
        }

        public bool DisallowPageLocks
        {
            get { return this.m_smoIndex.DisallowPageLocks; }
        }

        public bool DisallowRowLocks
        {
            get { return this.m_smoIndex.DisallowRowLocks; }
        }

        public byte FillFactor
        {
            get { return this.m_smoIndex.FillFactor; }
        }

        public bool IgnoreDuplicateKeys
        {
            get { return this.m_smoIndex.IgnoreDuplicateKeys; }
        }

        public bool IsDisabled
        {
            get
            {
                bool? isDisabled;
                Utils.TryGetPropertyValue(this.m_smoIndex, "IsDisabled", out isDisabled);

                return isDisabled.GetValueOrDefault();
            }
        }

        public bool PadIndex
        {
            get { return this.m_smoIndex.PadIndex; }
        }

        public IndexType Type
        {
            get
            {
                bool? value;

                if (Utils.TryGetPropertyValue(this.m_smoIndex, "IsXmlIndex", out value) && value.HasValue && value.Value)
                    return IndexType.Xml;

                if (Utils.TryGetPropertyValue(this.m_smoIndex, "IsSpatialIndex", out value) && value.HasValue && value.Value)
                    return IndexType.Spatial;
               
                return IndexType.Relational;
            }
        }

        #endregion
    }
}
