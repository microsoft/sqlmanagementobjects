// Copyright (c) Microsoft.
// Licensed under the MIT license.

// PURPOSE: This is the collection for EventInfo object. It is the requirement of
// SFC framework that each object should have its own collection object.
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for EventInfo.
    /// </summary>
    public sealed class EventInfoCollection : SfcCollatedDictionaryCollection<EventInfo, EventInfo.Key, Package>, IXEObjectInfoCollection<EventInfo>
    {
        /// <summary>
        /// Initialize a new instance of EventInfoCollection given the Parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal EventInfoCollection(Package parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.EventInfo"/> with the specified name.
        /// </summary>
        /// <value>name of the event</value>
        /// <returns>EventInfo object with the specify name.</returns>
        public EventInfo this[string name]
        {
            get { return this[new EventInfo.Key(name)]; }
        }

        /// <summary>
        /// Determines whether the collection contains Event.
        /// </summary>
        /// <param name="name">The name of the Event.</param>
        /// <returns>
        /// 	<c>true</c> if the collection contains Event; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new EventInfo.Key(name));
            }
        }

        /// <summary>
        /// Return the instance of object factory for EventInfo.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return EventInfo.GetObjectFactory();
        }

    }

}
