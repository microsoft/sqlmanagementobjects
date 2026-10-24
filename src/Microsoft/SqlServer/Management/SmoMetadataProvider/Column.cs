// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    partial class Column : IColumn
    {
        public static Column Create(ISchemaOwnedObject parent, Smo.Column smoColumn)
        {
            Debug.Assert(smoColumn != null, "SmoMetadataProvider Assert", "smoColumn != null");

            Column column;

            if (smoColumn.Identity)
            {
                // create identity column
                column = new IdentityColumn(parent, smoColumn);
            }
            else if (smoColumn.Computed)
            {
                // create computed column
                column = new ComputedColumn(parent, smoColumn);
            }
            else
            {
                // create regular column
                column = new Column(parent, smoColumn);
            }

            return column;
        }
    }

    partial class Column : IColumn, ISmoDatabaseObject
    {
        private readonly ISchemaOwnedObject m_parent;
        private readonly Smo.Column m_smoColumn;
        private DefaultConstraint m_defaultConstraint;

        private Column(ISchemaOwnedObject parent, Smo.Column smoColumn)
        {
            Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");
            Debug.Assert(parent is ITabular, "SmoMetadataProvider Assert", "parent is ITabular");

            Debug.Assert(smoColumn != null, "SmoMetadataProvider Assert", "smoColumn != null");
            
            this.m_parent = parent;
            this.m_smoColumn = smoColumn;
        }

        #region IColumn Members

        public ITabular Parent
        {
            get { return (ITabular)this.m_parent; }
        }

        public bool InPrimaryKey
        {
            get
            {
                bool? result = Utils.GetPropertyValue<bool>(this.m_smoColumn, "InPrimaryKey");
                return result.HasValue ? result.Value : false;
            }
        }

        public bool Nullable
        {
            get { return this.m_smoColumn.Nullable; }
        }

        public ICollation Collation
        {
            get
            {
                // we have to use the GetPropertyObject method so we can actually see the 
                // null'ness of the value since SMO will throw if we try to get the value through
                // normal means. 
                string collationName = Utils.GetPropertyObject<string>(this.m_smoColumn, "Collation");

                return string.IsNullOrEmpty(collationName) ? null : Utils.GetCollation(collationName);
            }
        }

        public virtual ComputedColumnInfo ComputedColumnInfo
        {
            get { return null; }
        }

        public virtual IDefaultConstraint DefaultValue
        {
            get
            {
                if (this.m_defaultConstraint == null)
                {
                    Smo.DefaultConstraint smoDefaultConstraint = this.m_smoColumn.DefaultConstraint;

                    if (smoDefaultConstraint != null)
                        this.m_defaultConstraint = new DefaultConstraint(this, smoDefaultConstraint);
                }

                return this.m_defaultConstraint;
            }
        }

        public virtual IdentityColumnInfo IdentityColumnInfo
        {
            get { return null; }
        }

        public bool RowGuidCol
        {
            get
            {
                // need to use GetPropertyObject because RowGuidCol might not be set and SMO will throw
                return Utils.GetPropertyValue<bool>(this.m_smoColumn, nameof(RowGuidCol)) ?? false;
            }
        }

        public bool IsSparse
        {
            get
            {
                Utils.TryGetPropertyValue<bool>(this.m_smoColumn, nameof(IsSparse), out bool? val);

                return val.GetValueOrDefault();
            }
        }

        public bool IsColumnSet
        {
            get
            {
                Utils.TryGetPropertyValue<bool>(this.m_smoColumn, nameof(IsColumnSet), out bool? val);

                return val.GetValueOrDefault();
            }
        }

        public bool IsGeneratedAlwaysAsRowStart
        {
            get
            {
                Utils.TryGetPropertyValue<bool>(this.m_smoColumn, nameof(IsGeneratedAlwaysAsRowStart), out bool? val);

                return val.GetValueOrDefault();
            }
        }

        public bool IsGeneratedAlwaysAsRowEnd
        {
            get
            {
                Utils.TryGetPropertyValue<bool>(this.m_smoColumn, nameof(IsGeneratedAlwaysAsRowEnd), out bool? val);

                return val.GetValueOrDefault();
            }
        }

        public bool IsGeneratedAlwaysAsTransactionIdStart
        {
            get
            {
                Utils.TryGetPropertyValue<bool>(this.m_smoColumn, nameof(IsGeneratedAlwaysAsTransactionIdStart), out bool? val);

                return val.GetValueOrDefault();
            }
        }

        public bool IsGeneratedAlwaysAsTransactionIdEnd
        {
            get
            {
                Utils.TryGetPropertyValue<bool>(this.m_smoColumn, nameof(IsGeneratedAlwaysAsTransactionIdEnd), out bool? val);

                return val.GetValueOrDefault();
            }
        }

        public bool IsGeneratedAlwaysAsSequenceNumberStart
        {
            get
            {
                Utils.TryGetPropertyValue<bool>(this.m_smoColumn, nameof(IsGeneratedAlwaysAsSequenceNumberStart), out bool? val);

                return val.GetValueOrDefault();
            }
        }

        public bool IsGeneratedAlwaysAsSequenceNumberEnd
        {
            get
            {
                Utils.TryGetPropertyValue<bool>(this.m_smoColumn, nameof(IsGeneratedAlwaysAsSequenceNumberEnd), out bool? val);

                return val.GetValueOrDefault();
            }
        }

        #endregion

        #region IScalar Members
        public ScalarType ScalarType
        {
            get { return ScalarType.Column; }
        }

        public IScalarDataType DataType
        {
            get
            {
                IDatabase database = this.m_parent.Schema.Database;
                IDataType dataType = Utils.GetDataType(database, this.m_smoColumn.DataType);

                Debug.Assert(dataType != null, "SmoMetadataProvider Assert", "dataType != null");
                Debug.Assert(dataType.IsScalar, "SmoMetadataProvider Assert", "Column must have scalar data type");

                return dataType as IScalarDataType;
            }
        }

        public IColumn AsColumn
        {
            get { return this; }
        }
        #endregion

        #region IMetadataObject Members
        public string Name
        {
            get { return this.m_smoColumn.Name; }
        }

        public T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }
        #endregion

        #region ISmoDatabaseObject Members

        public Microsoft.SqlServer.Management.Smo.SqlSmoObject SmoObject
        {
            get { return this.m_smoColumn; }
        }

        #endregion
    }


    partial class Column
    {
        // COMPUTED COLUMN
        //
        //  collation               : YES
        //  computed column into    : YES
        //  default value           : NO
        //  data type               : YES
        //  identity column info    : NO

        private sealed class ComputedColumn : Column
        {
            private readonly ComputedColumnInfo m_computedColumnInfo;

            public ComputedColumn(ISchemaOwnedObject parent, Smo.Column smoColumn)
                : base(parent, smoColumn)
            {
                Debug.Assert(smoColumn.Computed, "SmoMetadataProvider Assert", "Expected computed column!");

                bool? isPersisted;
                Utils.TryGetPropertyValue<bool>(smoColumn, "IsPersisted", out isPersisted);

                this.m_computedColumnInfo = new ComputedColumnInfo(smoColumn.ComputedText, isPersisted.GetValueOrDefault());
            }

            public override ComputedColumnInfo ComputedColumnInfo
            {
                get { return this.m_computedColumnInfo; }
            }

            public override IDefaultConstraint DefaultValue
            {
                get { return null; }
            }
        }
    }


    partial class Column
    {
        // IDENTITY COLUMN
        //
        //  collation               : YES (however data types that are valid for COLLATE conflict w/ those of IDENTITY)
        //  computed column into    : NO
        //  default value           : NO
        //  data type               : YES
        //  identity column info    : YES

        private sealed class IdentityColumn : Column
        {
            private readonly IdentityColumnInfo m_identityColumnInfo;

            public IdentityColumn(ISchemaOwnedObject parent, Smo.Column smoColumn)
                : base(parent, smoColumn)
            {
                Debug.Assert(smoColumn.Identity, "SmoMetadataProvider Assert", "Expected identity column!");

                this.m_identityColumnInfo = new IdentityColumnInfo(smoColumn.IdentitySeed, smoColumn.IdentityIncrement, smoColumn.NotForReplication);
            }

            public override IdentityColumnInfo IdentityColumnInfo
            {
                get { return this.m_identityColumnInfo; }
            }

            public override IDefaultConstraint DefaultValue
            {
                get { return null; }
            }
        }
    }
}
