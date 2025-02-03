// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// PURPOSE: This is the collection for Target object. It is the requirement of
// SFC framework that each object should have its own collection object.
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for Target.
    /// </summary>
    public sealed class TargetCollection : SfcCollatedDictionaryCollection<Target, Target.Key, Session>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal TargetCollection(Session parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.Target"/> with the specified name.
        /// </summary>
        /// <value>name of the Target</value>
        /// <returns>Target with the specify name</returns>
        public Target this[string name]
        {
            get { return this[new Target.Key(name)]; }
        }

        /// <summary>
        /// Determines whether the collection contains the Target.
        /// </summary>
        /// <param name="name">The name of the Target.</param>
        /// <returns>
        ///     <c>true</c> if the collection contains Target; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new Target.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return Target.GetObjectFactory();
        }

        /// <summary>
        /// Appends Alter scripts to given script.
        /// </summary>
        /// <param name="addScript">Add script.</param>
        /// <param name="dropScript">Drop script.</param>
        public void AppendAlterScripts(StringBuilder addScript, StringBuilder dropScript)
        {
            XEUtils.AppendAlterScripts<Target>(addScript, dropScript, this, this.Parent);
        }

    }

}
