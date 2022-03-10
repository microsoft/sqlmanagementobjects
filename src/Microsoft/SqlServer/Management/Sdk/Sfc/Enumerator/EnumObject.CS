// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;

    /// <summary>
    /// classes that implement a XPATH level must be derived from this class
    /// </summary>
    public abstract class EnumObject
    {
        SortedList m_properties;					//list of properties
        StringCollection m_propertyNames;		//list of properties name by their order in the xml file
        Request m_req;
        XPathExpressionBlock m_block;
        Object m_ci;
        Urn m_Urn;
        
        /// <summary>
        /// default constructor
        /// </summary>
        protected EnumObject()
        {
            m_properties = new SortedList(System.StringComparer.Ordinal);
            m_propertyNames = new StringCollection();
        }

        /// <summary>
        /// name of the xpath level
        /// </summary>
        /// <value></value>
        internal protected string Name
        {
            get
            {
                return this.Urn.Type;
            }
        }

        /// <summary>
        /// this property lives only while a request is serviced
        /// </summary>
        /// <value>holds the xpath exression for the curent request</value>
        internal protected Urn Urn
        {
            get
            {
                return m_Urn;
            }
            set
            {
                m_Urn = value;
            }
        }

        /// <summary>
        /// initialize this level
        /// </summary>
        /// <param name="ci">connection info</param>
        /// <param name="block">sysntatictal tree for the filter for this level</param>
        public virtual void Initialize(Object ci, XPathExpressionBlock block)
        {
            this.ConnectionInfo = ci;
            m_block = block;
        }

        /// <summary>
        /// list of the properties for which the value is know
        /// e.g. Name is know if the fileter is in the form
        /// [@Name='fff'] or [@Name='fff' and ( xxxxx ) ]
        /// </summary>
        /// <value></value>
        protected SortedList FixedProperties
        {
            get { return m_block.FixedProperties; }
        }

        /// <summary>
        /// get a fixed ( with known value from filter ) property
        /// of type string
        /// </summary>
        /// <param name="propertyName">name of the property requested</param>
        /// <param name="removeEscape">replace double quotes with one single quote</param>
        /// <returns></returns>
        protected string GetFixedStringProperty(string propertyName, bool removeEscape)
        {
            String sValue = (string)(FilterNodeConstant)this.FixedProperties[propertyName];
            if (removeEscape && null != sValue)
            {
                sValue = Urn.UnEscapeString(sValue);
            }
            return sValue;
        }

        /// <summary>
        /// add to the collection of properties
        /// </summary>
        /// <param name="op">property to add</param>
        protected void AddProperty(ObjectProperty op)
        {
            m_properties.Add(op.Name, op);
            m_propertyNames.Add(op.Name);
        }

        /// <summary>
        /// returns the object properties by usage
        /// </summary>
        /// <param name="usage">usage filter</param>
        /// <returns>array of ObjectProperty</returns>
        public ObjectProperty[] GetProperties(ObjectPropertyUsages usage)
        {
            int nCount = 0;
            ObjectProperty[] retriveAux = new ObjectProperty[m_propertyNames.Count];
            for(int i = 0; i < m_propertyNames.Count; i++)
            {
                ObjectProperty op = (ObjectProperty)m_properties[m_propertyNames[i]];
                if( 0 != (usage & op.Usage) )
                {
                    retriveAux[nCount++] = op;
                }
            }
            ObjectProperty[] retrive = new ObjectProperty[nCount];
            for(int i = 0; i < nCount; i++)
            {
                retrive[i] = retriveAux[i];
            }

            return retrive;
        }

        /// <summary>
        /// until we have a formal way so that each object declares which properties it uses for urn
        /// try this best guess aproach
        /// </summary>
        /// <returns>array of properties that are used in the Urn</returns>
        public ObjectProperty[] GetUrnProperties()
        {
            ArrayList urnprops = new ArrayList();
            ObjectProperty op = TryGetProperty("Urn", ObjectPropertyUsages.Request);
            if( null == op ) //if it doesn't have a Urn we have nothing to return
            {
                return null;
            }
            //if it has Name it is propbably in the Urn
            op = TryGetProperty("Name", ObjectPropertyUsages.Request);
            if( null != op )
            {
                urnprops.Add(op);
                //if it has Schema it is propbably in the Urn
                op = TryGetProperty("Schema", ObjectPropertyUsages.Request);
                if( null != op )
                {
                    urnprops.Add(op);
                }
            }
            else //else guess that maybe the first property after Urn is used
            {
                //get first after Urn
                ObjectProperty[] oplist = GetProperties(ObjectPropertyUsages.Request);
                if( oplist.Length > 1 )
                {
                    urnprops.Add(oplist[1]);
                }
            }

            //create the array
            ObjectProperty[] retrive = new ObjectProperty[urnprops.Count];
            for(int i = 0; i < urnprops.Count; i++)
            {
                retrive[i] = (ObjectProperty)urnprops[i];
            }
            //off with it
            return retrive;
        }

        /// <summary>
        /// accessor for a property by name and usage ( internal use )
        /// </summary>
        /// <param name="name">name of the property</param>
        /// <param name="usage">intended usage</param>
        /// <returns>null if the property does not meet the usage requirement or does
        /// not exist, else the coresponding property object</returns>
        protected ObjectProperty TryGetProperty(String name, ObjectPropertyUsages usage)
        {
            ObjectProperty op = (ObjectProperty)m_properties[name];
            if( null != op )
            {
                ObjectPropertyUsages usage_not_resolved = (ObjectPropertyUsages) ( ( (UInt32)op.Usage | (UInt32)usage ) - (UInt32)op.Usage );
                if( 0 != usage_not_resolved && ObjectPropertyUsages.Reserved1 != usage_not_resolved )
                {
                    return null;
                }
            }
            return op;
        }

        /// <summary>
        /// accessor for a property by name and usage ( internal use )
        /// </summary>
        /// <param name="name">name of the property</param>
        /// <param name="usage">intended usage</param>
        /// <returns>exception if the property does not meet the usage requirement or does
        /// not exist, else the coresponding property object</returns>
        protected ObjectProperty GetProperty(String name, ObjectPropertyUsages usage)
        {
            ObjectProperty op = (ObjectProperty)m_properties[name];
            if( null == op )
            {
                throw new UnknownPropertyEnumeratorException(name);
            }

            ObjectPropertyUsages usage_not_resolved = (ObjectPropertyUsages) ( ( (UInt32)op.Usage | (UInt32)usage ) - (UInt32)op.Usage );
            if( 0 != usage_not_resolved && ObjectPropertyUsages.Reserved1 != usage_not_resolved )
            {
                InvalidPropertyUsageEnumeratorException.Throw(name, usage_not_resolved);
            }

            return op;
        }

        /// <summary>
        /// the request for this level, it is received from one level down
        /// or from user if it is the last level
        /// </summary>
        /// <value></value>
        public Request Request
        {
            set { m_req = value; }
            get { return m_req; }
        }

        /// <summary>
        /// conection object
        /// </summary>
        /// <value></value>
        public Object ConnectionInfo
        {
            get { return m_ci; }
            set { m_ci = value; }
        }

        /// <summary>
        /// filter for this level
        /// </summary>
        /// <value></value>
        public FilterNode Filter
        {
            get { return m_block.Filter; }
            set 
            { 
                m_block = m_block.Copy(); 
                m_block.Filter = value; 
            }
        }

        /// <summary>
        /// calculate the list of fixed properties
        /// ( their value can be deduced from the filter )
        /// </summary>
        /// <returns></returns>
        public bool ComputeFixedProperties()
        {
            return 0 != m_block.FixedProperties.Count;
        }

        /// <summary>
        /// get the property name as it will be aliased in the result set to the user
        /// </summary>
        /// <param name="prop">property name</param>
        /// <returns>aliased name as it will be returned to the user</returns>
        internal protected string GetAliasPropertyName(string prop)
        {
            if( null == this.Request || null == this.Request.PropertyAlias )
            {
                return prop;
            }
            String alias;
            switch(this.Request.PropertyAlias.Kind)
            {
                case PropertyAlias.AliasKind.Prefix:
                    if( null == this.Request.PropertyAlias.Prefix )
                    {
                        throw new InternalEnumeratorException(SfcStrings.InvalidPrefixAlias(prop));
                    }
                    alias = this.Request.PropertyAlias.Prefix + prop;
                    break;
                case PropertyAlias.AliasKind.NodeName:
                    return this.Name + '_' + prop;
                case PropertyAlias.AliasKind.Each:
                    int i = 0;
                    for(; i < this.Request.Fields.Length; i++)
                    {
                        if( prop == this.Request.Fields[i] )
                        {
                            break;
                        }
                        if( i == this.Request.Fields.Length )
                        {
                            throw new InternalEnumeratorException(SfcStrings.PropertyCannotHaveAlias(prop));
                        }

                        if( null == this.Request.PropertyAlias.Aliases ||
                            this.Request.PropertyAlias.Aliases.Length <= i ||
                            null == this.Request.PropertyAlias.Aliases[i] )
                        {
                            throw new InternalEnumeratorException(SfcStrings.AliasNotSpecified(prop));
                        }
                    }
                    alias = this.Request.PropertyAlias.Aliases[i];
                    break;
                default:
                    throw new InternalEnumeratorException(SfcStrings.InvalidAlias);
            }
            return alias;
        }

        /// <summary>
        /// Override this method to generate a Request for the parent object
        /// The response from the parent object will give us the subset of parent objects for which 
        /// the current level must generate it's result
        /// </summary>
        /// <returns>Request for the parent object</returns>
        public virtual Request RetrieveParentRequest()
        {
            return null;
        }

        /// <summary>
        /// Gets the the EnumResult data acordingly to the Request wich was previoesly set by the Environment
        /// </summary>
        /// <param name="erParent"></param>
        /// <returns>result for the level Request</returns>
        public abstract EnumResult GetData(EnumResult erParent);


        /// <summary>
        /// return what types of results does this object support
        /// </summary>
        public abstract ResultType[] ResultTypes
        {
            get;
        }

        /// <summary>
        /// This is called after the data has been retrived by the final object so the chian is preparing to be freed
        /// Because the objects are persisted between calls free any call specific data
        /// </summary>
        /// <param name="erChildren"></param>
        public virtual void PostProcess(EnumResult erChildren)
        {
        }
    }
}
