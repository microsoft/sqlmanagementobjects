// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Class representing an AG replica
    /// </summary>
    public class AvailabilityGroupReplica
    {
        #region Fields
        /// <summary>
        /// The format string used in ExistsSID method
        /// can move to extension
        /// </summary>
        public const string SidConditionFormat = "Server/Login[@SidHexString = \"{0}\"]";

        /// <summary>
        /// The Default Endpoint Name
        /// </summary>
        public const string DefaultEndpointName = "Hadr_endpoint";

        /// <summary>
        /// Default endpoint port number
        /// </summary>
        public const int DefaultEndpointPortNumber = 5022;

        /// <summary>
        /// Server sleep time
        /// </summary>
        public const int GetServerSleepTime = 1000;

        /// <summary>
        /// The default backup priority
        /// </summary>
        public const int DefaultBackupPriority = 50;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupReplicaData">The replica data</param>
        public AvailabilityGroupReplica(AvailabilityGroupReplicaData availabilityGroupReplicaData)
        {
            if (availabilityGroupReplicaData == null)
            {
                throw new ArgumentNullException("availabilityGroupReplicaData");
            }

            this.AvailabilityGroupReplicaData = availabilityGroupReplicaData;
        }
        #endregion

        #region Properties

        /// <summary>
        /// The AvailabilityGroupReplicaData Object
        /// </summary>
        public AvailabilityGroupReplicaData AvailabilityGroupReplicaData { get; set; }

        /// <summary>
        /// Property for availabilityGroupReplicaData.backupPriority
        /// </summary>
        public int BackupPriority
        {
            get
            {
                return this.AvailabilityGroupReplicaData.BackupPriority;
            }
            set
            {
                this.AvailabilityGroupReplicaData.BackupPriority = value;
            }
        }

        /// <summary>
        /// Localizable String for InitialRole enum
        /// </summary>
        public String InitialRoleString
        {
            get
            {
                LocalizableEnumConverter replicaRoleEnumConverter = new LocalizableEnumConverter(typeof(ReplicaRole));
                return replicaRoleEnumConverter.ConvertToString(this.AvailabilityGroupReplicaData.InitialRole);
            }
        }

        /// <summary>
        /// Endpoint name
        /// </summary>
        public String EndpointName
        {
            get
            {
                return this.AvailabilityGroupReplicaData.EndpointName;
            }
            set
            {
                if (this.AvailabilityGroupReplicaData.EndpointCanBeConfigured &&
                    !this.AvailabilityGroupReplicaData.EndpointPresent &&
                    (string.CompareOrdinal(this.AvailabilityGroupReplicaData.EndpointName, value) != 0))
                {
                    this.AvailabilityGroupReplicaData.EndpointName = value;
                    if (string.IsNullOrEmpty(this.AvailabilityGroupReplicaData.EndpointUrl)) //we do not have end point url ever.
                    {
                        this.SetEndpointUrl();
                    }
                }
            }
        }

        /// <summary>
        /// Readable secondary role of the availability replica
        /// </summary>
        public AvailabilityReplicaConnectionModeInSecondaryRole ReadableSecondaryRole
        {
            get
            {
                return this.AvailabilityGroupReplicaData.ReadableSecondaryRole;
            }
            set
            {
                if (!Enum.IsDefined(typeof(AvailabilityReplicaConnectionModeInSecondaryRole), value))
                {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(AvailabilityReplicaConnectionModeInSecondaryRole));
                }

                if (this.AvailabilityGroupReplicaData.ReadableSecondaryRole != value)
                {
                    this.AvailabilityGroupReplicaData.ReadableSecondaryRole = value;
                }
            }
        }

        /// <summary>
        /// Endpoint port address
        /// </summary>
        public int EndpointPortNumber
        {
            get
            {
                return this.AvailabilityGroupReplicaData.EndpointPortNumber;
            }
            set
            {
                if (this.AvailabilityGroupReplicaData.EndpointCanBeConfigured && !this.AvailabilityGroupReplicaData.EndpointPresent)
                {
                    if (this.AvailabilityGroupReplicaData.EndpointPortNumber != value)
                    {
                        this.AvailabilityGroupReplicaData.EndpointPortNumber = value;
                        this.SetEndpointUrl();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the login with which the replicas are connected
        /// </summary>
        public string ConnectedAs
        {
            get
            {
                return ((this.AvailabilityGroupReplicaData.Connection != null) ? this.AvailabilityGroupReplicaData.Connection.TrueLogin : "NotConnected");
            }
        }

        /// <summary>
        /// internal class for get/set EndpointEncryptionAlgorithm, since encryptionAlgorithm need a initial value, we have to write get/set 
        /// </summary>
        public EndpointEncryptionAlgorithm EndpointEncryptionAlgorithm
        {
            get
            {
                return this.AvailabilityGroupReplicaData.EndpointEncryptionAlgorithm;
            }
            set
            {
                this.AvailabilityGroupReplicaData.EndpointEncryptionAlgorithm = value;
            }
        }

        /// <summary>
        /// True if the endpoint is encrypted
        /// </summary>
        public bool IsEndpointEncrypted
        {
            get
            {
                return (this.AvailabilityGroupReplicaData.EndpointEncryption != EndpointEncryption.Disabled);
            }
            set
            {
                if (this.AvailabilityGroupReplicaData.EndpointCanBeConfigured && !this.AvailabilityGroupReplicaData.EndpointPresent)
                {
                    this.AvailabilityGroupReplicaData.EndpointEncryption = value ? (EndpointEncryption.Required) : (EndpointEncryption.Disabled);
                }
            }
        }

        /// <summary>
        /// Use the EndpointPresent in place of (endpoint==null).
        /// When a replica does not have the database mirroring endpoint and users navigate to the 
        /// summary page and click the script button, the script action runs the whole process  
        /// to configure the endpoint in capture mode to generate the scripts and assigns a memory
        /// endpoint objects to the endpoint variable. After navigating back to replicate page and forward
        /// to the summary page again, the endpoint is not null this time and PropertyNotSet exception is
        /// thrown by endpoint.Payload.DatabaseMirroring.EndpointAuthenticationOrder 
        /// </summary>
        public bool IsValidDomainUserForWinAuthentication
        {
            get
            {
                bool useWinAuth = false;
                if (!this.AvailabilityGroupReplicaData.EndpointPresent) //new endpoint
                {
                    useWinAuth = true;
                }
                else
                {
                    EndpointAuthenticationOrder authMode = this.AvailabilityGroupReplicaData.Endpoint.Payload.DatabaseMirroring.EndpointAuthenticationOrder;
                    //endpoint will use certificate, we do not need to check service account
                    if (authMode == Microsoft.SqlServer.Management.Smo.EndpointAuthenticationOrder.Kerberos ||
                        authMode == Microsoft.SqlServer.Management.Smo.EndpointAuthenticationOrder.Negotiate ||
                        authMode == Microsoft.SqlServer.Management.Smo.EndpointAuthenticationOrder.Ntlm)
                    {
                        useWinAuth = true;
                    }
                }
                if (!useWinAuth) //has certificate, always return true
                {
                    return true;
                }
                if (string.IsNullOrEmpty(this.AvailabilityGroupReplicaData.EndpointServiceAccount)) //no service account, not valid
                {
                    return false;
                }
                string[] userparts = this.AvailabilityGroupReplicaData.EndpointServiceAccount.Split('\\');
                if (userparts.Length == 1) //no \, for sure it's not domain user
                {
                    return false;
                }
                if (userparts[0] == ".") //user uses the local machine account.
                {
                    return false;
                }
                //now we need to see if it's domain user name, this will run against the current machine
                if (UserSecurity.IsDomainUserAccount(this.AvailabilityGroupReplicaData.EndpointServiceAccount, Constants.WellKnownSidTypes))
                {
                    return true;
                }
                return false;

            }
        }

        /// <summary>
        /// property for this.availabilityGroupReplicaData.encryption, since encryption need a initial value, we have to write get/set 
        /// </summary>
        public EndpointEncryption EndpointEncryption
        {
            get
            {
                return this.AvailabilityGroupReplicaData.EndpointEncryption;
            }

            set
            {
                this.AvailabilityGroupReplicaData.EndpointEncryption = value;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Get the Server object
        /// </summary>
        /// <returns></returns>
        public Smo.Server GetServer()
        {
            return this.AvailabilityGroupReplicaData.Connection != null ?
                new Microsoft.SqlServer.Management.Smo.Server(this.AvailabilityGroupReplicaData.Connection) : null;
        }

        /// <summary>
        /// Grant the connect right to a set of users
        /// </summary>
        /// <param name="loginNames"></param>
        public void AddGrantServiceAccount(IEnumerable<string> loginNames)
        {
            if (loginNames == null)
            {
                throw new ArgumentNullException("loginNames");
            }

            foreach (string localServiceAccount in loginNames)
            {
                ObjectPermissionSet p = new ObjectPermissionSet(ObjectPermission.Connect);
                this.AvailabilityGroupReplicaData.Endpoint.Grant(p, localServiceAccount);
            }
        }

        /// <summary>
        /// Retrieve a list of pairs of a login name and if it exists in the replica
        /// KeyValuePair contains a localServiceAccount and a flag for the existance of sid
        /// </summary>
        /// <param name="sidServiceAccountPairCollection"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, bool>> GetLoginsForRemoteReplicas(IEnumerable<KeyValuePair<byte[], string>> sidServiceAccountPairCollection)
        {
            Dictionary<string, bool> loginNames = new Dictionary<string, bool>();

            if (sidServiceAccountPairCollection != null && sidServiceAccountPairCollection.Any())
            {
                Smo.Server server = GetServer();

                foreach (KeyValuePair<byte[], string> pair in sidServiceAccountPairCollection)
                {
                    byte[] sid = pair.Key;
                    string serviceAccount = pair.Value;

                    string localServiceAccount;

                    bool sidExisted = this.ExistsSID(server, sid, out localServiceAccount);

                    // if the sid does not exist, use the input the service account.
                    if (!sidExisted)
                    {
                        localServiceAccount = serviceAccount;
                    }

                    if (!this.ValidDomainUserFormat(localServiceAccount) || loginNames.ContainsKey(localServiceAccount))
                    {
                        continue;
                    }
                    loginNames.Add(localServiceAccount, sidExisted);
                }
            }

            return loginNames;
        }

        /// <summary>
        /// will use the primary replica endpoint encryption setting to adjust this replica endpoint encryption setting
        /// </summary>
        /// <param name="primaryReplica">primary replica</param>
        public void AdjustEndpointEncryption(AvailabilityGroupReplica primaryReplica)
        {
            if (this.AvailabilityGroupReplicaData.InitialRole != ReplicaRole.Primary && primaryReplica != null && !this.AvailabilityGroupReplicaData.EndpointPresent)
            {
                this.AvailabilityGroupReplicaData.EndpointEncryption = primaryReplica.EndpointEncryption;
                this.AvailabilityGroupReplicaData.EndpointEncryptionAlgorithm = primaryReplica.EndpointEncryptionAlgorithm;
            }
        }

        /// <summary>
        /// method to determine if it is a valid endpoint
        /// </summary>
        /// <returns></returns>
        public bool IsValidEndpoint()
        {
            if (!this.AvailabilityGroupReplicaData.EndpointCanBeConfigured)
                return true;
            if (string.IsNullOrEmpty(this.AvailabilityGroupReplicaData.EndpointUrl))
                return false;
            if (string.IsNullOrEmpty(this.AvailabilityGroupReplicaData.EndpointName))
                return false;

            if (this.AvailabilityGroupReplicaData.EndpointPortNumber <= 0)
                return false;
            return true;
        }

        /// <summary>
        /// Configure Endpoint of this replica
        /// </summary>
        public void ConfigureEndpoint()
        {
            if (!this.AvailabilityGroupReplicaData.EndpointCanBeConfigured)
            {
                throw new InvalidOperationException();
            }

            // Don't configure endpoints for existing replicas though we will grant permissions
            if (this.AvailabilityGroupReplicaData.State != AvailabilityObjectState.Existing)
            {
                if (this.AvailabilityGroupReplicaData.EndpointPresent)
                {
                    this.AlterEndpoint();
                }
                else
                {
                    this.CreateEndpoint();
                }
            }
        }

        /// <summary>
        /// method for altering the endpoint used by ConfigureEndpoint()
        /// </summary>
        private void AlterEndpoint()
        {
            if (this.AvailabilityGroupReplicaData.AvailabilityMode != AvailabilityReplicaAvailabilityMode.ConfigurationOnly
                && this.AvailabilityGroupReplicaData.Endpoint.Payload.DatabaseMirroring.ServerMirroringRole == Microsoft.SqlServer.Management.Smo.ServerMirroringRole.Witness)
            {
                this.AvailabilityGroupReplicaData.Endpoint.Payload.DatabaseMirroring.ServerMirroringRole = Microsoft.SqlServer.Management.Smo.ServerMirroringRole.All;
                this.AvailabilityGroupReplicaData.Endpoint.Alter();
            }

            this.AvailabilityGroupReplicaData.Endpoint.Start();
        }

        /// <summary>
        /// Set Endpoint URL method
        /// </summary>
        public void SetEndpointUrl()
        {
            if (string.IsNullOrEmpty(this.AvailabilityGroupReplicaData.EndpointName)) //this means there is no endpoint.
            {
                this.AvailabilityGroupReplicaData.EndpointUrl = string.Empty;
                return;
            }

            string hostName = string.Empty;
            try
            {
                Smo.Server server = this.GetServer();
                hostName = server.Information.FullyQualifiedNetName;
            }
            catch { hostName = this.AvailabilityGroupReplicaData.ReplicaName; }
            this.AvailabilityGroupReplicaData.EndpointUrl = "TCP://" + hostName + ":" + this.AvailabilityGroupReplicaData.EndpointPortNumber;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Check if the service account has the valid format.
        /// </summary>
        /// <param name="serviceAccount"></param>
        /// <returns></returns>
        private bool ValidDomainUserFormat(string serviceAccount)
        {
            if (string.IsNullOrEmpty(serviceAccount)) //no service account, not valid
            {
                return false;
            }

            string[] userparts = serviceAccount.Split('\\');

            if (userparts.Length == 1) //no \, for sure it's not domain user
            {
                return false;
            }

            if (userparts[0] == ".") //user uses the local machine account.
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determine if a login name exists in an server instance given its SID.
        /// If the login name already exists, a true value is returned and the local login name also
        /// output; otherwise, it returns false and the output of login name is empty.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="sid"></param>
        /// <param name="loginName"></param>
        /// <returns></returns>
        private bool ExistsSID(Smo.Server server, byte[] sid, out string loginName)
        {
            loginName = string.Empty;
            bool retValue = false;

            Enumerator e = new Enumerator();

            Request r = new Request(new Urn(string.Format(CultureInfo.InvariantCulture, SidConditionFormat, this.ByteArrayToString(sid))));
            r.Fields = new string[1] { "Name" };

            try
            {
                EnumResult result = e.Process(server.ConnectionContext, r);

                DataTable dataTable = result.Data as DataTable;

                if (dataTable != null)
                {
                    DataRowCollection dataRowCollection = dataTable.Rows;

                    if (dataRowCollection.Count > 0)
                    {
                        DataRow row = dataRowCollection[0];
                        loginName = (string)row["Name"];

                        // put this line after the login name access because the name value could be DB.NULL or the type mismatch
                        retValue = true;
                    }
                }
            }
            catch
            {
                // If an exception is thrown, we do not need to do any process here.
            }

            return retValue;
        }

        /// <summary>
        /// Transfer a byte array to the hex string
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        private string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);

            foreach (byte b in ba)
            {
                hex.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
            }

            return hex.ToString();
        }

        /// <summary>
        /// Create logins for the service accounts of other replicas, which do not exist.
        /// </summary>
        /// <param name="loginNames"></param>
        public void CreateLogins(IEnumerable<string> loginNames)
        {
            if (loginNames != null && loginNames.Count() > 0)
            {
                Smo.Server server = this.GetServer();

                foreach (string localServiceAccount in loginNames)
                {
                    if (!server.Logins.Contains(localServiceAccount) && !server.Logins.Contains(localServiceAccount.ToUpperInvariant())) //there will be sitiuation for WA VM, the login already exist.
                    {
                        Microsoft.SqlServer.Management.Smo.Login login = new Microsoft.SqlServer.Management.Smo.Login(server, localServiceAccount);
                        login.LoginType = LoginType.WindowsUser;
                        login.Create();
                    }
                }
            }
        }

        /// <summary>
        /// Get the Sid of the service account 
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        private byte[] GetServiceAccountSid(Smo.Server server)
        {
            byte[] sid = null;
            Enumerator enumerator = new Enumerator();

            Request r = new Request(new Urn("Server"));

            // include the ServiceAccount to generate T-SQL variable as input parameter for getting ServiceAccountSid
            r.Fields = new string[2] { "ServiceAccount", "ServiceAccountSid" };

            try
            {
                EnumResult result = enumerator.Process(server.ConnectionContext, r);

                DataTable dataTable = result.Data as DataTable;

                if (dataTable != null)
                {
                    DataRowCollection dataRowCollection = dataTable.Rows;

                    if (dataRowCollection.Count > 0)
                    {
                        DataRow row = dataRowCollection[0];
                        sid = (byte[])row["ServiceAccountSid"];
                    }
                }
            }
            catch
            {
                // Any exception happens, we do not need to do any process here.
            }

            return sid;
        }

        /// <summary>
        /// method for creating the endpoint used by ConfigureEndpoint()
        /// </summary>
        private void CreateEndpoint()
        {
            this.AvailabilityGroupReplicaData.Endpoint = new Endpoint(GetServer(), this.AvailabilityGroupReplicaData.EndpointName);
            this.AvailabilityGroupReplicaData.Endpoint.ProtocolType = ProtocolType.Tcp;
            this.AvailabilityGroupReplicaData.Endpoint.EndpointType = EndpointType.DatabaseMirroring;
            this.AvailabilityGroupReplicaData.Endpoint.Protocol.Tcp.ListenerPort = this.AvailabilityGroupReplicaData.EndpointPortNumber;
            this.AvailabilityGroupReplicaData.Endpoint.Payload.DatabaseMirroring.EndpointEncryption = this.AvailabilityGroupReplicaData.EndpointEncryption;

            // By default, we use the AES algorithm for endpoint encryption.
            // Previously we used the RC4 algorithm by default, but this algorithm
            // has been deprecated by the security team in SQL11, replaced by AES 
            // as the recommended default. (See TFS 687579)
            if (this.AvailabilityGroupReplicaData.EndpointEncryption != EndpointEncryption.Disabled)
            {
                this.AvailabilityGroupReplicaData.Endpoint.Payload.DatabaseMirroring.EndpointEncryptionAlgorithm = this.EndpointEncryptionAlgorithm;
            }

            this.AvailabilityGroupReplicaData.Endpoint.Payload.DatabaseMirroring.ServerMirroringRole =
                this.AvailabilityGroupReplicaData.AvailabilityMode == AvailabilityReplicaAvailabilityMode.ConfigurationOnly ?
                    ServerMirroringRole.Witness : ServerMirroringRole.All;

            this.AvailabilityGroupReplicaData.Endpoint.Create();

            try
            {
                this.AvailabilityGroupReplicaData.Endpoint.Start();
            }
            catch
            {
                // Drop the endpoint object if it failed to start, so that it can be recreated after fixing the problem
                this.AvailabilityGroupReplicaData.Endpoint.Drop();
                throw;
            }
        }

        #endregion
    }
}
