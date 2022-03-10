// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// This factory is used to obtain SfcSimpleNode from legacy Smo object. It allows Smo or Sfc 
    /// object model to be walked in the same code path.
    /// </summary>
    public class SfcSimpleNodeFactory
    {
        /// <summary>
        /// Singleton object
        /// </summary>
        private static SfcSimpleNodeFactory factory = new SfcSimpleNodeFactory();

        /// <summary>
        /// Obtains the singleton factory
        /// </summary>
        public static SfcSimpleNodeFactory Factory
        {
            get
            {
                return factory;
            }
        }

        private static IList<SimpleNodeAdapter> DEFAULT_ADAPTERS 
                = new List<SimpleNodeAdapter>(new SimpleNodeAdapter[] { new SfcSimpleNodeAdapter(), new IAlienObjectAdapter() });

        /// <summary>
        /// Obtains a SfcSimpleNode represents the specified legacy node. The specified node must
        /// be either a type of IAlienObject or SfcInstance.
        /// </summary>
        public ISfcSimpleNode GetSimpleNode(object node)
        {
            foreach (SimpleNodeAdapter adapter in DEFAULT_ADAPTERS)
            {
                if (adapter.CheckedIsSupported(node))
                {
                    return GetSimpleNode(node, adapter);
                }
            }
            throw new ArgumentException("node");
        }

        /// <summary>
        /// Obtains a SfcSimpleNode represents the specified legacy node. The specified node must
        /// be either a type of IAlienObject or SfcInstance.
        /// </summary>
        public ISfcSimpleNode GetSimpleNode(object node, SimpleNodeAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }
            if (!adapter.CheckedIsSupported(node))
            {
                throw new ArgumentException("adapter");
            }

            return new SfcSimpleNode(node, adapter);
        }

        /// <summary>
        /// Returns true of the specified object is supported.
        /// </summary>
        public bool IsSupported(object node)
        {
            foreach (SimpleNodeAdapter adapter in DEFAULT_ADAPTERS)
            {
                if (adapter.CheckedIsSupported(node))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Basic wrapper implementation of ISfcSimpleNode using reflection.
    /// 
    /// The data structure is designed to be as non-retaining as possible, meaning that
    /// the actual objects and wrappers are released as soon as they are not needed. Asking
    /// for any value from the node might result in different instance being returned. 
    /// (because the node does not track any of them.)
    /// 
    /// The class is not thread-safe. Multi-threaded program should start from distinct node.
    /// </summary>
    internal class SfcSimpleNode : ISfcSimpleNode
    {
        // the actual object
        private readonly object instance;

        // adapter
        private readonly SimpleNodeAdapter adapter;

        // A map contains all properties, keyed by name. The map is null, until the respective Property is invoked.
        private ISfcSimpleMap<string, object> properties;

        // A map contains all related children, keyed by name. The map is null, until the respective Property is invoked.
        private ISfcSimpleMap<string, ISfcSimpleList> container;

        // A map contains all related single object, keyed by name. The map is null, until the respective Property is invoked.
        private ISfcSimpleMap<string, ISfcSimpleNode> objects;

        /// <summary>
        /// Constructor with default access.
        /// 
        /// The implementation only support IAlienObject or SfcInstance.
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="adapter"></param>
        internal SfcSimpleNode(object reference, SimpleNodeAdapter adapter)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }

            if (reference is IAlienObject || reference is SfcInstance)
            {
                this.instance = reference;
            }
            else
            {
                throw new ArgumentException("reference");
            }
            this.adapter = adapter;

        }

        /// <summary>
        /// The actual object.
        /// </summary>
        public object ObjectReference
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// The Urn of the actual object.
        /// </summary>
        public Urn Urn
        {
            get
            {
                return adapter.CheckedGetUrn(instance);
            }
        }

        public ISfcSimpleMap<string, object> Properties
        {
            get
            {
                if (properties == null)
                {
                    properties = new SfcPropertyMap(instance, adapter);
                }
                return properties;
            }
        }

        public ISfcSimpleMap<string, ISfcSimpleList> RelatedContainers
        {
            get
            {
                if (container == null)
                {
                    container = new SfcContainerMap(instance, adapter);
                }
                return container;
            }
        }

        public ISfcSimpleMap<string, ISfcSimpleNode> RelatedObjects
        {
            get
            {
                if (objects == null)
                {
                    objects = new SfcNodeMap(instance, adapter);
                }
                return objects;
            }
        }

        public override String ToString()
        {
            return instance.ToString();
        }
    }

    /// <summary>
    /// A simple map implementation. It does not retain any value it returns. 
    /// 
    /// Asking the node multiple time will result in different instance backed by the same
    /// actual object.
    /// </summary>
    class SfcNodeMap : ISfcSimpleMap<string, ISfcSimpleNode>
    {
        private readonly object instance;

        private readonly SimpleNodeAdapter adapter;

        public SfcNodeMap(object reference, SimpleNodeAdapter adapter)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }

            this.instance = reference;
            this.adapter = adapter;
        }

        public ISfcSimpleNode this[string key]
        {
            get
            {
                object obj = adapter.CheckedGetObject(instance, key);
                if (obj != null)
                {
                    return new SfcSimpleNode(obj, adapter);
                }
                return null;
            }
        }
    }

    /// <summary>
    /// A simple map implementation. It does not retain any values it returns. 
    /// </summary>
    class SfcPropertyMap : ISfcSimpleMap<string, object>
    {
        private readonly object instance;

        private readonly SimpleNodeAdapter adapter;

        public SfcPropertyMap(object reference, SimpleNodeAdapter adapter)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }

            this.instance = reference;
            this.adapter = adapter;
        }

        public object this[string key]
        {
            get
            {
                return adapter.CheckedGetProperty(instance, key);
            }
        }
    }

    /// <summary>
    /// A simple map implementation. It does not retain any value it returns. 
    /// 
    /// Asking the children multiple time will result in different IEnumerable instance 
    /// to be returned.
    /// </summary>
    class SfcContainerMap : ISfcSimpleMap<string, ISfcSimpleList>
    {
        private readonly object instance;

        private readonly SimpleNodeAdapter adapter;

        public SfcContainerMap(object reference, SimpleNodeAdapter adapter)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }

            this.instance = reference;
            this.adapter = adapter;
        }

        public ISfcSimpleList this[string key]
        {
            get
            {
                return new SfcChildren(instance, key, adapter);
            }
        }
    }

    /// <summary>
    /// This is a support object for SfcContainerMap.
    /// </summary>
    class SfcChildren : ISfcSimpleList, IEnumerable<ISfcSimpleNode>
    {
        private readonly IEnumerable listReference;

        private readonly SimpleNodeAdapter adapter;

        public SfcChildren(object reference, string name, SimpleNodeAdapter adapter)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter");
            }

            this.listReference = adapter.CheckedGetEnumerable(reference, name);
            this.adapter = adapter;
        }

        public IEnumerable ListReference 
        {
            get
            {
                return listReference;
            }
        }

        public IEnumerator<ISfcSimpleNode> GetEnumerator()
        {
            return GetChildren();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected IEnumerator<ISfcSimpleNode> GetChildren()
        {
            if (listReference != null)
            {
                return new SfcChildrenEnumerator(listReference.GetEnumerator(), adapter);
            }
            return new List<ISfcSimpleNode>().GetEnumerator();
        }
    }

    /// <summary>
    /// This is a support object for SfcContainerMap.
    /// </summary>
    class SfcChildrenEnumerator : IEnumerator<ISfcSimpleNode>
    {
        private readonly IEnumerator children;

        private readonly SimpleNodeAdapter adapter;

        private ISfcSimpleNode current;

        public SfcChildrenEnumerator(IEnumerator children, SimpleNodeAdapter adapter)
        {
            this.children = children;
            this.adapter = adapter;
        }

        public bool MoveNext()
        {
            current = null;
            while (children.MoveNext())
            {
                Object childObject = children.Current;
                if (adapter.CheckedIsCriteriaMatched(childObject))
                {
                    return true;
                }
            }
            return false;
        }

        public ISfcSimpleNode Current
        {
            get
            {
                if (current == null)
                {
                    current = new SfcSimpleNode(children.Current, adapter);
                }
                return current;
            }
        }

        public void Reset()
        {
            current = null;
            children.Reset();
        }

        public void Dispose()
        {
            IDisposable disposable = children as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            current = null;
        }

        object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }
    }
}
