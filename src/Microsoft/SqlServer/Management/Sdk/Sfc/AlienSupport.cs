// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /*

    These interfaces represent functionality that needs to be implemented by "alien" (read: SMO) domains
    that are not based on SFC.
    This is only a stop-gap solution for now. It works like this:

    public void Discover(object instance)
    {
        ...
        if (instance is SfcInstance)
        {
            // implement SFC-specific Discover for this SfcInstance
            ...
        }
        else if (instance is IAlienObject)
        {
            // Delegate work to "alien" domain
            IAlienObject alien = instance as IAlienObject;
            alien.Discover();
        }
        ...
    }

    Here is how it should be done:

    public void Serialize(IDiscoverable instance)
    {
        instance.Discover(); // virtual call lands us in the right implementation
    }

    */

    public interface IAlienObject
    {
        // Resolve and Discover. Needed for serialization and attributes
        object Resolve(string urnString);
        List<object> Discover();

        // Get/Set property. Used in serialization. Overall, attempting to
        // encapsulate functionalies needed for (de)serialization so that
        // domain knowledge is not encoded in Sfc serialization code.
        void SetPropertyValue(string propertyName, Type propertyType, object value);
        Type GetPropertyType(string propertyName);
        object GetPropertyValue(string propertyName, Type propertyType);
        void SetObjectState(SfcObjectState state);
        ISfcDomainLite GetDomainRoot();

        // Get parent and Urn. Used in various places
        object GetParent();
        Urn GetUrn();
    }

    internal interface ISqlSmoObjectInitialize
    {
        // Refreshes the property bag with the values in the data reader
        void InitializeFromDataReader(IDataReader reader);
    }

    public interface IAlienRoot
    {
        // Helper-methods for Object Query
        DataTable SfcHelper_GetDataTable(object connection, string urn, string[] fields, OrderBy[] orderByFields);
        List<string> SfcHelper_GetSmoObjectQuery(string queryString, string[] fields, OrderBy[] orderByFields);
        object SfcHelper_GetSmoObject(string urn);

        void DesignModeInitialize();

        // Get connection and Name. Really random stuff
        ServerConnection ConnectionContext{ get; }
        String Name{ get; }
    }
}
