// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    internal sealed class Statistics : IStatistics
    {
        private readonly IDatabaseTable parent;
        private readonly Smo.Statistic smoStatistic;

        private readonly Utils.StatisticsColumnCollectionHelper columnCollection;

        public Statistics(Database database, IDatabaseTable parent, Smo.Statistic smoStatistic)
        {
            Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
            Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");
            Debug.Assert(smoStatistic != null, "SmoMetadataProvider Assert", "smoStatistic != null");
            
            this.parent = parent;
            this.smoStatistic = smoStatistic;
            this.columnCollection = new Utils.StatisticsColumnCollectionHelper(database, parent, this.smoStatistic.StatisticColumns);
        }

        public IMetadataOrderedCollection<IColumn> Columns
        {
            get { return this.columnCollection.MetadataCollection; }
        }

        public string FilterDefinition
        {
            get
            {
                string value;
                Utils.TryGetPropertyObject(this.smoStatistic, "FilterDefinition", out value);

                return value;
            }
        }

        public bool NoAutomaticRecomputation
        {
            get { return this.smoStatistic.NoAutomaticRecomputation; }
        }

        public ITabular Parent
        {
            get { return this.parent; }
        }

        public StatisticsType Type
        {
            get
            {
                if (this.smoStatistic.IsAutoCreated)
                {
                    return StatisticsType.Auto;
                }

                if (this.smoStatistic.IsFromIndexCreation)
                {
                    return StatisticsType.ImplicitViaIndex;
                }

                return StatisticsType.Explicit;
            }
        }

        public T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) 
            {
                throw new ArgumentNullException("visitor"); 
            }

            return visitor.Visit(this);
        }

        public string Name
        {
            get { return this.smoStatistic.Name; }
        }
    }
}
