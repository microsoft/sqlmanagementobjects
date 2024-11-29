// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Sql provider for Session.
    /// </summary>
    public abstract class SessionProviderBase : ISessionProvider
    {
        private const string SessionStart = "STATE=START";
        private const string SessionStop = "STATE=STOP";
        // string that specifies if the scope is Database or Server that would be used for scripting the session
        private readonly string ScopeName;
        private Session session = null;

        /// <summary>
        /// Constructs a new SessionProviderBase for the given session. 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="scopeName">Holds a value of either "DATABASE" or "SERVER"</param>
        protected SessionProviderBase(Session session, string scopeName)
        {
            this.session = session;
            this.ScopeName = scopeName;
        }

        /// <summary>
        /// Script Create for this session.
        /// </summary>
        /// <returns>Session Create script.</returns>
        public ISfcScript GetCreateScript()
        {
            return this.CreateScript(ProviderConstants.Create + GetBaseScript());
        }

        /// <summary>
        /// Script Alter for this session.
        /// </summary>
        /// <returns>Session Alter script.</returns>
        public ISfcScript GetAlterScript()
        {
            StringBuilder sessionAlterScripts = new StringBuilder();
            StringBuilder addEventScript = new StringBuilder();
            StringBuilder dropEventScript = new StringBuilder();
            StringBuilder addTargetScript = new StringBuilder();
            StringBuilder dropTargetScript = new StringBuilder();

            this.session.Events.AppendAlterScripts(addEventScript, dropEventScript);
            this.session.Targets.AppendAlterScripts(addTargetScript, dropTargetScript);

            string alterScriptPrefix = ProviderConstants.Alter + GetBaseScript();

            if (dropEventScript.Length > 0)
            {
                sessionAlterScripts.AppendLine(alterScriptPrefix).AppendLine(dropEventScript.ToString());
            }

            if (dropTargetScript.Length > 0)
            {
                sessionAlterScripts.AppendLine(alterScriptPrefix).AppendLine(dropTargetScript.ToString());
            }

            if (addTargetScript.Length > 0)
            {
                sessionAlterScripts.AppendLine(alterScriptPrefix).AppendLine(addTargetScript.ToString());
            }

            // put option here may save one clause
            this.AppendOptionsAlterScript(sessionAlterScripts);

            if (addEventScript.Length > 0)
            {
                sessionAlterScripts.AppendLine(alterScriptPrefix).AppendLine(addEventScript.ToString());
            }

            return new SfcTSqlScript(sessionAlterScripts.ToString().TrimEnd());
        }

        /// <summary>
        /// Scripts Drop for this session.
        /// </summary>
        /// <returns>Session Drop script.</returns>
        public ISfcScript GetDropScript()
        {
            string script = ProviderConstants.Drop + GetBaseScript();
            return new SfcTSqlScript(script);
        }

        private string GetBaseScript() => GetBaseScript(session.Name);

        private string GetBaseScript(string sessionName) => $"EVENT SESSION {SfcTsqlProcFormatter.MakeSqlBracket(sessionName)} ON {ScopeName} ";

        /// <summary>
        ///  Backend specfic validations to Alter the session.
        ///  NB: In case of sql engine the Alter statement is not atomic
        ///  so it uses a dummy session to validate before executing alter on the actual session.
        /// </summary>
        public void ValidateAlter()
        {
            bool eventsToBeCreated = false;
            foreach (Event evt in this.session.Events)
            {
                if (XEUtils.ToBeCreated(evt.State))
                {
                    eventsToBeCreated = true;
                    break;
                }
            }

            // don't validate through dummy session if there's no event
            if (eventsToBeCreated)
            {
                string dummySessionName = "dummy_session" + Guid.NewGuid().ToString();
                try
                {
                    SfcTSqlScript scriptDummy = this.GetCreateScript(dummySessionName) as SfcTSqlScript;
                    ((ISfcDomain)this.session.Parent).GetExecutionEngine().Execute(scriptDummy);
                }
                catch (Exception ex)
                {
                    throw new XEventException(ExceptionTemplates.AlterValidationFailure, ex);
                }
                finally
                {
                    Session dummySession = this.session.Parent.Sessions[dummySessionName];
                    if (dummySession != null)
                    {
                        // Drop is very quick
                        dummySession.Drop();
                    }
                }
            }
        }

        /// <summary>
        /// Starts this session.
        /// </summary>
        public void Start()
        {
            string sql = ProviderConstants.Alter + GetBaseScript() + SessionStart;
            ((ISfcDomain)this.session.Parent).GetExecutionEngine().Execute(new SfcTSqlScript(sql));
        }

        /// <summary>
        /// Stops this session.
        /// </summary>
        public void Stop()
        {
            string sql = ProviderConstants.Alter + GetBaseScript() + SessionStop;
            ((ISfcDomain)this.session.Parent).GetExecutionEngine().Execute(new SfcTSqlScript(sql));
        }

        /// <summary>
        /// Generates a script to create the XE session for the selected targets and with the selected options
        /// </summary>
        /// <param name="createStatment"></param>
        /// <returns></returns>
        private ISfcScript CreateScript(string createStatment)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(createStatment);

            var eventString = string.Join(",",
                this.session.Events.Where(ev => XEUtils.ToBeCreated(ev.State))
                    .Cast<ISessionObject>()
                    .Select(so => Environment.NewLine + so.GetCreateScript()).
                    ToArray());

            sb.Append(eventString);

            var targetString = string.Join(",",
                this.session.Targets.Where(tgt => XEUtils.ToBeCreated(tgt.State))
                    .Cast<ISessionObject>()
                    .Select(so => Environment.NewLine + so.GetCreateScript())
                    .ToArray());

            sb.Append(targetString);

            string optionString = this.GetOptionString(true);
            if (optionString.Length != 0)
            {
                sb.AppendLine();
                sb.Append("WITH (");
                sb.Append(optionString);
                sb.Append(")");
            }

            return new SfcTSqlScript(sb.ToString());
        }

        /// <summary>
        /// Script Create for this session.
        /// </summary>
        /// <param name="sessionName">A session name.</param>
        /// <returns>Session Create script.</returns>
        private ISfcScript GetCreateScript(string sessionName)
        {
            string statement = ProviderConstants.Create + GetBaseScript(sessionName);

            return CreateScript(statement);
        }

        /// <summary>
        /// Gets the session option string.
        /// </summary>
        /// <param name="create">Create flag.</param>
        /// <returns>Session option string.</returns>
        private string GetOptionString(bool create)
        {
            // when ScriptCreate on an existing session, Dirty property is always false.
            // in such case, option string will be added completely
            bool scriptCreateForExistingSession = create && (this.session.State == SfcObjectState.Existing);

            // complete option string is about 180-200 characters
            StringBuilder sb = new StringBuilder(256);
            if (scriptCreateForExistingSession || this.session.Properties[Session.MaxMemoryProperty].Dirty)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "MAX_MEMORY={0} KB,", this.session.MaxMemory);
            }

            if (scriptCreateForExistingSession || this.session.Properties[Session.EventRetentionModeProperty].Dirty)
            {
                string mode = String.Empty;
                switch (this.session.EventRetentionMode)
                {
                    // the property throws if an invalid value is set, this default is just to catch the case where the enum has  been updated but the UI hasn't been updated.
                    default:
                    case Session.EventRetentionModeEnum.AllowSingleEventLoss:
                        mode = "ALLOW_SINGLE_EVENT_LOSS";
                        break;
                    case Session.EventRetentionModeEnum.AllowMultipleEventLoss:
                        mode = "ALLOW_MULTIPLE_EVENT_LOSS";
                        break;
                    case Session.EventRetentionModeEnum.NoEventLoss:
                        mode = "NO_EVENT_LOSS";
                        break;
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, "EVENT_RETENTION_MODE={0},", mode);
            }

            if (scriptCreateForExistingSession || this.session.Properties[Session.MaxDispatchLatencyProperty].Dirty)
            {
                // 0 SECONDS is equivalent to INIFINITE
                sb.AppendFormat(CultureInfo.InvariantCulture, "MAX_DISPATCH_LATENCY={0} SECONDS,", this.session.MaxDispatchLatency);
            }

            if (scriptCreateForExistingSession || this.session.Properties[Session.MaxEventSizeProperty].Dirty)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "MAX_EVENT_SIZE={0} KB,", this.session.MaxEventSize);
            }

            if (scriptCreateForExistingSession || this.session.Properties[Session.MemoryPartitionModeProperty].Dirty)
            {
                string mode = String.Empty;
                switch (this.session.MemoryPartitionMode)
                {
                    case Session.MemoryPartitionModeEnum.None:
                        mode = "NONE";
                        break;
                    case Session.MemoryPartitionModeEnum.PerNode:
                        mode = "PER_NODE";
                        break;
                    case Session.MemoryPartitionModeEnum.PerCpu:
                        mode = "PER_CPU";
                        break;
                }

                sb.Append("MEMORY_PARTITION_MODE=");
                sb.Append(mode);
                sb.Append(",");
            }

            if (scriptCreateForExistingSession || this.session.Properties[Session.TrackCausalityProperty].Dirty)
            {
                sb.Append("TRACK_CAUSALITY=");
                sb.Append(this.session.TrackCausality ? "ON" : "OFF");
                sb.Append(",");
            }

            if (scriptCreateForExistingSession || this.session.Properties[Session.AutoStartProperty].Dirty)
            {
                sb.Append("STARTUP_STATE=");
                sb.Append(this.session.AutoStart ? "ON" : "OFF");
                sb.Append(",");
            }

            if (sb.Length > 0)
            {
                // remove the last comma
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Appends options for the session to the alter session script
        /// </summary>
        /// <param name="sessionAlterScripts"></param>
        private void AppendOptionsAlterScript(StringBuilder sessionAlterScripts)
        {
            string optionString = this.GetOptionString(false);

            if (optionString.Length != 0)
            {
                if (sessionAlterScripts.Length == 0)
                {
                    sessionAlterScripts.Append(ProviderConstants.Alter);
                    sessionAlterScripts.AppendLine(GetBaseScript());
                }

                sessionAlterScripts.Append(" WITH (");
                sessionAlterScripts.Append(optionString);
                sessionAlterScripts.AppendLine(") ");
            }
        }
    } 
}