// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.XEvent
{

    /// <summary>
    ///   interface for the child objects of Package
    /// </summary>
    public interface IXEObjectInfo
    {
        /// <summary>
        /// The name of the EventInfo
        /// </summary>
        string Name { get; }

        
        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>        
        string Description { get; }       
    }

}
