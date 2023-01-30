// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{

    public sealed class DatabasePermission
    {
        private DatabasePermissionSetValue m_value;

        internal DatabasePermission(DatabasePermissionSetValue permissionValue)
        {
            m_value = permissionValue;
        }

        internal DatabasePermissionSetValue Value
        {
            get { { return m_value; } }
        }

        static public implicit operator DatabasePermissionSet(DatabasePermission permission)
        {
            return new DatabasePermissionSet(permission);
        }

        static public DatabasePermissionSet ToDatabasePermissionSet(DatabasePermission permission)
        {
            // this will cause the implicit operator to invoke
            return permission;
        }

        static public DatabasePermissionSet operator +(DatabasePermission permissionLeft, DatabasePermission permissionRight)
        {
            DatabasePermissionSet permissionSet = new DatabasePermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public DatabasePermissionSet Add(DatabasePermission permissionLeft, DatabasePermission permissionRight)
        {
            return permissionLeft + permissionRight;
        }

        static public DatabasePermissionSet operator |(DatabasePermission permissionLeft, DatabasePermission permissionRight)
        {
            DatabasePermissionSet permissionSet = new DatabasePermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public DatabasePermissionSet BitwiseOr(DatabasePermission permissionLeft, DatabasePermission permissionRight)
        {
            return permissionLeft | permissionRight;
        }

        // Add new database permissions here, in alphabetical order

        public static DatabasePermission AdministerDatabaseBulkOperations
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AdministerDatabaseBulkOperations); }
        }

        public static DatabasePermission Alter
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Alter); }
        }

        public static DatabasePermission AlterAnyApplicationRole
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyApplicationRole); }
        }

        public static DatabasePermission AlterAnyAssembly
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyAssembly); }
        }

        public static DatabasePermission AlterAnyAsymmetricKey
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyAsymmetricKey); }
        }

        public static DatabasePermission AlterAnyCertificate
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyCertificate); }
        }

        public static DatabasePermission AlterAnyColumnEncryptionKey
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyColumnEncryptionKey); }
        }

        public static DatabasePermission AlterAnyColumnMasterKey
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyColumnMasterKey); }
        }

        public static DatabasePermission AlterAnyContract
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyContract); }
        }

        public static DatabasePermission AlterAnyDatabaseAudit
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseAudit); }
        }

        public static DatabasePermission AlterAnyDatabaseDdlTrigger
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseDdlTrigger); }
        }

        public static DatabasePermission AlterAnyDatabaseEventNotification
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventNotification); }
        }

        public static DatabasePermission AlterAnyDatabaseEventSession
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventSession); }
        }

        public static DatabasePermission AlterAnyDatabaseEventSessionAddEvent
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventSessionAddEvent); }
        }

        public static DatabasePermission AlterAnyDatabaseEventSessionAddTarget
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventSessionAddTarget); }
        }

        public static DatabasePermission AlterAnyDatabaseEventSessionDisable
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDisable); }
        }

        public static DatabasePermission AlterAnyDatabaseEventSessionDropEvent
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDropEvent); }
        }

        public static DatabasePermission AlterAnyDatabaseEventSessionDropTarget
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDropTarget); }
        }

        public static DatabasePermission AlterAnyDatabaseEventSessionEnable
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventSessionEnable); }
        }

        public static DatabasePermission AlterAnyDatabaseEventSessionOption
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseEventSessionOption); }
        }

        public static DatabasePermission AlterAnyDatabaseScopedConfiguration
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDatabaseScopedConfiguration); }
        }

        public static DatabasePermission AlterAnyDataspace
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyDataspace); }
        }

        public static DatabasePermission AlterAnyExternalDataSource
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyExternalDataSource); }
        }

        public static DatabasePermission AlterAnyExternalFileFormat
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyExternalFileFormat); }
        }

        public static DatabasePermission AlterAnyExternalJob
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyExternalJob); }
        }

        public static DatabasePermission AlterAnyExternalLanguage
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyExternalLanguage); }
        }

        public static DatabasePermission AlterAnyExternalLibrary
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyExternalLibrary); }
        }

        public static DatabasePermission AlterAnyExternalStream
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyExternalStream); }
        }

        public static DatabasePermission AlterAnyFulltextCatalog
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyFulltextCatalog); }
        }

        public static DatabasePermission AlterAnyMask
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyMask); }
        }

        public static DatabasePermission AlterAnyMessageType
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyMessageType); }
        }

        public static DatabasePermission AlterAnyRemoteServiceBinding
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyRemoteServiceBinding); }
        }

        public static DatabasePermission AlterAnyRole
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyRole); }
        }

        public static DatabasePermission AlterAnyRoute
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyRoute); }
        }

        public static DatabasePermission AlterAnySchema
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnySchema); }
        }

        public static DatabasePermission AlterAnySecurityPolicy
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnySecurityPolicy); }
        }

        public static DatabasePermission AlterAnySensitivityClassification
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnySensitivityClassification); }
        }

        public static DatabasePermission AlterAnyService
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyService); }
        }

        public static DatabasePermission AlterAnySymmetricKey
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnySymmetricKey); }
        }

        public static DatabasePermission AlterAnyUser
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterAnyUser); }
        }

        public static DatabasePermission AlterLedger
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterLedger); }
        }

        public static DatabasePermission AlterLedgerConfiguration
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.AlterLedgerConfiguration); }
        }

        public static DatabasePermission Authenticate
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Authenticate); }
        }

        public static DatabasePermission BackupDatabase
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.BackupDatabase); }
        }

        public static DatabasePermission BackupLog
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.BackupLog); }
        }

        public static DatabasePermission Checkpoint
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Checkpoint); }
        }

        public static DatabasePermission Connect
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Connect); }
        }

        public static DatabasePermission ConnectReplication
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ConnectReplication); }
        }

        public static DatabasePermission Control
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Control); }
        }

        public static DatabasePermission CreateAggregate
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateAggregate); }
        }

        public static DatabasePermission CreateAnyDatabaseEventSession
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateAnyDatabaseEventSession); }
        }

        public static DatabasePermission CreateAssembly
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateAssembly); }
        }

        public static DatabasePermission CreateAsymmetricKey
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateAsymmetricKey); }
        }

        public static DatabasePermission CreateCertificate
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateCertificate); }
        }

        public static DatabasePermission CreateContract
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateContract); }
        }

        public static DatabasePermission CreateDatabase
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateDatabase); }
        }

        public static DatabasePermission CreateDatabaseDdlEventNotification
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateDatabaseDdlEventNotification); }
        }

        public static DatabasePermission CreateDefault
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateDefault); }
        }

        public static DatabasePermission CreateExternalLanguage
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateExternalLanguage); }
        }

        public static DatabasePermission CreateExternalLibrary
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateExternalLibrary); }
        }

        public static DatabasePermission CreateFulltextCatalog
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateFulltextCatalog); }
        }

        public static DatabasePermission CreateFunction
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateFunction); }
        }

        public static DatabasePermission CreateMessageType
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateMessageType); }
        }

        public static DatabasePermission CreateProcedure
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateProcedure); }
        }

        public static DatabasePermission CreateQueue
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateQueue); }
        }

        public static DatabasePermission CreateRemoteServiceBinding
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateRemoteServiceBinding); }
        }

        public static DatabasePermission CreateRole
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateRole); }
        }

        public static DatabasePermission CreateRoute
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateRoute); }
        }

        public static DatabasePermission CreateRule
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateRule); }
        }

        public static DatabasePermission CreateSchema
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateSchema); }
        }

        public static DatabasePermission CreateService
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateService); }
        }

        public static DatabasePermission CreateSymmetricKey
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateSymmetricKey); }
        }

        public static DatabasePermission CreateSynonym
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateSynonym); }
        }

        public static DatabasePermission CreateTable
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateTable); }
        }

        public static DatabasePermission CreateType
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateType); }
        }

        public static DatabasePermission CreateUser
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateUser); }
        }

        public static DatabasePermission CreateView
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateView); }
        }

        public static DatabasePermission CreateXmlSchemaCollection
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.CreateXmlSchemaCollection); }
        }

        public static DatabasePermission Delete
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Delete); }
        }

        public static DatabasePermission DropAnyDatabaseEventSession
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.DropAnyDatabaseEventSession); }
        }

        public static DatabasePermission EnableLedger
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.EnableLedger); }
        }

        public static DatabasePermission Execute
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Execute); }
        }

        public static DatabasePermission ExecuteAnyExternalEndpoint
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ExecuteAnyExternalEndpoint); }
        }

        public static DatabasePermission ExecuteAnyExternalScript
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ExecuteAnyExternalScript); }
        }

        public static DatabasePermission Insert
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Insert); }
        }

        public static DatabasePermission KillDatabaseConnection
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.KillDatabaseConnection); }
        }

        public static DatabasePermission OwnershipChaining
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.OwnershipChaining); }
        }

        public static DatabasePermission References
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.References); }
        }

        public static DatabasePermission Select
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Select); }
        }

        public static DatabasePermission Showplan
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Showplan); }
        }

        public static DatabasePermission SubscribeQueryNotifications
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.SubscribeQueryNotifications); }
        }

        public static DatabasePermission TakeOwnership
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.TakeOwnership); }
        }

        public static DatabasePermission Unmask
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Unmask); }
        }

        public static DatabasePermission Update
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.Update); }
        }

        public static DatabasePermission ViewAnyColumnEncryptionKeyDefinition
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewAnyColumnEncryptionKeyDefinition); }
        }

        public static DatabasePermission ViewAnyColumnMasterKeyDefinition
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewAnyColumnMasterKeyDefinition); }
        }

        public static DatabasePermission ViewAnySensitivityClassification
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewAnySensitivityClassification); }
        }

        public static DatabasePermission ViewCryptographicallySecuredDefinition
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewCryptographicallySecuredDefinition); }
        }

        public static DatabasePermission ViewDatabasePerformanceState
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewDatabasePerformanceState); }
        }

        public static DatabasePermission ViewDatabaseSecurityAudit
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewDatabaseSecurityAudit); }
        }

        public static DatabasePermission ViewDatabaseSecurityState
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewDatabaseSecurityState); }
        }

        public static DatabasePermission ViewDatabaseState
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewDatabaseState); }
        }

        public static DatabasePermission ViewDefinition
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewDefinition); }
        }

        public static DatabasePermission ViewLedgerContent
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewLedgerContent); }
        }

        public static DatabasePermission ViewPerformanceDefinition
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewPerformanceDefinition); }
        }

        public static DatabasePermission ViewSecurityDefinition
        {
            get { return new DatabasePermission(DatabasePermissionSetValue.ViewSecurityDefinition); }
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return m_value.Equals(o);
        }

        public static bool operator ==(DatabasePermission p1, DatabasePermission p2)
        {
            if (null == (object)p1)
            {
                return null == (object)p2;
            }
            else
            {
                return p1.Equals(p2);
            }
        }

        public static bool operator !=(DatabasePermission p1, DatabasePermission p2)
        {
            return !(p1 == p2);
        }

    }

    public sealed class DatabasePermissionSet : PermissionSetBase
    {

        public DatabasePermissionSet()
        {
        }

        public DatabasePermissionSet(DatabasePermissionSet oDatabasePermissionSet)
            : base(oDatabasePermissionSet)
        {
        }

        public DatabasePermissionSet(DatabasePermission permission)
        {
            SetBit(permission);
        }

        public DatabasePermissionSet(params DatabasePermission[] permissions)
        {
            Storage = new BitArray(this.NumberOfElements);
            foreach (DatabasePermission permission in permissions)
            {
                SetBit(permission);
            }
        }

        internal void SetBit(DatabasePermission permission)
        {
            Storage[(int)permission.Value] = true;
        }

        internal void ResetBit(DatabasePermission permission)
        {
            Storage[(int)permission.Value] = false;
        }

        public DatabasePermissionSet Add(DatabasePermission permission)
        {
            SetBit(permission);
            return this;
        }

        public DatabasePermissionSet Remove(DatabasePermission permission)
        {
            ResetBit(permission);
            return this;
        }

        static public DatabasePermissionSet operator +(DatabasePermissionSet permissionLeft, DatabasePermission permissionRight)
        {
            DatabasePermissionSet permissionSet = new DatabasePermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public DatabasePermissionSet Add(DatabasePermissionSet permissionLeft, DatabasePermission permissionRight)
        {
            return permissionLeft + permissionRight;
        }

        static public DatabasePermissionSet operator -(DatabasePermissionSet permissionLeft, DatabasePermission permissionRight)
        {
            DatabasePermissionSet permissionSet = new DatabasePermissionSet(permissionLeft);
            permissionSet.ResetBit(permissionRight);
            return permissionSet;
        }

        static public DatabasePermissionSet Subtract(DatabasePermissionSet permissionLeft, DatabasePermission permissionRight)
        {
            return permissionLeft - permissionRight;
        }

        /// <summary>
        /// The number permissions in the SMO enumerator
        /// </summary>
        internal override int NumberOfElements
        {
            get { return Enum.GetNames(typeof (DatabasePermissionSetValue)).Length; }

        }

        internal override string PermissionCodeToPermissionName(int permissionCode)
        {
            return PermissionDecode.PermissionCodeToPermissionName<DatabasePermissionSetValue>(permissionCode);
        }

        internal override string PermissionCodeToPermissionType(int permissionCode)
        {
            return PermissionDecode.PermissionCodeToPermissionType<DatabasePermissionSetValue>(permissionCode);
        }

        /// Add getters/setters for new database permission set values here, in alphabetical order

        public bool AdministerDatabaseBulkOperations
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AdministerDatabaseBulkOperations]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AdministerDatabaseBulkOperations] = value; }
        }

        public bool Alter
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Alter]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Alter] = value; }
        }

        public bool AlterAnyApplicationRole
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyApplicationRole]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyApplicationRole] = value; }
        }

        public bool AlterAnyAssembly
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyAssembly]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyAssembly] = value; }
        }

        public bool AlterAnyAsymmetricKey
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyAsymmetricKey]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyAsymmetricKey] = value; }
        }

        public bool AlterAnyCertificate
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyCertificate]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyCertificate] = value; }
        }

        public bool AlterAnyColumnEncryptionKey
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyColumnEncryptionKey]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyColumnEncryptionKey] = value; }
        }

        public bool AlterAnyColumnMasterKey
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyColumnMasterKey]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyColumnMasterKey] = value; }
        }

        public bool AlterAnyContract
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyContract]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyContract] = value; }
        }

        public bool AlterAnyDatabaseAudit
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseAudit]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseAudit] = value; }
        }

        public bool AlterAnyDatabaseDdlTrigger
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseDdlTrigger]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseDdlTrigger] = value; }
        }

        public bool AlterAnyDatabaseEventNotification
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventNotification]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventNotification] = value; }
        }

        public bool AlterAnyDatabaseEventSession
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSession]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSession] = value; }
        }

        public bool AlterAnyDatabaseEventSessionAddEvent
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionAddEvent]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionAddEvent] = value; }
        }

        public bool AlterAnyDatabaseEventSessionAddTarget
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionAddTarget]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionAddTarget] = value; }
        }

        public bool AlterAnyDatabaseEventSessionDisable
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDisable]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDisable] = value; }
        }

        public bool AlterAnyDatabaseEventSessionDropEvent
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDropEvent]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDropEvent] = value; }
        }

        public bool AlterAnyDatabaseEventSessionDropTarget
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDropTarget]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionDropTarget] = value; }
        }

        public bool AlterAnyDatabaseEventSessionEnable
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionEnable]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionEnable] = value; }
        }

        public bool AlterAnyDatabaseEventSessionOption
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionOption]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseEventSessionOption] = value; }
        }

        public bool AlterAnyDatabaseScopedConfiguration
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseScopedConfiguration]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDatabaseScopedConfiguration] = value; }
        }

        public bool AlterAnyDataspace
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyDataspace]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyDataspace] = value; }
        }

        public bool AlterAnyExternalDataSource
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalDataSource]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalDataSource] = value; }
        }

        public bool AlterAnyExternalFileFormat
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalFileFormat]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalFileFormat] = value; }
        }

        public bool AlterAnyExternalJob
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalJob]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalJob] = value; }
        }

        public bool AlterAnyExternalLanguage
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalLanguage]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalLanguage] = value; }
        }

        public bool AlterAnyExternalLibrary
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalLibrary]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalLibrary] = value; }
        }

        public bool AlterAnyExternalStream
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalStream]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyExternalStream] = value; }
        }

        public bool AlterAnyFulltextCatalog
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyFulltextCatalog]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyFulltextCatalog] = value; }
        }

        public bool AlterAnyMask
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyMask]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyMask] = value; }
        }

        public bool AlterAnyMessageType
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyMessageType]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyMessageType] = value; }
        }

        public bool AlterAnyRemoteServiceBinding
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyRemoteServiceBinding]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyRemoteServiceBinding] = value; }
        }

        public bool AlterAnyRole
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyRole]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyRole] = value; }
        }

        public bool AlterAnyRoute
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyRoute]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyRoute] = value; }
        }

        public bool AlterAnySchema
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnySchema]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnySchema] = value; }
        }

        public bool AlterAnySecurityPolicy
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnySecurityPolicy]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnySecurityPolicy] = value; }
        }

        public bool AlterAnySensitivityClassification
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnySensitivityClassification]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnySensitivityClassification] = value; }
        }

        public bool AlterAnyService
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyService]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyService] = value; }
        }

        public bool AlterAnySymmetricKey
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnySymmetricKey]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnySymmetricKey] = value; }
        }

        public bool AlterAnyUser
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterAnyUser]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterAnyUser] = value; }
        }

        public bool AlterLedger
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterLedger]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterLedger] = value; }
        }

        public bool AlterLedgerConfiguration
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.AlterLedgerConfiguration]; }
            set { this.Storage[(int)DatabasePermissionSetValue.AlterLedgerConfiguration] = value; }
        }

        public bool Authenticate
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Authenticate]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Authenticate] = value; }
        }

        public bool BackupDatabase
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.BackupDatabase]; }
            set { this.Storage[(int)DatabasePermissionSetValue.BackupDatabase] = value; }
        }

        public bool BackupLog
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.BackupLog]; }
            set { this.Storage[(int)DatabasePermissionSetValue.BackupLog] = value; }
        }

        public bool Checkpoint
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Checkpoint]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Checkpoint] = value; }
        }

        public bool Connect
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Connect]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Connect] = value; }
        }

        public bool ConnectReplication
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ConnectReplication]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ConnectReplication] = value; }
        }

        public bool Control
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Control]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Control] = value; }
        }

        public bool CreateAggregate
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateAggregate]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateAggregate] = value; }
        }

        public bool CreateAnyDatabaseEventSession
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateAnyDatabaseEventSession]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateAnyDatabaseEventSession] = value; }
        }

        public bool CreateAssembly
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateAssembly]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateAssembly] = value; }
        }

        public bool CreateAsymmetricKey
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateAsymmetricKey]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateAsymmetricKey] = value; }
        }

        public bool CreateCertificate
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateCertificate]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateCertificate] = value; }
        }

        public bool CreateContract
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateContract]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateContract] = value; }
        }

        public bool CreateDatabase
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateDatabase]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateDatabase] = value; }
        }

        public bool CreateDatabaseDdlEventNotification
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateDatabaseDdlEventNotification]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateDatabaseDdlEventNotification] = value; }
        }

        public bool CreateDefault
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateDefault]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateDefault] = value; }
        }

        public bool CreateExternalLanguage
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateExternalLanguage]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateExternalLanguage] = value; }
        }

        public bool CreateExternalLibrary
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateExternalLibrary]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateExternalLibrary] = value; }
        }

        public bool CreateFulltextCatalog
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateFulltextCatalog]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateFulltextCatalog] = value; }
        }

        public bool CreateFunction
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateFunction]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateFunction] = value; }
        }

        public bool CreateMessageType
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateMessageType]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateMessageType] = value; }
        }

        public bool CreateProcedure
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateProcedure]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateProcedure] = value; }
        }

        public bool CreateQueue
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateQueue]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateQueue] = value; }
        }

        public bool CreateRemoteServiceBinding
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateRemoteServiceBinding]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateRemoteServiceBinding] = value; }
        }

        public bool CreateRole
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateRole]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateRole] = value; }
        }

        public bool CreateRoute
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateRoute]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateRoute] = value; }
        }

        public bool CreateRule
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateRule]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateRule] = value; }
        }

        public bool CreateSchema
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateSchema]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateSchema] = value; }
        }

        public bool CreateService
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateService]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateService] = value; }
        }

        public bool CreateSymmetricKey
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateSymmetricKey]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateSymmetricKey] = value; }
        }

        public bool CreateSynonym
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateSynonym]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateSynonym] = value; }
        }

        public bool CreateTable
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateTable]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateTable] = value; }
        }

        public bool CreateType
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateType]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateType] = value; }
        }

        public bool CreateUser
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateUser]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateUser] = value; }
        }

        public bool CreateView
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateView]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateView] = value; }
        }

        public bool CreateXmlSchemaCollection
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.CreateXmlSchemaCollection]; }
            set { this.Storage[(int)DatabasePermissionSetValue.CreateXmlSchemaCollection] = value; }
        }

        public bool Delete
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Delete]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Delete] = value; }
        }

        public bool DropAnyDatabaseEventSession
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.DropAnyDatabaseEventSession]; }
            set { this.Storage[(int)DatabasePermissionSetValue.DropAnyDatabaseEventSession] = value; }
        }

        public bool EnableLedger
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.EnableLedger]; }
            set { this.Storage[(int)DatabasePermissionSetValue.EnableLedger] = value; }
        }

        public bool Execute
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Execute]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Execute] = value; }
        }

        public bool ExecuteAnyExternalEndpoint
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ExecuteAnyExternalEndpoint]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ExecuteAnyExternalEndpoint] = value; }
        }

        public bool ExecuteAnyExternalScript
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ExecuteAnyExternalScript]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ExecuteAnyExternalScript] = value; }
        }

        public bool Insert
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Insert]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Insert] = value; }
        }

        public bool KillDatabaseConnection
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.KillDatabaseConnection]; }
            set { this.Storage[(int)DatabasePermissionSetValue.KillDatabaseConnection] = value; }
        }

        public bool OwnershipChaining
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.OwnershipChaining]; }
            set { this.Storage[(int)DatabasePermissionSetValue.OwnershipChaining] = value; }
        }

        public bool References
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.References]; }
            set { this.Storage[(int)DatabasePermissionSetValue.References] = value; }
        }

        public bool Select
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Select]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Select] = value; }
        }

        public bool Showplan
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Showplan]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Showplan] = value; }
        }

        public bool SubscribeQueryNotifications
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.SubscribeQueryNotifications]; }
            set { this.Storage[(int)DatabasePermissionSetValue.SubscribeQueryNotifications] = value; }
        }

        public bool TakeOwnership
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.TakeOwnership]; }
            set { this.Storage[(int)DatabasePermissionSetValue.TakeOwnership] = value; }
        }

        public bool Unmask
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Unmask]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Unmask] = value; }
        }

        public bool Update
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.Update]; }
            set { this.Storage[(int)DatabasePermissionSetValue.Update] = value; }
        }

        public bool ViewAnyColumnEncryptionKeyDefinition
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewAnyColumnEncryptionKeyDefinition]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewAnyColumnEncryptionKeyDefinition] = value; }
        }

        public bool ViewAnyColumnMasterKeyDefinition
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewAnyColumnMasterKeyDefinition]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewAnyColumnMasterKeyDefinition] = value; }
        }

        public bool ViewAnySensitivityClassification
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewAnySensitivityClassification]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewAnySensitivityClassification] = value; }
        }

        public bool ViewCryptographicallySecuredDefinition
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewCryptographicallySecuredDefinition]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewCryptographicallySecuredDefinition] = value; }
        }

        public bool ViewDatabasePerformanceState
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewDatabasePerformanceState]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewDatabasePerformanceState] = value; }
        }

        public bool ViewDatabaseSecurityAudit
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewDatabaseSecurityAudit]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewDatabaseSecurityAudit] = value; }
        }

        public bool ViewDatabaseSecurityState
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewDatabaseSecurityState]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewDatabaseSecurityState] = value; }
        }

        public bool ViewDatabaseState
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewDatabaseState]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewDatabaseState] = value; }
        }

        public bool ViewDefinition
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewDefinition]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewDefinition] = value; }
        }

        public bool ViewLedgerContent
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewLedgerContent]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewLedgerContent] = value; }
        }

        public bool ViewPerformanceDefinition
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewPerformanceDefinition]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewPerformanceDefinition] = value; }
        }

        public bool ViewSecurityDefinition
        {
            get { return this.Storage[(int)DatabasePermissionSetValue.ViewSecurityDefinition]; }
            set { this.Storage[(int)DatabasePermissionSetValue.ViewSecurityDefinition] = value; }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return base.Equals(o);
        }

        public static bool operator ==(DatabasePermissionSet p1, DatabasePermissionSet p2)
        {
            if (null == (object)p1)
            {
                return null == (object)p2;
            }
            else
            {
                return p1.Equals(p2);
            }
        }

        public static bool operator !=(DatabasePermissionSet p1, DatabasePermissionSet p2)
        {
            return !(p1 == p2);
        }

    }

    /// <summary>
    /// Object permissions are permissions for any object within a database,
    /// including tables, views, schemas, certificates, etc.
    /// </summary>
    public sealed class ObjectPermission
    {
        private ObjectPermissionSetValue m_value;

        internal ObjectPermission(ObjectPermissionSetValue permissionValue)
        {
            m_value = permissionValue;
        }

        internal ObjectPermissionSetValue Value
        {
            get { { return m_value; } }
        }

        static public implicit operator ObjectPermissionSet(ObjectPermission permission)
        {
            return new ObjectPermissionSet(permission);
        }

        static public ObjectPermissionSet ToObjectPermissionSet(ObjectPermission permission)
        {
            // this will cause the implicit operator to invoke
            return permission;
        }

        static public ObjectPermissionSet operator +(ObjectPermission permissionLeft, ObjectPermission permissionRight)
        {
            ObjectPermissionSet permissionSet = new ObjectPermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public ObjectPermissionSet Add(ObjectPermission permissionLeft, ObjectPermission permissionRight)
        {
            return permissionLeft + permissionRight;
        }

        static public ObjectPermissionSet operator |(ObjectPermission permissionLeft, ObjectPermission permissionRight)
        {
            ObjectPermissionSet permissionSet = new ObjectPermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public ObjectPermissionSet BitwiseOr(ObjectPermission permissionLeft, ObjectPermission permissionRight)
        {
            return permissionLeft | permissionRight;
        }

        /// Add new object permissions here, in alphabetical order

        public static ObjectPermission Alter
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Alter); }
        }

        public static ObjectPermission Connect
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Connect); }
        }

        public static ObjectPermission Control
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Control); }
        }

        public static ObjectPermission CreateSequence
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.CreateSequence); }
        }

        public static ObjectPermission Delete
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Delete); }
        }

        public static ObjectPermission Execute
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Execute); }
        }

        public static ObjectPermission ExecuteExternalScript
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.ExecuteExternalScript); }
        }

        public static ObjectPermission Impersonate
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Impersonate); }
        }

        public static ObjectPermission Insert
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Insert); }
        }

        public static ObjectPermission Receive
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Receive); }
        }

        public static ObjectPermission References
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.References); }
        }

        public static ObjectPermission Select
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Select); }
        }

        public static ObjectPermission Send
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Send); }
        }

        public static ObjectPermission TakeOwnership
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.TakeOwnership); }
        }

        public static ObjectPermission Unmask
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Unmask); }
        }

        public static ObjectPermission Update
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.Update); }
        }

        public static ObjectPermission ViewChangeTracking
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.ViewChangeTracking); }
        }

        public static ObjectPermission ViewDefinition
        {
            get { return new ObjectPermission(ObjectPermissionSetValue.ViewDefinition); }
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return m_value.Equals(o);
        }

        public static bool operator ==(ObjectPermission p1, ObjectPermission p2)
        {
            if (null == (object)p1)
            {
                return null == (object)p2;
            }
            else
            {
                return p1.Equals(p2);
            }
        }

        public static bool operator !=(ObjectPermission p1, ObjectPermission p2)
        {
            return !(p1 == p2);
        }

    }

    public sealed class ObjectPermissionSet : PermissionSetBase
    {

        public ObjectPermissionSet()
        {
        }

        public ObjectPermissionSet(ObjectPermissionSet oObjectPermissionSet)
            : base(oObjectPermissionSet)
        {
        }

        public ObjectPermissionSet(ObjectPermission permission)
        {
            SetBit(permission);
        }

        public ObjectPermissionSet(params ObjectPermission[] permissions)
        {
            Storage = new BitArray(this.NumberOfElements);
            foreach (ObjectPermission permission in permissions)
            {
                SetBit(permission);
            }
        }

        internal void SetBit(ObjectPermission permission)
        {
            Storage[(int)permission.Value] = true;
        }

        internal void ResetBit(ObjectPermission permission)
        {
            Storage[(int)permission.Value] = false;
        }

        public ObjectPermissionSet Add(ObjectPermission permission)
        {
            SetBit(permission);
            return this;
        }

        public ObjectPermissionSet Remove(ObjectPermission permission)
        {
            ResetBit(permission);
            return this;
        }

        static public ObjectPermissionSet operator +(ObjectPermissionSet permissionLeft, ObjectPermission permissionRight)
        {
            ObjectPermissionSet permissionSet = new ObjectPermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public ObjectPermissionSet Add(ObjectPermissionSet permissionLeft, ObjectPermission permissionRight)
        {
            return permissionLeft + permissionRight;
        }

        static public ObjectPermissionSet operator -(ObjectPermissionSet permissionLeft, ObjectPermission permissionRight)
        {
            ObjectPermissionSet permissionSet = new ObjectPermissionSet(permissionLeft);
            permissionSet.ResetBit(permissionRight);
            return permissionSet;
        }

        static public ObjectPermissionSet Subtract(ObjectPermissionSet permissionLeft, ObjectPermission permissionRight)
        {
            return permissionLeft - permissionRight;
        }

        internal override int NumberOfElements
        {
            get { return Enum.GetNames(typeof (ObjectPermissionSetValue)).Length; }
        }

        internal override string PermissionCodeToPermissionName(int permissionCode)
        {
            return PermissionDecode.PermissionCodeToPermissionName<ObjectPermissionSetValue>(permissionCode);
        }

        internal override string PermissionCodeToPermissionType(int permissionCode)
        {
            return PermissionDecode.PermissionCodeToPermissionType<ObjectPermissionSetValue>(permissionCode);
        }

        /// Add getters/setters for new object permission set values here, in alphabetical order

        public bool Alter
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Alter]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Alter] = value; }
        }

        public bool Control
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Control]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Control] = value; }
        }

        public bool Connect
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Connect]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Connect] = value; }
        }

        public bool Delete
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Delete]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Delete] = value; }
        }

        public bool Execute
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Execute]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Execute] = value; }
        }

        public bool ExecuteExternalScript
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.ExecuteExternalScript]; }
            set { this.Storage[(int)ObjectPermissionSetValue.ExecuteExternalScript] = value; }
        }

        public bool Impersonate
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Impersonate]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Impersonate] = value; }
        }

        public bool Insert
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Insert]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Insert] = value; }
        }

        public bool Receive
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Receive]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Receive] = value; }
        }

        public bool References
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.References]; }
            set { this.Storage[(int)ObjectPermissionSetValue.References] = value; }
        }

        public bool Select
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Select]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Select] = value; }
        }

        public bool Send
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Send]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Send] = value; }
        }

        public bool TakeOwnership
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.TakeOwnership]; }
            set { this.Storage[(int)ObjectPermissionSetValue.TakeOwnership] = value; }
        }

        public bool Unmask
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Unmask]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Unmask] = value; }
        }

        public bool Update
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.Update]; }
            set { this.Storage[(int)ObjectPermissionSetValue.Update] = value; }
        }

        public bool ViewDefinition
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.ViewDefinition]; }
            set { this.Storage[(int)ObjectPermissionSetValue.ViewDefinition] = value; }
        }

        public bool ViewChangeTracking
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.ViewChangeTracking]; }
            set { this.Storage[(int)ObjectPermissionSetValue.ViewChangeTracking] = value; }
        }

        public bool CreateSequence
        {
            get { return this.Storage[(int)ObjectPermissionSetValue.CreateSequence]; }
            set { this.Storage[(int)ObjectPermissionSetValue.CreateSequence] = value; }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return base.Equals(o);
        }

        public static bool operator ==(ObjectPermissionSet p1, ObjectPermissionSet p2)
        {
            if (null == (object)p1)
            {
                return null == (object)p2;
            }
            else
            {
                return p1.Equals(p2);
            }
        }

        public static bool operator !=(ObjectPermissionSet p1, ObjectPermissionSet p2)
        {
            return !(p1 == p2);
        }

    }

    public sealed class ServerPermission
    {
        private ServerPermissionSetValue m_value;

        internal ServerPermission(ServerPermissionSetValue permissionValue)
        {
            m_value = permissionValue;
        }

        internal ServerPermissionSetValue Value
        {
            get { { return m_value; } }
        }

        static public implicit operator ServerPermissionSet(ServerPermission permission)
        {
            return new ServerPermissionSet(permission);
        }

        static public ServerPermissionSet ToServerPermissionSet(ServerPermission permission)
        {
            // this will cause the implicit conversion operator to invoke
            return permission;
        }

        static public ServerPermissionSet operator +(ServerPermission permissionLeft, ServerPermission permissionRight)
        {
            ServerPermissionSet permissionSet = new ServerPermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public ServerPermissionSet Add(ServerPermission permissionLeft, ServerPermission permissionRight)
        {
            return permissionLeft + permissionRight;
        }

        static public ServerPermissionSet operator |(ServerPermission permissionLeft, ServerPermission permissionRight)
        {
            ServerPermissionSet permissionSet = new ServerPermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public ServerPermissionSet BitwiseOr(ServerPermission permissionLeft, ServerPermission permissionRight)
        {
            return permissionLeft | permissionRight;
        }

        // Add new server permissions here, in alphabetical order

        public static ServerPermission AdministerBulkOperations
        {
            get { return new ServerPermission(ServerPermissionSetValue.AdministerBulkOperations); }
        }

        public static ServerPermission AlterAnyServerAudit
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyServerAudit); }
        }

        public static ServerPermission AlterAnyCredential
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyCredential); }
        }

        public static ServerPermission AlterAnyConnection
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyConnection); }
        }

        public static ServerPermission AlterAnyDatabase
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyDatabase); }
        }

        public static ServerPermission AlterAnyEventNotification
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventNotification); }
        }

        public static ServerPermission AlterAnyEndpoint
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEndpoint); }
        }

        public static ServerPermission AlterAnyEventSessionAddEvent
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventSessionAddEvent); }
        }

        public static ServerPermission AlterAnyEventSessionAddTarget
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventSessionAddTarget); }
        }

        public static ServerPermission AlterAnyEventSessionDisable
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventSessionDisable); }
        }

        public static ServerPermission AlterAnyEventSessionDropEvent
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventSessionDropEvent); }
        }

        public static ServerPermission AlterAnyEventSessionDropTarget
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventSessionDropTarget); }
        }

        public static ServerPermission AlterAnyEventSessionEnable
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventSessionEnable); }
        }

        public static ServerPermission AlterAnyEventSessionOption
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventSessionOption); }
        }

        public static ServerPermission AlterAnyLogin
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyLogin); }
        }

        public static ServerPermission AlterAnyLinkedServer
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyLinkedServer); }
        }

        public static ServerPermission AlterResources
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterResources); }
        }

        public static ServerPermission AlterServerState
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterServerState); }
        }

        public static ServerPermission AlterSettings
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterSettings); }
        }

        public static ServerPermission AlterTrace
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterTrace); }
        }

        public static ServerPermission AuthenticateServer
        {
            get { return new ServerPermission(ServerPermissionSetValue.AuthenticateServer); }
        }

        public static ServerPermission ControlServer
        {
            get { return new ServerPermission(ServerPermissionSetValue.ControlServer); }
        }

        public static ServerPermission ConnectSql
        {
            get { return new ServerPermission(ServerPermissionSetValue.ConnectSql); }
        }

        public static ServerPermission CreateAnyDatabase
        {
            get { return new ServerPermission(ServerPermissionSetValue.CreateAnyDatabase); }
        }

        public static ServerPermission CreateAnyEventSession
        {
            get { return new ServerPermission(ServerPermissionSetValue.CreateAnyEventSession); }
        }

        public static ServerPermission CreateDdlEventNotification
        {
            get { return new ServerPermission(ServerPermissionSetValue.CreateDdlEventNotification); }
        }

        public static ServerPermission CreateEndpoint
        {
            get { return new ServerPermission(ServerPermissionSetValue.CreateEndpoint); }
        }

        public static ServerPermission CreateLogin
        {
            get { return new ServerPermission(ServerPermissionSetValue.CreateLogin); }
        }

        public static ServerPermission CreateTraceEventNotification
        {
            get { return new ServerPermission(ServerPermissionSetValue.CreateTraceEventNotification); }
        }

        public static ServerPermission DropAnyEventSession
        {
            get { return new ServerPermission(ServerPermissionSetValue.DropAnyEventSession); }
        }

        public static ServerPermission Shutdown
        {
            get { return new ServerPermission(ServerPermissionSetValue.Shutdown); }
        }

        public static ServerPermission ViewAnyCryptographicallySecuredDefinition
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewAnyCryptographicallySecuredDefinition); }
        }

        public static ServerPermission ViewAnyDefinition
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewAnyDefinition); }
        }

        public static ServerPermission ViewAnyDatabase
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewAnyDatabase); }
        }

        public static ServerPermission ViewAnyErrorLog
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewAnyErrorLog); }
        }

        public static ServerPermission ViewAnyPerformanceDefinition
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewAnyPerformanceDefinition); }
        }

        public static ServerPermission ViewAnySecurityDefinition
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewAnySecurityDefinition); }
        }

        public static ServerPermission ViewServerPerformanceState
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewServerPerformanceState); }
        }

        public static ServerPermission ViewServerSecurityAudit
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewServerSecurityAudit); }
        }

        public static ServerPermission ViewServerSecurityState
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewServerSecurityState); }
        }

        public static ServerPermission ViewServerState
        {
            get { return new ServerPermission(ServerPermissionSetValue.ViewServerState); }
        }

        public static ServerPermission ExternalAccessAssembly
        {
            get { return new ServerPermission(ServerPermissionSetValue.ExternalAccessAssembly); }
        }

        public static ServerPermission UnsafeAssembly
        {
            get { return new ServerPermission(ServerPermissionSetValue.UnsafeAssembly); }
        }

        public static ServerPermission AlterAnyServerRole
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyServerRole); }
        }

        public static ServerPermission CreateServerRole
        {
            get { return new ServerPermission(ServerPermissionSetValue.CreateServerRole); }
        }

        public static ServerPermission AlterAnyAvailabilityGroup
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyAvailabilityGroup); }
        }

        public static ServerPermission CreateAvailabilityGroup
        {
            get { return new ServerPermission(ServerPermissionSetValue.CreateAvailabilityGroup); }
        }

        public static ServerPermission AlterAnyEventSession
        {
            get { return new ServerPermission(ServerPermissionSetValue.AlterAnyEventSession); }
        }

        public static ServerPermission SelectAllUserSecurables
        {
            get { return new ServerPermission(ServerPermissionSetValue.SelectAllUserSecurables); }
        }
        public static ServerPermission ConnectAnyDatabase
        {
            get { return new ServerPermission(ServerPermissionSetValue.ConnectAnyDatabase); }
        }
        public static ServerPermission ImpersonateAnyLogin
        {
            get { return new ServerPermission(ServerPermissionSetValue.ImpersonateAnyLogin); }
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return m_value.Equals(o);
        }

        public static bool operator ==(ServerPermission p1, ServerPermission p2)
        {
            if (null == (object)p1)
            {
                return null == (object)p2;
            }
            else
            {
                return p1.Equals(p2);
            }
        }

        public static bool operator !=(ServerPermission p1, ServerPermission p2)
        {
            return !(p1 == p2);
        }

    }

    public sealed class ServerPermissionSet : PermissionSetBase
    {

        public ServerPermissionSet()
        {
        }

        public ServerPermissionSet(ServerPermissionSet oServerPermissionSet)
            : base(oServerPermissionSet)
        {
        }

        public ServerPermissionSet(ServerPermission permission)
        {
            SetBit(permission);
        }

        public ServerPermissionSet(params ServerPermission[] permissions)
        {
            Storage = new BitArray(this.NumberOfElements);
            foreach (ServerPermission permission in permissions)
            {
                SetBit(permission);
            }
        }

        internal void SetBit(ServerPermission permission)
        {
            Storage[(int)permission.Value] = true;
        }

        internal void ResetBit(ServerPermission permission)
        {
            Storage[(int)permission.Value] = false;
        }

        public ServerPermissionSet Add(ServerPermission permission)
        {
            SetBit(permission);
            return this;
        }

        public ServerPermissionSet Remove(ServerPermission permission)
        {
            ResetBit(permission);
            return this;
        }

        static public ServerPermissionSet operator +(ServerPermissionSet permissionLeft, ServerPermission permissionRight)
        {
            ServerPermissionSet permissionSet = new ServerPermissionSet(permissionLeft);
            permissionSet.SetBit(permissionRight);
            return permissionSet;
        }

        static public ServerPermissionSet Add(ServerPermissionSet permissionLeft, ServerPermission permissionRight)
        {
            return permissionLeft + permissionRight;
        }

        static public ServerPermissionSet operator -(ServerPermissionSet permissionLeft, ServerPermission permissionRight)
        {
            ServerPermissionSet permissionSet = new ServerPermissionSet(permissionLeft);
            permissionSet.ResetBit(permissionRight);
            return permissionSet;
        }

        static public ServerPermissionSet Subtract(ServerPermissionSet permissionLeft, ServerPermission permissionRight)
        {
            return permissionLeft - permissionRight;
        }

        internal override int NumberOfElements
        {
            get { return Enum.GetNames(typeof (ServerPermissionSetValue)).Length; }
        }

        internal override string PermissionCodeToPermissionName(int permissionCode)
        {
            return PermissionDecode.PermissionCodeToPermissionName<ServerPermissionSetValue>(permissionCode);
        }

        internal override string PermissionCodeToPermissionType(int permissionCode)
        {
            return PermissionDecode.PermissionCodeToPermissionType<ServerPermissionSetValue>(permissionCode);
        }

        /// Add getters/setters for new server permission set values here, in alphabetical order

        public bool AdministerBulkOperations
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AdministerBulkOperations]; }
            set { this.Storage[(int)ServerPermissionSetValue.AdministerBulkOperations] = value; }
        }

        public bool AlterAnyServerAudit
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyServerAudit]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyServerAudit] = value; }
        }

        public bool AlterAnyCredential
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyCredential]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyCredential] = value; }
        }

        public bool AlterAnyConnection
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyConnection]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyConnection] = value; }
        }

        public bool AlterAnyDatabase
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyDatabase]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyDatabase] = value; }
        }

        public bool AlterAnyEventNotification
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventNotification]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventNotification] = value; }
        }

        public bool AlterAnyEndpoint
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEndpoint]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEndpoint] = value; }
        }

        public bool AlterAnyEventSessionAddEvent
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionAddEvent]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionAddEvent] = value; }
        }

        public bool AlterAnyEventSessionAddTarget
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionAddTarget]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionAddTarget] = value; }
        }

        public bool AlterAnyEventSessionDisable
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionDisable]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionDisable] = value; }
        }

        public bool AlterAnyEventSessionDropEvent
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionDropEvent]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionDropEvent] = value; }
        }

        public bool AlterAnyEventSessionDropTarget
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionDropTarget]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionDropTarget] = value; }
        }

        public bool AlterAnyEventSessionEnable
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionEnable]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionEnable] = value; }
        }

        public bool AlterAnyEventSessionOption
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionOption]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSessionOption] = value; }
        }

        public bool AlterAnyLogin
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyLogin]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyLogin] = value; }
        }

        public bool AlterAnyLinkedServer
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyLinkedServer]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyLinkedServer] = value; }
        }

        public bool AlterResources
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterResources]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterResources] = value; }
        }

        public bool AlterServerState
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterServerState]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterServerState] = value; }
        }

        public bool AlterSettings
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterSettings]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterSettings] = value; }
        }

        public bool AlterTrace
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterTrace]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterTrace] = value; }
        }

        public bool AuthenticateServer
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AuthenticateServer]; }
            set { this.Storage[(int)ServerPermissionSetValue.AuthenticateServer] = value; }
        }

        public bool ControlServer
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ControlServer]; }
            set { this.Storage[(int)ServerPermissionSetValue.ControlServer] = value; }
        }

        public bool ConnectSql
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ConnectSql]; }
            set { this.Storage[(int)ServerPermissionSetValue.ConnectSql] = value; }
        }

        public bool CreateAnyDatabase
        {
            get { return this.Storage[(int)ServerPermissionSetValue.CreateAnyDatabase]; }
            set { this.Storage[(int)ServerPermissionSetValue.CreateAnyDatabase] = value; }
        }

        public bool CreateAnyEventSession
        {
            get { return this.Storage[(int)ServerPermissionSetValue.CreateAnyEventSession]; }
            set { this.Storage[(int)ServerPermissionSetValue.CreateAnyEventSession] = value; }
        }

        public bool CreateDdlEventNotification
        {
            get { return this.Storage[(int)ServerPermissionSetValue.CreateDdlEventNotification]; }
            set { this.Storage[(int)ServerPermissionSetValue.CreateDdlEventNotification] = value; }
        }

        public bool CreateEndpoint
        {
            get { return this.Storage[(int)ServerPermissionSetValue.CreateEndpoint]; }
            set { this.Storage[(int)ServerPermissionSetValue.CreateEndpoint] = value; }
        }

        public bool CreateLogin
        {
            get { return this.Storage[(int)ServerPermissionSetValue.CreateLogin]; }
            set { this.Storage[(int)ServerPermissionSetValue.CreateLogin] = value; }
        }

        public bool CreateTraceEventNotification
        {
            get { return this.Storage[(int)ServerPermissionSetValue.CreateTraceEventNotification]; }
            set { this.Storage[(int)ServerPermissionSetValue.CreateTraceEventNotification] = value; }
        }

        public bool DropAnyEventSession
        {
            get { return this.Storage[(int)ServerPermissionSetValue.DropAnyEventSession]; }
            set { this.Storage[(int)ServerPermissionSetValue.DropAnyEventSession] = value; }
        }

        public bool Shutdown
        {
            get { return this.Storage[(int)ServerPermissionSetValue.Shutdown]; }
            set { this.Storage[(int)ServerPermissionSetValue.Shutdown] = value; }
        }

        public bool ViewAnyCryptographicallySecuredDefinition
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewAnyCryptographicallySecuredDefinition]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewAnyCryptographicallySecuredDefinition] = value; }
        }

        public bool ViewAnyDefinition
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewAnyDefinition]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewAnyDefinition] = value; }
        }

        public bool ViewAnyDatabase
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewAnyDatabase]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewAnyDatabase] = value; }
        }

        public bool ViewAnyErrorLog
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewAnyErrorLog]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewAnyErrorLog] = value; }
        }

        public bool ViewAnyPerformanceDefinition
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewAnyPerformanceDefinition]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewAnyPerformanceDefinition] = value; }
        }

        public bool ViewAnySecurityDefinition
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewAnySecurityDefinition]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewAnySecurityDefinition] = value; }
        }

        public bool ViewServerPerformanceState
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewServerPerformanceState]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewServerPerformanceState] = value; }
        }

        public bool ViewServerSecurityAudit
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewServerSecurityAudit]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewServerSecurityAudit] = value; }
        }

        public bool ViewServerSecurityState
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewServerSecurityState]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewServerSecurityState] = value; }
        }

        public bool ViewServerState
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ViewServerState]; }
            set { this.Storage[(int)ServerPermissionSetValue.ViewServerState] = value; }
        }

        public bool ExternalAccessAssembly
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ExternalAccessAssembly]; }
            set { this.Storage[(int)ServerPermissionSetValue.ExternalAccessAssembly] = value; }
        }

        public bool UnsafeAssembly
        {
            get { return this.Storage[(int)ServerPermissionSetValue.UnsafeAssembly]; }
            set { this.Storage[(int)ServerPermissionSetValue.UnsafeAssembly] = value; }
        }

        public bool AlterAnyServerRole
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyServerRole]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyServerRole] = value; }
        }

        public bool CreateServerRole
        {
            get { return this.Storage[(int)ServerPermissionSetValue.CreateServerRole]; }
            set { this.Storage[(int)ServerPermissionSetValue.CreateServerRole] = value; }
        }

        public bool AlterAnyAvailabilityGroup
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyAvailabilityGroup]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyAvailabilityGroup] = value; }
        }

        public bool CreateAvailabilityGroup
        {
            get { return this.Storage[(int)ServerPermissionSetValue.CreateAvailabilityGroup]; }
            set { this.Storage[(int)ServerPermissionSetValue.CreateAvailabilityGroup] = value; }
        }

        public bool AlterAnyEventSession
        {
            get { return this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSession]; }
            set { this.Storage[(int)ServerPermissionSetValue.AlterAnyEventSession] = value; }
        }

        public bool SelectAllUserSecurables
        {
            get { return this.Storage[(int)ServerPermissionSetValue.SelectAllUserSecurables]; }
            set { this.Storage[(int)ServerPermissionSetValue.SelectAllUserSecurables] = value; }
        }

        public bool ConnectAnyDatabase
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ConnectAnyDatabase]; }
            set { this.Storage[(int)ServerPermissionSetValue.ConnectAnyDatabase] = value; }
        }

        public bool ImpersonateAnyLogin
        {
            get { return this.Storage[(int)ServerPermissionSetValue.ImpersonateAnyLogin]; }
            set { this.Storage[(int)ServerPermissionSetValue.ImpersonateAnyLogin] = value; }
        }


        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return base.Equals(o);
        }

        public static bool operator ==(ServerPermissionSet p1, ServerPermissionSet p2)
        {
            if (null == (object)p1)
            {
                return null == (object)p2;
            }
            else
            {
                return p1.Equals(p2);
            }
        }

        public static bool operator !=(ServerPermissionSet p1, ServerPermissionSet p2)
        {
            return !(p1 == p2);
        }

    }
}
