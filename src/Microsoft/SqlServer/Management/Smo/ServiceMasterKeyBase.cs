// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("MasterKey")]
    public partial class ServiceMasterKey : SqlSmoObject
    {
        internal ServiceMasterKey(Server parentsrv, ObjectKeyBase key, SqlSmoState state)
            :
            base(key, state)
        {
            singletonParent = parentsrv as Server;

            SetServerObject(singletonParent as Server);
            m_comparer = parentsrv.Databases["master"].StringComparer;
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as Server;
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        internal protected override string GetDBName()
        {
            return "master";
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "MasterKey";
            }
        }

        /// <summary>
        /// Re-encrypts the service master key with the speciefied new credentials.
        /// </summary>
        /// <param name="newAccount"></param>
        /// <param name="newPassword"></param>
        public void	ChangeAccount(string newAccount, string newPassword)
        {
            try
            {
                if (null == newAccount)
                {
                    throw new ArgumentNullException("newAccount");
                }

                if (null == newPassword)
                {
                    throw new ArgumentNullException("newPassword");
                }

                this.ExecutionManager.ExecuteNonQuery(string.Format(
                            SmoApplication.DefaultCulture,
                            "ALTER SERVICE MASTER KEY WITH NEW_ACCOUNT=N'{0}', NEW_PASSWORD=N'{1}'",
                            SqlString(newAccount), SqlString(newPassword)));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangeAcctMasterKey, this, e);
            }
        }

        /// <summary>
        /// Loads the service master key from the specified file. The password specifies 
        /// the password with which the service master key was encrypted when saved.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="password"></param>
        public void Import(string path, string password)
        {
            try
            {
                if (null == path)
                {
                    throw new ArgumentNullException("path");
                }

                if (null == password)
                {
                    throw new ArgumentNullException("password");
                }

                this.ExecutionManager.ExecuteNonQuery(string.Format(
                            SmoApplication.DefaultCulture,
                            "RESTORE SERVICE MASTER KEY FROM FILE = N'{0}' DECRYPTION BY PASSWORD = N'{1}'",
                            SqlString(path), SqlString(password)));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ImportMasterKey, this, e);
            }
        }

        /// <summary>
        /// Recovers a service master key, in case the service master key has been changed 
        /// outside of SQL Server. The master key will be decrypted with the supplied 
        /// credentials, and re-encrypted with the current service account credentials.
        /// </summary>
        /// <param name="oldAccount"></param>
        /// <param name="oldPassword"></param>
        public void Recover(string oldAccount, string oldPassword)	
        {
            try
            {
                if (null == oldAccount)
                {
                    throw new ArgumentNullException("oldAccount");
                }

                if (null == oldPassword)
                {
                    throw new ArgumentNullException("oldPassword");
                }

                this.ExecutionManager.ExecuteNonQuery(string.Format(
                            SmoApplication.DefaultCulture,
                            "ALTER SERVICE MASTER KEY WITH OLD_ACCOUNT=N'{0}', OLD_PASSWORD=N'{1}'",
                            SqlString(oldAccount), SqlString(oldPassword)));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RecoverMasterKey, this, e);
            }
        }

        /// <summary>
        /// Regenerates the service master key
        /// </summary>
        public void Regenerate()
        {
            Regenerate(false);
        }

        /// <summary>
        /// Regenerates the database master key using the specified password. 
        /// If the force parameter is set to true, the service master key will be 
        /// regenerated forcefully. This will cause all secrets that cannot be 
        /// decrypted by the old service master key to be dropped.
        /// </summary>
        /// <param name="forceRegeneration"></param>
        public void Regenerate(bool forceRegeneration)
        {
            try
            {
                this.ExecutionManager.ExecuteNonQuery(string.Format(
                            SmoApplication.DefaultCulture, 
                            "ALTER SERVICE MASTER KEY {0}REGENERATE", 
                            forceRegeneration?"FORCE ":""));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RegenerateMasterKey, this, e);
            }
        }

        /// <summary>
        /// Saves (dumps) the service master key to a file, encrypted with the supplied password. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="password"></param>
        public void Export(string path, string password)
        {
            try
            {
                if (null == path)
                {
                    throw new ArgumentNullException("path");
                }

                if (null == password)
                {
                    throw new ArgumentNullException("password");
                }

                this.ExecutionManager.ExecuteNonQuery(string.Format(
                            SmoApplication.DefaultCulture,
                            "BACKUP SERVICE MASTER KEY TO FILE = N'{0}' ENCRYPTION BY PASSWORD = N'{1}'",
                            SqlString(path), SqlString(password)));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ExportMasterKey, this, e);
            }
        }

    }
}


