// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class DatabaseDdlTrigger : DatabaseOwnedObject<Smo.DatabaseDdlTrigger>, IDatabaseDdlTrigger
    {
        private IExecutionContext m_executionContext;
        private TriggerEventTypeSet m_databaseEventSet;

        public DatabaseDdlTrigger(Smo.DatabaseDdlTrigger smoMetadataObject, Database parent)
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

        public override T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region IDatabaseDdlTrigger Members

        public ITriggerEventTypeSet DatabaseDdlEvents
        {
            get 
            {
                if (this.m_databaseEventSet == null)
                {
                    this.m_databaseEventSet = Utils.DdlTrigger.GetDatabaseTriggerEvents(this.m_smoMetadataObject);
                }

                Debug.Assert(this.m_databaseEventSet != null, "SmoMetadataProvider Assert", "this.m_databaseEventSet != null");
                return this.m_databaseEventSet;
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
                    IDatabase database = this.Database;
                    this.m_executionContext = Utils.GetExecutionContext(database, this.m_smoMetadataObject);
                }

                Debug.Assert(this.m_executionContext != null, "SmoMetadataProvider Assert", "executionContext != null");

                return this.m_executionContext;
            }
        }

        #endregion
    }
}
