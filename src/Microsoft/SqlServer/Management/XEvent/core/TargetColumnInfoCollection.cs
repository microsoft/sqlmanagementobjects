// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for TargetColumnInfo.
    /// </summary>
    public sealed class TargetColumnInfoCollection : SfcCollatedDictionaryCollection<TargetColumnInfo, TargetColumnInfo.Key, TargetInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetColumnInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal TargetColumnInfoCollection(TargetInfo parent)
            : base(parent, parent.Parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.TargetColumnInfo"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public TargetColumnInfo this[string name]
        {
            get { return this[new TargetColumnInfo.Key(name)]; }
        }

        /// <summary>
        /// Determines whether the collection contains the TargetColumn.
        /// </summary>
        /// <param name="name">The name of the TargetColumn.</param>
        /// <returns>
        /// 	<c>true</c> if the collection contains TargetColumn; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new TargetColumnInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return TargetColumnInfo.GetObjectFactory();
        }

    }

}
