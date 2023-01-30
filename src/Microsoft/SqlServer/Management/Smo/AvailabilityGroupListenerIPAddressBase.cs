// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// this is the partial class of code-gen AvailabilityGroupListenerIPAddress
    /// </summary>
    public partial class AvailabilityGroupListenerIPAddress : SqlSmoObject, ICreatable, IScriptable
    {
        #region Ctor

        public AvailabilityGroupListenerIPAddress() : base()
        {
        }

        public AvailabilityGroupListenerIPAddress(AvailabilityGroupListener availabilityGroupListener) : base()
        {
            this.Parent = availabilityGroupListener;
        }

        internal AvailabilityGroupListenerIPAddress(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        #endregion Ctor

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this is IP v4.
        /// from the DMV spec:ip_subnet_mask The availability group listener's configured IP subnet mask
        /// Lists the subnet mask for the ip_v4 address configured for the listener
        /// Null if it's Ipv6
        /// </summary>
        public bool IsIPv6
        {
            get
            {
                return string.IsNullOrEmpty(this.SubnetMask);
            }
        }

        /// <summary>
        /// Gets or sets the IPaddress of the Availability Group Listener
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string IPAddress
        {
            get
            {
                return ((AvailabilityGroupListenerIPAddressObjectKey)key).IPAddress;
            }

            set
            {
                try
                {
                    ((AvailabilityGroupListenerIPAddressObjectKey)key).IPAddress = value;
                    UpdateObjectState();
                }
                catch (Exception e)
                {
                    FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.SetIpAddress, this, e);
                }
            }
        }

        /// <summary>
        /// Gets or sets the subnet IP mask of the Availability Group Listener
        /// </summary>
        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string SubnetMask
        {
            get
            {
                return ((AvailabilityGroupListenerIPAddressObjectKey)key).SubnetMask;
            }

            set
            {
                try
                {
                    ((AvailabilityGroupListenerIPAddressObjectKey)key).SubnetMask = value;
                    UpdateObjectState();
                }
                catch (Exception e)
                {
                    FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.SetSubnetMask, this, e);
                }
            }
        }

        /// <summary>
        /// Gets or sets the subnet IP of the Availability Group Listener
        /// </summary>
        [SfcKey(2)]
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string SubnetIP
        {
            get
            {
                return ((AvailabilityGroupListenerIPAddressObjectKey)key).SubnetIP;
            }

            set
            {
                try
                {
                    ((AvailabilityGroupListenerIPAddressObjectKey)key).SubnetIP = value;
                    UpdateObjectState();
                }
                catch (Exception e)
                {
                    FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.SetSubnetIp, this, e);
                }
            }
        }

        /// <summary>
        /// Gets the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "AvailabilityGroupListenerIPAddress";
            }
        }

        #endregion

        #region Public Interface

        #region ICreatable Members

        /// <summary>
        /// add an availability group listener IP address into the availability group listener.
        /// </summary>
        public void Create()
        {
            this.CreateImpl();
        }

        #endregion

        #region IScriptable Members

        /// <summary>
        /// Generate a create script this availability group listener.
        /// </summary>
        /// <returns>string collection contains the T-SQL</returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Generate a script for this availability group listener using the
        /// specified scripting options.
        /// </summary>
        /// <param name="scriptingOptions">the script object</param>
        /// <returns>string collection contains the T-SQL</returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        #endregion

        #endregion

        #region override methods

        /// <summary>
        /// Composes the add ip address script for the availability group listener IP address object.
        /// This requires the AG listener must exist.
        /// </summary>
        /// <param name="query">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptCreate(System.Collections.Specialized.StringCollection query, ScriptingPreferences sp)
        {
            // sanity checks
            tc.Assert(null != query, "String collection should not be null");

            /*
             * ALTER AVAILABILITY GROUP 'group_name'
             * MODIFY LISTENER 'dnsName'
             * (
             *   ADD IP <ip_address_option>
             * )
             *
             *  <ip_address_option> ::=
             *  ( 4-part-ip ,  4-part-ip-mask )
             *   | ( 'ip_address_v6' )
             */

            // Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            // IsDHCP cannot be set to true, since no sensible script can be produced in this case.
            if (this.GetPropValueOptional(IsDHCPPropertyName) != null && this.IsDHCP == true)
            {
                throw new InvalidOperationException(ExceptionTemplates.CannotAddDHCPIPAddress(UrnSuffix, IsDHCPPropertyName));
            }

            // The IPAddress key must be set.
            if (String.IsNullOrEmpty(this.IPAddress))
            {
                throw new PropertyNotSetException(IpAddressPropertyName);
            }

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            // Script the availability group name
            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Parent.Name) + Globals.newline);
            script.Append(AvailabilityGroupListener.ModifyListenerScript + Globals.space + SqlSmoObject.MakeSqlString(this.Parent.Name) + Globals.newline);
            script.Append(Globals.LParen + Scripts.ADD + Globals.space + AvailabilityGroupListenerIPAddress.IPScript + Globals.space + Globals.LParen);

            if (this.IsIPv6)
            {
                script.Append(SqlSmoObject.MakeSqlString(this.IPAddress));
            }
            else
            {
                script.Append(SqlSmoObject.MakeSqlString(this.IPAddress) + Globals.comma + Globals.space + SqlSmoObject.MakeSqlString(this.SubnetMask));
            }

            script.Append(Globals.RParen);
            script.Append(Globals.newline + Globals.RParen);

            // Add statement terminator
            script.Append(Globals.statementTerminator);

            query.Add(script.ToString());
            return;
        }

        /// <summary>
        /// Gets an empty AvailabilityGroupListenerIPAddressObjectKey
        /// </summary>
        /// <returns>An empty key for this object</returns>
        internal override ObjectKeyBase GetEmptyKey()
        {
            return new AvailabilityGroupListenerIPAddressObjectKey();
        }

        /// <summary>
        /// This object does not need to have a key specified to enter the Creating state,
        /// since this key can be filled in after creation.  So we override this method
        /// to remove the "key!=null" constraint.
        /// </summary>
        internal override void UpdateObjectState()
        {
            if (this.State == SqlSmoState.Pending && null != ParentColl)
            {
                SetState(SqlSmoState.Creating);
            }
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// If the IP address was created with DHCP, no keys were specified by the user.
        /// After creation we need to fetch the keys from the server.  Note that we don't
        /// override PostCreate here because it's impossible to create a standalone IP Address
        /// with the DHCP setting (this is a limitation on the DDL).  You can only create a
        /// static IP address, in which case we do not need to fetch the key.  This method is
        /// called from PostCreate in the parent class, AvailabilityGroupListener.
        /// </summary>
        internal void FetchKeyPostCreate()
        {
            // Only fetch the key when we're in execution mode and the address is DHCP-leased.
            if (!ExecutionManager.Recording && this.IsDHCP)
            {
                try
                {
                    // We shall query the enumerator to read the key. We will find the row in the
                    // ip address table where [@IsDHCP=1]. There should only be one such row. If there
                    // is more than one we will throw an exception.
                    string[] fields = new string[] { IpAddressPropertyName, SubnetMaskPropertyName, SubnetIpPropertyName };
                    Urn urn = string.Format(CultureInfo.InvariantCulture, "{0}/{1}[@{2}={3}]", this.Parent.Urn, UrnSuffix, IsDHCPPropertyName, 1);
                    Request req = new Request(urn, fields);
                    DataTable resultTable = this.ExecutionManager.GetEnumeratorData(req);

                    if (resultTable != null && resultTable.Rows.Count == 1)
                    {
                        // This is the expected case. There should only be one row where IsDHCP is true.
                        DataRow resultRow = resultTable.Rows[0];
                        this.IPAddress = resultRow[IpAddressPropertyName] as string;
                        this.SubnetIP = resultRow[SubnetMaskPropertyName] as string;
                        this.SubnetMask = resultRow[SubnetIpPropertyName] as string;
                    }
                    else
                    {
                        // In this case, more than one result was returned, which should never happen.
                        throw new SmoException(ExceptionTemplates.GetDHCPAddressFailed(this.Parent.Name, resultTable.Rows.Count));
                    }
                }
                catch (Exception e)
                {
                    FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.GetDHCPAddress, this.Parent, e);
                }
            }
        }

        #endregion

        #region Constants

        internal const string IpAddressPropertyName = "IPAddress";
        internal const string SubnetMaskPropertyName = "SubnetMask";
        internal const string SubnetIpPropertyName = "SubnetIP";
        internal const string IsDHCPPropertyName = "IsDHCP";

        #endregion

        #region private members

        private static readonly TraceContext tc = TraceContext.GetTraceContext(SmoApplication.ModuleName, "AvailabilityGroupListenerIPAddress");
        private const string IPScript = "IP";

        #endregion
    }
}

