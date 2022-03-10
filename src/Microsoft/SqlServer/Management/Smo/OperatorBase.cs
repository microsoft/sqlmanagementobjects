// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public partial class Operator : AgentObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IAlterable, Cmn.IRenamable, IScriptable
    {
        internal Operator(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "Operator";
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
                createQuery.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_OPERATOR,
                    "NOT",
                    FormatFullNameForScripting(sp, false));
                createQuery.Append(sp.NewLine);
            }

            createQuery.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_add_operator @name=N'{0}'", SqlString(this.Name));
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
                        string alertName = Convert.ToString(row["AlertName"], SmoApplication.DefaultCulture);
                        string addNotification = String.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_add_notification @alert_name=N'{0}', @operator_name=N'{1}', @notification_method =", SqlString(alertName), SqlString(this.Name));

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
            bool isScriptingTargetManagedInstance =
                Cmn.DatabaseEngineEdition.SqlManagedInstance == sp.TargetDatabaseEngineEdition ||
                (this.ServerInfo != null &&
                this.ServerInfo.DatabaseEngineEdition == Cmn.DatabaseEngineEdition.SqlManagedInstance);

            GetBoolParameter( sb, sp, "Enabled", "@enabled={0}", ref count);
            GetTimeSpanParameterAsInt( sb, sp, "WeekdayPagerStartTime", "@weekday_pager_start_time={0}", ref count);
            GetTimeSpanParameterAsInt( sb, sp, "WeekdayPagerEndTime", "@weekday_pager_end_time={0}", ref count);
            GetTimeSpanParameterAsInt( sb, sp, "SaturdayPagerStartTime", "@saturday_pager_start_time={0}", ref count);
            GetTimeSpanParameterAsInt( sb, sp, "SaturdayPagerEndTime", "@saturday_pager_end_time={0}", ref count);
            GetTimeSpanParameterAsInt( sb, sp, "SundayPagerStartTime", "@sunday_pager_start_time={0}", ref count);
            GetTimeSpanParameterAsInt( sb, sp, "SundayPagerEndTime", "@sunday_pager_end_time={0}", ref count);
            
            // Don't script Pager options - not currently supported for Managed Instances
            //
            if (!isScriptingTargetManagedInstance)
            {
                GetEnumParameter(sb, sp, "PagerDays", "@pager_days={0}", typeof(WeekDays), ref count);
            }
            GetStringParameter( sb, sp, "EmailAddress", "@email_address=N'{0}'" , ref count);
            GetStringParameter( sb, sp, "PagerAddress", "@pager_address=N'{0}'" , ref count);
            GetStringParameter( sb, sp, "CategoryName", "@category_name=N'{0}'" , ref count);
            GetStringParameter( sb, sp, "NetSendAddress", "@netsend_address=N'{0}'" , ref count);
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
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_AGENT_OPERATOR,
                    "",
                    FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_delete_operator @name=N'{0}'", SqlString(this.Name));
            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            alterQuery.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_update_operator @name=N'{0}'", SqlString(this.Name));

            int count = 1;
            GetAllParams(alterQuery, sp, ref count);

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
            queries.Add( string.Format(SmoApplication.DefaultCulture,  "EXEC msdb.dbo.sp_update_operator @name=N'{0}', @new_name=N'{1}'",
                            SqlString(this.Name), SqlString(newName) ));
        }

        /// <summary>
        /// The AddNotification method associates operators with alerts. 
        /// Operators designated receive notification messages when an event 
        /// raising the alert occurs.
        /// </summary>
        /// <param name="alertName"></param>
        /// <param name="notifymethod"></param>
        public void AddNotification(string alertName, NotifyMethods notifymethod)
        {
            try
            {
                if( null == alertName )
                {
                    throw new ArgumentNullException("alertName");
                }

                StringCollection querycol = new StringCollection();
                querycol.Add( string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_add_notification @alert_name=N'{0}', @operator_name=N'{1}', @notification_method = {2}",
                                             SqlString(alertName), SqlString( this.Name ), Enum.Format( typeof(NotifyMethods), notifymethod, "d") ) );
                this.ExecutionManager.ExecuteNonQuery( querycol );
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddNotification, this, e);
            }
        }

        /// <summary>
        /// The RemoveNotification method drops all SQLServerAgent alert 
        /// notification assignments for an operator.
        /// </summary>
        /// <param name="alertName"></param>
        public void RemoveNotification(string alertName )
        {
            try
            {
                if( null == alertName )
                {
                    throw new ArgumentNullException("alertName");
                }

                StringCollection querycol = new StringCollection();
                querycol.Add( string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_delete_notification @alert_name=N'{0}', @operator_name=N'{1}'",
                                            SqlString(alertName), SqlString( this.Name )) );
                this.ExecutionManager.ExecuteNonQuery( querycol );
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemoveNotification, this, e);
            }
            
        }

        /// <summary>
        /// The UpdateNotification method configures SQL Server Agent operator 
        /// notification for alerts raised.
        /// </summary>
        /// <param name="alertName"></param>
        /// <param name="notifymethod"></param>
        public void UpdateNotification(string alertName, NotifyMethods notifymethod)
        {
            try
            {
                if( null == alertName )
                {
                    throw new ArgumentNullException("alertName");
                }

                StringCollection querycol = new StringCollection();
                querycol.Add( string.Format(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_update_notification @alert_name=N'{0}', @operator_name=N'{1}', @notification_method = {2}",
                                            SqlString(alertName), SqlString( this.Name ), Enum.Format( typeof(NotifyMethods), notifymethod, "d") ) );
                this.ExecutionManager.ExecuteNonQuery( querycol );
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.UpdateNotification, this, e);
            }
        }

        /// <summary>
        /// EnumNotifications method returns a DataTable object that enumerates 
        /// notification alerts for a given operator.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumNotifications()
        {
            return EnumNotifications(NotifyMethods.NotifyAll, string.Empty);
        }

        /// <summary>
        /// EnumNotifications method returns a DataTable object that enumerates 
        /// notification alerts for a given operator.
        /// </summary>
        /// <param name="notifyMethod"></param>
        /// <returns></returns>
        public DataTable EnumNotifications(NotifyMethods notifyMethod)
        {
            return EnumNotifications(notifyMethod, string.Empty);
        }

        /// <summary>
        /// EnumNotifications method returns a DataTable object that enumerates 
        /// notification alerts for a given operator.
        /// </summary>
        /// <param name="alertName"></param>
        /// <returns></returns>
        public DataTable EnumNotifications(string alertName)
        {
            return EnumNotifications(NotifyMethods.NotifyAll,alertName);
        }

        /// <summary>
        /// EnumNotifications method returns a DataTable object that enumerates 
        /// notification alerts for a given operator.
        /// </summary>
        /// <param name="notifyMethod"></param>
        /// <param name="alertName"></param>
        /// <returns></returns>
        public DataTable EnumNotifications(NotifyMethods notifyMethod, string alertName)
        {
            try
            {
                string urn = this.Urn.Value + "/Notification";
                bool openBracket = false;
                switch( notifyMethod )
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

                if( null != alertName && 0 < alertName.Length ) // parameter is supplied
                {
                    if( openBracket )
                    {
                        urn += " and ";
                    }
                    else
                    {
                        urn += "[";
                        openBracket = true;
                    }
                    urn += "@AlertName='";
                    urn += Urn.EscapeString(alertName);
                    urn += "'";
                }

                if( openBracket )
                {
                    urn += "]";
                }

                Request req = new Request( urn );
                req.OrderByList = new OrderBy[] {new OrderBy("AlertName", OrderBy.Direction.Asc) };
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch(Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumNotifications, this, e);
            }
        }

        /// <summary>
        /// The EnumJobNotifications method returns a DataTable object that 
        /// enumerates notifications made by SQLServerAgent on completion 
        /// of job execution.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumJobNotifications()
        {
            try
            {
                Request req = new Request( this.Urn.Value + "/JobNotification" );
                req.OrderByList = new OrderBy[] {new OrderBy("JobName", OrderBy.Direction.Asc) };
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch(Exception e)
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

    
}

