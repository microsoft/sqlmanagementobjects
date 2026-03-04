// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// XEStore is the root for all metadata classes and runtime classes.
    /// </summary>
    public abstract partial class BaseXEStore
    {
        /// <summary>
        /// Saves the session to template.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
        /// <exception cref="System.IO.IOException">The fileName includes an incorrect or invalid syntax for file name, directory name, or volume label syntax.</exception>
        /// <exception cref="Microsoft.SqlServer.Management.XEvent.XEventException">Parameters are wrong or failed to save session.</exception>
        public static void SaveSessionToTemplate(Session session, string fileName, bool overwrite)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("SaveSessionToTemplate"))
            {
                tm.TraceParameterIn("session", session);
                tm.TraceParameterIn("fileName", fileName);
                tm.TraceParameterIn("overwrite", overwrite);

                if (string.IsNullOrEmpty(fileName))
                {
                    throw new XEventException(ExceptionTemplates.InvalidParameter(fileName));
                }

                if (!overwrite && File.Exists(fileName))
                {
                    throw new XEventException(ExceptionTemplates.FileAlreadyExists);
                }

                if (session.State != SfcObjectState.Existing)
                {
                    throw new XEventException(ExceptionTemplates.SaveTemplateForExistingSession);
                }

                XmlDocument doc = new XmlDocument();

                XmlElement sessionsElement = doc.CreateElement("event_sessions");
                XmlAttribute namespaceAttr = doc.CreateAttribute("xmlns");
                namespaceAttr.InnerText = BaseXEStore.NameSpace;
                sessionsElement.Attributes.Append(namespaceAttr);

                XmlElement sessionElement = doc.CreateElement("event_session");
                XmlAttribute nameAttr = doc.CreateAttribute("name");
                nameAttr.InnerText = session.Name;
                sessionElement.Attributes.Append(nameAttr);
                XmlAttribute memoryAttr = doc.CreateAttribute("maxMemory");

                // convert MaxMemory from KB to MB as defined in the XML schema
                memoryAttr.InnerText = (session.MaxMemory / 1024).ToString(CultureInfo.InvariantCulture);

                sessionElement.Attributes.Append(memoryAttr);
                XmlAttribute retenionAttr = doc.CreateAttribute("eventRetentionMode");
                retenionAttr.InnerText = XEUtils.ConvertToXsdEnumerationValue(session.EventRetentionMode);
                sessionElement.Attributes.Append(retenionAttr);

                XmlAttribute trackCausalityAttr = doc.CreateAttribute("trackCausality");
                trackCausalityAttr.InnerText = session.TrackCausality ? "true" : "false";
                sessionElement.Attributes.Append(trackCausalityAttr);

                XmlAttribute maxDispatchLatencyAttr = doc.CreateAttribute("dispatchLatency");
                maxDispatchLatencyAttr.InnerText = session.MaxDispatchLatency.ToString(CultureInfo.InvariantCulture);
                sessionElement.Attributes.Append(maxDispatchLatencyAttr);

                // convert MaxEventSize from KB to MB as defined in the XML schema               
                XmlAttribute maxEventSizeAttr = doc.CreateAttribute("maxEventSize");
                maxEventSizeAttr.InnerText = (session.MaxEventSize / 1024).ToString(CultureInfo.InvariantCulture);
                sessionElement.Attributes.Append(maxEventSizeAttr);

                XmlAttribute memoryPartitionModeAttr = doc.CreateAttribute("memoryPartitionMode");
                memoryPartitionModeAttr.InnerText = XEUtils.ConvertToXsdEnumerationValue(session.MemoryPartitionMode);
                sessionElement.Attributes.Append(memoryPartitionModeAttr);

                foreach (Event evt in session.Events)
                {
                    XmlElement eventElement = doc.CreateElement("event");
                    AppendObjectAttributes<EventInfo>(session, doc, eventElement, evt.ModuleID, evt.PackageName, evt.Name);
                    foreach (Action action in evt.Actions)
                    {
                        XmlElement actionElement = doc.CreateElement("action");
                        AppendObjectAttributes<ActionInfo>(session, doc, actionElement, action.ModuleID, action.PackageName, action.Name);
                        eventElement.AppendChild(actionElement);
                    }

                    foreach (EventField evtField in evt.EventFields)
                    {
                        if (evtField.Value != null)
                        {
                            XmlElement fieldElement = doc.CreateElement("parameter");
                            AppendParameterAttributes(doc, fieldElement, evtField.Name, evtField.Value);
                            eventElement.AppendChild(fieldElement);
                        }
                    }

                    string predXml = evt.PredicateXml;
                    if (predXml != null && predXml.Trim() != string.Empty)
                    {
                        // can't use CDATA here since value element in PredicateXml may also contain CDATA
                        XmlElement predXmlElement = doc.CreateElement("predicate");
                        predXmlElement.InnerXml = predXml;
                        eventElement.AppendChild(predXmlElement);
                    }

                    sessionElement.AppendChild(eventElement);
                }

                foreach (Target target in session.Targets)
                {
                    XmlElement targetElement = doc.CreateElement("target");
                    AppendObjectAttributes<TargetInfo>(session, doc, targetElement, target.ModuleID, target.PackageName, target.Name);
                    foreach (TargetField targetField in target.TargetFields)
                    {
                        if (targetField.Value != null)
                        {
                            XmlElement fieldElement = doc.CreateElement("parameter");
                            AppendParameterAttributes(doc, fieldElement, targetField.Name, targetField.Value);
                            targetElement.AppendChild(fieldElement);
                        }
                    }

                    sessionElement.AppendChild(targetElement);
                }

                sessionsElement.AppendChild(sessionElement);
                doc.AppendChild(sessionsElement);

                XmlTextWriter writer = new XmlTextWriter(fileName, Encoding.UTF8);
                doc.WriteTo(writer);
                writer.Close();
            }
        }

        /// <summary>
        /// A wrapper for Session constructor to avoid accidentally passing an wrong parent.
        /// </summary>
        /// <param name="sessionName">Name of the session.</param>
        /// <returns>A new Session.</returns>
        public virtual Session CreateSession(string sessionName)
        {
            return new Session(this, sessionName);
        }

        /// <summary>
        /// Creates a session from template.
        /// </summary>
        /// <param name="sessionName">Name of the session.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <exception cref="System.UnauthorizedAccessException">The template file can't be accessed.</exception>
        /// <exception cref="System.Xml.XmlException">The template file is malformed.</exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaValidationException">The template file doesn't conform to the schema.</exception>
        /// <exception cref="Microsoft.SqlServer.Management.XEvent.XEventException">Parameters are wrong or failed to create session.</exception>
        /// <returns>A new Session.</returns>
        public virtual Session CreateSessionFromTemplate(string sessionName, string fileName)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("CreateSessionFromTemplate"))
            {
                tm.TraceParameterIn("sessionName", sessionName);
                tm.TraceParameterIn("fileName", fileName);

                if (string.IsNullOrEmpty(sessionName))
                {
                    throw new XEventException(ExceptionTemplates.InvalidParameter(sessionName));
                }

                if (!File.Exists(fileName))
                {
                    throw new XEventException(ExceptionTemplates.FileNotExist);
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(fileName);
                XmlSchema schema = XmlSchema.Read(new StringReader(XsdResource.xeconfig), null);
                doc.Schemas.Add(schema);
                doc.Validate(null);

                XmlElement sessionsElement = (XmlElement)doc.FirstChild;
                XmlElement sessionElement = (XmlElement)sessionsElement.FirstChild;
                Session session = new Session(this, sessionName);

                // convert maxMemory from MB (as defined in the XML schema) to KB
                session.MaxMemory = int.Parse(sessionElement.Attributes["maxMemory"].InnerText, CultureInfo.InvariantCulture) * 1024;

                session.EventRetentionMode =
                    (Session.EventRetentionModeEnum)Enum.Parse(typeof(Session.EventRetentionModeEnum), sessionElement.Attributes["eventRetentionMode"].InnerText, true);
                session.TrackCausality = sessionElement.Attributes["trackCausality"].InnerText == "true"
                    || sessionElement.Attributes["trackCausality"].InnerText == "1" ? true : false;
                session.MaxDispatchLatency = int.Parse(sessionElement.Attributes["dispatchLatency"].InnerText, CultureInfo.InvariantCulture);

                // convert maxEventSize from MB (as defined in the XML schema) to KB
                session.MaxEventSize = int.Parse(sessionElement.Attributes["maxEventSize"].InnerText, CultureInfo.InvariantCulture) * 1024;

                session.MemoryPartitionMode =
                    (Session.MemoryPartitionModeEnum)Enum.Parse(typeof(Session.MemoryPartitionModeEnum), sessionElement.Attributes["memoryPartitionMode"].InnerText, true);

                foreach (XmlNode node in sessionElement.ChildNodes)
                {
                    if (node.Name == "event")
                    {
                        XmlElement eventElement = (XmlElement)node;

                        string eventFullName = eventElement.Attributes["package"].InnerText
                            + "." + eventElement.Attributes["name"].InnerText;
                        if (eventElement.Attributes["module"] != null)
                        {
                            eventFullName = string.Format(CultureInfo.InvariantCulture, "[{0}].{1}", eventElement.Attributes["module"].InnerText, eventFullName);
                        }

                        Event evt = session.AddEvent(eventFullName);
                        foreach (XmlNode eventNode in eventElement.ChildNodes)
                        {
                            if (eventNode.Name == "action")
                            {
                                XmlElement actionElement = (XmlElement)eventNode;

                                string actionFullName = actionElement.Attributes["package"].InnerText + "." +
                                    actionElement.Attributes["name"].InnerText;

                                if (actionElement.Attributes["module"] != null)
                                {
                                    actionFullName = string.Format(CultureInfo.InvariantCulture, "[{0}].{1}", actionElement.Attributes["module"].InnerText, actionFullName);
                                }

                                evt.AddAction(actionFullName);
                            }
                            else if (eventNode.Name == "parameter")
                            {
                                XmlElement paramElement = (XmlElement)eventNode;
                                string fieldName = paramElement.Attributes["name"].InnerText;

                                EventColumnInfoCollection coll = this.ObjectInfoSet.Get<EventInfo>(eventFullName).EventColumnInfoSet;

                                if (!coll.Contains(fieldName))
                                {
                                    throw new XEventException(ExceptionTemplates.InvalidParameter(fieldName));
                                }

                                evt.EventFields[fieldName].Value = paramElement.Attributes["value"].InnerText; // when parsing, always create as a string
                            }
                            else if (eventNode.Name == "predicate")
                            {
                                PredExpr predExpr = PredExpr.ParsePredicateXml(this, ((XmlElement)eventNode).InnerXml);
                                evt.Predicate = predExpr;
                            }
                        }
                    }
                    else if (node.Name == "target")
                    {
                        XmlElement targetElement = (XmlElement)node;

                        string targetFullName = targetElement.Attributes["package"].InnerText
                            + "." + targetElement.Attributes["name"].InnerText;

                        if (targetElement.Attributes["module"] != null)
                        {
                            targetFullName =
                                string.Format(CultureInfo.InvariantCulture, "[{0}].{1}", targetElement.Attributes["module"].InnerText, targetFullName);
                        }

                        Target target = session.AddTarget(targetFullName);
                        foreach (XmlNode targetNode in targetElement.ChildNodes)
                        {
                            XmlElement paramElement = (XmlElement)targetNode;
                            string fieldName = paramElement.Attributes["name"].InnerText;
                            TargetColumnInfoCollection coll = this.ObjectInfoSet.Get<TargetInfo>(targetFullName).TargetColumnInfoSet;
                            if (!coll.Contains(fieldName))
                            {
                                throw new XEventException(ExceptionTemplates.InvalidParameter(fieldName));
                            }

                            target.TargetFields[fieldName].Value = paramElement.Attributes["value"].InnerText; // when parsing, always create as a string
                        }
                    }
                }

                return session;
            }
        }

        // Appends the object attributes to XmlElement. Objects include events, actions and targets.
        private static void AppendObjectAttributes<T>(Session session, XmlDocument doc, XmlElement element, Guid moduleID, string packageName, string objFullName)
            where T : SfcInstance, IXEObjectInfo
        {
            if (session.Parent.ObjectInfoSet.GetAll<T>(objFullName).Count > 1) 
            {
                // pkgName.objName is not unique
                XmlAttribute moduleAttr = doc.CreateAttribute("module");
                moduleAttr.InnerText = moduleID.ToString("D", CultureInfo.InvariantCulture);
                element.Attributes.Append(moduleAttr);
            }

            XmlAttribute packageAttr = doc.CreateAttribute("package");
            packageAttr.InnerText = packageName;
            element.Attributes.Append(packageAttr);

            XmlAttribute nameAttr = doc.CreateAttribute("name");
            nameAttr.InnerText = objFullName.Substring(objFullName.LastIndexOf('.') + 1); // remove object's package name
            element.Attributes.Append(nameAttr);
        }

        // Appends the parameter attributes to XmlElement. Parameters include event fields and target fields.
        private static void AppendParameterAttributes(XmlDocument doc, XmlElement element, string name, object value)
        {
            XmlAttribute nameAttr = doc.CreateAttribute("name");
            nameAttr.InnerText = name;
            element.Attributes.Append(nameAttr);
            XmlAttribute valueAttr = doc.CreateAttribute("value");
            if (value is bool)
            {
                valueAttr.InnerText = (bool)value ? "1" : "0";
            }
            else
            {
                valueAttr.InnerText = value.ToString();
            }

            element.Attributes.Append(valueAttr);
        }
    }
}
