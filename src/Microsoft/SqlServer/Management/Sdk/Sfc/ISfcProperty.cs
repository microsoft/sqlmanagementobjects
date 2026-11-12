// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Text;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    public interface ISfcProperty
    {
        /// <summary>
        /// Name of property
        /// </summary>
        string Name { get;}

        /// <summary>
        /// Type of property
        /// </summary>
        Type Type { get;}

        /// <summary>
        /// Check whether the value is enabled or not
        /// </summary>
        bool Enabled { get;}

        /// <summary>
        /// Value of property
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Indicates whether the property is required to persist the current state of the object
        /// </summary>
        bool Required { get; }

        /// <summary>
        /// Indicates that Consumer should be theat this property as read-only
        /// </summary>
        bool Writable { get; }

        /// <summary>
        /// Indicates whether the property value has been changed.
        /// </summary>
        bool Dirty { get; }

        /// <summary>
        /// Indicates whether the properties data has been read, and is null
        /// </summary>
        bool IsNull { get; }

        /// <summary>
        /// Aggregated list of custom attributes associated with property
        /// TODO: this needs to be delay-loaded
        /// </summary>
        AttributeCollection Attributes { get;}
    }
}