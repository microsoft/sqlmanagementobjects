// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// PURPOSE: This is the collection for ActionInfo object. It is the requirement of
// SFC framework that each object should have its own collection object.
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for ActionInfo.
    /// </summary>
    public sealed class ActionInfoCollection : SfcCollatedDictionaryCollection<ActionInfo, ActionInfo.Key, Package>, IXEObjectInfoCollection<ActionInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal ActionInfoCollection(Package parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.ActionInfo"/> with the specified name.
        /// </summary>
        /// <value>name of the action</value>
        /// <returns>ActionInfo with the specify name</returns>
        public ActionInfo this[string name]
        {
            get { return this[new ActionInfo.Key(name)]; }
        }

        /// <summary>
        /// Determines whether the collection contains the Action.
        /// </summary>
        /// <param name="name">The name of the Action.</param>
        /// <returns>
        ///     <c>true</c> if the collection contains Action; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new ActionInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return ActionInfo.GetObjectFactory();
        }

    }

}
