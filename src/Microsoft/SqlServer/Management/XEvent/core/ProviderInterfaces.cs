// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    ///   defines the interface that component providers need to implement
    ///     for the XEStore, which is the root for all metadata classes and runtime classes.
    /// </summary>
    public interface IXEStoreProvider
    {
        /// <summary>
        /// Gets the execution engine.
        /// </summary>
        /// <returns></returns>
        ISfcExecutionEngine GetExecutionEngine();

        /// <summary>
        /// Get the current connection to query on.
        /// Return a connection supporting either a single serial query or multiple simultaneously open queries as requested.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns>The connection to use, or null to use Cache mode. Cache mode avoids connection and open data reader issues.</returns>
        ISfcConnection GetConnection(SfcObjectQueryMode mode);

        /// <summary>
        /// Gets the name of the domain instance.
        /// </summary>
        /// <value>The name of the domain instance.</value>        
        string DomainInstanceName { get; }

        /// <summary>
        ///  Gets the comparer for the child collections
        /// </summary>
        IComparer<string> GetComparer();
    }

    /// <summary>
    ///   defines the interface that component providers need to implement
    ///     for the Session, the main object user code interacts with.
    /// </summary>
    public interface ISessionProvider
    {
        /// <summary>
        ///   Script create for this session.
        /// </summary>
        /// <returns></returns>
        ISfcScript GetCreateScript();

        /// <summary>
        /// Script alter for this session.
        /// </summary>
        /// <returns></returns>
        ISfcScript GetAlterScript();

        /// <summary>
        /// Scripts drop for this session
        /// </summary>
        /// <returns></returns>
        ISfcScript GetDropScript();
        
        /// <summary>
        ///  backend specfic validations to Alter the session.
        /// </summary>
        /// <returns> true iff validation succeeds</returns>
        void ValidateAlter();

        /// <summary>
        /// Starts this session.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops this session.
        /// </summary>
        void Stop();
    }

    /// <summary>
    ///   defines the interface that component providers need to implement
    ///     for the Event, the Runtime class for the Events.
    /// </summary>
    public interface IEventProvider
    {
        /// <summary>
        /// Script create for this session.
        /// </summary>
        /// <returns></returns>
        string GetCreateScript();

        /// <summary>
        /// Scripts drop for this session
        /// </summary>
        /// <returns></returns>
        string GetDropScript();
    }

    /// <summary>
    ///   defines the interface that component providers need to implement
    ///     for the Target, the Runtime class for Target
    /// </summary>
    public interface ITargetProvider
    {
        /// <summary>
        /// Script create for this session.
        /// </summary>
        /// <returns></returns>
        string GetCreateScript();

        /// <summary>
        /// Scripts drop for this session
        /// </summary>
        /// <returns></returns>
        string GetDropScript();

        /// <summary>
        /// Gets the target data.
        /// </summary>
        /// <returns>Target data xml string.</returns>
        string GetTargetData();
    }
}
