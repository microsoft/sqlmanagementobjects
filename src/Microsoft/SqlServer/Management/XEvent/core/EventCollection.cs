// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for Event.
    /// </summary>
    public sealed class EventCollection : SfcCollatedDictionaryCollection<Event, Event.Key, Session>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal EventCollection(Session parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.Event"/> with the specified name.
        /// </summary>
        /// <value>name of the Event</value>
        /// <returns>Event with the specify name</returns>
        public Event this[string name]
        {
            get { return this[new Event.Key(name)]; }
        }

        /// <summary>
        /// Determines whether the collection contains the Event.
        /// </summary>
        /// <param name="name">The name of the Event.</param>
        /// <returns>
        ///     <c>true</c> if the collection contains Event; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new Event.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return Event.GetObjectFactory();
        }




        /// <summary>
        /// Check the current state of the events and return the script based on it.
        /// This should be used only in generating the alter script for session.
        /// </summary>
        /// <returns></returns>      
        public void AppendAlterScripts(StringBuilder addScript, StringBuilder dropScript)
        {
            XEUtils.AppendAlterScripts<Event>(addScript, dropScript, this, this.Parent);
        }

    }
}
