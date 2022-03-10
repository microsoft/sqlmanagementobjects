// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class ServerDdlTrigger : ServerOwnedObject<Smo.ServerDdlTrigger>, IServerDdlTrigger
    {
        private IExecutionContext m_executionContext;
        private TriggerEventTypeSet m_serverEventSet;

        public ServerDdlTrigger(Smo.ServerDdlTrigger smoMetadataObject, Server parent)
            : base(smoMetadataObject, parent)
        {
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return this.m_smoMetadataObject.IsSystemObject; }
        }

        public override T Accept<T>(IServerOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region IServerDdlTrigger Members

        public ITriggerEventTypeSet ServerDdlEvents
        {
            get 
            {
                if (this.m_serverEventSet == null)
                {
                    if (this.m_serverEventSet == null)
                    {
                        this.m_serverEventSet = Utils.DdlTrigger.GetServerTriggerEvents(this.m_smoMetadataObject);
                    }

                    Debug.Assert(this.m_serverEventSet != null, "SmoMetadataProvider Assert", "this.m_serverEventSet != null");
                    return this.m_serverEventSet;
                }

                return this.m_serverEventSet;
            }
        }

        public bool IsQuotedIdentifierOn
        {
            get { return this.m_smoMetadataObject.QuotedIdentifierStatus; }
        }

        #endregion

        #region ITrigger Members

        public string BodyText
        {
            get { return this.m_smoMetadataObject.TextBody; }
        }

        public bool IsEncrypted
        {
            get { return this.m_smoMetadataObject.IsEncrypted; }
        }

        public bool IsEnabled
        {
            get { return this.m_smoMetadataObject.IsEnabled; }
        }

        public bool IsSqlClr
        {
            get { return this.m_smoMetadataObject.ImplementationType == Smo.ImplementationType.SqlClr; }
        }

        public IExecutionContext ExecutionContext
        {
            get
            {
                if (this.m_executionContext == null)
                {
                    IServer server = this.Server;
                    this.m_executionContext = Utils.GetExecutionContext(server, this.m_smoMetadataObject);
                }

                Debug.Assert(this.m_executionContext != null, "SmoMetadataProvider Assert", "executionContext != null");

                return this.m_executionContext;
            }
        }

        #endregion
    }
}
