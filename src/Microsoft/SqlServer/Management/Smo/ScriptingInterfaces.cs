// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Interface that defines filtering of T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items.
    /// </summary>
    internal interface ISmoFilter
    {
        /// <summary>
        /// Gets or sets the server for the T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn
        /// items provided.
        /// </summary>
        Server Server { get; set; }

        /// <summary>
        /// Filters the list of input T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items.
        /// </summary>
        /// <param name="urns">A enumeration of input
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items.</param>
        /// <returns>A enumeration of
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items.</returns>
        IEnumerable<Urn> Filter(IEnumerable<Urn> urns);
    }

    /// <summary>
    /// Interface that defines writing of scripts of sql server management objects
    /// </summary>
    public interface ISmoScriptWriter
    {
        /// <summary>
        /// Called with the full set of script lines for an object.
        /// </summary>
        /// <param name="script">Enumerable collection of string
        /// that contain scripts of database items.</param>
        /// <param name="obj">The <seealso cref="Microsoft.SqlServer.Management.Sdk.Sfc.Urn"></seealso> that identifies the object being scripted.</param>
        /// <returns></returns>
        void ScriptObject(IEnumerable<string> script, Urn obj);

        /// <summary>
        /// Writes table items' data scripts.
        /// </summary>
        /// <param name="dataScript">Enumerable collection of string
        /// objects that will contain data scripts</param>
        /// <param name="table">The table to be scripted.</param>
        /// <returns></returns>
        void ScriptData(IEnumerable<string> dataScript, Urn table);

        /// <summary>
        /// Writes a string that specifies the database being scripted.
        /// The database context is the equivalent of the USE [Database] segment of
        /// the database script.
        /// </summary>
        /// <param name="databaseContext">String that specifies the database to script.</param>
        /// <param name="obj">The T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn to script.</param>
        /// <returns></returns>
        void ScriptContext(String databaseContext, Urn obj);

        /// <summary>
        /// The header string to insert at the beginning of the script
        /// </summary>
        string Header { set; }
    }

    internal interface ISmoDependencyOrderer
    {
        /// <summary>
        /// Gets or sets the server for the
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items provided.
        /// </summary>
        Server Server { get; set; }

        /// <summary>
        /// Sorts a generic list of
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items.
        /// </summary>
        /// <param name="urns">Generic list of input
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items.</param>
        /// <returns>Generic list of T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items.</returns>
        List<Urn> Order(IEnumerable<Urn> urns);
    }

    /// <summary>
    /// Defines an interface for discovering URNs of dependent objects
    /// </summary>
    public interface ISmoDependencyDiscoverer
    {
        /// <summary>
        /// Gets or sets the server for the
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items provided.
        /// </summary>
        Server Server {get;set;}

        /// <summary>
        /// Discovers dependent objects for each
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn item in the list supplied
        /// by the urns parameter.
        /// </summary>
        /// <param name="urns">A enumeration of
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn input items.</param>
        /// <returns>A enumeration of
        /// T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn items.</returns>
        IEnumerable<Urn> Discover(IEnumerable<Urn> urns);
    }
}
