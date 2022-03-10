// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// This is the collection for TargetInfo objects.
    /// </summary>
    public sealed class TargetInfoCollection : SfcCollatedDictionaryCollection<TargetInfo, TargetInfo.Key, Package>, IXEObjectInfoCollection<TargetInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal TargetInfoCollection(Package parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.TargetInfo"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public TargetInfo this[string name]
        {
            get { return this[new TargetInfo.Key(name)]; }
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
                return Contains(new TargetInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return TargetInfo.GetObjectFactory();
        }

    }

}
