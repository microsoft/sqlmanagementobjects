// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class ForeignKeyConstraint : IForeignKeyConstraint, ISmoDatabaseObject
    {
        private readonly ITable m_table;
        private readonly Smo.ForeignKey m_smoForeignKey;
        private readonly Utils.ForeignKeyColumnCollectionHelper columnCollection;

        public ForeignKeyConstraint(Database database, ITable table, Smo.ForeignKey smoForeignKey)
        {
            Debug.Assert(database != null, "SmoMetadataProvider Assert", "database != null");
            Debug.Assert(table != null, "SmoMetadataProvider Assert", "table != null");
            Debug.Assert(smoForeignKey != null, "SmoMetadataProvider Assert", "smoForeignKey != null");

            this.m_table = table;
            this.m_smoForeignKey = smoForeignKey;
            this.columnCollection = new Utils.ForeignKeyColumnCollectionHelper(database, table, this.ReferencedTable, smoForeignKey.Columns);
        }

        #region IConstraint Members
        
        public ITabular Parent
        {
            get { return this.m_table; }
        }

        public bool IsSystemNamed
        {
            get { return this.m_smoForeignKey.IsSystemNamed; }
        }

        public ConstraintType Type
        {
            get { return ConstraintType.ForeignKey; }
        }
        
        #endregion

        #region IForeignKeyConstraint Members
        public IMetadataOrderedCollection<IForeignKeyColumn> Columns
        {
            get { return this.columnCollection.MetadataCollection; }
        }

        public ForeignKeyAction DeleteAction
        {
            get { return this.ConvertSmoForeignKeyAction(this.m_smoForeignKey.DeleteAction); }
        }

        public bool IsEnabled
        {
            get { return this.m_smoForeignKey.IsEnabled; }
        }

        public bool IsChecked
        {
            get { return this.m_smoForeignKey.IsChecked; }
        }

        public bool NotForReplication
        {
            get { return this.m_smoForeignKey.NotForReplication; }
        }

        public ITable ReferencedTable
        {
            get
            {
                IDatabase database = this.m_table.Schema.Database;
                
                ISchema schema = database.Schemas[this.m_smoForeignKey.ReferencedTableSchema];
                Debug.Assert(schema != null, "SmoMetadataProvider Assert", "schema != null");

                ITable refTable = schema.Tables[this.m_smoForeignKey.ReferencedTable];
                Debug.Assert(refTable != null, "SmoMetadataProvider Assert", "refTable != null");

                return refTable;
            }
        }

        public ForeignKeyAction UpdateAction
        {
            get { return this.ConvertSmoForeignKeyAction(this.m_smoForeignKey.UpdateAction); }
        }
        #endregion

        #region IMetadataObject Members
        public string Name
        {
            get { return this.m_smoForeignKey.Name; }
        }

        public T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }
        #endregion

        private ForeignKeyAction ConvertSmoForeignKeyAction(Smo.ForeignKeyAction smoForeignKeyAction)
        {
            switch (smoForeignKeyAction)
            {
                case Smo.ForeignKeyAction.Cascade:
                    return ForeignKeyAction.Cascade;
                case Smo.ForeignKeyAction.NoAction:
                    return ForeignKeyAction.NoAction;
                case Smo.ForeignKeyAction.SetDefault:
                    return ForeignKeyAction.SetDefault;
                case Smo.ForeignKeyAction.SetNull:
                    return ForeignKeyAction.SetNull;
                default:
                    Debug.Fail("SmoMetadataProvider Assert", "Unrecognized SMO ForeignKeyAction value '" + smoForeignKeyAction + "'!");
                    return ForeignKeyAction.NoAction;
            }
        }

        #region ISmoDatabaseObject Members

        public Microsoft.SqlServer.Management.Smo.SqlSmoObject SmoObject
        {
            get { return this.m_smoForeignKey; }
        }

        #endregion
    }
}
