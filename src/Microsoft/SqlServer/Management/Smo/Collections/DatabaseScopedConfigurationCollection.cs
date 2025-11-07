// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System;

namespace Microsoft.SqlServer.Management.Smo 
{
    public sealed partial class DatabaseScopedConfigurationCollection : SimpleObjectCollectionBase<DatabaseScopedConfiguration, Database>
    {
        ///<summary>
        ///Returns a DatabaseScopedConfiguration identified by the given name. 
        ///If the parent object is in Creating state and the configuration object is not in the collection,
        ///and new configuration with the given name and empty value will be added to the collection and returned.
        ///If the parent object is not in Creating state and the named configuration  is not found, 
        ///a SmoException will be thrown.
        ///</summary> 
        internal override SqlSmoObject GetObjectByName(string name)
        {
            if (!this.ContainsKey(new SimpleObjectKey(name)))
            {
                // The unknown configuration would be saved first on the offline scenario.
                if (Parent.State == SqlSmoState.Creating)
                {
                    DatabaseScopedConfiguration newConfig = new DatabaseScopedConfiguration(Parent, name)
                    {
                        Value = string.Empty,
                        ValueForSecondary = string.Empty
                    };
                    InternalStorage.Add(newConfig.key, newConfig);
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

            return base.GetObjectByName(name) as DatabaseScopedConfiguration;
        }

        /// <summary>
        /// Initializes the internal storage
        /// </summary>
        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList<DatabaseScopedConfiguration>(new DatabaseScopedConfigurationObjectComparer());
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
