// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Differencing.SPI;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Sdk.Differencing.Impl
{
    /// <summary>
    /// Implementation of IDifferencer. The implementation is designed to be 
    /// adaptable. Domain specific information and logic are delegated to providers.
    /// </summary>
    internal class Differencer : IDifferencer
    {
        internal const string ComponentName = "Differencing";

        // A default available value provider that always return true
        private readonly static AvailablePropertyValueProvider DEFAULT_AVAILABLE_PROPERTY_VALUE_PROVIDER
            = new DefaultAvailablePropertyValueProvider();

        // a default sorter that always sort item in list using the Urn
        private readonly static ContainerSortingProvider DEFAULT_SORT_PROVIDER
            = new DefaultContainerSortingProvider();

        // a default property comparer that simple compare with Object.Equals()
        private readonly static PropertyComparerProvider DEFAULT_PROP_COMPARER
            = new DefaultPropertyComparerProvider();

        // marker object for walking one side of the graph
        private readonly static ISfcSimpleNode DUMMY_NODE = new DummySfcSimpleNode();

        // the registry from which the provider are obtained
        private readonly ProviderRegistry registry;

        // option to indicate what type to emit
        private DiffType emittedChangeType;

        /// <summary>
        /// Constructor
        /// </summary>
        internal Differencer(ProviderRegistry registry)
        {
            this.registry = registry;

            SetTypeEmitted(DiffType.Updated);
            SetTypeEmitted(DiffType.Created);
            SetTypeEmitted(DiffType.Deleted);
            UnsetTypeEmitted(DiffType.Equivalent);
        }

        #region options
        public bool IsTypeEmitted(DiffType type)
        {
            return (emittedChangeType & type) != 0;
        }

        public void SetTypeEmitted(DiffType type)
        {
            emittedChangeType |= type;
        }

        public void UnsetTypeEmitted(DiffType type)
        {
            emittedChangeType &= ~type;
        }
        #endregion

        // implement the diff method
        public IDiffgram CompareGraphs(Object source, Object target)
        {
            TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: entering public method.");
            if (source == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: argument null 'source'.");
                throw new ArgumentNullException("source");
            }
            if (target == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: argument null 'target'.");
                throw new ArgumentNullException("target");
            }

            if (source.GetType() != target.GetType())
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: argument types do not match.");
                throw new ArgumentException(StringDifferencing.MismatchType(source.ToString(), target.ToString()));
            }

            SfcNodeAdapterProvider nodeAdapter = FindNodeAdapterProvider(source);
            if (nodeAdapter == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: cannot find node adapter that can navigate the specified input.");
                throw new ArgumentException(StringDifferencing.NotRecognizedGraph);
            }

            ISfcSimpleNode sourceAdapted = AdaptNode(nodeAdapter, source);
            ISfcSimpleNode targetAdapted = AdaptNode(nodeAdapter, target);
            NodeItemNamesAdapterProvider nameProvider = FindNameProvider(sourceAdapted);
            if (nameProvider == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: cannot find name (metadata) provider that can navigate the specified input.");
                throw new ArgumentException(StringDifferencing.CannotFindMetadataProvider);
            }
            AvailablePropertyValueProvider sourceValueProvider = FindAvailableValueProvider(sourceAdapted);
            if (sourceValueProvider == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: cannot find value available provider. default is used.");
                sourceValueProvider = DEFAULT_AVAILABLE_PROPERTY_VALUE_PROVIDER;
            }
            AvailablePropertyValueProvider targetValueProvider = FindAvailableValueProvider(targetAdapted);
            if (targetValueProvider == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: cannot find value available provider. default is used.");
                targetValueProvider = DEFAULT_AVAILABLE_PROPERTY_VALUE_PROVIDER;
            }
            PropertyComparerProvider propComparer = FindPropertyComparerProvider(sourceAdapted, targetAdapted);
            if (propComparer == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: cannot find property comparer provider. default is used.");
                propComparer = DEFAULT_PROP_COMPARER;
            }
            ContainerSortingProvider sortProvider = FindContainerSortingProvider(sourceAdapted, targetAdapted);
            if (sortProvider == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: cannot find value sorting provider. default is used.");
                sortProvider = DEFAULT_SORT_PROVIDER;
            }

            TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: parameter verified.");
            LateActivatedDiffgram result = new LateActivatedDiffgram(this, nameProvider, 
                sourceValueProvider, targetValueProvider, sortProvider, propComparer,
                emittedChangeType, sourceAdapted, targetAdapted);

            TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: exiting public method.");
            return result;
        }

        protected void ProcessNodes(IDiffContext context, ISfcSimpleNode source, ISfcSimpleNode target)
        {
            if (Object.ReferenceEquals(DUMMY_NODE, target))
            {
                // The DUMMY_NODE signifies that Target reached the leaf node. 
                // We simply walk the source side and add all children to result
                foreach (string name in GetRelatedContainerNames(context.NodeItemNamesAdapterProvider, source))
                {
                    WalkCreatedList(context, source.RelatedContainers[name]);
                }

                foreach (string name in GetRelatedObjectNames(context.NodeItemNamesAdapterProvider, source))
                {
                    ISfcSimpleNode node = source.RelatedObjects[name];
                    if (node != null)
                    {
                        EmitCreatedEntry(context, node);
                    }
                }

            }
            else if (Object.ReferenceEquals(DUMMY_NODE, source))
            {
                // The DUMMY_NODE signifies that Source reached the leaf node. 
                // We simply walk the target side and add all children to result
                foreach (string name in GetRelatedContainerNames(context.NodeItemNamesAdapterProvider, target))
                {
                    WalkDeletedList(context, target.RelatedContainers[name]);
                }

                foreach (string name in GetRelatedObjectNames(context.NodeItemNamesAdapterProvider, target))
                {
                    ISfcSimpleNode node = target.RelatedObjects[name];
                    if (node != null)
                    {
                        EmitDeletedEntry(context, node);
                    }
                }
            }
            else
            {
                CompareNodes(context, source, target);
            }
        }

        protected void CompareNodes(IDiffContext context, ISfcSimpleNode source, ISfcSimpleNode target)
        {
            TraceHelper.Assert(source != null && target != null, "assert input nodes are not null");
            TraceHelper.Assert(source.Urn != null && target.Urn != null, "assert input nodes' Urns are not null");
            TraceHelper.Trace(Differencer.ComponentName, "CompareNodes: comparing two nodes {0} and {1}.", source.Urn, target.Urn);

            CompareProperties(context, source, target);

            // Compare each children list
            TraceHelper.Trace(Differencer.ComponentName, "CompareNodes: looping all related containers (collection) of the node.");
            foreach (string name in GetRelatedContainerNames(context.NodeItemNamesAdapterProvider, source))
            {
                bool naturalOrder = GetNaturalOrder(context.NodeItemNamesAdapterProvider, source, name);
                CompareRelatedContainer(context, source.RelatedContainers[name], target.RelatedContainers[name], naturalOrder);
            }
            TraceHelper.Trace(Differencer.ComponentName, "CompareNodes: looped all related containers (collection) of the node.");

            // Compare each children singleton
            TraceHelper.Trace(Differencer.ComponentName, "CompareNodes: looping all related object (singleton) of the node.");
            foreach (string name in GetRelatedObjectNames(context.NodeItemNamesAdapterProvider, source))
            {
                CompareRelatedObject(context, source.RelatedObjects[name], target.RelatedObjects[name]);
            }

            TraceHelper.Trace(Differencer.ComponentName, "CompareNodes: looped all related object (singleton) of the node.");

            TraceHelper.Trace(Differencer.ComponentName, "CompareNodes: compared two nodes.");
        }

        protected void CompareRelatedContainer(IDiffContext context, ISfcSimpleList source, ISfcSimpleList target, bool naturalOrder)
        {
            // The algorithm works like this:
            // Imagine two sorted arrays, left and right, representing the children of a single relationship, 
            // each of the two cursors initialized at the first positions. We compare the children from each 
            // cursor position. 
            // If they are identical, we will iterately compare its properties and the grand-children. 
            // When the properties and grand-children is done, we move both the cursors.
            // If one side has (+) compare value, we know it is a CREATED, and the cursor on this side is 
            // moved. Vice versa for the (-) case.
            // The implementation uses IEnumerator instead of arrays and cursors.

            TraceHelper.Assert(source != null && target != null, "assert input is not null");
            TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: comparing element in two container.");

            IEnumerator<ISfcSimpleNode> leftEnum = null;
            IEnumerator<ISfcSimpleNode> rightEnum = null;
            try {
                if (naturalOrder)
                {
                    TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: use natural order (no sorting).");
                    leftEnum = source.GetEnumerator();
                    rightEnum = target.GetEnumerator();
                }
                else
                {
                    // if either list is empty, we know we don't need to sort it 
                    // (potentially expensive operation). So, we check for it first.
                    bool hasSourceElement = false;
                    bool hasTargetElement = false;
                    try
                    {
                        using (IEnumerator<ISfcSimpleNode> list = source.GetEnumerator())
                        {
                            hasSourceElement = list.MoveNext();
                        }

                        using (IEnumerator<ISfcSimpleNode> list = target.GetEnumerator())
                        {
                            hasTargetElement = list.MoveNext();
                        }


                        if (hasSourceElement && hasTargetElement)
                        {
                            TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: use sorting.");
                            IEnumerable<ISfcSimpleNode> leftList = null;
                            IEnumerable<ISfcSimpleNode> rightList = null;
                            GetSortedLists(context.ContainerSortingProvider, source, target, out leftList,
                                out rightList);
                            leftEnum = leftList.GetEnumerator();
                            rightEnum = rightList.GetEnumerator();
                        }
                        else
                        {
                            leftEnum = source.GetEnumerator();
                            rightEnum = target.GetEnumerator();
                        }
                    }
                    catch (InvalidVersionEnumeratorException)
                    {
                        leftEnum = Enumerable.Empty<ISfcSimpleNode>().GetEnumerator();
                        rightEnum = Enumerable.Empty<ISfcSimpleNode>().GetEnumerator();
                    }
                }

                ISfcSimpleNode left = null;
                ISfcSimpleNode right = null;
                IComparer<ISfcSimpleNode> comparer = null;

                while (true)
                {
                    // This algorithm uses multiple break points from a single method to avoid excessive 
                    // nesting, to make it shorter and to increase readablity.

                    // move cursor
                    TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: move cursor.");
                    if (left == null && leftEnum.MoveNext())
                    {
                        left = leftEnum.Current;
                    }
                    if (right == null && rightEnum.MoveNext())
                    {
                        right = rightEnum.Current;
                    }
                    if (left == null && right == null)
                    {
                        // if both cursor reaches the end, the walk is completed.
                        break; // = exit =
                    }

                    // figure out if it is "created", "deleted", or ("updated or "unchanged")
                    int comp = 0;
                    if (left == null)
                    {
                        // null value indicates we've reach the end on the array
                        comp = 1;
                    }
                    else if (right == null)
                    {
                        comp = -1;
                    }
                    else
                    {
                        if (comparer == null)
                        {
                            // Obtain comparer as late as possible to avoid obtaining comparer if source or target is empty.
                            comparer = GetComparer(context.ContainerSortingProvider, source, target);
                        }
                        comp = CompareIdentities(left, right, comparer);
                    }

                    // process each of the 4 cases
                    if (comp < 0)  // case: created
                    {
                        TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: could not find matched element on the other side (created case).");
                        EmitCreatedEntry(context, left);
                        left = null; // clear the pointer so that we will move the cursor at the next round
                        continue; // = exit =
                    }
                    else if (comp > 0)   // case: deleted
                    {
                        TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: could not find matched element on the other side (deleted case).");
                        EmitDeletedEntry(context, right);
                        right = null; // clear the pointer so that we will move the cursor at the next round
                        continue; // = exit =
                    }

                    TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: found matched elements. push for later comparison (breadth first)");
                    // case: updated (or unchanged) --> Push to the stack
                    context.Push(left, right);

                    left = null; // clear both pointers so that we will move the cursors at the next round
                    right = null;
                }
            }
            finally 
            {
                Dispose(leftEnum);
                Dispose(rightEnum);
            }
            TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: compared element in two containers.");
        }

        protected void CompareRelatedObject(IDiffContext context, ISfcSimpleNode source, ISfcSimpleNode target)
        {
            if (source == null && target == null)
            {
                // no object to compare
                return;
            } 
            else if (target == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedObject: related object is null (create case).");
                EmitCreatedEntry(context, source);
            }
            else if (source == null)
            {
                TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedObject: related object is null (delete case).");
                EmitDeletedEntry(context, target);
            }
            else
            {
                // need to look deeper to see if they are Equivalent or Updated
                TraceHelper.Trace(Differencer.ComponentName, "CompareRelatedContainer: found matched elements. push for later comparison (breadth first)");
                context.Push(source, target);
            }
        }

        protected void CompareProperties(IDiffContext context, ISfcSimpleNode source, ISfcSimpleNode target)
        {
            // Assumption: to this diff service, Property lists are always a static list. It is defined 
            // by the type of the graphs, and they're always fixed. New Property cannot be added or removed.
            // It is specified in the API that only structurally identical graphs can be compared.
            TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: comparing properties of two nodes.");

            IDictionary<string, IPair<Object>> pairedProps = null;
            foreach (string name in GetPropertyNames(context.NodeItemNamesAdapterProvider, source))
            {
                if (!GetIsValueAvailable(context.SourceAvailablePropertyValueProvider, source, name))
                {
                    // skip property value that isn't avaliable (or set)
                    continue;
                }
                if (!GetIsValueAvailable(context.TargetAvailablePropertyValueProvider, target, name))
                {
                    // skip property value that isn't avaliable (or set)
                    continue;
                }

                if (!context.PropertyComparerProvider.Compare(source, target, name))
                {
                    if (pairedProps == null)
                    {
                        pairedProps = new Dictionary<string, IPair<Object>>();
                    }
                    Object leftValue = source.Properties[name];
                    Object rightValue = target.Properties[name];
                    pairedProps.Add(name, new Pair<Object>(leftValue, rightValue));
                }
            }

            if (pairedProps != null)
            {
                EmitUpdatedEntry(context, source, target, pairedProps);
            }
            else
            {
                EmitEquivalentEntry(context, source, target);
            }

            TraceHelper.Trace(Differencer.ComponentName, "CompareGraphs: compared properties of two nodes.");
        }

        protected int CompareIdentities(ISfcSimpleNode left, ISfcSimpleNode right, IComparer<ISfcSimpleNode> comparer)
        {
            if (left == null && right == null)
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }

            if (left.Urn == null)
            {
                throw new ArgumentNullException("left.Urn");
            }
            if (right.Urn == null)
            {
                throw new ArgumentNullException("right.Urn");
            }
            return comparer.Compare(left, right);
        }

        private void EmitEquivalentEntry(IDiffContext context, ISfcSimpleNode left, ISfcSimpleNode right)
        {
            if (context.IsTypeEmitted(DiffType.Equivalent))
            {
                DiffEntry diff = new DiffEntry();
                diff.Source = left.Urn;
                diff.Target = right.Urn;
                diff.ChangeType = DiffType.Equivalent;
                context.Add(diff);
            }
        }

        private void EmitUpdatedEntry(IDiffContext context, ISfcSimpleNode left, ISfcSimpleNode right, IDictionary<String, IPair<Object>> props)
        {
            if (context.IsTypeEmitted(DiffType.Updated))
            {
                DiffEntry diff = new DiffEntry();
                diff.Source = left.Urn;
                diff.Target = right.Urn;
                diff.Properties = props;
                diff.ChangeType = DiffType.Updated;
                context.Add(diff);
            }
        }

        private void EmitCreatedEntry(IDiffContext context, ISfcSimpleNode left)
        {
            if (context.IsTypeEmitted(DiffType.Created))
            {
                DiffEntry diff = new DiffEntry();
                diff.Source = left.Urn;
                diff.Target = null;
                diff.ChangeType = DiffType.Created;
                context.Add(diff);
                context.Push(left, DUMMY_NODE);
            }
        }

        private void EmitDeletedEntry(IDiffContext context, ISfcSimpleNode right)
        {
            if (context.IsTypeEmitted(DiffType.Deleted))
            {
                DiffEntry diff = new DiffEntry();
                diff.Target = right.Urn;
                diff.Source = null;
                diff.ChangeType = DiffType.Deleted;
                context.Add(diff);
                context.Push(DUMMY_NODE, right);
            }
        }

        protected void WalkCreatedList(IDiffContext context, ISfcSimpleList list)
        {
            foreach (ISfcSimpleNode left in list)
            {
                EmitCreatedEntry(context, left);
            }
        }

        protected void WalkDeletedList(IDiffContext context, ISfcSimpleList list)
        {
            foreach (ISfcSimpleNode right in list)
            {
                EmitDeletedEntry(context, right);
            }
        }

        /// <summary>
        /// Do a best-effort to dispose the specified object. It will peacefully handle non-system-
        /// generated exception.
        /// </summary>
        private static void Dispose(IDisposable disposable)
        {
            if (disposable == null)
            {
                return;
            }

            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    // it is more important to report System exception than being peaceful.
                    throw e;
                }

                // otherwise simple log and return
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "Exception occurs in cleanup: {0}.", e);

            }
        }

        #region Provider
        protected ISfcSimpleNode AdaptNode(SfcNodeAdapterProvider provider, Object node)
        {
            TraceHelper.Trace(Differencer.ComponentName, "AdaptNode: obtaining adapter for node {0}.", node);
            try
            {
                ISfcSimpleNode result = provider.GetGraphAdapter(node);
                TraceHelper.Trace(Differencer.ComponentName, "AdaptNode: obtained adapter for node {0}.", node);
                return result;
            }
            catch (ArgumentException ae)
            {
                TraceHelper.LogExCatch(ae);
                TraceHelper.Trace(Differencer.ComponentName, "AdaptNode: exception occurred {0}.", ae);
                throw new ArgumentException("node", ae);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "AdaptNode: exception occurred {0}.", e);
                String msg = StringDifferencing.FailedProviderLookup(provider.ToString(), node.ToString());
                throw new InvalidOperationException(msg, e);
            }
        }

        protected IEnumerable<string> GetRelatedContainerNames(NodeItemNamesAdapterProvider provider, ISfcSimpleNode node)
        {
            // It calls to provider code that is outside of our control. Should handle exception in 
            // every calls to provider.
            TraceHelper.Trace(Differencer.ComponentName, "GetRelatedContainerNames: obtaining container (meta) names provider {0}.", node);
            try {
                IEnumerable<string> result = provider.GetRelatedContainerNames(node);
                TraceHelper.Trace(Differencer.ComponentName, "GetRelatedContainerNames: obtained container (meta) names provider {0}.", node);
                return result;
            }
            catch (ArgumentException ae)
            {
                TraceHelper.LogExCatch(ae);
                TraceHelper.Trace(Differencer.ComponentName, "GetRelatedContainerNames: exception occurred {0}.", ae);
                throw new ArgumentException("node", ae);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "GetRelatedContainerNames: exception occurred {0}.", e);
                String msg = StringDifferencing.FailedProviderOperation(provider.ToString(), node.ToString());
                throw new InvalidOperationException(msg, e);
            }
        }

        protected bool GetNaturalOrder(NodeItemNamesAdapterProvider provider, ISfcSimpleNode node, string name)
        {
            // It calls to provider code that is outside of our control. Should handle exception in 
            // every calls to provider.
            TraceHelper.Trace(Differencer.ComponentName, "GetNaturalOrder: determining if it is natural order {0}.", node);
            try
            {
                bool result = provider.IsContainerInNatrualOrder(node, name);
                TraceHelper.Trace(Differencer.ComponentName, "GetNaturalOrder: determined {0}.", result);
                return result;
            }
            catch (ArgumentException ae)
            {
                TraceHelper.LogExCatch(ae);
                TraceHelper.Trace(Differencer.ComponentName, "GetNaturalOrder: exception occurred {0}.", ae);
                throw new ArgumentException("node", ae);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "GetNaturalOrder: exception occurred {0}.", e);
                String msg = StringDifferencing.FailedProviderOperation(provider.ToString(), node.ToString());
                throw new InvalidOperationException(msg, e);
            }
        }

        protected bool GetIsValueAvailable(AvailablePropertyValueProvider provider, ISfcSimpleNode node, string name)
        {
            // It calls to provider code that is outside of our control. Should handle exception in 
            // every calls to provider.
            TraceHelper.Trace(Differencer.ComponentName, "GetIsValueAvailable: determining if it property is available {0}.", node);
            try
            {
                bool result = provider.IsValueAvailable(node, name);
                TraceHelper.Trace(Differencer.ComponentName, "GetIsValueAvailable: determined {0}.", result);
                return result;
            }
            catch (ArgumentException ae)
            {
                TraceHelper.LogExCatch(ae);
                TraceHelper.Trace(Differencer.ComponentName, "GetIsValueAvailable: exception occurred {0}.", ae);
                throw new ArgumentException("node", ae);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "GetIsValueAvailable: exception occurred {0}.", e);
                String msg = StringDifferencing.FailedProviderOperation(provider.ToString(), node.ToString());
                throw new InvalidOperationException(msg, e);
            }
        }

        protected void GetSortedLists(ContainerSortingProvider provider, ISfcSimpleList source, ISfcSimpleList target, 
            out IEnumerable<ISfcSimpleNode> sortedSource, out IEnumerable<ISfcSimpleNode> sortedTarget)
        {
            // It calls to provider code that is outside of our control. Should handle exception in 
            // every calls to provider.
            TraceHelper.Trace(Differencer.ComponentName, "GetSortedLists: obtaining sorted lists {0} and {1}.", source, target);
            try
            {
                IEnumerable<ISfcSimpleNode> sourceResult = null;
                IEnumerable<ISfcSimpleNode> targetResult = null;

                provider.SortLists(source, target, out sourceResult, out targetResult);

                // delay the change-of-state unless we know we don't get an exception
                sortedSource = sourceResult;
                sortedTarget = targetResult;
                TraceHelper.Trace(Differencer.ComponentName, "GetSortedLists: obtained sorted lists {0}.", source);
            }
            catch (ArgumentException ae)
            {
                TraceHelper.LogExCatch(ae);
                TraceHelper.Trace(Differencer.ComponentName, "GetSortedList: exception occurred {0}.", ae);
                throw new ArgumentException("list", ae);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "GetSortedList: exception occurred {0}.", e);
                String msg = StringDifferencing.FailedProviderOperation(provider.ToString(), source.ToString());
                throw new InvalidOperationException(msg, e);
            }
        }

        protected IComparer<ISfcSimpleNode> GetComparer(ContainerSortingProvider provider, ISfcSimpleList list, ISfcSimpleList list2)
        {
            // It calls to provider code that is outside of our control. Should handle exception in 
            // every calls to provider.
            TraceHelper.Trace(Differencer.ComponentName, "GetComparer: obtaining comparer {0}.", list);
            try
            {
                IComparer<ISfcSimpleNode> result = provider.GetComparer(list, list2);
                TraceHelper.Trace(Differencer.ComponentName, "GetComparer: obtained comparer {0}.", list);
                return result;
            }
            catch (ArgumentException ae)
            {
                TraceHelper.LogExCatch(ae);
                TraceHelper.Trace(Differencer.ComponentName, "GetComparer: exception occurred {0}.", ae);
                throw new ArgumentException("list", ae);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "GetComparer: exception occurred {0}.", e);
                String msg = StringDifferencing.FailedProviderOperation(provider.ToString(), list.ToString());
                throw new InvalidOperationException(msg, e);
            }
        }

        protected IEnumerable<string> GetRelatedObjectNames(NodeItemNamesAdapterProvider provider, ISfcSimpleNode node)
        {
            TraceHelper.Trace(Differencer.ComponentName, "GetRelatedObjectNames: obtaining related object name for node {0}.", node);
            try 
            {
                IEnumerable<string> result = provider.GetRelatedObjectNames(node);
                TraceHelper.Trace(Differencer.ComponentName, "GetRelatedObjectNames: obtained related object name for node.");
                return result;
            }
            catch (ArgumentException ae)
            {
                TraceHelper.LogExCatch(ae);
                TraceHelper.Trace(Differencer.ComponentName, "GetRelatedObjectNames: exception occurred {0}.", ae);
                throw new ArgumentException("node", ae);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "GetRelatedObjectNames: exception occurred {0}.", e);
                String msg = StringDifferencing.FailedProviderOperation(provider.ToString(), node.ToString());
                throw new InvalidOperationException(msg, e);
            }
        }

        protected IEnumerable<string> GetPropertyNames(NodeItemNamesAdapterProvider provider, ISfcSimpleNode node)
        {
            TraceHelper.Trace(Differencer.ComponentName, "GetPropertyNames: obtaining prop names for node {0}.", node);
            try
            {
                IEnumerable<string> result = provider.GetPropertyNames(node);
                TraceHelper.Trace(Differencer.ComponentName, "GetPropertyNames: obtained prop names for node.");
                return result;
            }
            catch (ArgumentException ae)
            {
                TraceHelper.LogExCatch(ae);
                TraceHelper.Trace(Differencer.ComponentName, "GetPropertyNames: exception occurred {0}.", ae);
                throw new ArgumentException("node", ae);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                TraceHelper.Trace(Differencer.ComponentName, "GetPropertyNames: exception occurred {0}.", e);
                String msg = StringDifferencing.FailedProviderOperation(provider.ToString(), node.ToString());
                throw new InvalidOperationException(msg, e);
            }
        }

        protected SfcNodeAdapterProvider FindNodeAdapterProvider(Object node)
        {
            TraceHelper.Trace(Differencer.ComponentName, "AdaptNode: finding adapter for node {0}.", node);
            foreach (SfcNodeAdapterProvider provider in registry.SfcNodeAdapterProviders)
            {
                try
                {
                    if (provider.IsGraphSupported(node))
                    {
                        TraceHelper.Trace(Differencer.ComponentName, "AdaptNode: found adapter for node.");
                        return provider;
                    }
                }
                catch (Exception e)
                {
                    if (IsSystemGeneratedException(e))
                    {
                        throw e;
                    }
                    TraceHelper.LogExCatch(e);
                    TraceHelper.Trace(Differencer.ComponentName, "AdaptNode: exception occurred {0}.", e);
                    String msg = StringDifferencing.FailedProviderLookup(provider.ToString(), node.ToString());
                    throw new InvalidOperationException(msg, e);
                }
            }
            TraceHelper.Trace(Differencer.ComponentName, "AdaptNode: not found");
            return null;
        }

        protected NodeItemNamesAdapterProvider FindNameProvider(ISfcSimpleNode node)
        {
            TraceHelper.Trace(Differencer.ComponentName, "FindNameProvider: finding name provider for node {0}.", node);
            foreach (NodeItemNamesAdapterProvider provider in registry.NodeItemNameAdapterProviders)
            {
                try
                {
                    if (provider.IsGraphSupported(node))
                    {
                        TraceHelper.Trace(Differencer.ComponentName, "FindNameProvider: found name provider for node.");
                        return provider;
                    }
                }
                catch (Exception e)
                {
                    if (IsSystemGeneratedException(e))
                    {
                        throw e;
                    }
                    TraceHelper.LogExCatch(e);
                    TraceHelper.Trace(Differencer.ComponentName, "FindNameProvider: exception occurred {0}.", e);
                    String msg = StringDifferencing.FailedProviderLookup(provider.ToString(), node.ToString());
                    throw new InvalidOperationException(msg, e);
                }
            }
            TraceHelper.Trace(Differencer.ComponentName, "FindNameProvider: not found");
            return null;
        }

        protected AvailablePropertyValueProvider FindAvailableValueProvider(ISfcSimpleNode node)
        {
            TraceHelper.Trace(Differencer.ComponentName, "FindAvailableValueProvider: finding provider for node {0}.", node);
            foreach (AvailablePropertyValueProvider provider in registry.AvailablePropertyValueProviders)
            {
                try
                {
                    if (provider.IsGraphSupported(node))
                    {
                        TraceHelper.Trace(Differencer.ComponentName, "FindAvailableValueProvider: found provider for node.");
                        return provider;
                    }
                }
                catch (Exception e)
                {
                    if (IsSystemGeneratedException(e))
                    {
                        throw e;
                    }
                    TraceHelper.LogExCatch(e);
                    TraceHelper.Trace(Differencer.ComponentName, "FindAvailableValueProvider: exception occurred {0}.", e);
                    String msg = StringDifferencing.FailedProviderLookup(provider.ToString(), node.ToString());
                    throw new InvalidOperationException(msg, e);
                }
            }
            TraceHelper.Trace(Differencer.ComponentName, "FindAvailableValueProvider: not found");
            return null;
        }

        protected ContainerSortingProvider FindContainerSortingProvider(ISfcSimpleNode source, ISfcSimpleNode target)
        {
            TraceHelper.Trace(Differencer.ComponentName, "FindContainerSortingProvider: finding provider for node {0} and {1}.", source, target);
            foreach (ContainerSortingProvider provider in registry.ContainerSortingProviders)
            {
                try
                {
                    if (provider.AreGraphsSupported(source, target))
                    {
                        TraceHelper.Trace(Differencer.ComponentName, "FindContainerSortingProvider: found provider for node {0} and {1}.", source, target);
                        return provider;
                    }
                }
                catch (Exception e)
                {
                    if (IsSystemGeneratedException(e))
                    {
                        throw e;
                    }
                    TraceHelper.LogExCatch(e);
                    TraceHelper.Trace(Differencer.ComponentName, "FindContainerSortingProvider: exception occurred {0}.", e);
                    String msg = StringDifferencing.FailedProviderLookup(provider.ToString(), source.ToString());
                    throw new InvalidOperationException(msg, e);
                }
            }
            TraceHelper.Trace(Differencer.ComponentName, "FindContainerSortingProvider: not found");
            return null;
        }

        protected PropertyComparerProvider FindPropertyComparerProvider(ISfcSimpleNode source, ISfcSimpleNode target)
        {
            TraceHelper.Trace(Differencer.ComponentName, "FindPropertyComparerProvider: finding provider for node {0} and {1}.", source, target);
            foreach (PropertyComparerProvider provider in registry.PropertyComparerProviders)
            {
                try
                {
                    if (provider.AreGraphsSupported(source, target))
                    {
                        TraceHelper.Trace(Differencer.ComponentName, "FindPropertyComparerProvider: found provider for node {0} and {1}.", source, target);
                        return provider;
                    }
                }
                catch (Exception e)
                {
                    if (IsSystemGeneratedException(e))
                    {
                        throw e;
                    }
                    TraceHelper.LogExCatch(e);
                    TraceHelper.Trace(Differencer.ComponentName, "FindPropertyComparerProvider: exception occurred {0}.", e);
                    String msg = StringDifferencing.FailedProviderLookup(provider.ToString(), source.ToString());
                    throw new InvalidOperationException(msg, e);
                }
            }
            TraceHelper.Trace(Differencer.ComponentName, "FindPropertyComparerProvider: not found");
            return null;
        }

        internal static bool IsSystemGeneratedException(Exception e)
        {
            // see, http://msdn.microsoft.com/en-us/library/ms229007.aspx
            if (e is OutOfMemoryException)
            {
                return true;
            }
            if (e is StackOverflowException)
            {
                return true;
            }
            if (e is System.Runtime.InteropServices.COMException || e is System.Runtime.InteropServices.SEHException)
            {
                return true;
            }
            //if (e is ExecutionEngineException) //This type previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.
            //{
            //    return true;
            //}
            return false;
        }
        #endregion

        #region LateActivation
        protected interface IDiffContext
        {
            void Push(ISfcSimpleNode source, ISfcSimpleNode target);
            void Add(IDiffEntry entry);
            NodeItemNamesAdapterProvider NodeItemNamesAdapterProvider
            {
                get;
            }
            AvailablePropertyValueProvider SourceAvailablePropertyValueProvider
            {
                get;
            }
            AvailablePropertyValueProvider TargetAvailablePropertyValueProvider
            {
                get;
            }
            ContainerSortingProvider ContainerSortingProvider
            {
                get;
            }
            PropertyComparerProvider PropertyComparerProvider
            {
                get;
            }
            bool IsTypeEmitted(DiffType type);
        }

        /// <summary>
        /// The late activated diffgram will compare all children and property of a pair of object
        /// at a time, until at least one DiffEntry is found (or end of list). 
        /// 
        /// When it is comparing the children, it will find pair and put them into the stack for
        /// later comparison.
        /// </summary>
        protected class LateActivatedDiffgram : Diffgram, IDiffgram
        {
            private Differencer differencer;

            private DiffType emitDiffTypes;

            private NodeItemNamesAdapterProvider nameProvider;

            private AvailablePropertyValueProvider sourceValueProvider;

            private AvailablePropertyValueProvider targetValueProvider;
            
            private ContainerSortingProvider sortProvider;

            private PropertyComparerProvider propComparer;

            private ISfcSimpleNode source;

            private ISfcSimpleNode target;

            public LateActivatedDiffgram(Differencer differencer, 
                NodeItemNamesAdapterProvider nameProvider,
                AvailablePropertyValueProvider sourceValueProvider, 
                AvailablePropertyValueProvider targetValueProvider, 
                ContainerSortingProvider sortProvider,
                PropertyComparerProvider propComparer,
                DiffType emitDiffTypes,
                ISfcSimpleNode source, ISfcSimpleNode target)
                : base(source.ObjectReference, target.ObjectReference)
            {
                this.differencer = differencer;
                this.nameProvider = nameProvider;
                this.sourceValueProvider = sourceValueProvider;
                this.targetValueProvider = targetValueProvider;
                this.sortProvider = sortProvider;
                this.propComparer = propComparer;
                this.emitDiffTypes = emitDiffTypes;
                this.source = source;
                this.target = target;

                TraceHelper.Trace(Differencer.ComponentName, "Diffgram: created late-activated diffgram.");
            }

            public Differencer Differencer
            {
                get
                {
                    return differencer;
                }
            }

            public NodeItemNamesAdapterProvider NodeItemNamesAdapterProvider
            {
                get
                {
                    return nameProvider;
                }
            }

            public AvailablePropertyValueProvider SourceAvailablePropertyValueProvider
            {
                get
                {
                    return sourceValueProvider;
                }
            }

            public AvailablePropertyValueProvider TargetAvailablePropertyValueProvider
            {
                get
                {
                    return targetValueProvider;
                }
            }

            public ContainerSortingProvider ContainerSortingProvider
            {
                get
                {
                    return sortProvider;
                }
            }

            public PropertyComparerProvider PropertyComparerProvider
            {
                get
                {
                    return propComparer;
                }
            }

            public DiffType EmitDiffTypes
            {
                get
                {
                    return emitDiffTypes;
                }
            }

            /// <summary>
            /// Top most node adapted
            /// </summary>
            public ISfcSimpleNode SourceSimpleNode
            {
                get
                {
                    return source;
                }
            }
            /// <summary>
            /// Top most node adapted
            /// </summary>
            public ISfcSimpleNode TargetSimpleNode
            {
                get
                {
                    return target;
                }
            }

            public override IEnumerator<IDiffEntry> GetEnumerator()
            {
                TraceHelper.Trace(Differencer.ComponentName, "Diffgram: entering GetEnumerator");
                LateActivatedDiffEntryEnumerator e = new LateActivatedDiffEntryEnumerator(this);
                e.Push(SourceSimpleNode, TargetSimpleNode);
                TraceHelper.Trace(Differencer.ComponentName, "Diffgram: exiting GetEnumerator");
                return e;
            }
        }

        /// <summary>
        /// This object is a late-activated Enumerator. There is two data structures: stack (stack) 
        /// holding pair of objects to be compared, and result (queue) holding result to be returned. 
        /// 
        /// When it is initialized, a single pair is added to the stack. Every time MoveNext() is
        /// called, acutal compare occurs. (see summary on the method)
        /// </summary>
        class LateActivatedDiffEntryEnumerator : IEnumerator<IDiffEntry>, IDiffContext
        {
            /// <summary>
            /// The IEnumerable in which this IEnumerator is obtained
            /// </summary>
            private LateActivatedDiffgram envelope;

            /// <summary>
            /// Store the items to be processed.
            /// </summary>
            private Stack<Pair<ISfcSimpleNode>> stack = new Stack<Pair<ISfcSimpleNode>>();

            /// <summary>
            /// The list of result
            /// </summary>
            private Queue<IDiffEntry> result = new Queue<IDiffEntry>();

            /// <summary>
            /// The Current item in the result
            /// </summary>
            private IDiffEntry current;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="envelop"></param>
            public LateActivatedDiffEntryEnumerator(LateActivatedDiffgram envelop)
            {
                this.envelope = envelop;
            }

            public NodeItemNamesAdapterProvider NodeItemNamesAdapterProvider
            {
                get
                {
                    return envelope.NodeItemNamesAdapterProvider;
                }
            }

            public AvailablePropertyValueProvider SourceAvailablePropertyValueProvider
            {
                get
                {
                    return envelope.SourceAvailablePropertyValueProvider;
                }
            }

            public AvailablePropertyValueProvider TargetAvailablePropertyValueProvider
            {
                get
                {
                    return envelope.TargetAvailablePropertyValueProvider;
                }
            }
            
            public ContainerSortingProvider ContainerSortingProvider
            {
                get
                {
                    return envelope.ContainerSortingProvider;
                }
            }

            public PropertyComparerProvider PropertyComparerProvider
            {
                get
                {
                    return envelope.PropertyComparerProvider;
                }
            }

            public bool IsTypeEmitted(DiffType type)
            {
                return (envelope.EmitDiffTypes & type) != 0;
            }

            /// <summary>
            /// Add an item to be processed.
            /// </summary>
            /// <param name="source"></param>
            /// <param name="target"></param>
            public void Push(ISfcSimpleNode source, ISfcSimpleNode target)
            {
                TraceHelper.Assert(source != null && target != null, "assert added node is not null");
                TraceHelper.Assert(source.Urn != null && target.Urn != null, "assert added node's urn is not null");
                TraceHelper.Trace(Differencer.ComponentName, "DiffEntryEnumerator: pushing a pair {0} and {1} for later comparison.", source.Urn, target.Urn);

                Pair<ISfcSimpleNode> newPair = new Pair<ISfcSimpleNode>(source, target);
                stack.Push(newPair);
                TraceHelper.Trace(Differencer.ComponentName, "DiffEntryEnumerator: pushed.");
            }
            public void Add(IDiffEntry entry)
            {
                TraceHelper.Assert(entry != null, "assert enqueueing entry is not null");
                TraceHelper.Trace(Differencer.ComponentName, "DiffEntryEnumerator: enqueueing result {0}.", entry);
                result.Enqueue(entry);
                TraceHelper.Trace(Differencer.ComponentName, "DiffEntryEnumerator: enqueued result.");
            }
            public IDiffEntry Current
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException();
                    }
                    return current;
                }
            }
            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            /// <summary>
            /// Determines if we have next element.
            /// 
            /// Every time MoveNext() is called, acutal compare occurs. It is a loop that
            /// pops a pair from the stack and compare. Each of such compare may result in 
            /// zero to more pair of children being added for comparison. It may also result 
            /// in zero to more pair of result. 
            /// 
            /// The loop ends either if we have an item in the result queue to be returned,
            /// or if there is no more pair in the stack to compare.
            /// </summary>
            public bool MoveNext()
            {
                TraceHelper.Trace(Differencer.ComponentName, "DiffEntryEnumerator: entering MoveNext.");
                current = null;
                while (true)
                {
                    if (result.Count > 0)
                    {
                        current = result.Dequeue();
                        TraceHelper.Trace(Differencer.ComponentName, "DiffEntryEnumerator: exiting MoveNext (returning true).");
                        return true;
                    }
                    if (stack.Count <= 0)
                    {
                        // if stack is empty, we reach the end
                        TraceHelper.Trace(Differencer.ComponentName, "DiffEntryEnumerator: exiting MoveNext (no more to compare. returning false.).");
                        return false;
                    }
                    Pair<ISfcSimpleNode> pair = stack.Pop();
                    envelope.Differencer.ProcessNodes(this, pair.Source, pair.Target);
                }
            }

            public void Reset()
            {
                current = null;
                stack.Clear();
                result.Clear();
            }

            public void Dispose()
            {
                // do nothing. none of resources need to dispose.
            }
        }
        #endregion

        /// <summary>
        /// A dummy placeholder class to represent leaf node is reached on one side.
        /// </summary>
        class DummySfcSimpleNode : ISfcSimpleNode
        {
            public object ObjectReference
            {
                get { throw new NotImplementedException(); }
            }

            public ISfcSimpleMap<string, object> Properties
            {
                get { throw new NotImplementedException(); }
            }

            public ISfcSimpleMap<string, ISfcSimpleList> RelatedContainers
            {
                get { throw new NotImplementedException(); }
            }

            public ISfcSimpleMap<string, ISfcSimpleNode> RelatedObjects
            {
                get { throw new NotImplementedException(); }
            }

            public Urn Urn
            {
                get { return "<empty>"; }
            }
        }
    }

    internal class DefaultAvailablePropertyValueProvider : AvailablePropertyValueProvider
    {
        public override bool IsGraphSupported(ISfcSimpleNode source)
        {
            return true;
        }

        public override bool IsValueAvailable(ISfcSimpleNode source, string propName)
        {
            return true;
        }
    }

    internal class DefaultContainerSortingProvider : ContainerSortingProvider
    {

        private readonly static IComparer<ISfcSimpleNode> URN_COMPARER = new UrnComparer();

        public override bool AreGraphsSupported(ISfcSimpleNode source, ISfcSimpleNode target)
        {
            return true;
        }

        public override IComparer<ISfcSimpleNode> GetComparer(ISfcSimpleList list, ISfcSimpleList list2)
        {
            return URN_COMPARER;
        }
    }

    internal class DefaultPropertyComparerProvider : PropertyComparerProvider
    {

        public override bool AreGraphsSupported(ISfcSimpleNode left, ISfcSimpleNode right)
        {
            return true;
        }

        public override bool Compare(ISfcSimpleNode left, ISfcSimpleNode right, String propName)
        {
            Object leftValue = left.Properties[propName];
            Object rightValue = right.Properties[propName];
            bool result = CompareUtil.CompareObjects(leftValue, rightValue);
            return result;
        }
    }

    internal class UrnComparer : IComparer<ISfcSimpleNode>
    {
        int IComparer<ISfcSimpleNode>.Compare(ISfcSimpleNode x, ISfcSimpleNode y)
        {
            int result = CompareUtil.CompareUrnLeaves(x.Urn, y.Urn);
            return result;
        }
    }

    /// <summary>
    /// A general compare utility. This class contains static compare methods.
    /// </summary>
    internal class CompareUtil
    {
        public static int CompareUrns(Urn left, Urn right)
        {
            if (left == null && right == null)
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }

            int parentComp = CompareUrns(left.Parent, right.Parent);
            if (parentComp != 0)
            {
                return parentComp;
            }

            int result = CompareStrings(left.Value, right.Value);
            return result;
        }

        public static int CompareUrnLeaves(Urn left, Urn right)
        {
            if (left == null && right == null)
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }

            int result = CompareStrings(left.Value, right.Value);
            return result;
        }

        public static bool CompareObjects(Object left, Object right)
        {
            if (left == null && right == null)
            {
                return true;
            }
            if (left == null)
            {
                return false;
            }
            if (right == null)
            {
                return false;
            }

            bool result = left.Equals(right);
            return result;
        }

        public static int CompareStrings(string left, string right)
        {
            if (left == null && right == null)
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }
            int result = System.StringComparer.Ordinal.Compare(left, right);
            return result;
        }
    }
}
