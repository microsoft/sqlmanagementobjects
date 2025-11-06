using System;
using System.Text;
using System.Data;
using System.Reflection;
using System.Management;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.Sdk.Sfc;
#if STRACE
using Microsoft.SqlServer.Management.Diagnostics;
#endif

namespace Microsoft.SqlServer.Management.Smo.Wmi
{

    ///<summary>
    /// Contains common functionality for all the WMI classes
    ///</summary>
    public abstract class WmiSmoObject : SmoObjectBase
    {
        protected internal WmiSmoObject(WmiCollectionBase parentColl, string name)
        {
#if DEBUG
            if(null==parentColl)
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("parentColl"));
            
            if(null==name)
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("name"));
#endif
            
            
            SetName(name);
            this.parentColl = parentColl;
            UpdateObjectState();
            Init();
        }

        // this default constructor has to be called by objects that do not know their parent
        protected internal WmiSmoObject(String name)
        {
            SetName(name);
            Init();
        }

        protected internal WmiSmoObject()
        {
            Init();
        }

        internal virtual PropertyMetadataProvider GetPropertyMetadataProvider()
        {
            return null;
        }

        // some initialization calls
        private void Init()
        {
            // sets initial state
            propertyBagState = PropertyBagState.Empty;					

            // inits the m_properties to null, we will populate the property 
            // collection with enumerator metadata when the user asks for it
            m_properties = null;
        }

        WmiCollectionBase parentColl;
        /// <summary>
        /// Pointer to the collection that holds the object, if any
        /// </summary>
        internal WmiCollectionBase ParentColl
        {
            get 
            {
                return parentColl;
            }
        }


        // this would be the ManagedComputer object that sits at the root of the local tree
        // we cache it, since there is going to be a lot of references to it
        private ManagedComputer m_ManagedComputer = null;
        internal ManagedComputer GetManagedComputer()
        {
            if( null == m_ManagedComputer )
            {
                // climb up the tree to the server object
                WmiSmoObject current = this;
                while( null!=current.ParentColl && !typeof(ManagedComputer).Equals(current.GetType()))
                {
                    current = current.ParentColl.ParentInstance;
                }

                if(null==current.ParentColl && !typeof(ManagedComputer).Equals(current.GetType()))
                    throw new InternalSmoErrorException(ExceptionTemplates.ObjectNotUnderServer(this.GetType().ToString()));

                m_ManagedComputer = current as ManagedComputer;
            }

            return m_ManagedComputer;
        }


        /// <summary>
        /// Returns the Urn of the object, computed on the fly
        /// </summary>
        public virtual Urn Urn
        {

            [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember")]
            get
            {
                // determine the suffix, which is static member of the class
                // watch out, UrnSuffix lives on TypeBase
                string urnsuffix = this.GetType().InvokeMember("UrnSuffix", 
                                    BindingFlags.Default | BindingFlags.Static | BindingFlags.NonPublic  | BindingFlags.GetProperty,
                                    null, null, new object[] {}, SmoApplication.DefaultCulture  ) as string;
                // if this is an empty string, we are in RootObject
                if(urnsuffix.Length == 0)
                    return new Urn(string.Empty);

                // the recursive call
                Urn parentUrn = string.Empty; 
                if( null != ParentColl )
                {
                    parentUrn = ParentColl.ParentInstance.Urn;
                }

                if(parentUrn.ToString().Length != 0)
                {
                    return new Urn(string.Format(SmoApplication.DefaultCulture, "{0}/{1}[@Name='{2}']", parentUrn, urnsuffix, Urn.EscapeString(Name)));
                }
                else
                {
                    // if the parenturn is empty we are in Server object, and we do not append any prefix
                    return new Urn(string.Format(SmoApplication.DefaultCulture, "{0}[@Name='{1}']", urnsuffix, Urn.EscapeString(Name)));
                }
                    
            }
        }

        string name;
        public string Name
        {
            get
            {
                return this.name;
            }
            set 
            {
                if( null == value )
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Name"));
                    
                if( State != SqlSmoState.Pending )
                    throw new InvalidSmoOperationException(ExceptionTemplates.OperationOnlyInPendingState);

                this.name = value;
                UpdateObjectState();
            }
        }
        
        internal protected void SetParentImpl(WmiSmoObject newParent)
        {
            if( null == newParent )
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("newParent"));

            // if the object is not in pending state, setting the parent is useless
            if( State != SqlSmoState.Pending )
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationOnlyInPendingState);

            // is parent is a pending object, we have to throw because we have no link up,
            // so we can't get metadata
            if( newParent.State == SqlSmoState.Pending )
                throw new InvalidSmoOperationException();

            // for the moment we can only have ServerAliases
            if( !(newParent is ManagedComputer ) )
                throw new FailedOperationException(ExceptionTemplates.InvalidType(newParent.GetType().ToString())).SetHelpContext("InvalidType");
            parentColl = null;
            if( this is ServerAlias )
                parentColl = ((ManagedComputer)newParent).ServerAliases;

            UpdateObjectState();
        }

        protected void UpdateObjectState()
        {
            if (this.State == SqlSmoState.Pending && null != name && null != parentColl)
                SetState(SqlSmoState.Creating);
        }
        
        internal protected void SetName(string name)
        {
            this.name = name;
            UpdateObjectState();
        }
        
        ///<summary>
        ///Called when one of the properties is missing from the property collection
        ///</summary>
        internal override object OnPropertyMissing(string propname, bool useDefaultValue)
        {
#if DEBUG
            Trace("Missing property " + propname);
#endif			
            switch(propertyBagState)
            {
                case PropertyBagState.Empty:
                    Initialize();
                    break;
                case PropertyBagState.Lazy:
                    Initialize();	
                    break;
                case PropertyBagState.Full:
                    throw new InternalSmoErrorException(ExceptionTemplates.FullPropertyBag(propname));
            }

            return Properties.Get(propname).Value;
        }

        /// <summary>
        /// Initializes the object, by reading its properties from the enumerator
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            // if the object does not exist or it has already been initialized there is
            // no point in initializing the object. 
            if( propertyBagState == PropertyBagState.Full )
            {
                return false;
            }
            
            string[] fields = null;
            OrderBy[] orderby = null; 

            bool bInit = ImplInitialize(fields, orderby);

            // change object state according to the result of the initialization
            if(bInit) 
            {
                propertyBagState = PropertyBagState.Full;
            }

            return bInit;
        }
        
        // initializes an object with a list of properties
        protected virtual bool ImplInitialize(string[] fields, OrderBy[] orderby)
        {
            Urn urn = this.Urn;
             
            // build the request object
            Request req = new Request();
            req.Urn = urn;
            req.Fields = fields;
            req.OrderByList = orderby;
            
            // call the enumerator
            DataSet ds = Proxy.ProcessRequest(req);

            // retrieve the data into the property collection
            // ONE table, with ONE row !
            DataTable dt = ds.Tables[0];

            // if the table has no rows this means that initialization of the object has failed
            if( dt.Rows.Count < 1)
            {
                Trace("Failed to Initialize urn " + urn);
                return false;
            }
            else
            {
#if DEBUG
                // we check this only on the debug version
                if(dt.Rows.Count > 1)
                    throw new InternalSmoErrorException(ExceptionTemplates.MultipleRowsForUrn(req.Urn));
#endif
                AddObjectProps(dt.Columns, dt.Rows[0]);
                return true;
            }
        }

        internal void AddObjectProps(DataColumnCollection columns, DataRow dr)
        {
            int propsSkipped = 0;
            
            // add all properties
            for(int i = 0; i < columns.Count;i++)
            {
                DataColumn dc = columns[i];

                // we gotta skip name since it is not in the property bag
                if((1>propsSkipped) &&
                     (0==string.Compare(dc.ColumnName, "Name",StringComparison.Ordinal)))
                {
                    propsSkipped++;
                    continue;
                }

                // get the property
                Property p = Properties.Get(dc.ColumnName);
                if(p==null)
                {
                    throw new InternalSmoErrorException(ExceptionTemplates.UnknownProperty(dc.ColumnName,  this.GetType().ToString()));
                }

                // if the property is an enumeration then we have to 
                // change its type from Int32 to the specific enum
                if(p.Enumeration)
                {
                    p.SetValue(Enum.ToObject(p.Type, Convert.ToInt32(dr[i], SmoApplication.DefaultCulture)));
                }
                else
                {
                    if( DBNull.Value.Equals(dr[i]))
                    {
                        if(p.Type.Equals(typeof(string)))
                        {
                            p.SetValue(string.Empty);
                        }
                        else
                        {
                            p.SetValue(null);
                        }
                    }
                    else
                    {
                        p.SetValue(dr[i]);
                    }
                }
                p.SetRetrieved(true);
            }
        }

        /// <summary>
        /// The property bag of the object
        /// </summary>
        public PropertyCollection Properties 
        { 
            get 
            { 
                if(null==m_properties)
                {
                    // create the property collection
                    m_properties = new PropertyCollection(this, this.GetPropertyMetadataProvider());
                }

                return m_properties;
            } 
        }
        internal PropertyCollection m_properties;

        internal protected bool IsObjectInitialized()
        {
            if ((this.State != SqlSmoState.Existing && this.State != SqlSmoState.ToBeDropped) || 
                propertyBagState == PropertyBagState.Full )
                return true;
            return false;
        }

        public virtual void Refresh()
        {
            m_properties = null;
            propertyBagState = PropertyBagState.Empty;
            Initialize();
        }


        protected void CheckObjectState()
        {
            if (this.State == SqlSmoState.Dropped)
                throw new SmoException(ExceptionTemplates.ObjectDroppedExceptionText(this.GetType().ToString(), this.Name));

            if (this.State == SqlSmoState.Pending)
                throw new InvalidSmoOperationException("", SqlSmoState.Pending);
        }

        // this is the proxy we use to call the enumerator
        private WMIProxy proxy = null;	

        /// <summary>
        /// The proxy object we use for SQL execution
        /// </summary>
        internal WMIProxy Proxy 		
        { 
            get	
            { 
                if(proxy==null) 
                {
                    // make all objects use the one from the ManagedComputer
                    if( this is ManagedComputer )
                    {
                        proxy=new WMIProxy(GetManagedComputer().ManagementScope); 
                    }
                    else
                    {
                        proxy = GetManagedComputer().Proxy;
                    }
                }
                
                return proxy;
            }
        }
        
        // populates the children collection of the specified type 

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember")]
        internal void EnumChildren(string childTypeName, WmiCollectionBase coll)
        {
#if DEBUG
            // check if the type of the child fits with the type of the child collection
            if(string.Compare(childTypeName + "Collection", coll.GetType().ToString(),StringComparison.Ordinal) != 0)
                throw new InternalSmoErrorException(ExceptionTemplates.UnknownChild);
#endif
            // get child object type
            Type childType = Type.GetType(childTypeName, true);

            // call enumerator 
            Request req = new Request();
            req.Urn = this.Urn + "/" + childType.InvokeMember("UrnSuffix", 
                            BindingFlags.Default | BindingFlags.GetProperty | BindingFlags.Static | BindingFlags.NonPublic,
                            null, null, new object[] {}, SmoApplication.DefaultCulture );

            // REVIEW: $BUG 361437: In order to improve performance, we retrieve here all fields.
            // For WMI objects retrieving all fields should not be any slower than retrieving 
            // just a few specific fields.
            req.Fields = null;
            
            // watch out, ordering is essential for scalability
            req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc)};
            
            DataSet ds = Proxy.ProcessRequest(req);
            
            // our data is in the first table
            DataTable dt = ds.Tables[0];

            
            // fill in the list of tables
            foreach(DataRow dr in dt.Rows)
            {
                object[] args = new object[] { coll, (string)dr["Name"] };
                // create the child object
                object newChild  = Activator.CreateInstance(	childType, 
                                                BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | 
                                                BindingFlags.NonPublic | BindingFlags.CreateInstance, 
                                                null, args,null);		
                if(null==newChild) 
                    throw new InternalSmoErrorException(ExceptionTemplates.CantCreateType(childTypeName ));

                // fill its properties
                if(dt.Columns.Count > 1)
                    ((WmiSmoObject)newChild).AddObjectProps( dt.Columns, dr);

                // update state
                //we're not getting the lazy set of properties so the object is empty now
                ((WmiSmoObject)newChild).SetState(PropertyBagState.Empty);
                ((WmiSmoObject)newChild).SetState(SqlSmoState.Existing);

                // add child object to collection
                coll.AddInternal((WmiSmoObject)newChild);
            }
            
        }
        
        // tracing stuff
        internal static void Trace(string traceText)
        {
            #if STRACE
            STrace.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, traceText);
            #else
            System.Diagnostics.Trace.TraceInformation(traceText);
            #endif
        }

        protected void InvokeMgmtMethod(ManagementObject mo, string methodName, 
                                            object[] parameters )
        {
            InvokeMgmtMethod(mo, null, methodName, parameters );
        }
        
        /// <summary>
        /// Wraps InvokeMethod calls. When the call fails, returned error codes  
        /// are mapped into exceptions. 
        /// </summary>
        /// <param name="mo"></param>
        /// <param name="observer"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        protected void InvokeMgmtMethod(ManagementObject mo, ManagementOperationObserver observer, 
                                            string methodName, object[] parameters )
        {
#if DEBUG
            StringBuilder paramList = new StringBuilder(Globals.INIT_BUFFER_SIZE );
            if( null != parameters )
            {
                foreach( object o in parameters )
                {
                    if( null != o )
                        paramList.Append(", " + o.ToString() );
                    else 
                        paramList.Append(", null" );
                }
            }
            
            Trace("InvokeMgmtMethod( " + mo.Path.ToString() + ", " + 
                    ( observer != null ? "wait, " : "nowait, ") +  methodName + 
                    (paramList.Length > 0 ? ", ": "" ) + paramList.ToString() + " )" );
#endif
            try
            {
                if( null == observer )
                {
                    UInt32 retcode = Convert.ToUInt32(mo.InvokeMethod( methodName, parameters ), SmoApplication.DefaultCulture);			
                    if( 0 != retcode )
                    {
                        // try to figure out what does this error code mean

                        // for services, error codes between 0 and 24 represent an error
                        // and will be mapped by SMO into specific error messages
                        if( ( this is Service ) && 0 < retcode && retcode <= 24 )
                        {
                            throw new ServiceRequestException(retcode);
                        }

                        // for anything other that Service, the equivalent of (FAILED) Win32
                        // macro applied in deciding if retcode is an error or just an 
                        // information code
                        if( 1 == (retcode & 0x80000000) )
                        {
                            throw new ServiceRequestException(retcode);
                        }
                    }
                }
                else
                {
                    // no return value, error or success is given through the delegate
                    mo.InvokeMethod( observer, methodName, parameters );
                }
            }
            catch( Exception e)
            {
                SqlSmoObject.FilterException(e);
                
                // wrap ManagementException in SmoException
                throw new FailedOperationException(ExceptionTemplates.FailedOperationExceptionText(methodName, this.GetType().Name, this.Name), e);
            }
            
        }
        
        protected virtual ProtocolPropertyCollection CreateProtocolPropertyCollection()
        {
            return null;
        }
        
        protected virtual ProtocolProperty GetPropertyObject(PropertyCollection properties,DataRow dr, object propValue)
        {
            return null;
        }

        protected ProtocolPropertyCollection GetProtocolPropertyCollection()
        {
            // create the property collection
            ProtocolPropertyCollection protocolProperties = CreateProtocolPropertyCollection();

            // create the property collection
            DynamicPropertyMetadataProvider dpmp = new DynamicPropertyMetadataProvider();

            Urn reqUrn = this.Urn + "/Property";

            // make a request to the enuemrator. Note that here the 
            // request brings not only the metadata, but also the 
            // values for the different properties
            DataSet ds = Proxy.ProcessRequest(new Request(reqUrn));

            // iterate through the result set, build properties and add 
            // them to the collection
            foreach( DataRow dr in ds.Tables[0].Rows )
            {
                
                string propType;
                if( (UInt32)dr["Type"] == 0 )
                {
                    propType = "System.Boolean";
                }
                else
                {
                    // get the property's value type. Use the following constants:
                    /*
                    // quote from ...\PlatformSDK\Include\winnt.h
                    ine REG_NONE                    ( 0 )   // No value type
                    ine REG_SZ                      ( 1 )   // Unicode nul terminated string
                    ine REG_EXPAND_SZ               ( 2 )   // Unicode nul terminated string
                                                            // (with environment variable references)
                    ine REG_BINARY                  ( 3 )   // Free form binary
                    ine REG_DWORD                   ( 4 )   // 32-bit number
                    ine REG_DWORD_LITTLE_ENDIAN     ( 4 )   // 32-bit number (same as REG_DWORD)
                    ine REG_DWORD_BIG_ENDIAN        ( 5 )   // 32-bit number
                    ine REG_LINK                    ( 6 )   // Symbolic Link (unicode)
                    ine REG_MULTI_SZ                ( 7 )   // Multiple Unicode strings
                    ine REG_RESOURCE_LIST           ( 8 )   // Resource list in the resource map
                    ine REG_FULL_RESOURCE_DESCRIPTOR ( 9 )  // Resource list in the hardware description
                    ine REG_RESOURCE_REQUIREMENTS_LIST ( 10 )
                    ine REG_QWORD                   ( 11 )  // 64-bit number
                    ine REG_QWORD_LITTLE_ENDIAN     ( 11 )  // 64-bit number (same as REG_QWORD)
                    */

                    // not all of the above type will apply to us
                    switch((UInt32)dr["ValType"] )
                    {
                        case 1 :
                            propType = "System.String";
                            break;
                        case 2 : 	goto case 1;
                        case 7 : goto case 1;
                        case 4 : 
                            propType = "System.Int32";
                            break;
                        case 5 : goto case 4;
                        case 11 : 
                            propType = "System.Int64";
                            break;
                        default :
                            // default is string
                            propType = "System.String";
                            break;
                    }
                }

                bool bReadOnly = false;
                if ((string)dr["Name"] == "Name")
                {
                    bReadOnly = true;
                }

                dpmp.AddMetadata((string)dr["Name"], bReadOnly, Type.GetType(propType));
            }

            PropertyCollection properties = new PropertyCollection(this, dpmp);

            // iterate through the result set, build properties and add 
            // them to the collection
            foreach( DataRow dr in ds.Tables[0].Rows )
            {
                
                object propValue = null;
                // Type == 0 means the property is a flag, ie a boolean
                if( (UInt32)dr["Type"] == 0 )
                {
                    // we try to convert, in order to get the value
                    try
                    {
                        propValue = Convert.ToBoolean(dr["NumValue"], SmoApplication.DefaultCulture);
                    }
                    catch(Exception e)
                    {
                        throw new SmoException(ExceptionTemplates.InnerException, e);
                    }
                }
                else
                {
                    // not all of the above type will apply to us
                    switch((UInt32)dr["ValType"] )
                    {
                        case 1 :
                            propValue = dr["StrValue"];
                            break;
                        case 2 : 	goto case 1;
                        case 7 : goto case 1;
                        case 4 : 
                            propValue = dr["NumValue"];
                            break;
                        case 5 : goto case 4;
                        case 11 : 
                            propValue = dr["NumValue"];
                            break;
                        default :
                            // default is string
                            propValue = dr["StrValue"];
                            break;
                    }
                }

                // create the Property object
                ProtocolProperty p = GetPropertyObject(properties, dr, propValue);

                // add the property object to the collection
                protocolProperties.Add(p);
            }

            return protocolProperties;
        }

        protected virtual ManagementObject GetPropertyManagementObject( ProtocolProperty pp)
        {
            return null;
        }
        
        protected void AlterProtocolProperties(ProtocolPropertyCollection protocolProperties)
        {

            // iterate through every protocol property, and propagate the changes 
            foreach( ProtocolProperty pp in protocolProperties )
            {
                if( pp.Dirty )
                {
                    // build the management object we'll use to set the property
                    ManagementObject moProp = GetPropertyManagementObject(pp);

                    // use different setting functions, according to the type
                    if( pp.Type.Equals( typeof(System.Boolean)))
                    {
                        InvokeMgmtMethod(moProp, "SetFlag", new object[] {(bool)pp.Value} );
                    }
                    else if( pp.Type.Equals(typeof(System.String)) )
                    {
                        InvokeMgmtMethod(moProp, "SetStringValue", new object[] {(string)pp.Value} );
                    }
                    else
                    {
                        InvokeMgmtMethod(moProp, "SetNumericalValue", new object[] { SmoApplication.ConvertInt32ToUInt32((Int32)pp.Value)} );
                    }
                }
            }
        }


    }
    
    /// <summary>
    /// proxy class for the sql execution and enumerator classes
    /// </summary>
    internal class WMIProxy
    {
        Enumerator m_EnumProxy;
        ManagementScope m_scope;

        /// <summary>
        /// The constructor for this class needs a reference to the server 
        /// </summary>
        /// <param name="scope"></param>
        internal WMIProxy(ManagementScope scope)
        {
            m_EnumProxy = new Enumerator();
            m_scope = scope;
        }


        internal DataSet ProcessRequest(Request req)
        {
#if DEBUG		
            // prepare log information
            StringBuilder sb = new StringBuilder();
            sb.Append("EnumeratorProxy.ProcessRequest( ");
            sb.Append(req.Urn.ToString());
            sb.Append(" )");
            sb.Append(Globals.newline);
            if(req.Fields != null)
            {
                sb.Append("Fields = ");
                foreach(string s in req.Fields)
                {
                    sb.Append(s);
                    sb.Append(" ");
                }
            }
            
            WmiSmoObject.Trace(sb.ToString());
            sb.Length = 0;
            
            DateTime dt = DateTime.Now;
#endif

            DataSet retds = (DataSet)ExecProcess(req);

#if DEBUG			
            TimeSpan ts = DateTime.Now-dt;

            sb.Append(Globals.newline);
            sb.AppendFormat(SmoApplication.DefaultCulture, "Duration = {0} minutes {1} seconds {2} milliseconds\n", 
                            ts.Minutes, ts.Seconds, ts.Milliseconds );

            // log the enumerator call
            WmiSmoObject.Trace(sb.ToString());
#endif
            return retds;
        }
        

        private object ExecProcess(object request)
        {
            try
            {
                if( request is RequestObjectInfo )
                {
                    return (ObjectInfo)m_EnumProxy.Process(m_scope, (RequestObjectInfo)request);
                }
                else
                {
                    return (DataSet)m_EnumProxy.Process(m_scope, (Request)request);
                }
            }
            catch( Exception e )
            {
                throw new FailedOperationException( ExceptionTemplates.InnerWmiException, e).SetHelpContext("InnerWmiException");
            }
        }
    }

}

