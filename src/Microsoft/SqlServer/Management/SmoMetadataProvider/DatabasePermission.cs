// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    internal class DatabasePermission : IDatabasePermission
    {
        private readonly string name;
        private readonly IDatabasePrincipal databasePrincipal;
        private readonly IDatabasePrincipal grantor;
        private readonly Smo.PermissionInfo permissionInfo;
        private readonly DatabasePermissionType permissionType;
        private readonly PermissionState permissionState;
        private IMetadataObject targetObject;
        private bool targetObjectSet;

        public DatabasePermission(IDatabasePrincipal databasePrincipal, Smo.PermissionInfo permissionInfo, DatabasePermissionType permissionType, PermissionState permissionState, IDatabasePrincipal grantor)
        {
            Debug.Assert(databasePrincipal != null, "MetadataProvider Assert", "databasePrincipal != null");
            Debug.Assert(permissionInfo != null, "MetadataProvider Assert", "permissionInfo != null");

            this.name = Guid.NewGuid().ToString();
            this.databasePrincipal = databasePrincipal;
            this.permissionInfo = permissionInfo;
            this.permissionType = permissionType;
            this.permissionState = permissionState;
            this.grantor = grantor;
        }

        public T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException("visitor");
            }

            return visitor.Visit(this);
        }

        #region IDatabasePermission Members

        public IDatabasePrincipal DatabasePrincipal
        {
            get { return this.databasePrincipal; }
        }

        public IDatabasePrincipal Grantor
        {
            get { return this.grantor; }
        }

        public PermissionState PermissionState
        {
            get { return this.permissionState; }
        }

        public DatabasePermissionType PermissionType
        {
            get { return this.permissionType; }
        }

        public IMetadataObject TargetObject
        {
            get
            {
                if (!this.targetObjectSet)
                {
                    this.targetObject = this.FindTargetObject();
                    this.targetObjectSet = true;
                }

                return this.targetObject;
            }
        }

        #endregion

        #region IMetadataObject Members

        public string Name
        {
            get { return this.name; }
        }

        #endregion

        private IMetadataObject FindTargetObject()
        {
            IDatabase database = this.databasePrincipal.Database;

            switch (this.permissionInfo.ObjectClass)
            {
                case Smo.ObjectClass.Database:
                    return database.Server.Databases[this.permissionInfo.ObjectName];

                case Smo.ObjectClass.ObjectOrColumn:
                    {
                        IMetadataObject obj =
                            (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].ExtendedStoredProcedures[this.permissionInfo.ObjectName] ??
                            (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].ScalarValuedFunctions[this.permissionInfo.ObjectName] ??
                            (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].StoredProcedures[this.permissionInfo.ObjectName] ??
                            (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].Tables[this.permissionInfo.ObjectName] ??
                            (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].TableValuedFunctions[this.permissionInfo.ObjectName] ??
                            (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].UserDefinedAggregates[this.permissionInfo.ObjectName] ??
                            (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].Views[this.permissionInfo.ObjectName] ??
                            (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].Synonyms[this.permissionInfo.ObjectName];

                        if (!string.IsNullOrEmpty(this.permissionInfo.ColumnName))
                        {
                            ITabular tabular = obj as ITabular;
                            Debug.Assert(tabular != null, "SmoMetadataProvider Assert", "tabular != null");

                            obj = tabular.Columns[this.permissionInfo.ColumnName];
                        }

                        return obj;
                    }

                case Smo.ObjectClass.Schema:
                    return database.Schemas[this.permissionInfo.ObjectName];

                case Smo.ObjectClass.UserDefinedType:
                    return
                        (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].UserDefinedDataTypes[this.permissionInfo.ObjectName] ??
                        (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].UserDefinedTableTypes[this.permissionInfo.ObjectName] ??
                        (IMetadataObject)database.Schemas[this.permissionInfo.ObjectSchema].UserDefinedClrTypes[this.permissionInfo.ObjectName];

                case Smo.ObjectClass.Certificate:
                    return database.Certificates[this.permissionInfo.ObjectName];

                case Smo.ObjectClass.AsymmetricKey:
                    return database.AsymmetricKeys[this.permissionInfo.ObjectName];

                case Smo.ObjectClass.ApplicationRole:
                    return database.ApplicationRoles[this.permissionInfo.ObjectName];

                case Smo.ObjectClass.User:
                    return database.Users[this.permissionInfo.ObjectName];

                case Smo.ObjectClass.DatabaseRole:
                    return database.Roles[this.permissionInfo.ObjectName];

                case Smo.ObjectClass.FullTextStopList:
                case Smo.ObjectClass.SearchPropertyList:
                case Smo.ObjectClass.AvailabilityGroup:
                case Smo.ObjectClass.SecurityExpression:
                case Smo.ObjectClass.XmlNamespace:
                case Smo.ObjectClass.MessageType:
                case Smo.ObjectClass.ServiceContract:
                case Smo.ObjectClass.Service:
                case Smo.ObjectClass.RemoteServiceBinding:
                case Smo.ObjectClass.ServiceRoute:
                case Smo.ObjectClass.FullTextCatalog:
                case Smo.ObjectClass.SqlAssembly:
                case Smo.ObjectClass.SymmetricKey:
                    // Not supported.
                    return null;

                case Smo.ObjectClass.Server:
                case Smo.ObjectClass.Login:
                case Smo.ObjectClass.Endpoint:
                case Smo.ObjectClass.ServerPrincipal:
                case Smo.ObjectClass.ServerRole:
                    Debug.Fail("SmoMetadataProvider Assert", "Invalid object class: " + this.permissionInfo.ObjectClass);
                    return null;

                default:
                    Debug.Fail("SmoMetadataProvider Assert", "Unexpected object class: " + this.permissionInfo.ObjectClass);
                    return null;
            }
        }
    }
}
