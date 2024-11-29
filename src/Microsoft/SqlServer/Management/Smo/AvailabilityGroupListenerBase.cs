// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// this is the partial class of code-gen AvailabilityGroupListener
    /// </summary>
    public partial class AvailabilityGroupListener : NamedSmoObject, ICreatable, IDroppable, IDropIfExists, IAlterable, IScriptable
    {
        #region Ctor

        internal AvailabilityGroupListener(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        #endregion

        #region Properties 

        /// <summary>
        /// Gets the collection of replicas participating in the availability group.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.OneToAny, typeof(AvailabilityGroupListenerIPAddress))]
        public AvailabilityGroupListenerIPAddressCollection AvailabilityGroupListenerIPAddresses
        {
            get
            {
                if (this.availabilityGroupListenerIPAddresses == null)
                {
                    this.availabilityGroupListenerIPAddresses = new AvailabilityGroupListenerIPAddressCollection(this);
                }

                return this.availabilityGroupListenerIPAddresses;
            }
        }

        /// <summary>
        /// Gets returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "AvailabilityGroupListener";
            }
        }

        #endregion 

        #region Public Interface

        /// <summary>
        /// Restart the listener
        /// </summary>
        public void RestartListener()
        {
            // ALTER AVAILABILITY GROUP ag_name
            // RESTART LISTENER N'dnsName'
            this.CheckObjectState(!this.ExecutionManager.Recording); // make sure the object has been retrieved from the backend if we are going to execute the script

            try
            {
                string script = Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space +
                    SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.space + 
                    AvailabilityGroupListener.RestartListenerScript + Globals.space + SqlSmoObject.MakeSqlString(this.Name) +
                    Globals.statementTerminator;

                this.ExecutionManager.ExecuteNonQuery(script);

                this.GenerateAlterEvent();
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.RestartListenerFailed(this.Name, this.Parent.Name), e);
            }
        }

        #region ICreatable Members

        /// <summary>
        /// Create an availability group listener for the availability group.
        /// </summary>
        public void Create()
        {
            this.CreateImpl();
        }

        #endregion

        #region IDroppable Members

        /// <summary>
        /// Drop an availaiblity group listener from the availability group.
        /// </summary>
        public void Drop()
        {
            this.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        #endregion

        #region IAlterable Members
        /// <summary>
        /// Alter an existing availabilit group listener options
        /// </summary>
        public void Alter()
        {
            this.CheckObjectState(true);
            this.AlterImpl();
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
        /// Composes the create script for the availability group listener object.
        /// This requires the AG must exist.
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
             * [ LISTENER 'dnsName' ( <listener_option> ) ]
             * 
             * <listener_option> ::=
             * WITH DHCP  [ ON   <ip_addressnetwork_subnet_option>  ]
             * |   WITH IP
             * ( <ip_address_option> [ , <ip_address_option>' ] )
             * [ ,  PORT=listenerPort ]

             * <ip_address_option> ::=
             * ( '4-part-ip' ,  '4-part-ip-mask' )
                | ( 'ip_address_v6' )
             *  <network_subnet_option> :: =
                ('4-part-ip', '4-part-ip-mask')
             * 
             */

            // Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersion);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);
            this.ValidateIPAddresses();

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // Check that the availability group listener exists before dropping it
                string name = this.FormatFullNameForScripting(sp, false);
                string parentName = this.Parent.FormatFullNameForScripting(sp, false);
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_GROUP_LISTENER, "NOT", name, parentName);
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            // Script the availability group name
            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.newline);
            script.Append(Scripts.ADD + Globals.space + AvailabilityGroup.ListenerScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlString(this.Name) + Globals.space + Globals.LParen);

            // Add script for listener options spec 
            script.Append(this.ScriptListenerOptions());
            script.Append(Globals.RParen);

            // Add statement terminator
            script.Append(Globals.statementTerminator);

            // Close existence check, if necessary
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            query.Add(script.ToString());

            return;
        }

        /// <summary>
        /// validate the IP addresses used for Create AG or Alter AG Add IP
        /// </summary>
        internal void ValidateIPAddresses()
        {
            // can not create the listener if there is no ipaddress
            if (this.AvailabilityGroupListenerIPAddresses == null || this.AvailabilityGroupListenerIPAddresses.Count < 1)
            {
                throw new SmoException(ExceptionTemplates.ObjectWithNoChildren(AvailabilityGroupListener.UrnSuffix, AvailabilityGroupListenerIPAddress.UrnSuffix));
            }

            bool useDHCP = this.AvailabilityGroupListenerIPAddresses[0].IsDHCP;

            // can not create/add the listener if there is hybrid setting in IP addresses (means DHCP+static IP)
            foreach (AvailabilityGroupListenerIPAddress ip in this.AvailabilityGroupListenerIPAddresses)
            {
                if (ip.IsDHCP != useDHCP)
                {
                    throw new SmoException(ExceptionTemplates.WrongHybridIPAddresses(AvailabilityGroupListener.UrnSuffix));
                }
            }

            // if it's DHCP case, we can  only have one IP address
            if (useDHCP)
            {
                if (this.AvailabilityGroupListenerIPAddresses.Count > 1)
                {
                    throw new SmoException(ExceptionTemplates.WrongMultiDHCPIPAddresses(AvailabilityGroupListener.UrnSuffix));
                }

                // DHCP must be IPv4 or empty
                if (!string.IsNullOrEmpty(this.AvailabilityGroupListenerIPAddresses[0].IPAddress) && this.AvailabilityGroupListenerIPAddresses[0].IsIPv6)
                {
                    throw new SmoException(ExceptionTemplates.WrongDHCPv6IPAddress(AvailabilityGroupListener.UrnSuffix));
                }
            }
        }

        /// <summary>
        /// script out the listener options
        /// </summary>
        /// <returns>the script for listener options</returns>
        internal string ScriptListenerOptions()
        {
            /*
             * <listener_option> ::=
             * WITH DHCP  [ ON   <ip_addressnetwork_subnet_option>  ]
             * |   WITH IP
             * ( <ip_address_option> [ , <ip_address_option>' ] )
             * [ ,  PORT=listenerPort ]

             * <ip_address_option> ::=
             * ( '4-part-ip' ,  '4-part-ip-mask' )
                | ( 'ip_address_v6' )
             *  <network_subnet_option> :: =
                ('4-part-ip', '4-part-ip-mask')
             */
            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            script.Append(Globals.newline);

            if (this.AvailabilityGroupListenerIPAddresses[0].IsDHCP)
            {
                script.Append(AvailabilityGroupListener.WithDHCPScript + Globals.newline);
                
                // DDL support  with DHCP without any IP address
                if (!string.IsNullOrEmpty(this.AvailabilityGroupListenerIPAddresses[0].SubnetIP))
                {
                    script.Append(Globals.space + Globals.On + Globals.space + Globals.LParen);
                    script.Append(SqlSmoObject.MakeSqlString(this.AvailabilityGroupListenerIPAddresses[0].SubnetIP) + Globals.comma + Globals.space + SqlSmoObject.MakeSqlString(this.AvailabilityGroupListenerIPAddresses[0].SubnetMask));
                    script.Append(Globals.newline + Globals.RParen + Globals.newline);
                }
            }
            else
            {
                script.Append(AvailabilityGroupListener.WithIPScript + Globals.newline + Globals.LParen);
                bool needComma = false;
                foreach (AvailabilityGroupListenerIPAddress ip in this.AvailabilityGroupListenerIPAddresses)
                {
                    if (needComma)
                    {
                        script.Append(Globals.comma + Globals.newline);
                    }

                    if (ip.IsIPv6)
                    {
                        script.Append(Globals.LParen + SqlSmoObject.MakeSqlString(ip.IPAddress) + Globals.RParen);
                    }
                    else
                    {
                        script.Append(Globals.LParen + SqlSmoObject.MakeSqlString(ip.IPAddress) + Globals.comma + Globals.space + SqlSmoObject.MakeSqlString(ip.SubnetMask) + Globals.RParen);
                    }

                    needComma = true;
                }

                script.Append(Globals.newline + Globals.RParen + Globals.newline);
            }

            script.Append(Globals.comma + Globals.space + AvailabilityGroup.PortScript + Globals.EqualSign + this.PortNumber.ToString());
            return script.ToString();
        }

        /// <summary>
        /// we can only change the port of a listener, add IP address will be done in AGListenerIPAddress class 
        /// </summary>
        /// <param name="query">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            // sanity checks
            tc.Assert(null != query, "String collection should not be null");

            /*
             * ALTER AVAILABILITY GROUP 'group_name'
             * MODIFY LISTENER 'dnsName'
             * (
             *   PORT=listenerPort
             * )
             */

            // Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersion);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // Check that the availability group listener exists before dropping it
                string name = this.FormatFullNameForScripting(sp, false);
                string parentName = this.Parent.FormatFullNameForScripting(sp, false);
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_GROUP_LISTENER, string.Empty, name, parentName);
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.newline);
            script.Append(AvailabilityGroupListener.ModifyListenerScript + Globals.space + SqlSmoObject.MakeSqlString(this.Name) + Globals.space + Globals.LParen);
            script.Append(AvailabilityGroup.PortScript + Globals.EqualSign + this.PortNumber);
            script.Append(Globals.RParen);

            // Add statement terminator
            script.Append(Globals.statementTerminator);

            // Close existence check, if necessary
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            query.Add(script.ToString());
            return;
        }

        /// <summary>
        /// Create the script to alter the availability group listener
        /// </summary>
        /// <param name="dropQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptDrop(System.Collections.Specialized.StringCollection dropQuery, ScriptingPreferences sp)
        {
            // sanity checks
            tc.Assert(null != dropQuery, "String collection should not be null");

            // ALTER AVAILABILITY GROUP ag_name
            // REMOVE LISTENER 'dnsName'

            // Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersion);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // Check that the availability group listener exists before dropping it
                string name = this.FormatFullNameForScripting(sp, false);
                string parentName = this.Parent.FormatFullNameForScripting(sp, false);
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_GROUP_LISTENER, string.Empty, name, parentName);
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.newline);
            script.Append(Scripts.REMOVE + Globals.space + AvailabilityGroup.ListenerScript + Globals.space + SqlSmoObject.MakeSqlString(this.Name));

            // Add statement terminator
            script.Append(Globals.statementTerminator);

            // Close existence check, if necessary
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            dropQuery.Add(script.ToString());
            return;
        }

        /// <summary>
        /// This method ensures that each IP Address in the AvailabilityGroupListenerIPAddresses collection
        /// has a key after creation.  The work of fetching the key is delegated to the FetchKeyPostCreate
        /// method in the AvailabilityGroupListenerIpAddress class.
        /// </summary>
        internal void FetchIpAddressKeysPostCreate()
        {
            foreach (AvailabilityGroupListenerIPAddress ip in this.AvailabilityGroupListenerIPAddresses)
            {
                ip.FetchKeyPostCreate();
            }
        }

        /// <summary>
        /// Propagate information to IP address collections
        /// </summary>
        /// <param name="action">the action to propagate</param>
        /// <returns>The propagation targets</returns>
        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (PropagateAction.Create == action)
            {
                PropagateInfo[] propagateInfoArray = { new PropagateInfo(this.AvailabilityGroupListenerIPAddresses, false) };
                return propagateInfoArray;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// After creation, but before propagation, we need to populate the keys for IP addresses 
        /// obtained through DHCP.
        /// </summary>
        protected override void PostCreate()
        {
            this.FetchIpAddressKeysPostCreate();
        }

        #endregion

        #region Constants

        // Scripting constants, the suffix 'Script' is added for disambiguation from class names
        internal const string ModifyListenerScript = "MODIFY LISTENER";
        internal const string RestartListenerScript = "RESTART LISTENER";
        internal const string WithDHCPScript = "WITH DHCP";
        internal const string WithIPScript = "WITH IP";

        #endregion

        #region private members

        private static readonly TraceContext tc = TraceContext.GetTraceContext(SmoApplication.ModuleName, "AvailabilityGroupListener");
        private AvailabilityGroupListenerIPAddressCollection availabilityGroupListenerIPAddresses = null;

        #endregion
    }
}

