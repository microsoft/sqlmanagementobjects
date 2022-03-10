// Copyright (c) Microsoft.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// This is collection class for DataEventColumnInfo.
    /// </summary>
    public sealed class DataEventColumnInfoCollection : SfcCollatedDictionaryCollection<DataEventColumnInfo, DataEventColumnInfo.Key, EventInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataEventColumnInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal DataEventColumnInfoCollection(EventInfo parent)
            : base(parent, parent.Parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.DataEventColumnInfo"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public DataEventColumnInfo this[string name]
        {
            get { return this[new DataEventColumnInfo.Key(name)]; }
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
                return Contains(new DataEventColumnInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return DataEventColumnInfo.GetObjectFactory();
        }

    }

}
