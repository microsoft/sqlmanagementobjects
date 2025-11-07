// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// base class for all generic collections
    /// </summary>
    public abstract class SimpleObjectCollectionBase<TObject, TParent> : SortedListCollectionBase<TObject, TParent>, INamedObjectCollection
        where TObject : SqlSmoObject
        where TParent : SqlSmoObject
    {
        internal SimpleObjectCollectionBase(TParent parent) : base(parent)
        {
        }

        /// <summary>
        /// Initializes the storage
        /// </summary>
        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<TObject>(new SimpleObjectComparer(StringComparer));

        /// <summary>
        /// Returns the parent of the collection
        /// </summary>
        public TParent Parent => ParentInstance as TParent;

        /// <summary>
        /// Adds the given object to the collection
        /// </summary>
        /// <param name="obj"></param>
        public void Add(TObject obj) => AddImpl(obj);

        /// <summary>
        /// Contains
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name) => null == name
                ? throw new FailedOperationException(ExceptionTemplates.Contains, this, new ArgumentNullException(nameof(name)))
                : Contains(KeyFromName(name));

        /// <summary>
        /// Returns the object identified by the given name after ensuring the collection is initialized
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TObject this[string name] => GetObjectByName(name) as TObject;

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        { 
            var name = urn.GetAttribute("Name");
            if (null == name || (name.Length == 0 && !CanHaveEmptyName(urn)))
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            return KeyFromName(name);
        }

    
        internal virtual ObjectKeyBase KeyFromName(string name) => new SimpleObjectKey(name);
    }

    /// <summary>
    /// Base class for collections that can have items removed
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TParent"></typeparam>
    public abstract class RemovableCollectionBase<TObject, TParent> : SimpleObjectCollectionBase<TObject, TParent>
        where TObject : NamedSmoObject
        where TParent : SqlSmoObject
    {
        internal RemovableCollectionBase(TParent parent) : base(parent)
        {
        }

        /// <summary>
        /// Removes an item with the given name from the collection. If no item with that name is in the collection, the collection remains unchanged.
        /// </summary>
        /// <param name="name"></param>
        public void Remove(string name) => Remove(KeyFromName(name));

        /// <summary>
        /// Removes the given object from the collection
        /// </summary>
        /// <param name="obj"></param>
        public void Remove(TObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            RemoveObj(obj, KeyFromName(obj.Name));
        }
    }

    internal class SimpleObjectComparer : ObjectComparerBase
    {
        internal SimpleObjectComparer(IComparer stringComparer) : base(stringComparer)
        {
        }

        public override int Compare(object obj1, object obj2) => stringComparer.Compare((obj1 as SimpleObjectKey).Name, (obj2 as SimpleObjectKey).Name);
    }

    internal class SimpleObjectKey : ObjectKeyBase
    {
    
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="name"></param>
        public SimpleObjectKey(string name) : base()
        {
            Name = name;
        }

        static SimpleObjectKey()
        {
            _ = fields.Add(nameof(Name));
        }

        internal static readonly StringCollection fields = new StringCollection();

        /// <summary>
        /// Name of the object
        /// </summary>
        /// <value></value>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"[{SqlSmoObject.SqlBraket(Name)}]";

        /// <summary>
        /// GetExceptionName
        /// </summary>
        /// <returns></returns>
        public override string GetExceptionName() => Name;

        /// <summary>
        /// Urn suffix that identifies this object
        /// </summary>
        /// <value></value>
        public override string UrnFilter => $"@Name='{Urn.EscapeString(Name)}'";

        /// <summary>
        /// Return all fields that are used by this key.
        /// </summary>
        /// <returns></returns>
        public override StringCollection GetFieldNames() => fields;

        /// <summary>
        /// Clone the object.
        /// </summary>
        /// <returns></returns>
        public override ObjectKeyBase Clone() => new SimpleObjectKey(Name);

        internal override void Validate(Type objectType)
        {
            var acceptEmptyName = objectType.Equals(typeof(UserDefinedAggregateParameter)) ||
                                    objectType.Equals(typeof(UserDefinedFunctionParameter));
            if( null == Name || (Name.Length == 0 && !acceptEmptyName))
            {
                throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(objectType.ToString())).SetHelpContext("UnsupportedObjectNameExceptionText");
            }
        }

        /// <summary>
        /// True if the key is null.
        /// </summary>
        /// <value></value>
        public override bool IsNull => null == Name;

        /// <summary>
        /// Returns string comparer needed to compare the string portion of this key.
        /// </summary>
        /// <param name="stringComparer"></param>
        /// <returns></returns>
        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new SimpleObjectComparer(stringComparer);

    }
}

