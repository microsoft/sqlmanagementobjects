// Copyright (c) Microsoft.
// Licensed under the MIT license.

// PURPOSE: This is the collection for MapInfo object. It is the requirement of
// SFC framework that each object should have its own collection object.
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for MapInfo.
    /// </summary>
    public sealed class MapInfoCollection : SfcCollatedDictionaryCollection<MapInfo, MapInfo.Key, Package>, IXEObjectInfoCollection<MapInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal MapInfoCollection(Package parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.MapInfo"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public MapInfo this[string name]
        {
            get { return this[new MapInfo.Key(name)]; }
        }

        /// <summary>
        /// Determines whether the collection contains the Map.
        /// </summary>
        /// <param name="name">The name of the Map.</param>
        /// <returns>
        /// 	<c>true</c> if the collection contains Map; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new MapInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return MapInfo.GetObjectFactory();
        }

    }

}
