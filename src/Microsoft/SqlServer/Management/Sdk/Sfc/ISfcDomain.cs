// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    /// <summary>
    /// A light weight interface exposing basic properties of a SfcDomain.
    /// </summary>
    public interface ISfcDomainLite : ISfcHasConnection
    {
        /// <summary>
        /// Logical version indicates the changes in the OM of the domain. This acts independent of
        /// assembly fileversion or version.
        /// </summary>
        int GetLogicalVersion();

        /// <summary>
        /// The name of the domain used to distinguish it from other domains. This is usually the end of the namespace qualifier.
        /// </summary>
        string DomainName { get; }

        /// <summary>
        /// The logical name of a domain instance usually derived from the connection and domain information.
        /// This name does not have to be unique on the client, but should be different whenever the server representation would be.
        /// </summary>
        string DomainInstanceName { get; }
    }

    /// <summary>
    /// A root SfcInstance-derived object must implement ISfcDomain
    /// </summary>
    // TODO: this needs rework in v2. Most items end up in SfcDomainMetadata, and DomainInstanceName would be in SfcDomain.
    public interface ISfcDomain : ISfcDomainLite
    {
        /// <summary>
        /// Get the System.Type of the Sfc object class within the domain for the given string name.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns>The type object, or null if the name is not a type in the domain.</returns>
        Type GetType(string typeName);

        /// <summary>
        /// Gets an instance of Key given urn fragment interface and domain
        /// </summary>
        /// <param name="urnFragment"></param>
        /// <returns></returns>
        SfcKey GetKey(IUrnFragment urnFragment);

        /// <summary>
        /// Returns execution engine for this domain
        /// </summary>
        ISfcExecutionEngine GetExecutionEngine();

        /// <summary>
        /// Given type, return metadata for that type
        /// </summary>
        SfcTypeMetadata GetTypeMetadata(string typeName);

        /// <summary>
        /// Return true if you want SFC-provided State management, or false otherwise
        /// </summary>
        bool UseSfcStateManagement();
    }

    /// <summary>
    /// Extension for ISfcDomain to accommodate domain specific functionality
    /// like generating 'view' path for an object (as opposed to its 'physical' path)
    /// </summary>
    public interface ISfcDomain2 : ISfcDomain 
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputType"></param>
        /// <returns></returns>
        List<String> GetUrnSkeletonsFromType(Type inputType);
    }
}
