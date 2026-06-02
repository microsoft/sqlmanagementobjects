// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class DmlTrigger : IDmlTrigger, ISmoDatabaseObject
    {
        private readonly ITableViewBase m_parent;
        private readonly Smo.Trigger m_smoTrigger;
        private IExecutionContext m_executionContext;
        private string bodyText;
        private bool isBodyTextSet;

        public DmlTrigger(ITableViewBase parent, Smo.Trigger smoTrigger)
        {
            Debug.Assert(smoTrigger != null, "SmoMetadataProvider Assert", "smoTrigger != null");
            Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");

            this.m_smoTrigger = smoTrigger;
            this.m_parent = parent;
        }

        public T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region IDmlTrigger Members

        public ITableViewBase Parent
        {
            get { return this.m_parent; }
        }

        public bool NotForReplication
        {
            get { return this.m_smoTrigger.NotForReplication; }
        }

        public bool InsteadOf
        {
            get { return this.m_smoTrigger.InsteadOf; }
        }

        public bool Delete
        {
            get { return this.m_smoTrigger.Delete; }
        }

        public bool Insert
        {
            get { return this.m_smoTrigger.Insert; }
        }

        public bool Update
        {
            get { return this.m_smoTrigger.Update; }
        }

        public ActivationOrder DeleteActivationOrder
        {
            get { return GetActivationOrder(this.m_smoTrigger.DeleteOrder); }
        }

        public ActivationOrder InsertActivationOrder
        {
            get { return GetActivationOrder(this.m_smoTrigger.InsertOrder); }
        }

        public ActivationOrder UpdateActivationOrder
        {
            get { return GetActivationOrder(this.m_smoTrigger.UpdateOrder); }
        }

        public bool IsQuotedIdentifierOn
        {
            get
            {
                return this.IsSqlClr ? false : this.m_smoTrigger.QuotedIdentifierStatus;
            }
        }

        #endregion

        #region ITrigger Members

        public string BodyText
        {
            get
            {
                if (this.HasBodyText() &&
                    !this.isBodyTextSet)
                {
                    string sql;
                    if (Utils.TryGetPropertyObject<string>(this.m_smoTrigger, "Text", out sql))
                    {
                        Debug.Assert(sql != null, "SmoMetadataProvider Assert", "sql != null");

                        this.bodyText = Utils.RetriveTriggerBody(sql, this.IsQuotedIdentifierOn);
                    }
                    else
                    {
                        this.bodyText = null;
                    }
                    this.isBodyTextSet = true;
                }
                return this.bodyText;
            }
        }

        public bool IsEncrypted
        {
            get { return this.m_smoTrigger.IsEncrypted; }
        }

        public bool IsEnabled
        {
            get { return this.m_smoTrigger.IsEnabled; }
        }

        public bool IsSqlClr
        {
            get { return this.m_smoTrigger.ImplementationType == Smo.ImplementationType.SqlClr; }
        }

        public IExecutionContext ExecutionContext
        {
            get
            {
                if (this.m_executionContext == null)
                {
                    IDatabase database = this.m_parent.Schema.Database;
                    this.m_executionContext = Utils.GetExecutionContext(database, this.m_smoTrigger);
                }

                Debug.Assert(this.m_executionContext != null, "SmoMetadataProvider Assert", "executionContext != null");

                return this.m_executionContext;
            }
        }

        #endregion

        #region IMetadataObject Members
        public string Name
        {
            get { return this.m_smoTrigger.Name; }
        }

        #endregion

        #region ISmoDatabaseObject Members

        public Microsoft.SqlServer.Management.Smo.SqlSmoObject SmoObject
        {
            get { return this.m_smoTrigger; }
        }

        #endregion

        private static ActivationOrder GetActivationOrder(Smo.Agent.ActivationOrder smoActivationOrder)
        {
            switch (smoActivationOrder)
            {
                case Smo.Agent.ActivationOrder.None:
                    return ActivationOrder.None;

                case Smo.Agent.ActivationOrder.First:
                    return ActivationOrder.First;

                case Smo.Agent.ActivationOrder.Last:
                    return ActivationOrder.Last;

                default:
                    Debug.Fail("SmoMetadataProvider", "Unexpected smo activation order: "+smoActivationOrder);
                    return ActivationOrder.None;
            }
        }

        private bool HasBodyText()
        {
            return !this.IsSqlClr && !this.IsEncrypted;
        }
    }
}
