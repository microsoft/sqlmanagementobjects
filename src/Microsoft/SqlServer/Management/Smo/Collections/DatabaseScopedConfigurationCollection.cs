// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.Collections;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// DatabaseScopedConfigurationCollection
    ///</summary>
    public sealed class DatabaseScopedConfigurationCollection : SimpleObjectCollectionBase
    {
        internal DatabaseScopedConfigurationCollection(SqlSmoObject parentInstance)
            : base(parentInstance)
        {
        }

        public Database Parent
        {
            get
            {
                return this.ParentInstance as Database;
            }
        }

        public DatabaseScopedConfiguration this[Int32 index]
        {
            get
            {
                return GetObjectByIndex(index) as DatabaseScopedConfiguration;
            }
        }

        public DatabaseScopedConfiguration this[string name]
        {
            get
            {
                if (!this.Contains(name))
                {
                    // The unknown configuration would be saved first on the offline scenario.
                    if (Parent.State == SqlSmoState.Creating)
                    {
                        DatabaseScopedConfiguration newConfig = new DatabaseScopedConfiguration(Parent, name);
                        newConfig.Value = string.Empty;
                        newConfig.ValueForSecondary = string.Empty;
                        this.Add(newConfig);
                    }
                    else
                    {
                        if (Parent.State == SqlSmoState.Existing)
                        {
                            this.Refresh();
                        }

                        if (!this.Contains(name))
                        {
                            throw new SmoException(ExceptionTemplates.UnsupportedDatabaseScopedConfiguration(name));
                        }
                    }
                }

                return GetObjectByName(name) as DatabaseScopedConfiguration;
            }
        }

        public void CopyTo(DatabaseScopedConfiguration[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        public DatabaseScopedConfiguration ItemById(int id)
        {
            return (DatabaseScopedConfiguration)GetItemById(id);
        }

        protected override Type GetCollectionElementType()
        {
            return typeof(DatabaseScopedConfiguration);
        }

        internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return new DatabaseScopedConfiguration(this, key, state);
        }

        public void Add(DatabaseScopedConfiguration databaseScopedConfiguration)
        {
            AddImpl(databaseScopedConfiguration);
        }

        internal SqlSmoObject GetObjectByName(string name)
        {
            return GetObjectByKey(new SimpleObjectKey(name));
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            string name = urn.GetAttribute("Name");

            if (null == name || name.Length == 0)
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            return new SimpleObjectKey(name);
        }

        /// <summary>
        /// Initializes the internal storage
        /// </summary>
        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList(new DatabaseScopedConfigurationObjectComparer());
        }
    }

    internal class DatabaseScopedConfigurationObjectComparer : ObjectComparerBase
    {
        internal DatabaseScopedConfigurationObjectComparer()
            : base(null)
        {
        }

        ///<summary>
        /// The name of the database scoped configuration is case insensitive and .non-linguistic (i.e.
        /// not affected by the current culture of the running thread).
        ///</summary>
        public override int Compare(object obj1, object obj2)
        {
            return string.Compare((obj1 as SimpleObjectKey).Name, (obj2 as SimpleObjectKey).Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
