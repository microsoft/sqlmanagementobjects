// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// This is collection class for PredSourceInfo.
    /// </summary>
    public sealed class PredSourceInfoCollection : SfcCollatedDictionaryCollection<PredSourceInfo, PredSourceInfo.Key, Package>, IXEObjectInfoCollection<PredSourceInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PredSourceInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal PredSourceInfoCollection(Package parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.PredSourceInfo"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public PredSourceInfo this[string name]
        {
            get { return this[new PredSourceInfo.Key(name)]; }
        }

        /// <summary>
        /// Determines whether [contains] [the specified name].
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        /// 	<c>true</c> if [contains] [the specified name]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new PredSourceInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return PredSourceInfo.GetObjectFactory();
        }

    }

}
