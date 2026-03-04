// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// The mode of the object tree for client and server processing rules.
    /// Offline - no connection to the server for reads or writes at all.
    /// Online - full connection to the server for both reads and writes.
    /// TransactedBatch - full connection for reads, queue all writes to action log.
    ///     mode changed to Online, or explicit FlushActionLog() - perform all writes with transaction bracketing, do not pop queue.
    ///     mode changed to Offline - discard action log.
    /// NonTransactedBatch - full connection for reads, queue all writes to action log.
    ///     mode changed to Online, or explicit FlushActionLog() - perform all writes, pop queue as each action done.
    ///     mode changed to Offline - discard action log.
    /// </summary>
    public enum SfcConnectionContextMode
    {
        Offline,
        Online,
        TransactedBatch,
        NonTransactedBatch
    }

    /// <summary>
    /// ISfcDomain domain roots must implement this interface.
    /// </summary>
    public interface ISfcHasConnection
    {
        /// <summary>
        /// Get the connection query on to backing storage. Defaults to assuming a single open query will exist at one time.
        /// </summary>
        /// <returns>The connection to use.</returns>
        ISfcConnection GetConnection();


        /// <summary>
        /// Sets the active connection for the domain root. This is used for domain instantiation / hopping.
        /// </summary>
        void SetConnection(ISfcConnection connection);

        /// <summary>
        /// Get the connection to backing storage to support the requested query processing mode.
        /// Any connection which supports multiple open queries must assume that the regular connection returned by GetConnection() may be busy at any time.
        /// <param name="activeQueriesMode">Cache results, or use a live data reader iterator where Single or multiple open queries are expected.</param>
        /// </summary>
        /// <returns>The connection to use, or null to use Cache mode. Cache mode avoids connection and open data reader issues.</returns>
        ISfcConnection GetConnection(SfcObjectQueryMode activeQueriesMode);

        SfcConnectionContext ConnectionContext { get; }
    }

    public class SfcConnectionContext
    {
        private ISfcHasConnection domain = null;
        private SfcConnectionContextMode mode= SfcConnectionContextMode.Offline;

        /// <summary>
        /// Construct the context for tracking and transitioning between offline, online and batch update modes.
        /// </summary>
        /// <param name="domain">The domain instance for this context. 
        /// If null, then the mode is fixed as Offline and cannot be changed, otherwise it is initialized to Online.</param>
        public SfcConnectionContext(ISfcHasConnection domain)
        {
            this.domain = domain;
            if (domain.GetConnection() != null)
            {
                mode = SfcConnectionContextMode.Online;
            }
            else
            {
                mode = SfcConnectionContextMode.Offline;
            }
        }

        // Mode access and transitions
        public SfcConnectionContextMode Mode
        {
            get
            {
                // Force the mode to Offline if the connnection is ever missing (null).
                if (domain.GetConnection() == null && mode != SfcConnectionContextMode.Offline)
                {
                    mode = SfcConnectionContextMode.Offline;
                }
                return mode;
            }
            set
            {
                if (mode == value)
                {
                    return;
                }

                if (value != SfcConnectionContextMode.TransactedBatch && value != SfcConnectionContextMode.NonTransactedBatch)
                {
                    switch (mode)
                    {
                        case SfcConnectionContextMode.Offline:
                            // Offline -> any mode not allowed
                            break;

                        case SfcConnectionContextMode.Online:
                            // Online -> any mode allowed
                            if (value == SfcConnectionContextMode.Offline)
                            {
                                ForceDisconnected();
                            }
                            mode = value;
                            return;

                        case SfcConnectionContextMode.TransactedBatch:
                        case SfcConnectionContextMode.NonTransactedBatch:
                            // [Non]TransactedBatch -> Offline (discard log), Online (flush log) allowed
                            switch (value)
                            {
                                case SfcConnectionContextMode.Offline:
                                    DiscardActionLog();
                                    mode = value;
                                    return;

                                case SfcConnectionContextMode.Online:
                                    FlushActionLog();
                                    mode = value;
                                    return;

                                default:
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                }

                // If it gets to here we have an invalid mode transition
                throw new SfcInvalidConnectionContextModeChangeException(mode.ToString(), value.ToString());
            }
        }

        // Explicit action log flush (can also be called on its own to checkpoint-update)
        public void FlushActionLog()
        {
        }

        // Discard entire action log
        private void DiscardActionLog()
        {
        }

        private void ForceDisconnected()
        {
            ISfcConnection connection = domain.GetConnection();
            if (null != connection)
            {
                connection.ForceDisconnected();
            }
        }
    }


    public abstract class SfcConnection : ISfcConnection
    {
        public override abstract int GetHashCode();
        public abstract bool Equals(SfcConnection connection);

        #region ISfcConnection Members

        public abstract bool Connect();
        public abstract bool Disconnect();
        public abstract ISfcConnection Copy();

        public abstract bool IsOpen
        {
            get;
        }
        public abstract string ServerInstance
        {
            get;
            set;
        }

        public abstract Version ServerVersion
        {
            get;
            set;
        }

        public abstract ServerType ConnectionType 
        { 
            get; 
        }
        
        public abstract int ConnectTimeout
        {
            get;
            set;
        }

        public abstract int StatementTimeout
        {
            get;
            set;
        }

        public virtual void ForceDisconnected() { }

        public virtual bool IsForceDisconnected
        {
            get { return false; }
        }

        #region Obsolete ISfcConnection members
        virtual public object ToEnumeratorObject()
        {
            return this;
        }

        #endregion

        #endregion
    }
}

