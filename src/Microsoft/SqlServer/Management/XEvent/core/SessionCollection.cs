// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// This is the collection for Sessions.
    /// </summary>
    public sealed class SessionCollection : SfcCollatedDictionaryCollection<Session, Session.Key, BaseXEStore>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal SessionCollection(BaseXEStore parent)
            : base(parent, parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.Session"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public Session this[string name]
        {
            get { return this[new Session.Key(name)]; }
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
                return Contains(new Session.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return Session.GetObjectFactory();
        }

    }

}
