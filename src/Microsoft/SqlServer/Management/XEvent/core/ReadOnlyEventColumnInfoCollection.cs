// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// This is collection class for ReadOnlyEventColumnInfo.
    /// </summary>
    public sealed class ReadOnlyEventColumnInfoCollection : SfcCollatedDictionaryCollection<ReadOnlyEventColumnInfo, ReadOnlyEventColumnInfo.Key, EventInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyEventColumnInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal ReadOnlyEventColumnInfoCollection(EventInfo parent)
            : base(parent, parent.Parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.ReadOnlyEventColumnInfo"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public ReadOnlyEventColumnInfo this[string name]
        {
            get { return this[new ReadOnlyEventColumnInfo.Key(name)]; }
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
                return Contains(new ReadOnlyEventColumnInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return ReadOnlyEventColumnInfo.GetObjectFactory();
        }

    }

}
