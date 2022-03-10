// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class DatabasePrincipal<S> : DatabaseOwnedObject<S>, IDatabasePrincipal
        where S : Smo.NamedSmoObject
    {
        private static DatabasePermissionType[] allPermissionTypes;
        private IMetadataCollection<IDatabaseRole> memberOfRoles;
        private IMetadataCollection<IDatabasePermission> permissions;

        static DatabasePrincipal()
        {
            allPermissionTypes = (DatabasePermissionType[])Enum.GetValues(typeof(DatabasePermissionType));
        }

        protected DatabasePrincipal(S smoMetadataObject, Database parent)
            : base(smoMetadataObject, parent)
        {
        }

        #region IDatabasePrincipal Members

        public IMetadataCollection<IDatabaseRole> MemberOfRoles
        {
            get 
            {
                if (this.memberOfRoles == null)
                {
                    this.memberOfRoles = this.CreateMemberOfRolesCollection();
                }

                Debug.Assert(this.memberOfRoles != null, "SmoMetadataProvider Assert",
                    "memberOfRoles collection cannot be null!");

                return this.memberOfRoles;
            }
        }

        public IMetadataCollection<IDatabasePermission> Permissions
        {
            get
            {
                if (this.permissions == null)
                {
                    this.permissions = this.CreatePermissionCollection();
                }

                Debug.Assert(this.permissions != null, "SmoMetadataProvider Assert",
                    "Permissions collection cannot be null!");

                return this.permissions;
            }
        }

        #endregion

        protected abstract IEnumerable<string> GetMemberOfRoleNames();

        private IMetadataCollection<IDatabaseRole> CreateMemberOfRolesCollection()
        {
            IEnumerable<string> roleNames = this.GetMemberOfRoleNames();

            DatabaseRoleCollection result = new DatabaseRoleCollection(this.Database.CollationInfo);

            foreach (string roleName in roleNames)
            {
                IDatabaseRole role = this.Database.Roles[roleName];
                Debug.Assert(role != null, "SmoMetadataProvider Assert", "role != null");

                result.Add(role);
            }

            return result;
        }

        private IMetadataCollection<IDatabasePermission> CreatePermissionCollection()
        {
            IEnumerable<Smo.ObjectPermissionInfo> objectPermissions = this.Parent.SmoObject.EnumObjectPermissions(this.m_smoMetadataObject.Name);
            IEnumerable<Smo.DatabasePermissionInfo> databasePermissions = this.Parent.SmoObject.EnumDatabasePermissions(this.m_smoMetadataObject.Name);

            DatabasePermissionCollection result = new DatabasePermissionCollection(this.Database.CollationInfo);

            foreach (Smo.ObjectPermissionInfo permissionInfo in objectPermissions)
            {
                PermissionState permissionState = GetPermissionState(permissionInfo.PermissionState);

                IEnumerable<DatabasePermissionType> permissionTypes = GetPermissionTypes(permissionInfo.PermissionType);
                foreach (DatabasePermissionType permissionType in permissionTypes)
                {
                    IDatabasePermission permission = new DatabasePermission(
                        this,
                        permissionInfo,
                        permissionType,
                        permissionState,
                        null);

                    result.Add(permission);
                }
            }

            foreach (Smo.DatabasePermissionInfo permissionInfo in databasePermissions)
            {
                PermissionState permissionState = GetPermissionState(permissionInfo.PermissionState);

                IEnumerable<DatabasePermissionType> permissionTypes = GetPermissionTypes(permissionInfo.PermissionType);
                foreach (DatabasePermissionType permissionType in permissionTypes)
                {
                    IDatabasePermission permission = new DatabasePermission(
                        this,
                        permissionInfo,
                        permissionType,
                        permissionState,
                        null);

                    result.Add(permission);
                }
            }

            return result;
        }

        private static IEnumerable<DatabasePermissionType> GetPermissionTypes(Smo.ObjectPermissionSet permissionSet)
        {
            Debug.Assert(permissionSet != null, "SmoMetadataProvider Assert", "permissionSet != null");

            foreach (DatabasePermissionType permissionType in allPermissionTypes)
            {
                if (ContainsPermissionType(permissionSet, permissionType))
                {
                    yield return permissionType;
                }
            }
        }

        private static IEnumerable<DatabasePermissionType> GetPermissionTypes(Smo.DatabasePermissionSet permissionSet)
        {
            Debug.Assert(permissionSet != null, "SmoMetadataProvider Assert", "permissionSet != null");

            foreach (DatabasePermissionType permissionType in allPermissionTypes)
            {
                if (ContainsPermissionType(permissionSet, permissionType))
                {
                    yield return permissionType;
                }
            }
        }

        private static bool ContainsPermissionType(Smo.ObjectPermissionSet permissionSet, DatabasePermissionType permissionType)
        {
            Debug.Assert(permissionSet != null, "SmoMetadataProvider Assert", "permissionSet != null");

            switch (permissionType)
            {
                case DatabasePermissionType.Alter: return permissionSet.Alter;
                case DatabasePermissionType.Connect: return permissionSet.Connect;
                case DatabasePermissionType.Control: return permissionSet.Control;
                case DatabasePermissionType.Delete: return permissionSet.Delete;
                case DatabasePermissionType.Execute: return permissionSet.Execute;
                case DatabasePermissionType.Impersonate: return permissionSet.Impersonate;
                case DatabasePermissionType.Insert: return permissionSet.Insert;
                case DatabasePermissionType.Receive: return permissionSet.Receive;
                case DatabasePermissionType.References: return permissionSet.References;
                case DatabasePermissionType.Select: return permissionSet.Select;
                case DatabasePermissionType.Send: return permissionSet.Send;
                case DatabasePermissionType.TakeOwnership: return permissionSet.TakeOwnership;
                case DatabasePermissionType.Update: return permissionSet.Update;
                case DatabasePermissionType.ViewChangeTracking: return permissionSet.ViewChangeTracking;
                case DatabasePermissionType.ViewDefinition: return permissionSet.ViewDefinition;

                case DatabasePermissionType.AlterAnyApplicationRole:
                case DatabasePermissionType.AlterAnyAssembly:
                case DatabasePermissionType.AlterAnyAsymmetricKey:
                case DatabasePermissionType.AlterAnyCertificate:
                case DatabasePermissionType.AlterAnyContract:
                case DatabasePermissionType.AlterAnyDatabaseAudit:
                case DatabasePermissionType.AlterAnyDatabaseDdlTrigger:
                case DatabasePermissionType.AlterAnyDatabaseEventNotification:
                case DatabasePermissionType.AlterAnyDataspace:
                case DatabasePermissionType.AlterAnyFulltextCatalog:
                case DatabasePermissionType.AlterAnyMessageType:
                case DatabasePermissionType.AlterAnyRemoteServiceBinding:
                case DatabasePermissionType.AlterAnyRole:
                case DatabasePermissionType.AlterAnyRoute:
                case DatabasePermissionType.AlterAnySchema:
                case DatabasePermissionType.AlterAnyService:
                case DatabasePermissionType.AlterAnySymmetricKey:
                case DatabasePermissionType.AlterAnyUser:
                case DatabasePermissionType.Authenticate:
                case DatabasePermissionType.BackupDatabase:
                case DatabasePermissionType.BackupLog:
                case DatabasePermissionType.Checkpoint:
                case DatabasePermissionType.ConnectReplication:
                case DatabasePermissionType.CreateAggregate:
                case DatabasePermissionType.CreateAssembly:
                case DatabasePermissionType.CreateAsymmetricKey:
                case DatabasePermissionType.CreateCertificate:
                case DatabasePermissionType.CreateContract:
                case DatabasePermissionType.CreateDatabase:
                case DatabasePermissionType.CreateDatabaseDdlEventNotification:
                case DatabasePermissionType.CreateDefault:
                case DatabasePermissionType.CreateFulltextCatalog:
                case DatabasePermissionType.CreateFunction:
                case DatabasePermissionType.CreateMessageType:
                case DatabasePermissionType.CreateProcedure:
                case DatabasePermissionType.CreateQueue:
                case DatabasePermissionType.CreateRemoteServiceBinding:
                case DatabasePermissionType.CreateRole:
                case DatabasePermissionType.CreateRoute:
                case DatabasePermissionType.CreateRule:
                case DatabasePermissionType.CreateSchema:
                case DatabasePermissionType.CreateService:
                case DatabasePermissionType.CreateSymmetricKey:
                case DatabasePermissionType.CreateSynonym:
                case DatabasePermissionType.CreateTable:
                case DatabasePermissionType.CreateType:
                case DatabasePermissionType.CreateView:
                case DatabasePermissionType.CreateXmlSchemaCollection:
                case DatabasePermissionType.Showplan:
                case DatabasePermissionType.SubscribeQueryNotifications:
                case DatabasePermissionType.ViewDatabaseState:
                    return false;

                default:
                    Debug.Fail("SmoMetadataProvider Assert", "Unexpected permissionType: " + permissionType);
                    return false;
            }
        }

        private static bool ContainsPermissionType(Smo.DatabasePermissionSet permissionSet, DatabasePermissionType permissionType)
        {
            Debug.Assert(permissionSet != null, "SmoMetadataProvider Assert", "permissionSet != null");

            switch (permissionType)
            {
                case DatabasePermissionType.Alter: return permissionSet.Alter;
                case DatabasePermissionType.AlterAnyApplicationRole: return permissionSet.AlterAnyApplicationRole;
                case DatabasePermissionType.AlterAnyAssembly: return permissionSet.AlterAnyAssembly;
                case DatabasePermissionType.AlterAnyAsymmetricKey: return permissionSet.AlterAnyAsymmetricKey;
                case DatabasePermissionType.AlterAnyCertificate: return permissionSet.AlterAnyCertificate;
                case DatabasePermissionType.AlterAnyContract: return permissionSet.AlterAnyContract;
                case DatabasePermissionType.AlterAnyDatabaseAudit: return permissionSet.AlterAnyDatabaseAudit;
                case DatabasePermissionType.AlterAnyDatabaseDdlTrigger: return permissionSet.AlterAnyDatabaseDdlTrigger;
                case DatabasePermissionType.AlterAnyDatabaseEventNotification: return permissionSet.AlterAnyDatabaseEventNotification;
                case DatabasePermissionType.AlterAnyDataspace: return permissionSet.AlterAnyDataspace;
                case DatabasePermissionType.AlterAnyFulltextCatalog: return permissionSet.AlterAnyFulltextCatalog;
                case DatabasePermissionType.AlterAnyMessageType: return permissionSet.AlterAnyMessageType;
                case DatabasePermissionType.AlterAnyRemoteServiceBinding: return permissionSet.AlterAnyRemoteServiceBinding;
                case DatabasePermissionType.AlterAnyRole: return permissionSet.AlterAnyRole;
                case DatabasePermissionType.AlterAnyRoute: return permissionSet.AlterAnyRoute;
                case DatabasePermissionType.AlterAnySchema: return permissionSet.AlterAnySchema;
                case DatabasePermissionType.AlterAnyService: return permissionSet.AlterAnyService;
                case DatabasePermissionType.AlterAnySymmetricKey: return permissionSet.AlterAnySymmetricKey;
                case DatabasePermissionType.AlterAnyUser: return permissionSet.AlterAnyUser;
                case DatabasePermissionType.Authenticate: return permissionSet.Authenticate;
                case DatabasePermissionType.BackupDatabase: return permissionSet.BackupDatabase;
                case DatabasePermissionType.BackupLog: return permissionSet.BackupLog;
                case DatabasePermissionType.Checkpoint: return permissionSet.Checkpoint;
                case DatabasePermissionType.Connect: return permissionSet.Connect;
                case DatabasePermissionType.ConnectReplication: return permissionSet.ConnectReplication;
                case DatabasePermissionType.Control: return permissionSet.Control;
                case DatabasePermissionType.CreateAggregate: return permissionSet.CreateAggregate;
                case DatabasePermissionType.CreateAssembly: return permissionSet.CreateAssembly;
                case DatabasePermissionType.CreateAsymmetricKey: return permissionSet.CreateAsymmetricKey;
                case DatabasePermissionType.CreateCertificate: return permissionSet.CreateCertificate;
                case DatabasePermissionType.CreateContract: return permissionSet.CreateContract;
                case DatabasePermissionType.CreateDatabase: return permissionSet.CreateDatabase;
                case DatabasePermissionType.CreateDatabaseDdlEventNotification: return permissionSet.CreateDatabaseDdlEventNotification;
                case DatabasePermissionType.CreateDefault: return permissionSet.CreateDefault;
                case DatabasePermissionType.CreateFulltextCatalog: return permissionSet.CreateFulltextCatalog;
                case DatabasePermissionType.CreateFunction: return permissionSet.CreateFunction;
                case DatabasePermissionType.CreateMessageType: return permissionSet.CreateMessageType;
                case DatabasePermissionType.CreateProcedure: return permissionSet.CreateProcedure;
                case DatabasePermissionType.CreateQueue: return permissionSet.CreateQueue;
                case DatabasePermissionType.CreateRemoteServiceBinding: return permissionSet.CreateRemoteServiceBinding;
                case DatabasePermissionType.CreateRole: return permissionSet.CreateRole;
                case DatabasePermissionType.CreateRoute: return permissionSet.CreateRoute;
                case DatabasePermissionType.CreateRule: return permissionSet.CreateRule;
                case DatabasePermissionType.CreateSchema: return permissionSet.CreateSchema;
                case DatabasePermissionType.CreateService: return permissionSet.CreateService;
                case DatabasePermissionType.CreateSymmetricKey: return permissionSet.CreateSymmetricKey;
                case DatabasePermissionType.CreateSynonym: return permissionSet.CreateSynonym;
                case DatabasePermissionType.CreateTable: return permissionSet.CreateTable;
                case DatabasePermissionType.CreateType: return permissionSet.CreateType;
                case DatabasePermissionType.CreateView: return permissionSet.CreateView;
                case DatabasePermissionType.CreateXmlSchemaCollection: return permissionSet.CreateXmlSchemaCollection;
                case DatabasePermissionType.Delete: return permissionSet.Delete;
                case DatabasePermissionType.Execute: return permissionSet.Execute;
                case DatabasePermissionType.Insert: return permissionSet.Insert;
                case DatabasePermissionType.References: return permissionSet.References;
                case DatabasePermissionType.Select: return permissionSet.Select;
                case DatabasePermissionType.Showplan: return permissionSet.Showplan;
                case DatabasePermissionType.SubscribeQueryNotifications: return permissionSet.SubscribeQueryNotifications;
                case DatabasePermissionType.TakeOwnership: return permissionSet.TakeOwnership;
                case DatabasePermissionType.Update: return permissionSet.Update;
                case DatabasePermissionType.ViewDatabaseState: return permissionSet.ViewDatabaseState;
                case DatabasePermissionType.ViewDefinition: return permissionSet.ViewDefinition;

                case DatabasePermissionType.Impersonate:
                case DatabasePermissionType.Receive:
                case DatabasePermissionType.Send:
                case DatabasePermissionType.ViewChangeTracking:
                    return false;

                default:
                    Debug.Fail("SmoMetadataProvider Assert", "Unexpected permissionType: " + permissionType);
                    return false;
            }
        }

        private PermissionState GetPermissionState(Smo.PermissionState permissionState)
        {
            switch (permissionState)
            {
                case Smo.PermissionState.Deny:
                    return PermissionState.Deny;
                case Smo.PermissionState.Grant:
                    return PermissionState.Grant;
                case Smo.PermissionState.GrantWithGrant:
                    return PermissionState.GrantWithGrant;
                case Smo.PermissionState.Revoke:
                    return PermissionState.Revoke;
                default:
                    Debug.Fail("SmoMetadataProvider Assert", "Unexpected permissionState: " + permissionState);
                    return default(PermissionState);
            }
        }
    }
}
