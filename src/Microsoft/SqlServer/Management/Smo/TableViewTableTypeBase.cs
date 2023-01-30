// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    // This class contains the common properties of TableViewBase and UserDefinedTableType
    // TableViewBase now extends this class
    public class TableViewTableTypeBase : ScriptSchemaObjectBase, IExtendedProperties, IScriptable
    {
        internal TableViewTableTypeBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {
            m_Indexes = null;
        }

        protected internal TableViewTableTypeBase()
            : base()
        { }

        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        [CLSCompliant(false)]
        [SfcReference(typeof(Schema), typeof(SchemaCustomResolver), "Resolve")]
        public override System.String Schema
        {
            get
            {
                return base.Schema;
            }
            set
            {
                base.Schema = value;
            }
        }

        private IndexCollection m_Indexes;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Index), SfcObjectFlags.Design)]
        public virtual IndexCollection Indexes
        {
            get
            {
                if (this is View)
                {
                    ThrowIfBelowVersion80();
                }
                CheckObjectState();

                if (null == m_Indexes)
                {
                    m_Indexes = new IndexCollection(this);

                    // if the index exists don't allow changes to its columns 
                    if ((this is UserDefinedTableType) && (this.State == SqlSmoState.Existing))
                    {
                        m_Indexes.LockCollection(ExceptionTemplates.ReasonObjectAlreadyCreated(UserDefinedTableType.UrnSuffix));
                    }
                }
                return m_Indexes;
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        private ColumnCollection m_Columns = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(Column), SfcObjectFlags.Design | SfcObjectFlags.NaturalOrder)]
        public ColumnCollection Columns
        {
            get
            {
                CheckObjectState();
                if (null == m_Columns)
                {
                    m_Columns = new ColumnCollection(this);
                    View v = this as View;
                    if (null != v)
                    {
                        SetCollectionTextMode(v.TextMode, m_Columns);
                    }
                }
                return m_Columns;
            }
        }

        /// <summary>
        /// Overrides the permission scripting - No columns added here - TableViewBase will override this method
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // add the object-level permissions
            AddScriptPermissions(query, PermissionWorker.PermissionEnumKind.Object, sp);
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();

            if (null != m_Indexes)
            {
                m_Indexes.MarkAllDropped();
            }

            if (null != m_ExtendedProperties)
            {
                m_ExtendedProperties.MarkAllDropped();
            }

            if (null != m_Columns)
            {
                m_Columns.MarkAllDropped();
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        public override void Refresh()
        {
            base.Refresh();
        }
    }
}


