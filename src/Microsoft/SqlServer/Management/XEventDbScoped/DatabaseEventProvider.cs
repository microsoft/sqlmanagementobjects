// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlServer.Management.XEventDbScoped
{
    /// <summary>
    /// Sql provider for Event.
    /// </summary>
    internal class DatabaseEventProvider : IEventProvider
    {
        private Event xevent = null;

        public DatabaseEventProvider(Event parent)
        {
            this.xevent = parent;
        }

        /// <summary>
        /// Script Create for the Event.
        /// </summary>
        /// <returns>Event Create script.</returns>
        public string GetCreateScript()
        {
            StringBuilder sb = new StringBuilder(128);

            sb.Append(string.Format(CultureInfo.InvariantCulture, "ADD EVENT {0}", this.xevent.ScriptName));

            StringBuilder actionsBuilder = new StringBuilder();
            int count = 0;
            foreach (Action action in this.xevent.Actions)
            {
                if (XEUtils.ToBeCreated(action.State))
                {
                    count++;
                    actionsBuilder.Append(action.GetScriptCreate() + ",");
                }
            }

            // '()' statement is added if the event has customizable fields or actions or preidcate
            if (this.xevent.HasCustomizableField() || count > 0 || (this.xevent.PredicateExpression != null && string.Empty != this.xevent.PredicateExpression.Trim()))
            {
                sb.Append("(");

                // add the customizabel columns.
                if (this.xevent.HasCustomizableField())
                {
                    sb.Append("SET ");

                    // only those non-null field counts.
                    foreach (EventField field in this.xevent.EventFields)
                    {
                        if (field.Value != null)
                        {
                            BaseXEStore store = this.xevent.Parent.Parent;
                            EventColumnInfo columnInfo = store.ObjectInfoSet.Get<EventInfo>(this.xevent.Name).EventColumnInfoSet[field.Name];
                            sb.Append(
                                string.Format(
                                CultureInfo.InvariantCulture, 
                                "{0}={1},", 
                                field.Name, 
                                store.FormatFieldValue(field.Value.ToString(), columnInfo.TypePackageID, columnInfo.TypeName)));
                        }
                    }

                    // remove the last comma for set statement
                    sb.Remove(sb.Length - 1, 1);
                }

                if (count > 0)
                {
                    sb.AppendLine();
                    
                    // add some spaces to indent action
                    sb.Append("    ACTION(");   
                    sb.Append(actionsBuilder);
                    
                    // remove the last comma for action statement
                    sb.Remove(sb.Length - 1, 1);

                    // close for action statement
                    sb.Append(")");
                }

                // append predicate
                if (null != this.xevent.PredicateExpression && string.Empty != this.xevent.PredicateExpression.Trim())
                {
                    sb.AppendLine();

                    // add some spaces to indent predicate
                    sb.Append("    WHERE ");    
                    sb.Append(this.xevent.PredicateExpression);
                }

                // close for the whole set and action statement.
                sb.Append(")");
            }

            return sb.ToString();            
        }

        /// <summary>
        /// Scripts Drop for this event.
        /// </summary>
        /// <returns>Event Drop script.</returns>
        public string GetDropScript()
        {
            return string.Format(CultureInfo.InvariantCulture, "DROP EVENT {0}", this.xevent.ScriptName);                      
        }
    }
}