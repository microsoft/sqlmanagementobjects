// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Represents a collection of indexed JSON paths.
    /// </summary>
    public sealed partial class IndexedJsonPathCollection : SortedListCollectionBase<IndexedJsonPath, Index>
    {
        internal IndexedJsonPathCollection(SqlSmoObject parentInstance) : base((Index)parentInstance)
        {
        }

        /// <summary>
        /// Returns the parent object
        /// </summary>
        public Index Parent => ParentInstance as Index;

        protected override string UrnSuffix => IndexedJsonPath.UrnSuffix;

        /// <summary>
        /// Internal Storage
        /// </summary>
        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<IndexedJsonPath>(new IndexedJsonPathObjectComparer());

        /// <summary>
        /// Contains Method
        /// </summary>
        /// <param name="path">The JSON path string</param>
        /// <returns>Returns if there is an IndexedJsonPath with the given path.</returns>
        public bool Contains(string path) => Contains(new IndexedJsonPathObjectKey(path));

        /// <summary>
        /// Gets the indexed JSON path for a given path string
        /// </summary>
        /// <param name="path">The JSON path</param>
        /// <returns>The IndexedJsonPath if found, null otherwise</returns>
        public IndexedJsonPath GetItemByPath(string path) => InternalStorage[new IndexedJsonPathObjectKey(path)];

        /// <summary>
        /// Adds an IndexedJsonPath to the collection.
        /// </summary>
        /// <param name="indexedJsonPath">The IndexedJsonPath object to add</param>
        public void Add(IndexedJsonPath indexedJsonPath) => InternalStorage.Add(new IndexedJsonPathObjectKey(indexedJsonPath.Path), indexedJsonPath);

        /// <summary>
        /// Removes an IndexedJsonPath from the collection by path.
        /// </summary>
        /// <returns></returns>
        public void Remove(IndexedJsonPath indexedJsonPath) => InternalStorage.Remove(new IndexedJsonPathObjectKey(indexedJsonPath.Path));

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            var path = urn.GetAttribute(nameof(IndexedJsonPath.Path));
            return new IndexedJsonPathObjectKey(path);
        }

        internal override IndexedJsonPath GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new IndexedJsonPath(this, key, state);
    }

    internal class IndexedJsonPathObjectComparer : ObjectComparerBase
    {
        internal IndexedJsonPathObjectComparer()
            : base(null)
        {
        }

        public override int Compare(object obj1, object obj2) => 
            string.Compare(
                ((IndexedJsonPathObjectKey)obj1).Path, 
                ((IndexedJsonPathObjectKey)obj2).Path, 
                StringComparison.Ordinal);
    }

    internal class IndexedJsonPathObjectKey : ObjectKeyBase
    {
        public string Path;

        public IndexedJsonPathObjectKey(string path)
            : base()
        {
            Path = path;
        }

        static IndexedJsonPathObjectKey()
        {
            _ = fields.Add(nameof(Path));
        }

        internal static readonly StringCollection fields = new StringCollection();

        public override string ToString() => string.Format(SmoApplication.DefaultCulture, "{0}", Path);

        /// <summary>
        /// This is the one used for constructing the Urn
        /// </summary>
        public override string UrnFilter => string.Format(SmoApplication.DefaultCulture, "@Path='{0}'", Urn.EscapeString(Path));

        public override StringCollection GetFieldNames() => fields;

        public override ObjectKeyBase Clone() => new IndexedJsonPathObjectKey(Path);

        public override bool IsNull => string.IsNullOrEmpty(Path);

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new IndexedJsonPathObjectComparer();
    }
}
