// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    public class AuditSpecification : ScriptNameObjectBase, ICreatable, IAlterable, IDroppable, IDropIfExists, IScriptable
    {

        internal AuditSpecification(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
            isInitialized = false;
        }

        protected internal AuditSpecification()
            : base()
        {
            isInitialized = false;
        }

        /// <summary>
        /// Name of the Audit Specification
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// If Audit Specification Details need to be refreshed.
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Stores enumerated audit specification details as it is an expensive operation.
        /// </summary>
        private List<AuditSpecificationDetail> enumAuditSpecDetails;

        private List<AuditSpecificationDetail> auditSpecificationDetailsList;

        /// <summary>
        /// Used for storing those Audit Specification Details which are yet to be persisted into the server.
        /// Is useful only at create time.
        /// </summary>
        private List<AuditSpecificationDetail> AuditSpecificationDetailsList
        {
            get
            {
                if (this.auditSpecificationDetailsList == null)
                {
                    this.auditSpecificationDetailsList = new List<AuditSpecificationDetail>();
                }
                return this.auditSpecificationDetailsList;
            }
            set
            {
                this.auditSpecificationDetailsList = value;
            }
        }

        public ICollection<AuditSpecificationDetail> EnumAuditSpecificationDetails()
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            if (this.State == SqlSmoState.Creating)
            {
                return null;
            }
            CheckObjectState();

            if (this.IsDesignMode)
            {
                enumAuditSpecDetails = new List<AuditSpecificationDetail>();
                foreach (AuditSpecificationDetail auditSpecDetail in this.AuditSpecificationDetailsList)
                {
                    enumAuditSpecDetails.Add(auditSpecDetail);
                }
            }
            else
            {                
                if (!isInitialized)
                {
                    Debug.Assert(this.Urn.Value != null, "The Urn value is null");
                    Urn urn = new Urn(this.Urn.Value + "/" + this.Urn.Type + "Detail");
                    Request req = new Request(urn);
                    DataTable dt = this.ExecutionManager.GetEnumeratorData(req);
                    enumAuditSpecDetails = new List<AuditSpecificationDetail>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        enumAuditSpecDetails.Add(new AuditSpecificationDetail((AuditActionType)AuditSpecificationDetail.StringToEnum[dr["AuditActionType"].ToString().Trim()],
                                                                        dr["ObjectClass"].ToString(),
                                                                        dr["ObjectSchema"].ToString(),
                                                                        dr["ObjectName"].ToString(),
                                                                        dr["Principal"].ToString()
                                                                        ));
                    }
                    isInitialized = true;
                }                
            }
            return enumAuditSpecDetails;
        }

        /// <summary>
        /// Creates a new Server/Database Audit Specification with no actions to be audited
        /// </summary>
        public void Create()
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            base.CreateImpl();
        }

        /// <summary>
        /// Alter an existing Server/Database Audit Specification - Alters the AuditName
        /// </summary>
        public void Alter()
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            base.AlterImpl();
        }

        /// <summary>
        /// Drops an existing Server/Database Audit Specification
        /// </summary>
        public void Drop()
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            base.DropImpl(true);
        }

        /// <summary>
        /// Refresh a Server/Database Audit Specification
        /// </summary>
        public override void Refresh()
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            this.isInitialized = false;
            base.Refresh();
        }

        /// <summary>
        /// Script a Server/Database Audit Specification
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            return base.ScriptImpl();
        }

        /// <summary>
        /// Script a Server/Database Audit Specification
        /// </summary>
        /// <param name="scriptingOptions">Options to script the Server/Database Audit Specification</param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            return base.ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Add an Audit Specification Detail to the Audit Specification. 
        /// </summary>
        /// <param name="auditSpecificationDetail"></param>
        public void AddAuditSpecificationDetail(AuditSpecificationDetail auditSpecificationDetail)
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            if (this.State == SqlSmoState.Existing && !this.IsDesignMode)
            {
                try
                {
                    List<AuditSpecificationDetail> list = new List<AuditSpecificationDetail>();
                    list.Add(auditSpecificationDetail);
                    StringCollection query = AddRemoveAuditSpecificationDetail(list, true, true);
                    this.ExecutionManager.ExecuteNonQuery(query);
                    isInitialized = false;
                }
                catch (Exception e)
                {
                    FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.AddAuditSpecificationDetail, this, e);
                }
            }
            else
            {
                this.AuditSpecificationDetailsList.Add(auditSpecificationDetail);
            }
        }

        /// <summary>
        /// Adds a collection of Audit Specification Details to the Audit Specification
        /// </summary>
        /// <param name="auditSpecificationDetails"></param>
        public void AddAuditSpecificationDetail(ICollection<AuditSpecificationDetail> auditSpecificationDetails)
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            if (this.State == SqlSmoState.Existing && !this.IsDesignMode)
            {
                try
                {
                    StringCollection query = AddRemoveAuditSpecificationDetail(auditSpecificationDetails, true, true);
                    this.ExecutionManager.ExecuteNonQuery(query);
                    isInitialized = false;
                }
                catch (Exception e)
                {
                    FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.AddAuditSpecificationDetail, this, e);
                }
            }
            else
            {
                foreach (AuditSpecificationDetail detail in auditSpecificationDetails)
                {
                    this.AuditSpecificationDetailsList.Add(detail);
                }
            }
        }

        /// <summary>
        /// Remove an Audit Specification Detail from the Audit Specification
        /// </summary>
        /// <param name="auditSpecificationDetail"></param>
        public void RemoveAuditSpecificationDetail(AuditSpecificationDetail auditSpecificationDetail)
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            if (this.State == SqlSmoState.Existing && !this.IsDesignMode)
            {
                try
                {
                    List<AuditSpecificationDetail> list = new List<AuditSpecificationDetail>();
                    list.Add(auditSpecificationDetail);
                    StringCollection query = AddRemoveAuditSpecificationDetail(list, false, true);
                    this.ExecutionManager.ExecuteNonQuery(query);
                    isInitialized = false;
                }
                catch (Exception e)
                {
                    FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.RemoveAuditSpecificationDetail, this, e);
                }
            }
            else
            {
                if (this.AuditSpecificationDetailsList != null)
                {
                    this.AuditSpecificationDetailsList.Remove(auditSpecificationDetail);
                }
            }
        }

        /// <summary>
        /// Removes a collection of Audit Specification Details from the Audit Specification
        /// </summary>
        /// <param name="auditSpecificationDetails"></param>
        public void RemoveAuditSpecificationDetail(ICollection<AuditSpecificationDetail> auditSpecificationDetails)
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            if (this.State == SqlSmoState.Existing && !this.IsDesignMode)
            {
                try
                {
                    StringCollection query = AddRemoveAuditSpecificationDetail(auditSpecificationDetails, false, true);
                    this.ExecutionManager.ExecuteNonQuery(query);
                    isInitialized = false;
                }
                catch (Exception e)
                {
                    FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.AddAuditSpecificationDetail, this, e);
                }
            }
            else
            {
                if (this.AuditSpecificationDetailsList != null)
                {
                    foreach (AuditSpecificationDetail detail in auditSpecificationDetails)
                    {
                        this.AuditSpecificationDetailsList.Remove(detail);
                    }
                }

            }
        }

        /// <summary>
        /// Add/Remove a collection of Audit Specification Details to/from the Audit Specification
        /// </summary>
        /// <param name="auditSpecificationDetails"></param>
        /// <param name="add"></param>
        /// <param name="useDb"></param>
        /// <returns>script to add/remove details</returns>
        private StringCollection AddRemoveAuditSpecificationDetail(ICollection<AuditSpecificationDetail> auditSpecificationDetails, bool add, bool useDb)
        {
            CheckObjectState();
            if (auditSpecificationDetails.Count < 1)
            {
                throw new ArgumentNullException("auditSpecificationDetails");
            }
            string type = this.GetType().InvokeMember("ParentType",
                System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetProperty,
                null, null, new object[] { }, SmoApplication.DefaultCulture) as string;
            StringBuilder sb = new StringBuilder();
            StringCollection query = new StringCollection();

            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER {0} AUDIT SPECIFICATION {1}", type, FullQualifiedName);

            sb.Append(ScriptAddDropAuditActionTypePart(auditSpecificationDetails, add));

            if (useDb)
            {
                AddDatabaseContext(query, new ScriptingPreferences(this));
            }
            query.Add(sb.ToString());
            return query;
        }

        /// <summary>
        /// Generates the script to create an Audit Specification with no actions to audit.
        /// If the audit specification already exists, it will script the actions added too.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion100(sp.TargetServerVersion);
            string type = this.GetType().InvokeMember("ParentType",
                System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetProperty,
                null, null, new object[] { }, SmoApplication.DefaultCulture) as string;

            StringBuilder sb = new StringBuilder();
            Property auditName = this.Properties.Get("AuditName");
            if (auditName.IsNull)
            {
                throw new PropertyNotSetException("AuditName");
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AUDIT_SPECIFICATION, "NOT", type.ToLower(SmoApplication.DefaultCulture), FormatFullNameForScripting(sp, false));
                sb.AppendLine();
                sb.Append(Scripts.BEGIN);
                sb.AppendLine();
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE {0} AUDIT SPECIFICATION {1}", type, FullQualifiedName);
            sb.AppendLine();
            sb.AppendFormat(SmoApplication.DefaultCulture, "FOR SERVER AUDIT {0}", MakeSqlBraket(auditName.Value.ToString()));

            if (this.State != SqlSmoState.Creating && !this.IsDesignMode)
            {
                //Do a shallow copy since we don't want modifications of this to affect
                //the list returned from EnumAuditSpecificationDetails
                this.AuditSpecificationDetailsList = new List<AuditSpecificationDetail>(this.EnumAuditSpecificationDetails());
            }

            if (this.AuditSpecificationDetailsList != null)
            {
                sb.Append(ScriptAddDropAuditActionTypePart(this.AuditSpecificationDetailsList, true));
            }

            Property enabled = this.Properties.Get("Enabled");
            if (!enabled.IsNull) // checking state for the case of Create Audit Specification Script Generation
            {
                sb.AppendLine();
                sb.AppendFormat(SmoApplication.DefaultCulture, "WITH (STATE = {0})", (bool)enabled.Value ? "ON" : "OFF");
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendLine();
                sb.Append(Scripts.END);
            }

            query.Add(sb.ToString());

            if (this.AuditSpecificationDetailsList != null && !this.IsDesignMode)
            {
                this.AuditSpecificationDetailsList.Clear();
            }
        }

        private string ScriptAddDropAuditActionTypePart(ICollection<AuditSpecificationDetail> auditSpecificationDetails, bool add)
        {
            StringBuilder sb = new StringBuilder();
            AuditActionTypeConverter converter = new AuditActionTypeConverter();
            int i = 0;
            foreach (AuditSpecificationDetail detail in auditSpecificationDetails)
            {
                i++;
                sb.AppendLine();
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0} ({1}", add ? "ADD" : "DROP", converter.ConvertToInvariantString(detail.Action));
                string name = string.Empty;
                if (!string.IsNullOrEmpty(detail.ObjectClass))
                {
                    name += detail.ObjectClass + "::";
                }
                if (!string.IsNullOrEmpty(detail.ObjectSchema))
                {
                    name += MakeSqlBraket(detail.ObjectSchema) + ".";
                }
                if (!string.IsNullOrEmpty(detail.ObjectName))
                {
                    name += MakeSqlBraket(detail.ObjectName);
                }
                if (!string.IsNullOrEmpty(name))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " ON {0}", name);
                    if (!string.IsNullOrEmpty(detail.Principal))
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " BY {0}", MakeSqlBraket(detail.Principal));
                    }
                }
                sb.Append(Globals.RParen);
                if (i < auditSpecificationDetails.Count)
                {
                    sb.Append(Globals.comma);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates the script to drop an Audit Specification
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        internal override void ScriptDrop(StringCollection query, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion100(sp.TargetServerVersion);
            string type = this.GetType().InvokeMember("ParentType",
                System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetProperty,
                null, null, new object[] { }, SmoApplication.DefaultCulture) as string;
            StringBuilder dropQuery = new StringBuilder();
            if (sp.IncludeScripts.ExistenceCheck)
            {
                dropQuery.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AUDIT_SPECIFICATION, "", type.ToLower(SmoApplication.DefaultCulture), FormatFullNameForScripting(sp, false));
                dropQuery.AppendLine();
            }
            dropQuery.AppendFormat(SmoApplication.DefaultCulture, "DROP {0} AUDIT SPECIFICATION {1}", type, FullQualifiedName);

            query.Add(dropQuery.ToString());
        }

        /// <summary>
        /// Generates the script to alter an Audit Specification
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            string type = this.GetType().InvokeMember("ParentType",
                System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetProperty,
                null, null, new object[] { }, SmoApplication.DefaultCulture) as string;
            bool alter = false;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER {0} AUDIT SPECIFICATION {1}", type, FullQualifiedName);
            Property auditName = this.Properties.Get("AuditName");
            //audit name can't be null or empty.
            if (auditName.Dirty && !string.IsNullOrEmpty(auditName.Value as string))
            {
                sb.AppendLine();
                sb.AppendFormat(SmoApplication.DefaultCulture, "FOR SERVER AUDIT {0}", MakeSqlBraket(auditName.Value.ToString()));
                alter = true;
            }
            if (alter)
            {
                query.Add(sb.ToString());
            }
        }


        /// <summary>
        /// Enable an Audit Specification
        /// </summary>
        public void Enable()
        {
            EnableDisable(true);
        }

        /// <summary>
        /// Disable an Audit Specification
        /// </summary>
        public void Disable()
        {
            EnableDisable(false);
        }

        /// <summary>
        /// Enable or Disable an Audit Specification
        /// </summary>
        /// <param name="enable">true enables the audit specification</param>
        private void EnableDisable(bool enable)
        {
            // Since this is the base class of ServerAuditSpecification and DatabaseAuditSpecification,
            // and both introduced in Katmai, adding a check for one type will be enough
            this.ThrowIfNotSupported(typeof(DatabaseAuditSpecification));
            CheckObjectState();
            try
            {
                string type = this.GetType().InvokeMember("ParentType",
                    System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetProperty,
                    null, null, new object[] { }, SmoApplication.DefaultCulture) as string;

                if (!this.IsDesignMode)
                {
                    StringCollection query = new StringCollection();

                    AddDatabaseContext(query, new ScriptingPreferences(this));

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER {0} AUDIT SPECIFICATION {1}", type, FullQualifiedName);
                    sb.AppendLine();

                    if (enable)
                    {
                        sb.Append("WITH (STATE = ON)");
                    }
                    else
                    {
                        sb.Append("WITH (STATE = OFF)");
                    }

                    query.Add(sb.ToString());
                    ExecutionManager.ExecuteNonQuery(query);
                }

                Property p = this.Properties.Get("Enabled");
                p.SetValue(enable);
                p.SetRetrieved(true);
                if (!this.ExecutionManager.Recording)
                {
                    if (!SmoApplication.eventsSingleton.IsNullObjectAltered())
                    {
                        SmoApplication.eventsSingleton.CallObjectAltered(GetServerObject(), new ObjectAlteredEventArgs(this.Urn, this));
                    }
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                if (enable)
                {
                    throw new FailedOperationException(ExceptionTemplates.Enable, this, e);
                }
                else
                {
                    throw new FailedOperationException(ExceptionTemplates.Disable, this, e);
                }
            }
        }
    }




    public struct AuditSpecificationDetail
    {
        AuditActionType action;
        string objectClass;
        string objectName;
        string objectSchema;
        string principal;

        public AuditActionType Action { get { return action; } }
        public string ObjectClass { get { return objectClass; } }
        public string ObjectName { get { return objectName; } }
        public string ObjectSchema { get { return objectSchema; } }
        public string Principal { get { return principal; } }

        public AuditSpecificationDetail(AuditActionType action, string objectClass, string objectSchema, string objectName, string principal)
        {
            this.action = action;
            this.objectClass = objectClass;
            this.objectSchema = objectSchema;
            this.objectName = objectName;
            this.principal = principal;
        }

        public AuditSpecificationDetail(AuditActionType action, string objectSchema, string objectName, string principal)
        {
            this.action = action;
            this.objectClass = string.Empty;
            this.objectSchema = objectSchema;
            this.objectName = objectName;
            this.principal = principal;
        }

        public AuditSpecificationDetail(AuditActionType action, string objectName, string principal)
        {
            this.action = action;
            this.objectClass = string.Empty;
            this.objectSchema = string.Empty;
            this.objectName = objectName;
            this.principal = principal;
        }

        public AuditSpecificationDetail(AuditActionType action)
        {
            this.action = action;
            this.objectClass = string.Empty;
            this.objectSchema = string.Empty;
            this.objectName = string.Empty;
            this.principal = string.Empty;
        }

        private static readonly Hashtable stringToEnum = new Hashtable();

        /// <summary>
        /// Returns a hashtable whose keys are the SQL strings representing AuditActionType values and whose values are the AuditActionType enum values
        /// </summary>
        public static Hashtable StringToEnum
        {
            get
            {
                if (stringToEnum.Count == 0)
                {
                    AuditActionTypeConverter converter = new AuditActionTypeConverter();
                    foreach (int i in Enum.GetValues(typeof(AuditActionType)))
                    {
                        stringToEnum.Add(converter.ConvertToInvariantString((AuditActionType)i), (AuditActionType)i);
                    }
                }
                return stringToEnum;
            }
        }
    }
}
