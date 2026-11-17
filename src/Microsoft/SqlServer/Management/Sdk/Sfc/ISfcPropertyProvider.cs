// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// This interface needs to be provided by top level classes (or by a base class of a top level class).
    /// It provides an interface into the object's properties.
    /// </summary>
    public interface ISfcPropertyProvider : ISfcNotifyPropertyMetadataChanged, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the interface reference to the set of properties of this object
        /// </summary>
        /// <returns></returns>
        ISfcPropertySet GetPropertySet();
    }

}
