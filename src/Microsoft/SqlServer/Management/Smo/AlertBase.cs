// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class Alert : AgentObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IAlterable, Cmn.IRenamable, IScriptable
    {
        internal Alert(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Alert";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(createQuery, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                createQuery.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AGENT_ALERT, "NOT", FormatFullNameForScripting(sp, false));
                createQuery.Append(sp.NewLine);
            }

            createQuery.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_add_alert @name=N'{0}'", SqlString(this.Name));
            int count = 1;
            GetAllParams(createQuery, sp, ref count);
            queries.Add(createQuery.ToString());

            if (sp.Agent.Notify)
            {
                DataTable dt = this.EnumNotifications();
                DataRow row;
                for (int iRow = 0; iRow < dt.Rows.Count; iRow++)
                {
                    row = dt.Rows[iRow];

                    bool email = (Convert.ToBoolean(row["HasEmail"], SmoApplication.DefaultCulture) && Convert.ToBoolean(row["UseEmail"], SmoApplication.DefaultCulture));
                    bool pager = (Convert.ToBoolean(row["HasPager"], SmoApplication.DefaultCulture) && Convert.ToBoolean(row["UsePager"], SmoApplication.DefaultCulture));
                    bool netsend = (Convert.ToBoolean(row["HasNetSend"], SmoApplication.DefaultCulture) && Convert.ToBoolean(row["UseNetSend"], SmoApplication.DefaultCulture));
                    if (email || pager || netsend)
                    {
                        string operatorName = Convert.ToString(row["OperatorName"], SmoApplication.DefaultCulture);
                        string addNotification = String.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_add_notification @alert_name=N'{0}', @operator_name=N'{1}', @notification_method =", SqlString(this.Name), SqlString(operatorName));

                        if (email && pager && netsend)
                        {
                            queries.Add(addNotification + (int)NotifyMethods.NotifyAll);
                        }
                        else
                        {
                            if (email)
                            {
                                queries.Add(addNotification + (int)NotifyMethods.NotifyEmail);
                            }

                            if (pager)
                            {
                                queries.Add(addNotification + (int)NotifyMethods.Pager);
                            }

                            if (netsend)
                            {
                                queries.Add(addNotification + (int)NotifyMethods.NetSend);
                            }
                        }
                    }
                }
            }
        }

        private void GetAllParams(StringBuilder sb, ScriptingPreferences sp, ref int count)
        {
            GetParameter(sb, sp, "MessageID", "@message_id={0}", ref count);
            GetParameter(sb, sp, "Severity", "@severity={0}", ref count);
            GetBoolParameter(sb, sp, "IsEnabled", "@enabled={0}", ref count);
            GetParameter(sb, sp, "DelayBetweenResponses", "@delay_between_responses={0}", ref count);
            GetEnumParameter(sb, sp, "IncludeEventDescription", "@include_event_description_in={0}",
                            typeof(NotifyMethods), ref count);
            GetStringParameter(sb, sp, "DatabaseName", "@database_name=N'{0}'", ref count);
            GetStringParameter(sb, sp, "NotificationMessage", "@notification_message=N'{0}'", ref count);
            GetStringParameter(sb, sp, "EventDescriptionKeyword", "@event_description_keyword=N'{0}'", ref count);
            GetStringParameter(sb, sp, "CategoryName", "@category_name=N'{0}'", ref count);
            GetStringParameter(sb, sp, "PerformanceCondition", "@performance_condition=N'{0}'", ref count);

            // add wmi parameters only if version 9.0+
            if (ServerVersion.Major >= 9)
            {
                GetStringParameter(sb, sp, "WmiEventNamespace", "@wmi_namespace=N'{0}'", ref count);
                GetStringParameter(sb, sp, "WmiEventQuery", "@wmi_query=N'{0}'", ref count);
            }

            // Attempt to script JobName only if corresponding scripting option is set, 
            // if fail still script job id instead
            if (sp.Agent.JobId == false)
            {
                // do not script out job ID
            }
            else
                if ((!sp.Agent.AlertJob && !sp.ScriptForAlter && !sp.ScriptForCreateDrop) ||
                    !GetStringParameter(sb, sp, "JobName", "@job_name=N'{0}'", ref count))
                {
                    GetGuidParameter(sb, sp, "JobID", "@job_id=N'{0}'", ref count);
                }
        }

        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AGENT_ALERT, "", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_delete_alert @name=N'{0}'", SqlString(this.Name));

            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            alterQuery.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_update_alert @name=N'{0}'", SqlString(this.Name));
            int count = 1;
            GetAllParams(alterQuery, sp, ref count);

            // those properties can only be altered, they are not set at creation time
            GetDateTimeParameter(alterQuery, sp, "CountResetDate", "@count_reset_{0}={1}", ref count);
            GetDateTimeParameter(alterQuery, sp, "LastOccurrenceDate", "@last_occurrence_{0}={1}", ref count);
            GetDateTimeParameter(alterQuery, sp, "LastResponseDate", "@last_response_{0}={1}", ref count);

            if (count > 1)
            {
                queries.Add(alterQuery.ToString());
            }

        }

        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_update_alert @name=N'{0}', @new_name=N'{1}'",
                            SqlString(this.Name), SqlString(newName)));
        }

        /// <summary>
        /// The ResetOccurrenceCount method reinitializes history data for a 
        /// SQLServerAgent alert.
        /// </summary>
        public void ResetOccurrenceCount()
        {
            try
            {
                StringCollection querycol = new StringCollection();
                StringBuilder query = new StringBuilder();

                query.Append("DECLARE @curr_date INT, 	@curr_time INT ");
                query.Append(Globals.newline);
                query.Append("SELECT @curr_date = CONVERT(INT, CONVERT(CHAR, GETDATE(), 112)), @curr_time = (DATEPART(hh, GETDATE()) * 10000) + (DATEPART(mi, GETDATE()) * 100) + (DATEPART(ss, GETDATE())) ");
                query.AppendFormat(SmoApplication.DefaultCulture, "EXECUTE msdb.dbo.sp_update_alert @name = N'{0}', @count_reset_date = @curr_date, @count_reset_time = @curr_time, @occurrence_count = 0", SqlString(this.Name));
                querycol.Add(query.ToString());

                this.ExecutionManager.ExecuteNonQuery(querycol);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ResetOccurrenceCount, this, e);
            }

        }

        /// <summary>
        /// The AddNotification method associates operators with alerts. 
        /// Operators designated receive notification messages when an 
        /// event raising the alert occurs.
        /// </summary>
        /// <param name="operatorName"></param>
        /// <param name="notifymethod"></param>
        public void AddNotification(string operatorName, NotifyMethods notifymethod)
        {
            try
            {
                if (null == operatorName)
                {
                    throw new ArgumentNullException("operatorName");
                }

                StringCollection querycol = new StringCollection();
                querycol.Add(string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_add_notification @alert_name=N'{0}', @operator_name=N'{1}', @notification_method = {2}",
                                            SqlString(this.Name), SqlString(operatorName), Enum.Format(typeof(NotifyMethods), notifymethod, "d")));
                this.ExecutionManager.ExecuteNonQuery(querycol);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddNotification, this, e);
            }
        }

        /// <summary>
        /// The RemoveNotification method drops all SQLServerAgent alert 
        /// notification assignments for an operator.
        /// </summary>
        /// <param name="operatorName"></param>
        public void RemoveNotification(string operatorName)
        {
            try
            {
                if (null == operatorName)
                {
                    throw new ArgumentNullException("operatorName");
                }

                StringCollection querycol = new StringCollection();
                querycol.Add(string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_delete_notification @alert_name=N'{0}', @operator_name=N'{1}'",
                                            SqlString(this.Name), SqlString(operatorName)));
                this.ExecutionManager.ExecuteNonQuery(querycol);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveNotification, this, e);
            }

        }

        /// <summary>
        /// The UpdateNotification method configures SQL Server Agent operator 
        /// notification for alerts raised.
        /// </summary>
        /// <param name="operatorName"></param>
        /// <param name="notifymethod"></param>
        public void UpdateNotification(string operatorName, NotifyMethods notifymethod)
        {
            try
            {
                if (null == operatorName)
                {
                    throw new ArgumentNullException("operatorName");
                }

                StringCollection querycol = new StringCollection();
                querycol.Add(string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_update_notification @alert_name=N'{0}', @operator_name=N'{1}', @notification_method = {2}",
                                            SqlString(this.Name), SqlString(operatorName), Enum.Format(typeof(NotifyMethods), notifymethod, "d")));
                this.ExecutionManager.ExecuteNonQuery(querycol);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.UpdateNotification, this, e);
            }
        }

        /// <summary>
        /// EnumNotifications method returns a DataTable object that enumerates 
        /// notification operators for a given alert.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumNotifications()
        {
            return EnumNotifications(NotifyMethods.NotifyAll, string.Empty);
        }

        /// <summary>
        /// EnumNotifications method returns a DataTable object that enumerates 
        /// notification operators for a given alert.
        /// </summary>
        /// <param name="notifyMethod"></param>
        /// <returns></returns>
        public DataTable EnumNotifications(NotifyMethods notifyMethod)
        {
            return EnumNotifications(notifyMethod, string.Empty);
        }

        /// <summary>
        /// EnumNotifications method returns a DataTable object that enumerates 
        /// notification operators for a given alert.
        /// </summary>
        /// <param name="operatorName"></param>
        /// <returns></returns>
        public DataTable EnumNotifications(string operatorName)
        {
            return EnumNotifications(NotifyMethods.NotifyAll, operatorName);
        }

        /// <summary>
        /// EnumNotifications method returns a DataTable object that enumerates 
        /// notification operators for a given alert.
        /// </summary>
        /// <param name="notifyMethod"></param>
        /// <param name="operatorName"></param>
        /// <returns></returns>
        public DataTable EnumNotifications(NotifyMethods notifyMethod, string operatorName)
        {
            try
            {
                string urn = this.Urn.Value + "/Notification";
                bool openBracket = false;
                switch (notifyMethod)
                {
                    case NotifyMethods.NotifyEmail:
                        urn += "[@UseEmail=1";
                        openBracket = true;
                        break;

                    case NotifyMethods.Pager:
                        urn += "[@UsePager=1";
                        openBracket = true;
                        break;

                    case NotifyMethods.NetSend:
                        urn += "[@UseNetSend=1";
                        openBracket = true;
                        break;

                    case NotifyMethods.NotifyAll: // no need to set a filter
                        break;

                    case NotifyMethods.None: goto default;
                    default:
                        return null;
                }

                if (null != operatorName && 0 < operatorName.Length) // parameter is supplied
                {
                    if (openBracket)
                    {
                        urn += " and ";
                    }
                    else
                    {
                        urn += "[";
                        openBracket = true;
                    }
                    urn += "@OperatorName='";
                    urn += Urn.EscapeString(operatorName);
                    urn += "'";
                }

                if (openBracket)
                {
                    urn += "]";
                }

                Request req = new Request(urn);
                req.OrderByList = new OrderBy[] { new OrderBy("OperatorName", OrderBy.Direction.Asc) };
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumNotifications, this, e);
            }
        }

        /// <summary>
        /// Generate object creation script using default scripting options
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting options
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

    }
    /// <summary>
    /// Notification type
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum NotifyType
    {
        /// <summary>
        /// Return all operators or alerts.
        /// </summary>
        All = 1,
        /// <summary>
        /// Return only those operators or alerts configured for notification.
        /// </summary>
        Actual = 2,
        /// <summary>
        /// Return a result set that enumerates notification for the operator 
        /// or alert specified in the alertOrOperator argument.
        /// </summary>
        Target = 3
    }

}


