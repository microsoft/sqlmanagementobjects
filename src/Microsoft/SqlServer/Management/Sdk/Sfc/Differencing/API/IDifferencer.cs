// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Sdk.Differencing
{
    /// <summary>
    /// This interface defines the Differential Service on SFC and SMO objects. The service walks 
    /// and compares two specified graphs and returns an Diffgram (IEnumerable&lt;DiffEntry&gt;)
    /// representing the difference.
    /// 
    /// It is late-activated, meaning that it does not walk the graphs until the enumerator 
    /// is accessed.
    /// </summary>
    public interface IDifferencer
    {
        /// <summary>
        /// Compares two  graphs (defined below) 
        /// and returns the difference.
        /// 
        /// The result of CompareGraphs represents the difference 
        /// to be applied to the target to obtain the source. 
        /// (i.e., target + diff => source) 
        /// 
        /// To support both SMO and SFC, the input are left as 'object' type. However, it 
        /// allows only SqlSmoObject or SfcObject. This method throws InvalidArgumentException 
        /// if the type of the specified objects is invalid.
        /// 
        /// Two graphs are said to comparable (i.e., have the same type), if they have the same 
        /// structure at all levels, which means each node has the same type of children and 
        /// properties. If the specified graphs are not comparable, this method throws 
        /// InvalidArgumentException.
        /// 
        /// When a nodes on each graph has the same identity (represented by Urn in both 
        /// SMO and SFC), it is said to be identical. When two identical nodes have the same 
        /// values for all Properties, they are  said to be equivalent (disregard the children 
        /// they have).
        /// 
        /// During the comparison, this Service will examine all relevant nodes. (The relevance 
        /// of nodes and properties is domain-specific.) For each pair of identical nodes from both sides, 
        /// the Service compares all relevent Properties. If the nodes are not equivalent, a 
        /// DiffEntry with DiffType.Updated are added to the result. The children will then be 
        /// compared.
        /// 
        /// When a child appears in the source but not in the target, an DiffEntry with 
        /// DiffType.Created is added to the result, likewise for DiffType.Deleted.
        /// 
        /// The Differencer walks the children (of all level) of a Created or Deleted node.
        /// The children entries are included in the result.
        /// 
        /// Finally, each pair of identical children is checked for equivalence.
        /// 
        /// CompareGraphs() scales to very large graphs and does not retain the entire graphs in 
        /// memory if possible. Applications using this service should not have logic that depends 
        /// on the ordering of the results, which may be different when the implementation changes 
        /// in the future for further optimization.
        /// 
        /// The current implementation walks the tree in late-activate fashion. Resetting the result 
        /// IEnumerator might result in the graphs being walked again. 
        /// 
        /// The result IDiffgram is not thread-safe.
        /// </summary>
        /// <param name="source">The root node of the source graph to compare.</param>
        /// <param name="target">The root node of the target graph to compare.</param>
        /// <returns></returns>
        IDiffgram CompareGraphs(Object source, Object target);

        /// <summary>
        /// True to indicate the specified DiffType entries are included in the result
        /// diffgram.
        /// </summary>
        /// <param name="type">The DiffType to check</param>
        /// <returns>true to indicate the specified DiffType entries are included in the result
        /// diffgram.</returns>
        bool IsTypeEmitted(DiffType type);

        /// <summary>
        /// Set the specified DiffType to be included in the result diffgram. This option
        /// affects the DiffEntry only if it is set before CompareGraphs() is called.
        /// </summary>
        /// <param name="type">The DiffType to check</param>
        void SetTypeEmitted(DiffType type);

        /// <summary>
        /// Unset (or clear) the specified DiffType to be excluded from the result diffgram.
        /// This option affects the DiffEntry only if it is set before CompareGraphs() is called.
        /// </summary>
        /// <param name="type">The DiffType to check</param>
        void UnsetTypeEmitted(DiffType type);

    }
}
