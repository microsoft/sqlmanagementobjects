// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// ExecutionCache is a simple cache that manages cached items. The cache holds up to #capacity# number 
    /// of items and will remove the first item it added to the cache when it is full.
    /// 
    /// It is intent to be used by a single thread.
    /// 
    /// Implementation note: The access time is linear to the number of item in the cache. The cache is initially 
    /// extracted from ServerConnection such that the cache and item management can be unit tested thoughtfully 
    /// without configuration. It is not designed to be a widely-used cache utilities class. It is generalized 
    /// for testability. 
    /// </summary>
    /// <typeparam name="C"></typeparam>
    /// <typeparam name="K"></typeparam>
    internal class ExecutionCache<K, C>
        where K : class
        where C : CacheItem<K>
    {
        private List<C> items;

        private readonly int capacity;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">The capacity of the cache. If item is added when the cache is full, the first item of the cache will be evicted.</param>
        public ExecutionCache(int capacity)
        {
            if (capacity <= 1)
            {
                throw new ArgumentOutOfRangeException(StringConnectionInfo.InvalidArgumentCacheCapacity(1));
            }
            this.capacity = capacity;
        }

        /// <summary>
        /// Returns true of the cache contains an item with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (items == null)
                return false;
            foreach (C item in items)
            {
                if (key.Equals(item.Key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add a cache item into the cache. The item must have a unique key. The item and key must not be null.
        /// </summary>
        /// <param name="item">Item to be added</param>
        /// <exception cref="ArgumentException">Throws to indicate another item in the cache has the same key as the specified item</exception>
        public void Add(C item)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }
            if (item.Key == null)
            {
                throw new ArgumentException(StringConnectionInfo.InvalidArgumentCacheNullKey);
            }

            if (ContainsKey(item.Key))
            {
                throw new ArgumentException(StringConnectionInfo.InvalidArgumentCacheDuplicateKey(item.Key));
            }

            if (items == null)
            {
                items = new List<C>(capacity);
            }
            else
            {
                while (items.Count >= capacity)
                {
                    items.RemoveAt(0);
                }
            }

            items.Add(item);
        }

        /// <summary>
        /// Obtain the cache item with the specified key.
        /// </summary>
        public C this[K key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException();
                }

                if (items == null)
                    return null;
                foreach (C item in items)
                {
                    if (key.Equals(item.Key))
                    {
                        return item;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Clear results in all cache items.
        /// </summary>
        public void ClearResults()
        {
            if (items == null)
            {
                return;
            }
            foreach (C item in items)
            {
                item.ClearResult();
            }
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        public void Clear()
        {
            if (items == null)
            {
                return;
            }
            items.Clear();
        }

        /// <summary>
        /// Report if the cache is empty.
        /// </summary>
        public bool IsEmpty()
        {
            return (Count == 0);
        }

        /// <summary>
        /// Report the number of item in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                if (items == null)
                    return 0;

                return items.Count;
            }
        }
    }

    /// <summary>
    /// CacheItem is a item container for the payloads identified the same key.
    /// 
    /// The CacheItem has three specific concepts, Key, Result and ExecutionCount. Each item is identified
    /// by its key, and it must be unique. Adding non-unique item to the cache results in exception. 
    /// 
    /// The Result and ExecutionCount is specific payload that a CacheItem supports. The result distinguishes 
    /// between null result and no result by hasResult(). ExecutionCount is nothing more than a manual counter. 
    /// Sub-class of CacheItem can add more payload that suite the purpose of them.
    /// </summary>    
    internal abstract class CacheItem<K>
        where K : class
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public CacheItem()
        {
        }

        /// <summary>
        /// The key of the cache item.
        /// </summary>
        public abstract K Key
        {
            get;
        }

        private object result;
        private bool hasResult;

        /// <summary>
        /// The result of the item
        /// </summary>
        public object Result
        {
            get
            {
                if (!hasResult)
                    return null;
                return result;
            }
            set
            {
                hasResult = true;
                this.result = value;
            }
        }

        /// <summary>
        /// Returns to if result has been set.
        /// </summary>
        public bool HasResult()
        {
            return hasResult;
        }

        /// <summary>
        /// Unset the result
        /// </summary>
        public void ClearResult()
        {
            this.hasResult = false;
            result = null;
        }

        private int executionCount;
        /// <summary>
        /// Execution count
        /// </summary>
        public int ExecutionCount
        {
            get
            {
                return this.executionCount;
            }
            set
            {
                this.executionCount = value;
            }
        }
    }
}
