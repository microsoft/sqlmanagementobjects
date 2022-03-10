// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliant(false)]
    public class AuditExtender : SmoObjectExtender<Audit>, ISfcValidate
    {
        public AuditExtender() : base() { }
        public AuditExtender(Audit audit) : base(audit) { }

        [ExtendedPropertyAttribute()]
        public SqlSmoState State { get { return this.Parent.State; } }

        [ExtendedPropertyAttribute()]
        public ServerConnection ConnectionContext { get { return this.Parent.Parent.ConnectionContext; } }

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            if (string.IsNullOrEmpty(this.Parent.Name))
            {
                return new ValidationState(ExceptionTemplates.EnterAuditName, "Name");
            }
            if (this.Parent.DestinationType == AuditDestinationType.File)
            {
                if (string.IsNullOrEmpty(this.Parent.FilePath))
                {
                    return new ValidationState(ExceptionTemplates.EnterFilePath, "FilePath");
                }
                if (this.Parent.ReserveDiskSpace && this.Parent.MaximumFileSize == 0) //Maximum File Size is Unlimited
                {
                    return new ValidationState(ExceptionTemplates.ReserveDiskSpaceNotAllowedWhenMaxFileSizeIsUnlimited, "ReserveDiskSpace");
                }
            }
            else if (this.Parent.DestinationType == AuditDestinationType.Url && string.IsNullOrEmpty(this.Parent.FilePath))
            {
                return new ValidationState(ExceptionTemplates.EnterStoragePath, "Path");
            }

            return this.Parent.Validate(methodName, arguments);
        }

        #endregion
    }
}
