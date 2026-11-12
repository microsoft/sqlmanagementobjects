// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class SpatialIndex : Index, ISpatialIndex
    {
        public SpatialIndex(IDatabaseTable parent, Smo.Index smoIndex)
            : base(parent, smoIndex)
        {
            Debug.Assert(smoIndex.IsSpatialIndex, "SmoMetadataProvider Assert", "Expected spatial SMO index!");
        }

        public override T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        public double BoundingBoxXMax
        {
            get { return this.GetBoundingBoxValue("BoundingBoxXMax"); }
        }

        public double BoundingBoxXMin
        {
            get { return this.GetBoundingBoxValue("BoundingBoxXMin"); }
        }

        public double BoundingBoxYMax
        {
            get { return this.GetBoundingBoxValue("BoundingBoxYMax"); }
        }

        public double BoundingBoxYMin
        {
            get { return this.GetBoundingBoxValue("BoundingBoxYMin"); }
        }

        public int CellsPerObject
        {
            get
            {
                // $TODO-SHIREEST-1/15: VSTS 649999 - SMO is throwing an exception on these property on Azure		
                int? cellsPerObject;
                Utils.TryGetPropertyValue(this.m_smoIndex, "CellsPerObject", out cellsPerObject);

                return cellsPerObject.HasValue ? cellsPerObject.Value : 16;
            }
        }

        public IColumn IndexedColumn
        {
            get
            {
                Debug.Assert(this.m_smoIndex.IndexedColumns.Count == 1, "SmoMetadataProvider Assert", "Expected spatial SMO index!");
                string columnName = this.m_smoIndex.IndexedColumns[0].Name;

                Debug.Assert(this.m_parent.Columns.Contains(columnName), "SmoMetadataProvider Assert", "Parent Table expected to have spatial column!");
                return this.m_parent.Columns[columnName];
            }
        }

        public GridDensity Level1Density
        {
            get { return SpatialIndex.GetGridDensity(this.m_smoIndex.Level1Grid); }
        }

        public GridDensity Level2Density
        {
            get { return SpatialIndex.GetGridDensity(this.m_smoIndex.Level2Grid); }
        }

        public GridDensity Level3Density
        {
            get { return SpatialIndex.GetGridDensity(this.m_smoIndex.Level3Grid); }
        }

        public GridDensity Level4Density
        {
            get { return SpatialIndex.GetGridDensity(this.m_smoIndex.Level4Grid); }
        }

        public bool NoAutomaticRecomputation
        {
            get
            {
                // $TODO-SHIREEST-1/15: VSTS 649999 - SMO is throwing an exception on these property on Azure		
                bool? noAutomaticRecomputation;
                Utils.TryGetPropertyValue(this.m_smoIndex, "NoAutomaticRecomputation", out noAutomaticRecomputation);

                return noAutomaticRecomputation.GetValueOrDefault();
            }
        }

        private static GridDensity GetGridDensity(SpatialGeoLevelSize spatialGeoLevelSize)
        {
            switch (spatialGeoLevelSize)
            {
                case SpatialGeoLevelSize.High:
                    return GridDensity.High;

                case SpatialGeoLevelSize.Low:
                    return GridDensity.Low;

                // The default SpatialGeoLevelSize  is medium
                case SpatialGeoLevelSize.Medium:
                case SpatialGeoLevelSize.None:
                    return GridDensity.Medium;

                default:
                    Debug.Fail("SmoMetadataProvider Assert", "Unexpected SpatialGeoLevelSize!");
                    return GridDensity.Medium;
            }
        }

        private double GetBoundingBoxValue(string propertyName)
        {
            Debug.Assert(propertyName != null, "SmoMetadataProvider Assert!", "propertyName != null");

            double? boundingBoxValue;
            Utils.TryGetPropertyValue<double>(this.m_smoIndex, propertyName, out boundingBoxValue);

            return boundingBoxValue.GetValueOrDefault();            
        }
    }
}
