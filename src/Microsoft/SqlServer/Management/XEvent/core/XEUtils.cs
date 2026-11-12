// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    ///  Provides helper methods for scripting.
    /// </summary>
    public class XEUtils
    {
        /// <summary>
        /// Check the current state of the events and return the script based on it.
        /// This should be used only in generating the alter script for session.
        /// </summary>
        /// <returns></returns>
        internal static void AppendAlterScripts<T>(StringBuilder addScript, StringBuilder dropScript, IEnumerable<T> coll, Session session) where T : ISessionObject
        {
            string separator = $", {Environment.NewLine}";
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("AppendAlterScripts"))
            {
                addScript.Append(string.Join(separator, coll
                    .Where(obj => (obj.State == SfcObjectState.Pending) || (obj.State == SfcObjectState.Recreate
                        || (obj.State == SfcObjectState.Existing && obj.IsDirty())))
                    .Select(obj => obj.GetCreateScript()).ToList()));
                dropScript.Append(string.Join(separator, coll
                    .Where(obj => (obj.State == SfcObjectState.ToBeDropped) || (obj.State == SfcObjectState.Recreate
                        || (obj.State == SfcObjectState.Existing && obj.IsDirty())))
                    .Select(obj => obj.GetDropScript()).ToList()));
                
            }
        }

        /// <summary>
        /// Indicates whether given state is for creation
        /// </summary>
        /// <param name="state">Object state.</param>
        /// <returns>True if object to be created.</returns>
        public static bool ToBeCreated(SfcObjectState state)
        {
            return state == SfcObjectState.Pending || state == SfcObjectState.Existing || state == SfcObjectState.Recreate;
        }

        /// <summary>
        /// Converts <see cref="Session.EventRetentionModeEnum"/> to string defined in XSD.
        /// </summary>
        /// <param name="retentionMode">Value to convert.</param>
        /// <returns>XSD defined mode string.</returns>
        public static string ConvertToXsdEnumerationValue(Session.EventRetentionModeEnum retentionMode)
        {
            // Xsd Enumeration Values are defined in Sql/Common/xsd/sqlserver/2008/07/extendedeventsconfig/xeconfig.xsd
            // The enumerations in the Xsd continue to start with lowercase only, for backward compatibility reasons

            switch (retentionMode)
            {
                case Session.EventRetentionModeEnum.AllowMultipleEventLoss: return "allowMultipleEventLoss";
                case Session.EventRetentionModeEnum.AllowSingleEventLoss: return "allowSingleEventLoss";
                case Session.EventRetentionModeEnum.NoEventLoss: return "noEventLoss";
                default:
                    TraceHelper.TraceContext.Assert(false, "Unknown EventRetentionMode");
                    return String.Empty;
            }
        }

        /// <summary>
        /// Converts <see cref="Session.MemoryPartitionModeEnum"/> to string defined in XSD.
        /// </summary>
        /// <param name="partitionMode">Value to convert.</param>
        /// <returns>XSD defined mode string.</returns>
        public static string ConvertToXsdEnumerationValue(Session.MemoryPartitionModeEnum partitionMode)
        {
            // Xsd Enumeration Values are defined in Sql/Common/xsd/sqlserver/2008/07/extendedeventsconfig/xeconfig.xsd
            // The enumerations in the Xsd continue to start with lowercase only, for backward compatibility reasons

            switch (partitionMode)
            {
                case Session.MemoryPartitionModeEnum.None: return "none";
                case Session.MemoryPartitionModeEnum.PerCpu: return "perCpu";
                case Session.MemoryPartitionModeEnum.PerNode: return "perNode";
                default:
                    TraceHelper.TraceContext.Assert(false, "Unknown MemoryPartitionMode");
                    return String.Empty;
            }
        }
    }
}
