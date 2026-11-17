// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// This is collection class for TypeInfo.
    /// </summary>
    public sealed class TypeInfoCollection : SfcCollatedDictionaryCollection<TypeInfo, TypeInfo.Key, Package>, IXEObjectInfoCollection<TypeInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeInfoCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal TypeInfoCollection(Package parent)
            : base(parent, parent.Parent.GetComparer())
        {
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.TypeInfo"/> with the specified name.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        public TypeInfo this[string name]
        {
            get { return this[new TypeInfo.Key(name)]; }
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
                return Contains(new TypeInfo.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return TypeInfo.GetObjectFactory();
        }

    }

}
