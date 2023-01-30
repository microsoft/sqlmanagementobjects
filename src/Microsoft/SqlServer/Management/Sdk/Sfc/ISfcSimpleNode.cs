// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// ISfcSimpleNode is a simple interface to walk a Sfc or Sfc-adapted graph.
    /// </summary>
    public interface ISfcSimpleNode
    {
		/// <summary>
		/// The actual list that this simple list references
		/// </summary>
        Object ObjectReference { get; }

        /// <summary>
        /// The Urn this simple node represents
        /// </summary>
        Urn Urn { get; }

        /// <summary>
        /// A simple indexer of children list. It is keyed by the Relation's 
        /// property name as defined by SfcMetadataDiscovery.
        /// 
        /// The value of the dictionary is a list of children.
        /// </summary>
        ISfcSimpleMap<string, ISfcSimpleList> RelatedContainers { get; }

        /// <summary>
        /// A simple indexer of children. It is keyed by the Relation's 
        /// property name as defined by SfcMetadataDiscovery.
        /// 
        /// The value of the dictionary is a list of children.
        /// </summary>
        ISfcSimpleMap<string,  ISfcSimpleNode> RelatedObjects { get; }

        /// <summary>
        /// A simple indexer of the properties list.
        /// </summary>
        ISfcSimpleMap<string, object> Properties { get; }
    }

    /// <summary>
    /// A simple interface for indexer
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface ISfcSimpleMap<TKey, TValue>
    {
        TValue this[TKey key] { get; }
    }

    /// <summary>
    /// A simple interface for IEnumerable
    /// </summary>
    public interface ISfcSimpleList : IEnumerable<ISfcSimpleNode>
    {
        /// <summary>
        /// The actual list that this simple list represents
        /// </summary>
        IEnumerable ListReference { get; }
    }
}
