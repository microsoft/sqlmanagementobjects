// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Diagnostics.STrace;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    // All of the Events information goes into this file
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class Policy
    {
        /// <summary>
        /// Signals the start of policy execution.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void PolicyEvaluationStartedEventHandler(object sender, EventArgs e);

        /// <summary>
        /// Event fired before the policy execution starts.
        /// </summary>
        public event PolicyEvaluationStartedEventHandler PolicyEvaluationStarted;

        /// <summary>
        /// Signals the start of policy execution for one connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ConnectionProcessingStartedEventHandler(object sender, ConnectionProcessingStartedEventArgs e);

        /// <summary>
        /// Event fired before the policy execution starts for one connection.
        /// </summary>
        public event ConnectionProcessingStartedEventHandler ConnectionProcessingStarted;

        /// <summary>
        /// Argument for ConnectionProcessingStarted event.
        /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
        public sealed class ConnectionProcessingStartedEventArgs : EventArgs
        {
            static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ConnectionProcessingStartedEventArgs");
            /// <summary>
            /// Internal ctor.
            /// </summary>
            /// <param name="connection"></param>
            internal ConnectionProcessingStartedEventArgs(ISfcConnection connection)
            {
                this.connection = connection;
            }

            ISfcConnection connection;
            /// Connection used to evaluate the policy
            public ISfcConnection Connection
            {
                get { return connection; }
                set
                {
                    traceContext.TraceVerbose("Setting Connection to: {0}", value);
                    connection = value;
                }
            }
        }

        /// <summary>
        /// Delegate for target execution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void TargetProcessedEventHandler(object sender, TargetProcessedEventArgs e);

        /// <summary>
        /// Event fired after a target has been processed.
        /// </summary>
        public event TargetProcessedEventHandler TargetProcessed;

        /// <summary>
        /// Arguments for the TargetProcessed event.
        /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
        public sealed class TargetProcessedEventArgs : EventArgs
        {
            static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "TargetProcessedEventArgs");
            /// <summary>
            /// Internal ctor.
            /// </summary>
            /// <param name="targetObject"></param>
            /// <param name="targetExpression"></param>
            /// <param name="result"></param>
            /// <param name="isConfigurable"></param>
            /// <param name="configMessage"></param>
            /// <param name="expressionNode"></param>
            /// <param name="exception"></param>
            /// <param name="serverName"></param>
            internal TargetProcessedEventArgs(
                                                object targetObject,
                                                string targetExpression,
                                                bool result,
                                                bool isConfigurable,
                                                string configMessage,
                                                ExpressionNode expressionNode,
                                                PolicyEvaluationException exception,
                                                string serverName)
            {
                traceContext.TraceMethodEnter("TargetProcessedEventArgs");
                // Tracing Input Parameters
                traceContext.TraceParameters(targetObject, targetExpression, result, isConfigurable, configMessage, expressionNode, exception, serverName);
                this.targetObject = targetObject;
                this.targetExpression = targetExpression;
                this.result = result;
                this.isConfigurable = isConfigurable;
                this.configMessage = configMessage;
                this.expressionNode = expressionNode;
                this.exception = exception;
                this.serverName = serverName;
                traceContext.TraceMethodExit("TargetProcessedEventArgs");
            }

            object targetObject;
            /// The actual target object that was processed. This could be anything at all, so it
            /// is of type Object.
            public object TargetObject
            {
                get { return targetObject; }
            }

            string targetExpression;
            /// query expression representing the target
            public string TargetExpression
            {
                get { return targetExpression; }
                set
                {
                    traceContext.TraceVerbose("Setting TargetExpression to: {0}", value);
                    targetExpression = value;
                }
            }

            bool result;
            /// Result of the evaluation
            public bool Result
            {
                get { return result; }
                set
                {
                    traceContext.TraceVerbose("Setting Result to: {0}", value);
                    result = value;
                }
            }

            ExpressionNode expressionNode;
            /// Expression node that contains the result.
            public ExpressionNode ExpressionNode
            {
                get { return expressionNode; }
                set
                {
                    traceContext.TraceVerbose("Setting ExpressionNode to: {0}", value);
                    expressionNode = value;
                }
            }


            PolicyEvaluationException exception;
            /// Exception thrown during evaluation, null if no exception occured.
            public PolicyEvaluationException Exception
            {
                get { return exception; }
                set
                {
                    traceContext.TraceVerbose("Setting Exception to: {0}", value);
                    exception = value;
                }
            }

            string serverName;
            /// Connection used to evaluate the policy
            public string ServerName
            {
                get { return serverName; }
                set
                {
                    traceContext.TraceVerbose("Setting ServerName to: {0}", value);
                    serverName = value;
                }
            }

            bool isConfigurable;
            /// indicates the object can be configured
            public bool IsConfigurable
            {
                get { return isConfigurable; }
                set
                {
                    traceContext.TraceVerbose("Setting IsConfigurable to: {0}", value);
                    isConfigurable = value;
                }
            }

            string configMessage;
            /// provides additional information on why the object cannot be configured
            public string ConfigurationErrorMessage
            {
                get { return configMessage; }
                set
                {
                    traceContext.TraceVerbose("Setting ConfigurationErrorMessage to: {0}", value);
                    configMessage = value;
                }
            }
        }

        /// <summary>
        /// Delegate that will be called when policy execution finishes for that connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ConnectionProcessingFinishedEventHandler(object sender, ConnectionProcessingFinishedEventArgs e);

        /// <summary>
        /// Event that gets fired after the policy execution has ended.
        /// </summary>
        public event ConnectionProcessingFinishedEventHandler ConnectionProcessingFinished;

        /// <summary>
        /// Argument for ConnectionProcessingFinished event.
        /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
        public sealed class ConnectionProcessingFinishedEventArgs : EventArgs
        {
            static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ConnectionProcessingFinishedEventArgs");
            /// <summary>
            /// Internal ctor.
            /// </summary>
            /// <param name="result"></param>
            /// <param name="exception"></param>
            /// <param name="connection"></param>
            /// <param name="targetsEvaluated"></param>
            /// <param name="rootCheckPassed"></param>
            internal ConnectionProcessingFinishedEventArgs(bool result, PolicyEvaluationException exception, ISfcConnection connection, int targetsEvaluated, bool rootCheckPassed)
            {
                traceContext.TraceMethodEnter("ConnectionProcessingFinishedEventArgs");
                // Tracing Input Parameters
                traceContext.TraceParameters(result, exception, connection);
                this.result = result;
                this.exception = exception;
                this.connection = connection;
                this.targetsEvaluated = targetsEvaluated;
                this.rootCheckPassed = rootCheckPassed;
                traceContext.TraceMethodExit("ConnectionProcessingFinishedEventArgs");
            }

            /// <summary>
            /// Internal ctor.
            /// </summary>
            /// <param name="result"></param>
            /// <param name="exception"></param>
            /// <param name="connection"></param>
            /// <param name="targetsEvaluated"></param>
            internal ConnectionProcessingFinishedEventArgs(bool result, PolicyEvaluationException exception, ISfcConnection connection, int targetsEvaluated)
            {
                this.result = result;
                this.exception = exception;
                this.connection = connection;
                this.targetsEvaluated = targetsEvaluated;
                this.rootCheckPassed = true;
            }

            bool result;
            /// Final result of the policy execution.
            public bool Result
            {
                get { return result; }
                set
                {
                    traceContext.TraceVerbose("Setting Result to: {0}", value);
                    result = value;
                }
            }

            PolicyEvaluationException exception;
            /// Exception thrown during evaluation, null if no exception occured.
            public PolicyEvaluationException Exception
            {
                get { return exception; }
                set
                {
                    traceContext.TraceVerbose("Setting Exception to: {0}", value);
                    exception = value;
                }
            }

            ISfcConnection connection;
            /// Conneciton used to evaluate the policy
            public ISfcConnection Connection
            {
                get { return connection; }
                set
                {
                    traceContext.TraceVerbose("Setting Connection to: {0}", value);
                    connection = value;
                }
            }

            int  targetsEvaluated;
            /// Number of targets evaluated
            public int TargetsEvaluated
            {
                get { return targetsEvaluated; }
                set { targetsEvaluated = value; }
            }

            bool rootCheckPassed;
            /// Indicates if connection passed RootCheck
            public bool RootCheckPassed
            {
                get { return rootCheckPassed; }
                set { rootCheckPassed = value; }
            }
        }

        /// <summary>
        /// Delegate that will be called when policy execution finishes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void PolicyEvaluationFinishedEventHandler(object sender, PolicyEvaluationFinishedEventArgs e);

        /// <summary>
        /// Event that gets fired after the policy execution has ended.
        /// </summary>
        public event PolicyEvaluationFinishedEventHandler PolicyEvaluationFinished;

        /// <summary>
        /// Argument for PolicyEvaluatioFinished event.
        /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
        public sealed class PolicyEvaluationFinishedEventArgs : EventArgs
        {
            static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyEvaluationFinishedEventArgs");
            /// <summary>
            /// Constructs a new PolicyEvaluationFinishedEventArgs
            /// </summary>
            /// <param name="result"></param>
            /// <param name="exception"></param>
            public PolicyEvaluationFinishedEventArgs(bool result, PolicyEvaluationException exception)
            {
                traceContext.TraceMethodEnter("PolicyEvaluationFinishedEventArgs");
                // Tracing Input Parameters
                traceContext.TraceParameters(result, exception);
                this.result = result;
                this.exception = exception;
                traceContext.TraceMethodExit("PolicyEvaluationFinishedEventArgs");
            }

            bool result;
            /// Final result of the policy execution.
            public bool Result
            {
                get { return result; }
                set
                {
                    traceContext.TraceVerbose("Setting Result to: {0}", value);
                    result = value;
                }
            }

            PolicyEvaluationException exception;
            /// Exception thrown during evaluation, null if no exception occured.
            public PolicyEvaluationException Exception
            {
                get { return exception; }
                set
                {
                    traceContext.TraceVerbose("Setting Exception to: {0}", value);
                    exception = value;
                }
            }

            EvaluationHistory history;
            /// Evaluation history for this run of the policy
            public EvaluationHistory EvaluationHistory
            {
                get { return history; }
                set
                {
                    traceContext.TraceVerbose("Setting EvaluationHistory to: {0}", value);
                    history = value;
                }
            }
        }

        EvaluationHistory hiddenHistory;
        ConnectionEvaluationHistory hiddenConnectionHistory;
        int connectionCount = 1;
        int detailCount = 1;

        /// <summary>
        /// Policy execution started worker.
        /// </summary>
        private void PolicyEvaluationStartedHistoryBuilder()
        {
            // the history will be associated with a dummy policy store
            // in order to avoid polluting the existing PolicyStore that the policy came from.
            PolicyStore ps = new PolicyStore();
            ((ISfcHasConnection)ps).ConnectionContext.Mode = SfcConnectionContextMode.Offline;
            Policy p = new Policy(ps, this.Name);
            this.hiddenHistory = new EvaluationHistory(p);
            this.hiddenHistory.PolicyName = p.Name;
            connectionCount = 1;
            detailCount = 1;

            this.hiddenHistory.StartDate = DateTime.Now;
        }

        /// <summary>
        /// Policy execution worker.
        /// </summary>
        /// <param name="e">The <see cref="Microsoft.SqlServer.Management.Dmf.Policy.PolicyEvaluationFinishedEventArgs"/> instance containing the event data.</param>
        private void PolicyEvaluationFinishedHistoryBuilder(Policy.PolicyEvaluationFinishedEventArgs e)
        {
            traceContext.TraceMethodEnter("PolicyEvaluationFinishedHistoryBuilder");
            // Tracing Input Parameters
            traceContext.TraceParameters(e);
            traceContext.DebugAssert(this.hiddenHistory != null, "The history object is null, but it should have been created on the evaluation start event");

            if (this.hiddenHistory != null)
            {
                this.hiddenHistory.EndDate = DateTime.Now;
                this.hiddenHistory.Exception = String.Empty;
                if (e.Exception != null)
                {
                    this.hiddenHistory.Exception = e.Exception.ToString();
                }
                this.hiddenHistory.Result = e.Result;
                this.hiddenHistory.ID = 1;
            }

            e.EvaluationHistory = this.hiddenHistory;

            // Processing is finish reset
            this.hiddenHistory = null;
            this.hiddenConnectionHistory = null;
            connectionCount = 1;
            detailCount = 1;
            traceContext.TraceMethodExit("PolicyEvaluationFinishedHistoryBuilder");
        }


        /// <summary>
        /// Connection Processing started worker.
        /// </summary>
        private void ConnectionProcessingStartedHistoryBuilder(Policy.ConnectionProcessingStartedEventArgs e)
        {
            traceContext.TraceMethodEnter("ConnectionProcessingStartedHistoryBuilder");
            // Tracing Input Parameters
            traceContext.TraceParameters(e);
            this.hiddenConnectionHistory = new ConnectionEvaluationHistory(this.hiddenHistory);
            this.hiddenConnectionHistory.ID = connectionCount++;
            this.hiddenConnectionHistory.ServerInstance = String.Empty;
            if (null != e.Connection)
            {
                this.hiddenConnectionHistory.ServerInstance = e.Connection.ServerInstance;
            }
            traceContext.TraceMethodExit("ConnectionProcessingStartedHistoryBuilder");
        }

        /// <summary>
        /// Policy execution finished started worker.
        /// </summary>
        private void ConnectionProcessingFinishedHistoryBuilder(Policy.ConnectionProcessingFinishedEventArgs e)
        {
            traceContext.TraceMethodEnter("ConnectionProcessingFinishedHistoryBuilder");
            // Tracing Input Parameters
            traceContext.TraceParameters(e);
            traceContext.DebugAssert(this.hiddenConnectionHistory != null, "The history connection object is null, but it should have been created on the connection start event");

            if (this.hiddenConnectionHistory != null)
            {
                this.hiddenConnectionHistory.Result = e.Result;
                this.hiddenConnectionHistory.Exception = String.Empty;
                if (e.Exception != null)
                {
                    this.hiddenConnectionHistory.Exception = e.Exception.ToString();
                }

                this.hiddenHistory.ConnectionEvaluationHistories.Add(this.hiddenConnectionHistory);
            }

            // Connection processing is currently finshed, let go
            this.hiddenConnectionHistory = null;
            traceContext.TraceMethodExit("ConnectionProcessingFinishedHistoryBuilder");
        }

        /// <summary>
        /// Target processed started worker.
        /// </summary>
        private void TargetProcessedHistoryBuilder(Policy.TargetProcessedEventArgs e)
        {
            traceContext.TraceMethodEnter("TargetProcessedHistoryBuilder");
            // Tracing Input Parameters
            traceContext.TraceParameters(e);
            traceContext.DebugAssert(this.hiddenConnectionHistory != null, "The history connection object is null, but it should have been created on the connection start event");
            if (this.hiddenConnectionHistory != null)
            {
                EvaluationDetail detail = new EvaluationDetail(this.hiddenConnectionHistory);
                detail.ID = detailCount++;
                detail.Result = e.Result;
                detail.TargetQueryExpression = String.Empty;
                if (!String.IsNullOrEmpty(e.TargetExpression))
                {
                    detail.TargetQueryExpression = e.TargetExpression;
                }
                detail.Exception = String.Empty;
                if (e.Exception != null)
                {
                    detail.Exception = e.Exception.ToString();
                }
                detail.ResultDetail = String.Empty;
                if (e.ExpressionNode != null)
                {
                    detail.ResultDetail = ExpressionNode.SerializeNodeWithResult(e.ExpressionNode);
                }

                this.hiddenConnectionHistory.EvaluationDetails.Add(detail);
            }
            traceContext.TraceMethodExit("TargetProcessedHistoryBuilder");
        }



        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        private void FireEvent(Delegate del, params object[] args)
        {
            if (del != null)
            {
                try
                {
                    del.DynamicInvoke(args);
                }
                catch (Exception ex)
                {
                    traceContext.TraceCatch(ex);
                    throw traceContext.TraceThrow(new PolicyEvaluationException(ExceptionTemplatesSR.PolicyEvaluationFailedOnDelegate(this.Name), ex));
                }

            }
        }

        private void FirePolicyEvaluationStarted()
        {
            // Only build history if someone is listening to the finished event
            if (PolicyEvaluationFinished != null)
            {
                this.PolicyEvaluationStartedHistoryBuilder();
            }
            this.FireEvent(PolicyEvaluationStarted, this, EventArgs.Empty);
        }

        private void FirePolicyEvaluationFinished(Policy.PolicyEvaluationFinishedEventArgs e)
        {
            // Only build history if someone is listening to the finished event
            if (PolicyEvaluationFinished != null)
            {
                this.PolicyEvaluationFinishedHistoryBuilder(e);
            }
            this.FireEvent(PolicyEvaluationFinished, this, e);
        }

        private void FireConnectionProcessingStarted(Policy.ConnectionProcessingStartedEventArgs e)
        {
            // Only build history if someone is listening to the finished event
            if (PolicyEvaluationFinished != null)
            {
                this.ConnectionProcessingStartedHistoryBuilder(e);
            }
            this.FireEvent(ConnectionProcessingStarted, this, e);
        }

        private void FireConnectionProcessingFinished(Policy.ConnectionProcessingFinishedEventArgs e)
        {
            // Only build history if someone is listening to the finished event
            if (PolicyEvaluationFinished != null)
            {
                this.ConnectionProcessingFinishedHistoryBuilder(e);
            }
            this.FireEvent(ConnectionProcessingFinished, this, e);
        }

        private void FireTargetProcessed(Policy.TargetProcessedEventArgs e)
        {
            // Only build history if someone is listening to the finished event
            if (PolicyEvaluationFinished != null)
            {
                this.TargetProcessedHistoryBuilder(e);
            }
            this.FireEvent(TargetProcessed, this, e);
        }


    }
}
