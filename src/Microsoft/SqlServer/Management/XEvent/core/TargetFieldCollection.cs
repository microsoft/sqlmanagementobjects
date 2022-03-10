// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Diagnostics.STrace;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for TargetField.
    /// </summary>
    public sealed class TargetFieldCollection : SfcCollatedDictionaryCollection<TargetField, TargetField.Key, Target>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFieldCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal TargetFieldCollection(Target parent)
            : base(parent, parent.Parent.Parent.GetComparer())
        {
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.TargetField"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public TargetField this[string name]
        {
            get { return this[new TargetField.Key(name)]; }
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
                return Contains(new TargetField.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return TargetField.GetObjectFactory();
        }

    }

}
