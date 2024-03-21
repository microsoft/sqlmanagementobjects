// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    internal abstract class DatabaseObjectBase
    {
        #region Metadata Collection Helper

        /// <summary>
        /// Represents a list of SMO objects.
        /// </summary>
        /// <typeparam name="T">Type of SMO objects in the list.</typeparam>
        public interface IMetadataList<T> : IEnumerable<T>
            where T : Smo.NamedSmoObject
        {
            int Count { get; }
        }

        public sealed class SmoCollectionMetadataList<T> : IMetadataList<T>
            where T : Smo.NamedSmoObject
        {
            private readonly Smo.SmoCollectionBase smoCollection;
            private readonly int count;

            public SmoCollectionMetadataList(Server server, Smo.SmoCollectionBase smoCollection)
            {
                Debug.Assert(server != null, "SmoMetadataProvider Assert", "server != null");
                Debug.Assert(smoCollection != null, "SmoMetadataProvider Assert", "smoCollection != null");

                Config.SmoInitFields initFields = Config.SmoInitFields.GetInitFields(typeof(T));
                server.TryRefreshSmoCollection(smoCollection, initFields);
                this.smoCollection = smoCollection;
                this.count = GetCount(smoCollection, server.IsConnected);
            }

            private static int GetCount(Smo.SmoCollectionBase smoCollectionBase, bool isConnected)
            {
                Debug.Assert(smoCollectionBase != null, "SmoMetadataProvider Assert", "smoCollectionBase != null");

                int count;

                try
                {
                    count = smoCollectionBase.Count;
                }
                catch (InvalidVersionEnumeratorException)
                {
                    //
                    // Suppress this exception, thrown when the property is generally not supported on Azure. 
                    // Since a collection does not have a propertybag, adding this access directly over here instead of 
                    // a generic method for now.
                    count = 0;
                }
                catch (Smo.UnsupportedVersionException)
                {
                    // server version is < 10 (pre Katmai)
                    count = 0;
                }
                catch (Exception)
                {
                    //
                    // Suppress all exceptions when working with connected server. 
                    if (isConnected)
                    {
                        count = 0;
                    }
                    else
                    {
                        throw;
                    }
                }

                return count;
            }

            private static IEnumerator<T> GetEmptyEnumerator()
            {
                yield break;
            }

            #region IMetadataList<T> Members

            public int Count
            {
                get { return this.count; }
            }

            #endregion

            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return this.count > 0 ? this.smoCollection.Cast<T>().GetEnumerator() : GetEmptyEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.count > 0 ? this.smoCollection.GetEnumerator() : GetEmptyEnumerator();
            }

            #endregion
        }

        public sealed class EnumerableMetadataList<T> : IMetadataList<T>
            where T : Smo.NamedSmoObject
        {
            private readonly IEnumerable<T> collection;

            public EnumerableMetadataList(IEnumerable<T> collection)
            {
                Debug.Assert(collection != null, "SmoMetadataProvider Assert", "collection != null");

                this.collection = collection;                
            }

            #region IMetadataList<T> Members

            public int Count
            {
                get { return this.collection.Count(); }
            }

            #endregion

            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return this.collection.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.collection.GetEnumerator();
            }

            #endregion
        }

        public abstract class CollectionHelperBase<T, V>
            where T : class, IMetadataObject
            where V : IMetadataCollection<T>
        {
            private readonly object m_syncRoot = new object();
            private V m_metadataCollection;

            public V MetadataCollection
            {
                get
                {
                    if (this.m_metadataCollection == null)
                        this.CreateAndSetMetadataCollection();

                    Debug.Assert(this.m_metadataCollection != null, "SmoMetadataProvider Assert", "m_metadataCollection != null");

                    return this.m_metadataCollection;
                }
            }

            protected abstract Server Server { get; }

            private void CreateAndSetMetadataCollection()
            {
                if (this.m_metadataCollection == null)
                {
                    lock (this.m_syncRoot)
                    {
                        if (this.m_metadataCollection == null)
                        {
                            try
                            {
                                this.m_metadataCollection = this.CreateMetadataCollection();
                            }
                            catch (Smo.SmoException ex1)
                            {
                                TraceHelper.TraceContext.TraceCatch(ex1);
                                this.m_metadataCollection = this.GetEmptyCollection();
                            }
                            catch (ConnectionException ex2)
                            {
                                TraceHelper.TraceContext.TraceCatch(ex2);
                                this.m_metadataCollection = this.GetEmptyCollection();
                            }
                            catch (Exception ex3)
                            {
                                // 
                                // Suppress all exceptions if in a connected mode.
                                if (this.Server.IsConnected)
                                {
                                    TraceHelper.TraceContext.TraceCatch(ex3);
                                    this.m_metadataCollection = this.GetEmptyCollection();
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
            }

            protected abstract V GetEmptyCollection();

            protected abstract V CreateMetadataCollection();
        }

        public abstract class MetadataListCollectionHelperBase<T, U, V> : CollectionHelperBase<T, V>
            where T : class, IMetadataObject
            where U : Smo.NamedSmoObject
            where V : IMetadataCollection<T>
        {
            protected override sealed V CreateMetadataCollection()
            {
                IMetadataList<U> smoMetadataList = this.RetrieveSmoMetadataList();

                Debug.Assert(smoMetadataList != null, "SmoMetadataProvider Assert", "smoMetadataList != null");

                V metadataCollection;

                int count = smoMetadataList.Count;
                CollationInfo collationInfo = this.GetCollationInfo();

                switch (count)
                {
                    case 0:
                        metadataCollection = this.GetEmptyCollection();
                        break;
                    case 1:
                        {
                            U item0 = null;
                            int idx = 0;

                            foreach (U smoObject in smoMetadataList)
                            {
                                Debug.Assert(idx < 1, "SmoMetadataProvider Assert", "idx < 1");
                                item0 = smoObject;
                                idx++;
                            }

                            Debug.Assert(item0 != null, "SmoMetadataProvider Assert", "item0 != null");

                            metadataCollection = this.CreateOneElementCollection(
                                collationInfo, this.CreateMetadataObject(item0));
                            break;
                        }
                    case 2:
                        {
                            U item0 = null;
                            U item1 = null;
                            int idx = 0;
                            foreach (U smoObject in smoMetadataList)
                            {
                                Debug.Assert(idx < 2, "SmoMetadataProvider Assert", "idx < 2");

                                if (idx == 0)
                                {
                                    item0 = smoObject;
                                }
                                else
                                {
                                    item1 = smoObject;
                                }
                                
                                idx++;
                            }

                            Debug.Assert(item0 != null, "SmoMetadataProvider Assert", "item0 != null");
                            Debug.Assert(item1 != null, "SmoMetadataProvider Assert", "item1 != null");

                            metadataCollection = this.CreateTwoElementsCollection(
                                collationInfo, this.CreateMetadataObject(item0), this.CreateMetadataObject(item1));
                            break;
                        }
                    default:
                        {
                            IEnumerable<T> items = smoMetadataList.Select<U, T>(this.CreateMetadataObject);
                            metadataCollection = this.CreateManyElementsCollection(collationInfo, items, count);
                            break;
                        }
                }

                return metadataCollection;
            }

            protected abstract CollationInfo GetCollationInfo();

            protected abstract IMetadataList<U> RetrieveSmoMetadataList();

            protected abstract V CreateOneElementCollection(CollationInfo collationInfo, T item0);

            protected abstract V CreateTwoElementsCollection(CollationInfo collationInfo, T item0, T item1);

            protected abstract V CreateManyElementsCollection(CollationInfo collationInfo, IEnumerable<T> items, int count);

            protected abstract T CreateMetadataObject(U smoObject);
        }

        public abstract class UnorderedCollectionHelperBase<T, U> : MetadataListCollectionHelperBase<T, U, IMetadataCollection<T>>
            where T : class, IMetadataObject
            where U : Smo.NamedSmoObject
        {
            protected override sealed IMetadataCollection<T> GetEmptyCollection()
            {
                return Collection<T>.Empty;
            }

            protected override sealed IMetadataCollection<T> CreateOneElementCollection(CollationInfo collationInfo, T item0)
            {
                Debug.Assert(collationInfo != null, "SmoMetadataProvider Assert", "collationInfo != null");
                Debug.Assert(item0 != null, "SmoMetadataProvider Assert", "item0 != null");

                return Collection<T>.CreateOrderedCollection(collationInfo, item0);
            }

            protected override sealed IMetadataCollection<T> CreateTwoElementsCollection(CollationInfo collationInfo, T item0, T item1)
            {
                Debug.Assert(collationInfo != null, "SmoMetadataProvider Assert", "collationInfo != null");
                Debug.Assert(item0 != null, "SmoMetadataProvider Assert", "item0 != null");
                Debug.Assert(item1 != null, "SmoMetadataProvider Assert", "item1 != null");

                return Collection<T>.CreateOrderedCollection(collationInfo, item0, item1);
            }

            protected override sealed IMetadataCollection<T> CreateManyElementsCollection(CollationInfo collationInfo, IEnumerable<T> items, int count)
            {
                IMutableMetadataCollection<T> mutableCollection = this.CreateMutableCollection(count, collationInfo);
                mutableCollection.AddRange(items);
                return mutableCollection;
            }

            protected abstract IMutableMetadataCollection<T> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo);
        }

        public abstract class OrderedCollectionHelperBase<T, U> : MetadataListCollectionHelperBase<T, U, IMetadataOrderedCollection<T>>
            where T : class, IMetadataObject
            where U : Smo.NamedSmoObject
        {
            protected override sealed IMetadataOrderedCollection<T> GetEmptyCollection()
            {
                return Collection<T>.EmptyOrdered;
            }

            protected override sealed IMetadataOrderedCollection<T> CreateOneElementCollection(CollationInfo collationInfo, T item0)
            {
                Debug.Assert(collationInfo != null, "SmoMetadataProvider Assert", "collationInfo != null");
                Debug.Assert(item0 != null, "SmoMetadataProvider Assert", "item0 != null");

                return Collection<T>.CreateOrderedCollection(collationInfo, item0);
            }

            protected override sealed IMetadataOrderedCollection<T> CreateTwoElementsCollection(CollationInfo collationInfo, T item0, T item1)
            {
                Debug.Assert(collationInfo != null, "SmoMetadataProvider Assert", "collationInfo != null");
                Debug.Assert(item0 != null, "SmoMetadataProvider Assert", "item0 != null");
                Debug.Assert(item1 != null, "SmoMetadataProvider Assert", "item1 != null");

                return Collection<T>.CreateOrderedCollection(collationInfo, item0, item1);
            }

            protected override sealed IMetadataOrderedCollection<T> CreateManyElementsCollection(CollationInfo collationInfo, IEnumerable<T> items, int count)
            {
                return Collection<T>.CreateOrderedCollection(collationInfo, items.ToArray());
            }
        }

        #endregion
    }
}
