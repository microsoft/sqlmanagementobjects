// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Notifies clients that a property metadata has changed.
    /// </summary>
    public interface ISfcNotifyPropertyMetadataChanged
    {
        event EventHandler<SfcPropertyMetadataChangedEventArgs> PropertyMetadataChanged;
    }

    public class SfcPropertyMetadataChangedEventArgs : PropertyChangedEventArgs
    {
        public SfcPropertyMetadataChangedEventArgs(string propertyName)
            : base(propertyName)
        {
        }
    }
}
