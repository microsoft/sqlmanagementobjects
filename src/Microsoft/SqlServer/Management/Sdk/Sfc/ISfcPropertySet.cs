// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    // TODO: unify methods with SFC PropertyCollection object
    /// <summary>
    /// This is the interface that gives access to a set (collection, list or other aggregation)
    /// of properties.
    /// </summary>
    public interface ISfcPropertySet
    {
        /// <summary>
        /// Checks if the property with specified name exists
        /// </summary>
        /// <param name="propertyName">property name</param>
        /// <returns>true if succeeded</returns>
        bool Contains(string propertyName);

        /// <summary>
        /// Checks if the property with specified metadata exists
        /// </summary>
        /// <param name="property">Property</param>
        /// <returns>true if succeeded</returns>
        bool Contains(ISfcProperty property);

        /// <summary>
        /// Checks if the property with specified name and type exists
        /// </summary>
        /// <typeparam name="T">property type</typeparam>
        /// <param name="name">property name</param>
        /// <returns>true if succeeded</returns>
        bool Contains<T>(string name);

        /// <summary>
        /// Attempts to get property value from provider
        /// </summary>
        /// <typeparam name="T">property type</typeparam>
        /// <param name="name">name name</param>
        /// <param name="value">property value</param>
        /// <returns>true if succeeded</returns>
        bool TryGetPropertyValue<T>(string name, out T value);

        /// <summary>
        /// Attempts to get property value from provider
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="value">property value</param>
        /// <returns>true if succeeded</returns>
        bool TryGetPropertyValue(string name, out object value);

        /// <summary>
        /// Attempts to get property metadata
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="property">property information</param>
        /// <returns></returns>
        bool TryGetProperty(string name, out ISfcProperty property);

        /// <summary>
        /// Enumerates all properties
        /// </summary>
        /// <returns></returns>
        IEnumerable<ISfcProperty> EnumProperties();
    }
}
