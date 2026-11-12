// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Sdk.Differencing.Impl
{
    /// <summary>
    /// The data structure that holds an enumerable of Diff entries.
    /// </summary>
    internal abstract class Diffgram : IDiffgram, IEnumerable<IDiffEntry>
    {
        private Object source;

        private Object target;

        public Diffgram(Object source, Object target)
        {
            this.source = source;
            this.target = target;
        }

        /// <summary>
        /// Top most node of the source object (the object passed to the diff service)
        /// </summary>
        public Object SourceRoot 
        {
            get
            {
                return source;
            }
        }

        /// <summary>
        /// Top most node of the target object (the object passed to the diff service)
        /// </summary>
        public Object TargetRoot 
        {
            get
            {
                return target;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract IEnumerator<IDiffEntry> GetEnumerator();
    }

    class Pair<T> : IPair<T>
    {
        private T source;
        private T target;
        public Pair(T source, T target)
        {
            this.source = source;
            this.target = target;
        }
        public T Source 
        {
            get { return source; }
        }
        public T Target
        {
            get { return target; }
        }
    }
    /// <summary>
    /// Represent the difference of two versions of an identical object.
    /// </summary>
    class DiffEntry : IDiffEntry
    {
        private DiffType changeType;
        private Urn source;
        private Urn target;
        private IDictionary<string, IPair<Object>> properties;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ChangeType.ToString());
            sb.Append("{");
            sb.Append(source + ", " + target);
            if (ChangeType == DiffType.Updated)
            {
                sb.Append("- (" + (properties.Count) + ")");
            }
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// The type of change between the Source and Target node. The change type disregards the
        /// nodes' children.
        /// </summary>
        public DiffType ChangeType 
        {
            get
            {
                return changeType;
            }
            set
            {
                changeType = value;
            }
        }

        /// <summary>
        /// The Urn representing the Source node
        /// </summary>
        public Urn Source 
        {
            get
            {
                return source;
            }
            set
            {
                source = value;
            }
        }

        /// <summary>
        /// The Urn representing the Target node
        /// </summary>
        public Urn Target 
        {
            get
            {
                return target;
            }
            set
            {
                target = value;
            }
        }

        /// <summary>
        /// A Collection of all relevant Properties. 
        /// 
        /// If the ChangeType is DiffType.Updated, this Dictionary contains paris of 
        /// source (updated) and target (original) property values, keyed their property 
        /// name. Otherwise, it contains no Property.
        /// </summary>
        public IDictionary<String, IPair<Object>> Properties
        {
            get
            {
                if (properties == null)
                {
                    properties = new Dictionary<String, IPair<Object>>();
                }
                return properties;
            }
            set
            {
                properties = value;
            }
        }
    }
}
