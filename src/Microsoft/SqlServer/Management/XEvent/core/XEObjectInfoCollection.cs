// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.XEvent
{
    internal interface IXEObjectInfoCollection<T> where T : IXEObjectInfo
    {
        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> with the specified name.
        /// </summary>
        /// <value>name of the Object</value>
        /// <returns>Object with the specify name</returns>
        T this[string name] { get; }
        
    }
}
