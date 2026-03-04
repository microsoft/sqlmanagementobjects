using System;
using System.Text;
using System.Management;
using System.Data;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	public partial class Service : WmiSmoObject, Cmn.IAlterable
	{
		internal Service(WmiCollectionBase parentColl, string name) : 
			base(parentColl, name)
		{
		}

		// returns the name of the type in the urn expression
		internal static string UrnSuffix
		{
			get 
			{
				return "Service";
			}
		}

		public event CompletedEventHandler ManagementStateChange;
		
		public void Alter()	
		{
			try
			{
				AlterImplWorker();
                
				// generate internal events
				if( ! SmoApplication.eventsSingleton.IsNullObjectAltered() )
				{
					SmoApplication.eventsSingleton.CallObjectAltered(GetManagedComputer(), 
						new ObjectAlteredEventArgs(this.Urn, this));
				}
			}
			catch( Exception e)
			{
				SqlSmoObject.FilterException(e);

				throw new FailedOperationException(ExceptionTemplates.Alter, this, e);
			}
		}

		private void AlterImplWorker()	
		{
			ManagementObject mo = GetManagementObject();
			
			Property pStartMode = Properties.Get("StartMode");
			if( pStartMode.Dirty )
			{
				ServiceStartMode sm =  (ServiceStartMode)pStartMode.Value;
				InvokeMgmtMethod( mo, "SetStartMode", new object[] { Convert.ToUInt32(sm, SmoApplication.DefaultCulture) } );
			}

			// iterate the property bag and see what has changed
			if( null != advancedProperties )
			{
				foreach( Property p in advancedProperties )
				{
					if( p.Writable && p.Dirty )
					{
						ManagementObject mop = GetPropertyManagementObject(p.Name);
						if (null == mop)
							throw new InternalSmoErrorException(ExceptionTemplates.CouldNotFindManagementObject("AdvancedProperty", p.Name));

						if (p.Type.Equals(typeof(System.Boolean)))
						{
							mop.SetPropertyValue("PropertyNumValue", (bool)p.Value);
							InvokeMgmtMethod(mop, "SetNumericalValue", new object[] { (bool)p.Value });
						}
						else if (p.Type.Equals(typeof(System.Int64)))
						{
							mop.SetPropertyValue("PropertyNumValue", (Int64)p.Value);
							InvokeMgmtMethod( mop, "SetNumericalValue", new object[] { (Int64)p.Value} );
						}
						else
						{
							mop.SetPropertyValue("PropertyStrValue", p.Value.ToString());
							InvokeMgmtMethod( mop, "SetStringValue", new object[] { p.Value.ToString() } );
						}
					}
				}
			}

		}

		// This function returns the management object that we can use to 
		// alter the protocol's properties
		private ManagementObject GetManagementObject()
		{
			try
			{
				ManagementScope ms = GetManagedComputer().ManagementScope;
				
				// get the path to the service
				StringBuilder sbServicePath = new StringBuilder( Globals.INIT_BUFFER_SIZE );
				
				sbServicePath.AppendFormat(SmoApplication.DefaultCulture, "SqlService.ServiceName=\"{0}\"",  Name );
				
				sbServicePath.AppendFormat(SmoApplication.DefaultCulture, ",SQLServiceType={0}", 
											(int)Properties["Type"].Value );
				ManagementPath mp = new ManagementPath(sbServicePath.ToString());
				
				return new ManagementObject(ms, mp, new ObjectGetOptions() );
			}
			catch( Exception e )
			{
				throw new ServiceRequestException( ExceptionTemplates.InnerWmiException, e);
			}
		}

		private void InvokeServiceMethod( string methodName )
		{
			try
			{
				ManagementOperationObserver observer = new ManagementOperationObserver();
				observer.Completed += new CompletedEventHandler(OnCompletedMessage);
				InvokeMgmtMethod( GetManagementObject(), observer, methodName, null);
			}
			catch(Exception e)
			{
				SqlSmoObject.FilterException(e);

				throw new FailedOperationException(ExceptionTemplates.FailedOperationExceptionText(methodName, ExceptionTemplates.Service, this.Name), e);
			}
		}
		
		public void Start()
		{
			InvokeServiceMethod( "StartService" );
		}

		public void Stop()
		{
			InvokeServiceMethod( "StopService" );
		}
		
		public void Pause()
		{
			InvokeServiceMethod( "PauseService" );
		}
		
		public void Resume()
		{
			InvokeServiceMethod( "ResumeService" );
		}

        public void ChangeHadrServiceSetting(bool enable)
        {
            ManagementObject hadrObject = GetHADRManagementObject();
            if (hadrObject == null)
            {
                throw new NotSupportedException("ChangeHadrServiceSetting");
            }
            InvokeMgmtMethod(hadrObject, "ChangeHADRService",
                new object[] { (UInt32)(enable ? 1 : 0) });
        }

        public bool IsHadrEnabled
        {
            get
            {
                ManagementObject hadrObject = GetHADRManagementObject();
                if (hadrObject != null)
                {
                    object o = hadrObject.GetPropertyValue("HADRServiceEnabled");
                    if (o != null)
                    {
                        return (bool)o;
                    }
                }
                throw new NotSupportedException("IsHadrEnabled");
            }
        }
        private ManagementObject GetHADRManagementObject()
        {
            const string sqlserverPrefix = "MSSQL$";
            if ((ManagedServiceType)Properties["Type"].Value != ManagedServiceType.SqlServer)
            {
                return null;
            }

            string query = "select * from HADRServiceSettings where InstanceName = '{0}'";
            //strip out MSSQL$ for instanced SQL server
            if (this.Name.StartsWith(sqlserverPrefix, StringComparison.OrdinalIgnoreCase))
            {
                query = String.Format(SmoApplication.DefaultCulture, query, this.Name.Substring(sqlserverPrefix.Length));
            }
            else
            {
                query = String.Format(SmoApplication.DefaultCulture, query, this.Name);
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            searcher.Scope = GetManagedComputer().ManagementScope;
            //we shall only get one
            foreach (ManagementObject prop in searcher.Get())
            {
                return prop;
            }
            return null;
        }
		public void SetServiceAccount(string userName, string password)
		{
			try
			{
				if (null == userName)
					throw new ArgumentNullException("userName");

				InvokeMgmtMethod(GetManagementObject(), "SetServiceAccount", 
									new object[] { userName, password });
				Properties.Get("ServiceAccount").SetValue(userName);
			}
			catch (Exception e)
			{
				SqlSmoObject.FilterException(e);

				throw new FailedOperationException(ExceptionTemplates.SetServiceAccount, this, e);
			}
		}
		
		public void ChangePassword(string oldPassword, string newPassword)
		{
			try
			{
				InvokeMgmtMethod(GetManagementObject(), "SetServiceAccountPassword", 
									new object[] { oldPassword, newPassword });
			}
			catch (Exception e)
			{
				SqlSmoObject.FilterException(e);

				throw new FailedOperationException(ExceptionTemplates.ChangeServicePassword, this, e);
			}
		}

        internal void OnCompletedMessage(object sender, CompletedEventArgs e)
        {
            // pass the event to the user
            if (null != ManagementStateChange)
            {
                ManagementStateChange(this, e);
            }
        }

        private ManagementObject GetPropertyManagementObject(string propertyName)
        {
			ManagementScope ms = GetManagedComputer().ManagementScope;

			ManagementObjectSearcher searcher = new 
			ManagementObjectSearcher("select * from SqlServiceAdvancedProperty");
			searcher.Scope = GetManagedComputer().ManagementScope;
			foreach (ManagementObject prop in searcher.Get()) 
			{
				// service name is compared without case sensitivity
				if( 0 == string.Compare((string)prop.GetPropertyValue("ServiceName"), this.Name, StringComparison.OrdinalIgnoreCase) && 
					(ManagedServiceType)(UInt32)prop.GetPropertyValue("SqlServiceType") == (ManagedServiceType)Properties["Type"].Value &&
					(string)prop.GetPropertyValue("PropertyName") == propertyName )
				{
							return prop;
				}
			}

			return null;
		}

		public string StartupParameters
		{
			get 
			{
				Property p = AdvancedProperties["STARTUPPARAMETERS"];

				if( null != p )
				{
					return p.Value as string;
				}
				else
				{
                    return null;
				}
			}
			set 
			{
				Property p = AdvancedProperties["STARTUPPARAMETERS"];

				if( null != p )
				{
					p.Value = value;
				}
				else
				{
					throw new UnknownPropertyException("StartupParameters");
				}
			}
		}

		/// <summary>
		/// This is a property bag that holds the Server and Client 
		/// Protocol properties
		/// </summary>
		public StringCollection Dependencies 
		{ 
			get 
			{ 
				if(null==dependencies)
				{
					try
					{
						dependencies = GetDependencies();
					}
					catch(Exception e)
					{
						SqlSmoObject.FilterException(e);

						throw new FailedOperationException(ExceptionTemplates.FailedOperationExceptionText(ExceptionTemplates.AdvancedProperties, this.GetType().Name, this.Name), e);
					}
				}

 				return dependencies;
			} 
		}
		internal StringCollection dependencies;

		private StringCollection GetDependencies()
		{
			string dep = this.Properties["Dependencies"].Value as string;
			string[] deps = dep.Split(new char[] {';'});
			StringCollection depColl = new StringCollection();
			if( dep.Length > 0)
				depColl.AddRange(deps);
			return depColl;
		}

		/// <summary>
		/// This is a property bag that holds the Server and Client 
		/// Protocol properties
		/// </summary>
		public PropertyCollection AdvancedProperties 
		{ 
			get 
			{ 
				if(null==advancedProperties)
				{
					try
					{
						advancedProperties = GetAdvancedProperties();
					}
					catch(Exception e)
					{
						SqlSmoObject.FilterException(e);

						throw new FailedOperationException(ExceptionTemplates.FailedOperationExceptionText(ExceptionTemplates.AdvancedProperties, this.GetType().Name, this.Name), e);
					}
				}

 				return advancedProperties;
			} 
		}
		internal PropertyCollection advancedProperties;

		private PropertyCollection GetAdvancedProperties()
		{
			// create the property collection
			DynamicPropertyMetadataProvider dpmp = new DynamicPropertyMetadataProvider();

			Urn reqUrn = this.ParentColl.ParentInstance.Urn;
			// client filters on protocol name
			reqUrn = this.ParentColl.ParentInstance.Urn + string.Format(SmoApplication.DefaultCulture, 
											"/ServiceAdvancedProperty[@ServiceName='{0}' and @ServiceType={1}]", 
											Urn.EscapeString(this.Name), 
											Enum.Format( typeof(ManagedServiceType), Properties["Type"].Value, "d") );
			
			// make a request to the enuemrator. Note that here the 
			// request brings not only the metadata, but also the 
			// values for the different properties
			DataSet ds = Proxy.ProcessRequest(new Request(reqUrn));

			foreach( DataRow dr in ds.Tables[0].Rows )
			{
				string propType;
				switch((UInt32)dr["ValueType"] )
				{
					case 0:
						propType = "System.String";
						break;
					
					case 1 :
						propType = "System.Boolean";
						break;
					case 2 :
						propType = "System.Int64";
						break;
					case 3 :
						goto case 2;
					default :
						goto case 0;
				}

				bool bReadOnly = true;
				if( (string)dr["Name"] != "Name" )
				{
					bReadOnly = (bool)dr["IsReadOnly"];
				}


				dpmp.AddMetadata((string)dr["Name"], bReadOnly, System.Type.GetType(propType));
			}

			PropertyCollection properties = new PropertyCollection(this, dpmp);

            
			// iterate through the result set, build properties and add 
			// them to the collection
			foreach( DataRow dr in ds.Tables[0].Rows )
			{
				
				string propType;
				object propValue = null;

				// not all of the above type will apply to us
				switch((UInt32)dr["ValueType"] )
				{
					case 0:
						propType = "System.String";
						propValue = (string)dr["StringValue"];
						break;
					
					case 1 :
						propType = "System.Boolean";
                        propValue = Convert.ToBoolean(dr["NumericValue"], SmoApplication.DefaultCulture);
                        break;
					case 2 :
						propType = "System.Int64";
                        propValue = Convert.ToInt64(dr["NumericValue"], SmoApplication.DefaultCulture);
                        break;
					case 3 :
						goto case 2;
					default :
						goto case 0;
				}

				// create the Property object
				Property p = properties.Get((string)dr["Name"]);
				
				// set its fields, Value among them
				if( propValue is DBNull )
				{
					if(propType=="System.Boolean")
						p.SetValue(false);
					else if( propType == "System.Int64")
						p.SetValue(0);
					else 
						p.SetValue(string.Empty);
				}
				else
				{
					p.SetValue(propValue);
				}
				p.SetRetrieved(true);
			}

			return properties;
		}

		public override void Refresh()
		{
			base.Refresh();
			advancedProperties = GetAdvancedProperties();
		}
		

	}
	
}

