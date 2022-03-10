// Copyright (c) Microsoft.
// Licensed under the MIT license.

// PURPOSE: This is the collection for PredCompareInfo object. It is the requirement of
// SFC framework that each object should have its own collection object.
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for PredCompareInfo.
    /// </summary>
    public sealed class PredCompareInfoCollection : SfcCollatedDictionaryCollection<PredCompareInfo, PredCompareInfo.Key, Package>, IXEObjectInfoCollection<PredCompareInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PredCompareInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal PredCompareInfoCollection(Package parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.PredCompareInfo"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public PredCompareInfo this[string name]
        {
            get { return this[new PredCompareInfo.Key(name)]; }
        }

        /// <summary>
        /// Determines whether the collection contains the Pred_Source.
        /// </summary>
        /// <param name="name">The name of the Pred_Source.</param>
        /// <returns>
        /// 	<c>true</c> if the collection contains Pred_Source; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new PredCompareInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return PredCompareInfo.GetObjectFactory();
        }

    }

}
