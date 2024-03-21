// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Sdk.Differencing.SPI
{

    /// <summary>
    /// Base interface for all Differencing providers.
    /// </summary>
    public interface Provider
	{
	}

	/// <summary>
	/// The diff algorithm walks the graphs with the help of ISimpleSfcNode. 
	/// The object model can either directly implement the interface, or 
	/// return a wrapper.
	/// 
	/// If a graph to be compared natively implements ISimpleSfcNode, the
	/// node can be returned directly. Otherwise, a wrapper should 
	/// be returned.
	/// </summary>
	public abstract class SfcNodeAdapterProvider : Provider
    {
		/// <summary>
		/// Indicates if this provider supports the specified graph. If it is, 
		/// returns true, false otherwise.
		/// 
		/// If true is returned, calls to GetGraphAdapter() must return a 
		/// valid non-null graph adapter.
		/// 
		/// The method is called only once on the root node of each graph.
		/// </summary>
        public abstract bool IsGraphSupported(Object obj);

		/// <summary>
		/// The method is called only once on the root node of the graphs
		/// to be compared.
		/// </summary>
        public abstract ISfcSimpleNode GetGraphAdapter(Object obj);
    }

	/// <summary>
	/// The adapter from which the list of names of relations and properties is
	/// obtained. It is the metadata counterpart of ISimpleNode.
	/// </summary>
	public abstract class NodeItemNamesAdapterProvider : Provider
    {
		/// <summary>
		/// Indicates if this provider supports the specified graph. If it is, 
		/// returns true, false otherwise.
		/// 
		/// If true is returned,  calls to GetRelatedContainerNames(), GetRelatedObjectNames(), and GetPropertyNames() 
		/// must return a valid non-null IEnumerable.
		/// 
		/// The method is called only once on the root node of each graph.
		/// </summary>
        public abstract bool IsGraphSupported(ISfcSimpleNode node);

		/// <summary>
		/// Obtains a list of related container names for the specified nodes
		/// </summary>
        public abstract IEnumerable<string> GetRelatedContainerNames(ISfcSimpleNode node);

		/// <summary>
		/// Obtains a list of names for the specified nodes
		/// </summary>
        public abstract IEnumerable<string> GetRelatedObjectNames(ISfcSimpleNode node);

        /// <summary>
        /// Obtain a list of name for the specified nodes
        /// </summary>
        public abstract IEnumerable<string> GetPropertyNames(ISfcSimpleNode node);

		/// <summary>
		/// Checks if the specified container collection follows a natural order . 
		/// If true, the 
		/// returned container IEnumerable does not needed to be sorted.
		/// </summary>
        public abstract bool IsContainerInNatrualOrder(ISfcSimpleNode node, string name);
    }

    /// <summary>
    /// The provider supplies sorting 
    /// </summary>
	public abstract class ContainerSortingProvider : Provider
    {

		/// <summary>
		/// Indicates if this provider supports the specified graphs. If it is, 
		/// returns true, false otherwise.
		/// 
		/// If true is returned, the following calls to GetSortedList() and
		/// GetComparer() method must return a valid non-null object.
		/// 
		/// The method is called only one on the top most node of each graph.
		/// </summary>
		public abstract bool AreGraphsSupported(ISfcSimpleNode source, ISfcSimpleNode target);

		/// <summary>
		/// Obtains a sorted list. This implementation sorts the list using standard 
		/// List&gt;Object&lt; .Sort(IComparer) method. The IComparer is obtained by 
		/// GetComparer(ISfcSimpleNode, ISfcSimpleNode) method.
		/// 
		/// The method is called only if AreListsComparable(ISfcSimpleList, ISfcSimpleList) 
		/// returns true.
		/// 
		/// A provider overrides this method if it can provide a faster sorting. If the list
		/// is already sorted, the orignal list can be returned.
		/// </summary>
		public void SortLists(ISfcSimpleList source, ISfcSimpleList target, 
				out IEnumerable<ISfcSimpleNode> sortedSource, out IEnumerable<ISfcSimpleNode> sortedTarget)
        {
			IEnumerable<ISfcSimpleNode> sourceResult = null;
			IEnumerable<ISfcSimpleNode> targetResult = null;

			if (source.GetEnumerator().MoveNext())
			{
				List<ISfcSimpleNode> result = new List<ISfcSimpleNode>(source);
				result.Sort(GetComparer(source, source));
				sourceResult = result;
			}
			else
			{
				// empty list, don't do busy work, just return the original
				sourceResult = source;
			}
			if (target.GetEnumerator().MoveNext())
			{
				List<ISfcSimpleNode> result = new List<ISfcSimpleNode>(target);
				result.Sort(GetComparer(target, target));
				targetResult = result;
			}
			else
			{
				// empty list, don't do busy work, just return the original
				targetResult = target;
			}

			// do not to pollute the out params: do the assignment only at the end.
			sortedSource = sourceResult;
			sortedTarget = targetResult;
		}

		/// <summary>
		/// Obtain a Comparer for the specified list. The method is called only if
		/// AreListsComparable(ISfcSimpleList, ISfcSimpleList) returns true.
		/// 
		/// If AreListsComparable() returns true, this method must return a valid
		/// comparer.
		/// </summary>
        public abstract IComparer<ISfcSimpleNode> GetComparer(ISfcSimpleList list, ISfcSimpleList list2);

	}

    /// <summary>
    /// The provider supplies sorting 
    /// </summary>
    public abstract class PropertyComparerProvider : Provider
    {
        /// <summary>
        /// Indicates if this provider supports the specified graphs. If it is, 
        /// returns true, false otherwise.
        /// 
        /// If true is returned, the following calls to GetSortedList() and
        /// GetComparer() method must return a valid non-null object.
        /// 
        /// The method is called only one on the top most node of each graph.
        /// </summary>
        public abstract bool AreGraphsSupported(ISfcSimpleNode source, ISfcSimpleNode target);

        /// <summary>
        /// Compare the specified properties
        /// </summary>
        public abstract bool Compare(ISfcSimpleNode source, ISfcSimpleNode target, String propName);

    }

	/// <summary>
	/// A provider that enables a partial graph to participate in differencing
	/// with a complete graph.
	/// 
	/// A partial graph is a graph that does not contain all properties 
	/// because some values are not set, or the values are set to some 
	/// default values.
	/// 
	/// Before a graph is walked, the differencer looks up all known 
	/// AvailablePropertyValueProvider in the system and asks if any of the
	/// providers supports one of the graphs to be compared. If a provider
	/// indicates that it supports the graph, the graph is a partial graph.
	/// 
	/// When a partial graph is walked, the differencer calls  
	/// IsValueAvailable() for each property of each object. If true is returned,
	/// the property is compared. If false is returned, then the
	/// property is skipped.
	/// </summary>
	public abstract class AvailablePropertyValueProvider : Provider
    {

		/// <summary>
		/// Indicates if this provider supports the specified graph. If it is, 
		/// returns true, false otherwise.
		/// 
		/// If true is returned, calls to IsValueAvailable() 
		/// must return valid meaningful value.
		/// 
		/// The method is called only once on the root node of each graph.
		/// </summary>
        public abstract bool IsGraphSupported(ISfcSimpleNode node);

		/// <summary>
		/// Checks whether a property value is available. If it is not, the comparison 
		/// is not performed; else, consider it in the comparison.
		/// </summary>
        public abstract bool IsValueAvailable(ISfcSimpleNode node, string propName);
    }
}
