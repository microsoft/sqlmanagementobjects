// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// The interface's only purpose is to “mark” types that we want to work in Design Mode. 
    /// A type (class) needs to inherit from this interface if it wants to work in Design Mode. 
    /// That mechanism also allows a client of the model to programmatically discover which classes 
    /// support Design Mode, which can be useful in certain scenarios. 
    /// </summary>
    public interface ISfcSupportsDesignMode
    {
        bool IsDesignMode { get; }
    }
}

