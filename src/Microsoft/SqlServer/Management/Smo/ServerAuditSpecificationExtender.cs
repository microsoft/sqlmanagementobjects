// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliant(false)]
    public class ServerAuditSpecificationExtender : SmoObjectExtender<ServerAuditSpecification>, ISfcValidate
    {
        StringCollection audits;
        DataTable auditSpecDetails;
        ValidationState gridValidationState;

        public ServerAuditSpecificationExtender() : base() { }
        public ServerAuditSpecificationExtender(ServerAuditSpecification serverAuditSpecification) : base(serverAuditSpecification) { }

        [ExtendedPropertyAttribute()]
        public StringCollection Audits
        {
            get
            {
                if (this.audits == null)
                {
                    this.audits = new StringCollection();
                    Server server = this.Parent.Parent;
                    if (server != null)
                    {
                        Urn urn = "Server/Audit";
                        string[] fields = new string[] { "Name" };
                        Request req = new Request(urn, fields);
                        DataTable dt = new Enumerator().Process(server.ConnectionContext, req);
                        foreach (DataRow dr in dt.Rows)
                        {
                            this.audits.Add(dr["Name"].ToString());
                        }
                    }
                }
                return this.audits;
            }
        }

        [ExtendedPropertyAttribute()]
        public DataTable AuditSpecificationDetails
        {
            get
            {
                if (auditSpecDetails == null)
                {
                    Urn urn = new Urn(this.Parent.Urn.Value + "/ServerAuditSpecificationDetail");
                    string[] fields = new string[] { "AuditActionType",
                                                    "ObjectClass",
                                                    "ObjectSchema",
                                                    "ObjectName",
                                                    "Principal"
                                                    };
                    Request req = new Request(urn, fields);
                    auditSpecDetails = this.Parent.ExecutionManager.GetEnumeratorData(req);
                }
                return auditSpecDetails;
            }
            set
            {
                if (this.Parent.State == SqlSmoState.Creating)
                {
                    List<AuditSpecificationDetail> auditSpecificationDetailsList = new List<AuditSpecificationDetail>();
                    foreach (DataRow row in value.Rows)
                    {
                        AuditSpecificationDetail auditSpecificationDetail = new AuditSpecificationDetail
                                                                                (
                                                                                (AuditActionType)AuditSpecificationDetail.StringToEnum[row["AuditActionType"].ToString()],
                                                                                row["ObjectClass"].ToString(),
                                                                                row["ObjectSchema"].ToString(),
                                                                                row["ObjectName"].ToString(),
                                                                                row["Principal"].ToString()
                                                                                );
                        auditSpecificationDetailsList.Add(auditSpecificationDetail);
                    }
                    this.Parent.AddAuditSpecificationDetail(auditSpecificationDetailsList);
                }
                auditSpecDetails = value;
            }
        }

        [ExtendedPropertyAttribute()]
        public ValidationState GridValidationState
        {
            get
            {
                if (gridValidationState == null)
                {
                    gridValidationState = new ValidationState();
                }
                return gridValidationState;
            }
            set
            {
                gridValidationState = value;
            }
        }

        [ExtendedPropertyAttribute()]
        public string Type { get { return "ServerAuditSpecification"; } }

        [ExtendedPropertyAttribute()]
        public SqlSmoState State { get { return this.Parent.State; } }

        [ExtendedPropertyAttribute()]
        public string DatabaseName { get { return "master"; } }

        [ExtendedPropertyAttribute()]
        public ServerConnection ConnectionContext { get { return this.Parent.Parent.ConnectionContext; } }

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            if (string.IsNullOrEmpty(this.Parent.Name))
            {
                return new ValidationState(ExceptionTemplates.EnterName, "Name");
            }
            if (string.IsNullOrEmpty(this.Parent.AuditName))
            {
                return new ValidationState(ExceptionTemplates.EnterServerAudit, "AuditName");
            }

            return GridValidationState;
        }
    }
}
