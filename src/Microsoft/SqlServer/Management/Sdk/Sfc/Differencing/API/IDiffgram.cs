// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Sdk.Differencing
{
    /// <summary>
    /// The data structure that holds an enumerable of Diff entries.
    /// </summary>
    public interface IDiffgram : IEnumerable<IDiffEntry>
    {
		/// <summary>
		/// The root node of the source object passed to the Diff service.
		/// </summary>
		Object SourceRoot { get; }

		/// <summary>
		/// The root node of the target object passed to the Diff service.
		/// </summary>
		Object TargetRoot { get; }
    }

    /// <summary>
    /// Enum to indicate the type of change
    /// </summary>
    [Flags]
    public enum DiffType
    {
        None = 0,
        Equivalent = 1,
        Created = 2,
        Deleted = 4,
        Updated = 8,
    }

    /// <summary>
    /// Represent the difference of two versions of an identical object.
    /// </summary>
    public interface IDiffEntry
    {
		/// <summary>
		/// The type of change between the Source and Target nodes. It does not describe the changes of the
		/// nodes' children.
		/// </summary>
        DiffType ChangeType { get; }

		/// <summary>
		/// The Urn representing the Source node. It is different from the Target Urn when
		/// the two nodes have different parents. It is null if ChangeType is Deleted.
		/// </summary>
        Urn Source { get; }

		/// <summary>
		/// The Urn representing the Target node. It is different from the Source Urn when
		/// the two nodes have different parents. It is null if ChangeType is Created.
		/// </summary>
        Urn Target { get; }

        /// <summary>
        /// A Collection of all relevant Properties. 
        /// 
        /// If the ChangeType is DiffType.Updated, this Dictionary contains paris of 
        /// source (updated) and target (original) property values, keyed their property 
        /// name. Otherwise, it contains no Property.
        /// </summary>
        IDictionary<String, IPair<Object>> Properties { get; }
    }

    /// <summary>
    /// Represent a source and target pair of a single generic type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPair<T>
    {
        T Source { get; }

        T Target { get; }
    }
}
