// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;

using Microsoft.SqlServer.Diagnostics.STrace;
using SMO = Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Management.Dmf
{
    public sealed partial class PolicyCategorySubscription
    {
        /// <summary>
        /// Validates object
        /// </summary>
        /// <param name="mode"></param>
        public void Validate(string mode)
        {
            ValidateProperties(mode);
        }

        /// <summary>
        /// Validates object properties
        /// </summary>
        /// <param name="mode"></param>
        public void ValidateProperties(string mode)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateProperties", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(mode);
                if (null == this.Parent)
                {
                    throw methodTraceContext.TraceThrow(new Microsoft.SqlServer.Management.Sdk.Sfc.SfcMissingParentException());
                }

                if (String.IsNullOrEmpty(this.TargetType))
                {
                    throw methodTraceContext.TraceThrow(new Microsoft.SqlServer.Management.Sdk.Sfc.SfcPropertyNotSetException("TargetType"));
                }
                if (this.TargetType != "DATABASE")
                {
                    throw methodTraceContext.TraceThrow(new UnsupportedObjectTypeException(this.TargetType, ExceptionTemplatesSR.CategorySubscription));
                }

                if (String.IsNullOrEmpty(this.PolicyCategory))
                {
                    throw methodTraceContext.TraceThrow(new Microsoft.SqlServer.Management.Sdk.Sfc.SfcPropertyNotSetException("PolicyCategory"));
                }

                if (String.IsNullOrEmpty(this.Target))
                {
                    throw methodTraceContext.TraceThrow(new Microsoft.SqlServer.Management.Sdk.Sfc.SfcPropertyNotSetException("Target"));
                }

                // $ISSUE
                // Hard-coding this to SMO. To be removed by Anand in his work on removing SMO dependency
                SMO.Server server = new SMO.Server(this.Parent.SqlStoreConnection.ServerConnection);
                if (!server.Databases.Contains(this.Target))
                {
                    throw methodTraceContext.TraceThrow(new MissingObjectException(typeof(SMO.Database).Name, this.Target));
                }
            }
        }
    }
}
