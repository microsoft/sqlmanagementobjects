// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    /// <summary>
    /// Used as a base class for objects that needs to be extended with additional properties
    /// Allows to add these new properties to the PropertySet, so they can be accessed using PropertyProvider
    /// Also, it helps to establish dependancy between extended property and original one
    /// </summary>
    /// <typeparam name="TSfcInstance"></typeparam>
    //[Obsolete("Do not use class. It is going to be removed by Katmai RTM. If you need to use it, please contact vladyang", false)]
    public class SfcObjectExtender<TSfcInstance> : ISfcPropertyProvider
                                                    , INotifyPropertyChanged
                                                    , ISfcNotifyPropertyMetadataChanged
                                                    where TSfcInstance : ISfcPropertyProvider, new()
    {
        TSfcInstance parent;
        SfcPropertyDictionary properties;
        Dictionary<string, string> propertyMapper;

        /// <summary>
        /// default ctor. Used in code-generation scenarios
        /// </summary>
        public SfcObjectExtender()
            : this(new TSfcInstance())
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="parent">parent object. Used as a bese property provider</param>
        public SfcObjectExtender(TSfcInstance parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            this.parent = parent;
            this.parent.PropertyChanged += new PropertyChangedEventHandler(parent_PropertyChanged);
            this.parent.PropertyMetadataChanged += new EventHandler<SfcPropertyMetadataChangedEventArgs>(parent_PropertyMetadataChanged);
        }

       

        /// <summary>
        /// Register an additional reflected property
        /// </summary>
        /// <param name="propertyInfo"></param>
        protected void RegisterProperty(System.Reflection.PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException("propertyInfo");
            }

            this.PropertyDictionary.Properties[propertyInfo.Name] = new ReflectedProperty(this, propertyInfo);
        }

        /// <summary>
        /// Register property of the parent, that has not been included in properties collection
        /// (like 'Name' in SMO or collections
        /// </summary>
        /// <param name="propertyInfo"></param>
        protected void RegisterParentProperty(System.Reflection.PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException("propertyInfo");
            }

            this.PropertyDictionary.Properties[propertyInfo.Name] = new ReflectedProperty(this.Parent, propertyInfo);
        }
        /// <summary>
        /// Register an additional reflected property, dependant on the property from the parent object
        /// </summary>
        /// <param name="propertyInfo">Reflected property</param>
        /// <param name="parentPropertyName">Parent property name</param>
        protected void RegisterProperty(System.Reflection.PropertyInfo propertyInfo, string parentPropertyName)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException("propertyInfo");
            }
            if (string.IsNullOrEmpty(parentPropertyName))
            {
                throw new ArgumentNullException("parentPropertyName");
            }

            SfcPropertyDictionary set = this.PropertyDictionary;

            set.Properties[propertyInfo.Name] = new ParentedReflectedProperty(this, propertyInfo, set.Properties[parentPropertyName]);

            if (propertyMapper == null)
            {
                this.propertyMapper = new Dictionary<string, string>();
            }

            this.propertyMapper[parentPropertyName] = propertyInfo.Name;
        }


        /// <summary>
        /// Parent object
        /// </summary>
        protected TSfcInstance Parent
        {
            get { return this.parent; }
        }

        /// <summary>
        /// Property dictionary accessor
        /// Garantied to return an instance of SfcPropertyDictionary
        /// </summary>
        SfcPropertyDictionary PropertyDictionary
        {
            get
            {
                if (this.properties == null)
                {
                    this.properties = new SfcPropertyDictionary(this.GetParentSfcPropertySet());

                    // register additional properties
                    foreach (System.Reflection.PropertyInfo pi in GetType().GetProperties())
                    {
                        foreach (ExtendedPropertyAttribute attribute in pi.GetCustomAttributes(typeof(ExtendedPropertyAttribute), true))
                        {
                            if (attribute.HasParent)
                            {
                                RegisterProperty(pi, attribute.ParentPropertyName);
                            }
                            else
                            {
                                RegisterProperty(pi);
                            }
                        }
                    }
                }
                return this.properties;
            }
        }

        /// <summary>
        /// Returns an instance of the parent's property set
        /// </summary>
        /// <returns></returns>
        protected virtual ISfcPropertySet GetParentSfcPropertySet()
        {
            return this.parent.GetPropertySet();
        }



        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires PropertyChanged event, when it has any subscriber
        /// </summary>
        /// <param name="propertyName"></param>
        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.propertyMapper != null)
            {
                string targetPropertyName;
                if (this.propertyMapper.TryGetValue(e.PropertyName, out targetPropertyName))
                {
                    OnPropertyChanged(targetPropertyName);
                }
            }
        }

        #endregion

        #region ISfcNotifyPropertyMetadataChanged Members

        public event EventHandler<SfcPropertyMetadataChangedEventArgs> PropertyMetadataChanged;

        /// <summary>
        /// Fires PropertyMetadataChanged event, when it has any subscriber
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyMetadataChanged(string propertyName)
        {
            if (PropertyMetadataChanged != null)
            {
                PropertyMetadataChanged(this, new SfcPropertyMetadataChangedEventArgs(propertyName));
            }
        }

        protected virtual void parent_PropertyMetadataChanged(object sender, SfcPropertyMetadataChangedEventArgs e)
        {
            if (this.propertyMapper != null)
            {
                string targetPropertyName;
                if (this.propertyMapper.TryGetValue(e.PropertyName, out targetPropertyName))
                {
                    OnPropertyMetadataChanged(targetPropertyName);
                }
            }
        }

        #endregion

        #region ISfcPropertyProvider Members

        /// <summary>
        /// returns collection of the properties
        /// </summary>
        /// <returns></returns>
        public ISfcPropertySet GetPropertySet()
        {
            return this.PropertyDictionary;
        }

        #endregion

        /// <summary>
        /// Property dictionary
        /// It encapsulates properties form the parent provider and allows to add 
        /// an additional properties.
        /// </summary>
        class SfcPropertyDictionary : ISfcPropertySet
        {
            ISfcPropertySet parent;
            Dictionary<string, ISfcProperty> properties;

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="parent">parent propety provider</param>
            public SfcPropertyDictionary(ISfcPropertySet parent)
            {
                if (parent == null)
                {
                    throw new ArgumentNullException("parent");

                }

                this.parent = parent;
                this.properties = new Dictionary<string, ISfcProperty>();
                foreach (ISfcProperty prop in this.parent.EnumProperties())
                {
                    this.properties[prop.Name] = prop;
                }
            }

            /// <summary>
            /// Internal property dictionary
            /// </summary>
            public IDictionary<string, ISfcProperty> Properties
            {
                get { return this.properties; }
            }

            #region ISfcPropertySet Members

            bool ISfcPropertySet.Contains<T>(string name)
            {
                ISfcProperty property;
                if (this.properties.TryGetValue(name, out property))
                {
                    return typeof(T).IsAssignableFrom(property.Type);
                }
                return false;
            }

            bool ISfcPropertySet.Contains(ISfcProperty property)
            {
                return this.properties.ContainsValue(property);
            }

            bool ISfcPropertySet.Contains(string propertyName)
            {
                return this.properties.ContainsKey(propertyName);
            }

            IEnumerable<ISfcProperty> ISfcPropertySet.EnumProperties()
            {
                return this.properties.Values;
            }

            bool ISfcPropertySet.TryGetProperty(string name, out ISfcProperty property)
            {
                return this.properties.TryGetValue(name, out property);
            }

            bool ISfcPropertySet.TryGetPropertyValue(string name, out object value)
            {
                return ((ISfcPropertySet)this).TryGetPropertyValue<object>(name, out value);
            }

            bool ISfcPropertySet.TryGetPropertyValue<T>(string name, out T value)
            {
                ISfcProperty property;
                if (this.properties.TryGetValue(name, out property))
                {
                    if (typeof(T).IsAssignableFrom(property.Type))
                    {
                        if (!property.IsNull)
                        {
                            value = (T)property.Value;
                            return true;
                        }
                    }
                }
                value = default(T);
                return false;
            }

            #endregion
        }

        class ReflectedProperty : ISfcProperty
        {
            System.Reflection.PropertyInfo property;
            AttributeCollection attributes;
            object owner;
            bool dirty;

            public ReflectedProperty(object owner, System.Reflection.PropertyInfo property)
            {
                if (owner == null)
                {
                    throw new ArgumentNullException("owner");
                }

                if (property == null)
                {
                    throw new ArgumentNullException("property");
                }

                this.owner = owner;
                this.property = property;
            }

            #region ISfcProperty Members

            public AttributeCollection Attributes
            {
                get
                {
                    if (this.attributes == null)
                    {
                        AttributeCollection typeAttrs = TypeDescriptor.GetAttributes(this.property.PropertyType);
                        object[] propAttrs = this.property.GetCustomAttributes(true);

                        // we have to combine all attributes without duplication
                        Dictionary<Type, Attribute> attributeDict = new Dictionary<Type, Attribute>();

                        foreach (Attribute attr in typeAttrs)
                        {
                            attributeDict[attr.GetType()] = attr;
                        }
                        foreach (Attribute attr in propAttrs)
                        {
                            attributeDict[attr.GetType()] = attr;
                        }

                        Attribute[] attrArray = new Attribute[attributeDict.Count];
                        attributeDict.Values.CopyTo(attrArray, 0);
                        this.attributes = new AttributeCollection(attrArray);
                    }
                    return this.attributes;
                }
            }

            public bool Dirty
            {
                get { return this.dirty; }
            }

            public bool Enabled
            {
                get { return this.property.CanWrite; }
            }

            public bool IsNull
            {
                get { return this.Value == null; }
            }

            public string Name
            {
                get { return this.property.Name; }
            }

            public bool Required
            {
                get { return false; }
            }

            public Type Type
            {
                get { return this.property.PropertyType; }
            }

            public object Value
            {
                get
                {
                    return this.property.GetValue(this.owner, null);
                }
                set
                {
                    this.property.SetValue(this.owner, value, null);
                    this.dirty = true;
                }
            }

            public bool Writable
            {
                get { return this.property.CanWrite; }
            }

            #endregion
        }

        class ParentedReflectedProperty : ISfcProperty
        {
            System.Reflection.PropertyInfo propertyInfo;
            ISfcProperty parentProperty;
            AttributeCollection attributes;
            object owner;

            public ParentedReflectedProperty(object owner, System.Reflection.PropertyInfo propertyInfo, ISfcProperty parentProperty)
            {
                if (owner == null)
                {
                    throw new ArgumentNullException("owner");
                }
                if (propertyInfo == null)
                {
                    throw new ArgumentNullException("propertyInfo");
                }
                if (parentProperty == null)
                {
                    throw new ArgumentNullException("parentProperty");
                }

                this.owner = owner;
                this.propertyInfo = propertyInfo;
                this.parentProperty = parentProperty;
            }

            #region ISfcProperty Members

            public AttributeCollection Attributes
            {
                get
                {
                    if (this.attributes == null)
                    {
                        AttributeCollection parentAttrs = this.parentProperty.Attributes;
                        AttributeCollection typeAttrs = TypeDescriptor.GetAttributes(this.propertyInfo.PropertyType);
                        object[] propAttrs = this.propertyInfo.GetCustomAttributes(true);

                        // we have to combine all attributes without duplication
                        Dictionary<Type, Attribute> attributeDict = new Dictionary<Type, Attribute>();
                        if (null != parentAttrs)
                        {
                            foreach (Attribute attr in parentAttrs)
                            {
                                attributeDict[attr.GetType()] = attr;
                            }
                        }
                        if (null != typeAttrs)
                        {
                            foreach (Attribute attr in typeAttrs)
                            {
                                attributeDict[attr.GetType()] = attr;
                            }
                        }
                        if (null != propAttrs)
                        {
                            foreach (Attribute attr in propAttrs)
                            {
                                attributeDict[attr.GetType()] = attr;
                            }
                        }

                        Attribute[] attrArray = new Attribute[attributeDict.Count];
                        attributeDict.Values.CopyTo(attrArray, 0);
                        this.attributes = new AttributeCollection(attrArray);
                    }
                    return this.attributes;
                }
            }

            public bool Dirty
            {
                get { return this.parentProperty.Dirty; }
            }

            public bool Enabled
            {
                get { return this.parentProperty.Enabled; }
            }

            public bool IsNull
            {
                get { return this.parentProperty.IsNull; }
            }

            public string Name
            {
                get { return this.propertyInfo.Name; }
            }

            public bool Required
            {
                get { return this.parentProperty.Required; }
            }

            public Type Type
            {
                get { return this.propertyInfo.PropertyType; }
            }

            public object Value
            {
                get
                {
                    return this.propertyInfo.GetValue(this.owner, null);
                }
                set
                {
                    this.propertyInfo.SetValue(this.owner, value, null);
                }
            }

            public bool Writable
            {
                get { return this.parentProperty.Writable; }
            }

            #endregion
        }
    }



    /// <summary>
    /// Indicated that this property depends on another property
    /// for metadata and value changes
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)] 
    public class ExtendedPropertyAttribute : Attribute 
    {
        string parentPropertyName;

        /// <summary>
        /// default ctor. no parent propety provided
        /// </summary>
        public ExtendedPropertyAttribute()
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="parentPropertyName">parent property</param>
        public ExtendedPropertyAttribute(string parentPropertyName)
        {
            // null or empty value is OK.
            this.parentPropertyName = parentPropertyName;
        }

        /// <summary>
        /// Parent Property name
        /// </summary>
        public string ParentPropertyName
        {
            get { return this.parentPropertyName; }
        }

        /// <summary>
        /// Indicates that property has a parent properety
        /// </summary>
        public bool HasParent
        {
            get
            {
                return !string.IsNullOrEmpty(this.parentPropertyName);
            }
        }
    }
}
