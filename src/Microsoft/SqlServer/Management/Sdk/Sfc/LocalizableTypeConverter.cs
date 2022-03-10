// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Resources;

// !!!! IMPORTANT !!!!
//
//      There is a copy of the types defined here in Sql\ssms\shared\ConnectionInfo\src\LocalizableTypeConverter.cs. See
//      that file for more information on why - but it's important to note that any changes made here should be
//      considered for porting to that file as well.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    #region PropertyOrder Attribute
    /// <summary>
    /// add this attribute to a property to specify the position that will be used
    /// when designer doesnt apply a "Categorized" or "Alphabetical" sort order
    /// (e.g. when PropertyGrid doesnt override the sort order - has PropertySort=NoSort)
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class PropertyOrderAttribute : Attribute
    {
        private int m_iOrder = -1;
        public PropertyOrderAttribute(int iOrder)
        {
            m_iOrder = iOrder;
        }

        public int Order
        {
            get
            {
                return m_iOrder;
            }
        }
    }
    #endregion

    #region LocalizedPropertyResources Attribute
    /// <summary>
    /// The name of the resources containing localized property category, name, and description strings
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class LocalizedPropertyResourcesAttribute : System.Attribute
    {
        private string resourcesName;
        private bool useDefaultKeys;

        /// <summary>
        /// The name of the resources containing localized property category and name strings
        /// </summary>
        public string ResourcesName
        {
            get
            {
                return resourcesName;
            }
        }
        ///<summary>
        ///Returns true if the keys should be picked up by defaults or if they should be retrieve as attributes
        ///</summary>
        public bool UseDefaultKeys
        {
            get
            {
                return useDefaultKeys;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resourcesName">The name of the resources (e.g. Microsoft.SqlServer.Foo.BarStrings)</param>
        public LocalizedPropertyResourcesAttribute(string resourcesName)
        {
            if (string.IsNullOrEmpty(resourcesName))
            {
                throw new ArgumentNullException("resourcesName");
            }

            TraceHelper.Assert(resourcesName != null, "unexpected null resourcesName parameter");
            TraceHelper.Assert(0 < resourcesName.Length, "unexpected empty resourcesName");

            this.resourcesName = resourcesName;
            this.useDefaultKeys = false;
        }

        /// <summary>
        /// Constructor
        ///  </summary>
        /// <param name="resourcesName">the name of the resource (e.g. Microsoft.SqlServer.Foo.BarStrings)</param>
        /// <param name="useDefaultKeys"></param>
        public LocalizedPropertyResourcesAttribute(string resourcesName, bool useDefaultKeys)
        {
            TraceHelper.Assert(resourcesName != null, "unexpected null resourcesName parameter");
            TraceHelper.Assert(0 < resourcesName.Length, "unexpected empty resourcesName");
            this.resourcesName = resourcesName;
            this.useDefaultKeys = useDefaultKeys;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resourceType">The type of the resources (e.g. Microsoft.SqlServer.Foo.BarStrings)</param>
        public LocalizedPropertyResourcesAttribute(Type resourceType)
        {
            if (resourceType == null)
            {
                throw new ArgumentNullException("resourceType");
            }

            TraceHelper.Assert(resourceType != null, "unexpected null resourceType parameter");
            this.resourcesName = resourceType.FullName;
        }
    }
    #endregion

    #region IDisplayKey Interface
    internal interface IDisplayKey
    {
        string Key { get; }
        string GetDefaultKey(PropertyInfo property);
        string GetDefaultKey(Type type);
        string GetDefaultKey(FieldInfo field);
    }

    internal static class DisplayKeyHelper
    {
        // Some dummy objects so that we can get the DefaultKey() values from the class
        private static DisplayNameKeyAttribute displayNameKey = new DisplayNameKeyAttribute(" ");
        private static DisplayDescriptionKeyAttribute displayDiscKey = new DisplayDescriptionKeyAttribute(" ");
        private static DisplayCategoryKeyAttribute displayCatKey = new DisplayCategoryKeyAttribute(" ");

        /// <summary>
        /// A factory method for getting an instance of the type that implements IDisplayKey
        /// </summary>
        /// <param name="keyAttribute"></param>
        /// <returns></returns>
        static private IDisplayKey GetDisplayKey(Type keyAttribute)
        {
            IDisplayKey key = null;
            if (keyAttribute.Equals(typeof(DisplayNameKeyAttribute)))
            {
                key = displayNameKey;
            }
            else if (keyAttribute.Equals(typeof(DisplayDescriptionKeyAttribute)))
            {
                key = displayDiscKey;
            }
            else if (keyAttribute.Equals(typeof(DisplayCategoryKeyAttribute)))
            {
                key = displayCatKey;
            }
            else
            {
                TraceHelper.Assert(false, "keyAttribute " + keyAttribute.Name + " is of unknown type");
            }

            return key;
        }

        /// <summary>
        /// Gets the Display value for a field
        /// </summary>
        /// <param name="field"></param>
        /// <param name="keyAttribute"></param>
        /// <param name="resourceManager"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        static public string GetValueFromCustomAttribute(FieldInfo field, Type keyAttribute, ResourceManager resourceManager, bool isDefault)
        {
            string displayValue = null;
            if (isDefault)
            {
                string keyDefault = GetDisplayKey(keyAttribute).GetDefaultKey(field);
                displayValue = GetDisplayValue(keyDefault, resourceManager);
            }
            else
            {
                object[] customAttributes = null;
                customAttributes = field.GetCustomAttributes(keyAttribute, true);
                displayValue = GetCustomDisplayValue(customAttributes, resourceManager);
            }
            return displayValue;
        }

        /// <summary>
        /// Gets the Display value for a property
        /// </summary>
        /// <param name="property"></param>
        /// <param name="keyAttribute"></param>
        /// <param name="resourceManager"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        static public string GetValueFromCustomAttribute(PropertyInfo property, Type keyAttribute, ResourceManager resourceManager, bool isDefault)
        {
            string displayValue = null;
            if (isDefault)
            {
                string keyDefault = GetDisplayKey(keyAttribute).GetDefaultKey(property);
                displayValue = GetDisplayValue(keyDefault, resourceManager);
            }
            else
            {
                object[] customAttributes = null;
                customAttributes = property.GetCustomAttributes(keyAttribute, true);
                displayValue = GetCustomDisplayValue(customAttributes, resourceManager);
            }
            return displayValue;
        }

        /// <summary>
        /// Gets the Display value for a Type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="keyAttribute"></param>
        /// <param name="resourceManager"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        static public string GetValueFromCustomAttribute(Type type, Type keyAttribute, ResourceManager resourceManager, bool isDefault)
        {
            string displayValue = null;
            if (isDefault)
            {
                string keyDefault = GetDisplayKey(keyAttribute).GetDefaultKey(type);
                displayValue = GetDisplayValue(keyDefault, resourceManager);
            }
            else
            {
                object[] customAttributes = null;
                customAttributes = type.GetCustomAttributes(keyAttribute, true);
                displayValue = GetCustomDisplayValue(customAttributes, resourceManager);
            }
            return displayValue;
        }

        /// <summary>
        /// Retrieves the key from the resource manager
        /// </summary>
        /// <param name="key"></param>
        /// <param name="resourceManager"></param>
        /// <returns></returns>
        static private string GetDisplayValue(string key, ResourceManager resourceManager)
        {
            string result = null;
            if (resourceManager != null)
            {
                result = resourceManager.GetString(key);
            }
            return result;
        }

        /// <summary>
        /// Retrieves the first key value from the customAttribute and retrives the value from the resource manager
        /// </summary>
        /// <param name="customAttributes"></param>
        /// <param name="resourceManager"></param>
        /// <returns></returns>
        static private string GetCustomDisplayValue(object[] customAttributes, ResourceManager resourceManager)
        {
            string result = null;
            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)) && (resourceManager != null))
            {
                string key = ((IDisplayKey)customAttributes[0]).Key;
                result = resourceManager.GetString(key);
            }

            return result;
        }

        /// <summary>
        /// A helper class for getting an empty string if the value is null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public string ConvertNullToEmptyString(string value)
        {
            return value == null ? String.Empty : value;
        }

        /// <summary>
        /// Returns the default key for a property
        /// </summary>
        /// <param name="postfix"></param>
        /// <param name="delim"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        static public string ConstructDefaultKey(string postfix, string delim, PropertyInfo property)
        {
            return property.DeclaringType.Name + delim + property.Name + postfix;
        }

        /// <summary>
        /// /// Returns the default key for a type
        /// </summary>
        /// <param name="postfix"></param>
        /// <param name="delim"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        static public string ConstructDefaultKey(string postfix, string delim, Type type)
        {
            return type.Name + delim + postfix;
        }

        /// <summary>
        /// /// Returns the default key for a field
        /// </summary>
        /// <param name="postfix"></param>
        /// <param name="delim"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        static public string ConstructDefaultKey(string postfix, string delim, FieldInfo field)
        {
            return field.DeclaringType.Name + delim + field.MemberType + delim + field.Name + postfix;
        }

    }
    #endregion

    #region DisplayCategoryKey Attribute
    /// <summary>
    /// The key used to look up the localized property category
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayCategoryKeyAttribute : System.Attribute, IDisplayKey
    {
        private string key;
        private static string postfix = "Cat";
        private static string delim = "_";

        /// <summary>
        /// The key used to look up the localized property category
        /// </summary>
        public string Key
        {
            get
            {
                return key;
            }
        }


        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public string GetDefaultKey(PropertyInfo property)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, property);
        }

        /// <summary>
        /// The key used to look up a localized type category in a default resource file
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetDefaultKey(Type type)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, type);
        }

        /// <summary>
        /// The key used to look up a localized field category in a default resource file
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public string GetDefaultKey(FieldInfo field)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, field);
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key used to look up the localized property category</param>
        public DisplayCategoryKeyAttribute(string key)
        {
            TraceHelper.Assert(key != null, "unexpected null key parameter");
            TraceHelper.Assert(0 < key.Length, "unexpected empty key");

            this.key = key;
        }
    }
    #endregion

    #region DisplayNameKey Attribute
    /// <summary>
    /// The key used to look up the localized property name
    /// </summary>
    /// <remarks>
    /// The AttributeTargets.Field is added to allow this attribute to be placed on Enum
    /// elements which the EnumConverter will use to localize each Enum value
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface)]
    public class DisplayNameKeyAttribute : System.Attribute, IDisplayKey
    {
        private string key;
        private static string postfix = "Name";
        private static string delim = "_";

        /// <summary>
        /// The key used to look up the localized property name
        /// </summary>
        public string Key
        {
            get
            {
                return key;
            }
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public string GetDefaultKey(PropertyInfo property)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, property);
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetDefaultKey(Type type)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, type);
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public string GetDefaultKey(FieldInfo field)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, field);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key used to look up the localized property name</param>
        public DisplayNameKeyAttribute(string key)
        {
            TraceHelper.Assert(key != null, "unexpected null key parameter");
            TraceHelper.Assert(0 < key.Length, "unexpected empty key");

            this.key = key;
        }
    }
    #endregion

    #region DisplayDescriptionKey Attribute
    /// <summary>
    /// The key used to look up the localized description
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface)]
    public class DisplayDescriptionKeyAttribute : System.Attribute, IDisplayKey
    {
        private string key;
        private static string postfix = "Desc";
        private static string delim = "_";

        /// <summary>
        /// The key used to look up the localized property description
        /// </summary>
        public string Key
        {
            get
            {
                return key;
            }
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public string GetDefaultKey(PropertyInfo property)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, property);
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetDefaultKey(Type type)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, type);
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public string GetDefaultKey(FieldInfo field)
        {
            return DisplayKeyHelper.ConstructDefaultKey(postfix, delim, field);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key used to look up the localized property description</param>
        public DisplayDescriptionKeyAttribute(string key)
        {
            TraceHelper.Assert(key != null, "unexpected null key parameter");
            TraceHelper.Assert(0 < key.Length, "unexpected empty key");

            this.key = key;
        }
    }
    #endregion


    #region IDynamicVisible Interface
    /// <summary>
    /// The IDynamicVisible interface should be implemented by any class that wants to limit
    /// the visibility of certain Enum values based on state of the class at that moment.
    ///
    /// If a class contains a property which exposes an Enum and that class implements the
    /// IDynamicVisible interface then it will be called for each property that is of an Enum type.
    ///
    /// The context param can be used to determine for which Enum this method is being called. If a class
    /// only has one Enum it is not necessary to make this check.
    ///
    /// The way to limit the visibility of certain items is to simply remove the unwanted Enum values
    /// from the values ArrayList. This method is called on every drop down of the enum so it is possible
    /// to change the list on each and every drop down. If the list will not change once it has been
    /// initially determined caching the ArrayList and returning it would be helpful.
    ///
    /// Care should be taken to ensure that you are not removing values that the Enum property is already
    /// set to. This will not cause any errors as all Enum values are still valid but when the user clicks
    /// on the dropdown they will not see the current choice as an option.
    ///
    /// Also no new values should be added to the list since these values will not be convertable to valid
    /// Enum values and an error will be thrown at runtime. If more dynamic control is needed then
    /// consider using the DynamicValues design.
    /// </summary>
    public interface IDynamicVisible
    {
        /// <summary>
        /// Removing items from the values list and returning that new list will control
        /// the values shown in the Enum specified in context.
        ///
        /// The enum can be determined with code similar to the following
        ///     if (context.PropertyDescriptor.PropertyType == typeof(myEnum))
        /// </summary>
        /// <param name="context"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        ICollection ConfigureVisibleEnumFields(ITypeDescriptorContext context, ArrayList values);
    }
    #endregion

    #region IDynamicValues Interface
    /// <summary>
    /// The IDynamicValues interface is used to create dynamic lists for string properties. A class
    /// should implement this interface, and use the DynamicValuesAttribute (below), to enable the
    /// generation of dynamic values.
    ///
    /// This interace and attribute can be used when a list of strings that will not be known until run-time
    /// should be shown in a drop down list. Good examples of this are database names, users, collations, etc.
    ///
    ///
    /// </summary>
    /// <example>
    /// public TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    /// {
    ///     string[] collationName = new string[] { "first item", "second itme" };
    ///     return new TypeConverter.StandardValuesCollection(collationName);
    /// }
    /// </example>
    public interface IDynamicValues
    {
        /// <summary>
        /// GetStandardValues should return a StandardValuesCollection which contains all the
        /// items to be displayed in the list of the property specifiec in context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context);
    }
    #endregion

    #region IDynamicProperties Interface
    /// <summary>
    /// Allows an object to dynamicaly add properties to the property grid
    /// </summary>
    public interface IDynamicProperties
    {
        /// <summary>
        /// Dynamically add properties to the Properties Collection.
        /// </summary>
        /// <param name="properties">Properties collection</param>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="attributes"></param>
        void AddProperties(PropertyDescriptorCollection properties, ITypeDescriptorContext context, object value, Attribute[] attributes);
    }
    #endregion

    #region IDynamicReadOnly Interface
    /// <summary>
    /// Allows an object to dynamicaly override the IsReadOnly value for any property
    /// </summary>
    public interface IDynamicReadOnly
    {
        /// <summary>
        /// Called with a list of LocalizablePropertyDescriptor which can then be called
        /// to override the IsReadOnly attribute of any property
        /// </summary>
        void OverrideReadOnly(IList<LocalizablePropertyDescriptor> properties, ITypeDescriptorContext context, object value, Attribute[] attributes);
        event EventHandler<ReadOnlyPropertyChangedEventArgs> ReadOnlyPropertyChanged;
    }

    /// <summary>
    /// EventArgs raised by the ReadOnlyPropertyChagned event to indicate which property was changed
    /// </summary>
    public class ReadOnlyPropertyChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Construct a new ReadOnlyPropertyChangedEventArgs with a property name
        /// </summary>
        public ReadOnlyPropertyChangedEventArgs(string propertyName)
        {
            this.propertyName = propertyName;
        }

        private string propertyName;
        /// <summary>
        /// Set or retrieve the Property Name
        /// </summary>
        public string PropertyName
        {
            get { return this.propertyName; }
            set { this.propertyName = value; }
        }
    }

    #endregion

    #region DynamicValues Attribute
    /// <summary>
    /// add this attribute to a property where you would like the values to be a dynamic list.
    /// The class that has a property with this attribute set must implement the IDynamicValues interface as
    /// the GetStandardValues mothod on that interface will be called to retrieve the valid values.
    ///
    /// If the attribute is present but not the interface no list will be returned so it will usually result
    /// in an empty drop down list being shown.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DynamicValuesAttribute : Attribute
    {
        private bool dynamicValuesEnabled = false;      // are we using dynamic values

        /// <summary>
        /// Used to enable or disable dynamic values. Passing false in the constructor can be helpful
        /// in debugging without removing the attribute
        /// </summary>
        /// <param name="enabled"></param>
        public DynamicValuesAttribute(bool enabled)
        {
            dynamicValuesEnabled = enabled;
        }

        public bool Enabled
        {
            get { return dynamicValuesEnabled; }
        }

    }
    #endregion

    #region LocalizablePropertyDescriptor Class
    /// <summary>
    /// LocalizablePropertyDescriptor.
    /// </summary>
    public class LocalizablePropertyDescriptor : PropertyDescriptor
    {
        private PropertyInfo property;
        private string displayName;
        private string displayCategory;
        private string displayDescription;
        private int displayOrdinal;
        private TypeConverter typeConverter = null;
        private bool readonlyOverride = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="property">The property that is being described</param>
        /// <param name="resourceManager">The resource manager containing the localized category and name strings</param>
        /// <param name="isDefaultResourceManager"></param>
        public LocalizablePropertyDescriptor(PropertyInfo property, ResourceManager resourceManager, bool isDefaultResourceManager)
            : base(property.Name, null)
        {
            TraceHelper.Assert(property != null, "unexpected null property object");
            TraceHelper.Assert(resourceManager != null, "resourceManager is null, is the resource string name correct?");

            // throw exceptions for null parameters
            if (property == null)
            {
                throw new ArgumentNullException("property");
            }

            this.property = property;

            // initialize display category - check DisplayCategory, then Category
            this.displayCategory = DisplayKeyHelper.GetValueFromCustomAttribute(property, typeof(DisplayCategoryKeyAttribute), resourceManager, isDefaultResourceManager);
            if (this.displayCategory == null)
            {
                this.displayCategory = DisplayKeyHelper.ConvertNullToEmptyString(GetCategoryAttribute(property));
            }

            // initialize display name - check DisplayNameKey, then type Name
            this.displayName = DisplayKeyHelper.GetValueFromCustomAttribute(property, typeof(DisplayNameKeyAttribute), resourceManager, isDefaultResourceManager);
            if ((displayName == null) || (displayName.Length == 0))
            {
                displayName = property.Name;
            }

            // initialize display description - check DisplayDescriptionKey, then Description
            this.displayDescription = DisplayKeyHelper.GetValueFromCustomAttribute(property, typeof(DisplayDescriptionKeyAttribute), resourceManager, isDefaultResourceManager);
            if (this.displayDescription == null)
            {
                this.displayDescription = DisplayKeyHelper.ConvertNullToEmptyString(GetDescriptionAttribute(property));
            }



            object[] customAttributes = null;

            // initialize display ordinal
            customAttributes = property.GetCustomAttributes(typeof(PropertyOrderAttribute), true);

            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
            {
                displayOrdinal = ((PropertyOrderAttribute)customAttributes[0]).Order;
            }
            else
            {
                displayOrdinal = 0;
            }

            // setup the special type converter for dynamic values
            customAttributes = property.GetCustomAttributes(typeof(DynamicValuesAttribute), true);

            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
            {
                typeConverter = new DynamicValueTypeConverter();
            }

            // for anything that the type of the property is an Enum we want to do enum translations
            // the enum translater is smart enough to not do translations if there are none
            if (property.PropertyType.IsEnum())
            {
                typeConverter = new LocalizableEnumConverter(property.PropertyType, resourceManager);
            }
        }


        /// <summary>
        /// Whether resetting an object changes its value
        /// </summary>
        /// <remarks>
        /// Property can't be reset using this descriptor
        /// </remarks>
        /// <param name="component">The object to test for reset capability</param>
        /// <returns>true if value can be reset, false otherwise</returns>
        public override bool CanResetValue(object component)
        {
            return false;
        }

        /// <summary>
        /// Whether the value of this property should be persisted.
        /// </summary>
        /// <remarks>
        /// This property should not be persisted..  If the underlying class is serializable,
        /// the underlying field (if any) should be persisted instead.
        /// </remarks>
        /// <param name="component">Referenced object</param>
        /// <returns>true if value should be serialized, false otherwise</returns>
        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        /// <summary>
        /// Reset to the default value
        /// </summary>
        /// <param name="component">Referenced object</param>
        public override void ResetValue(object component)
        {
        }
        /// <summary>
        /// Get the property value
        /// </summary>
        /// <param name="component">The object whose property value is to be retrieved</param>
        /// <returns>The property value</returns>
        public override object GetValue(object component)
        {
            if (component == null)
            {
                throw new ArgumentNullException("component");
            }

            if (!this.ComponentType.IsInstanceOfType(component))
            {
                throw new ArgumentException("Unexpected argument type", "component");
            }

            return property.GetValue(component, null);
        }

        /// <summary>
        /// Set the property value
        /// </summary>
        /// <param name="component">The object whose property value is to be set</param>
        /// <param name="value">The new property value</param>
        public override void SetValue(object component, object value)
        {
            if (component == null)
            {
                throw new ArgumentNullException("component");
            }

            if (!this.ComponentType.IsInstanceOfType(component))
            {
                throw new ArgumentException("Unexpected argument type", "component");
            }

            //for enum property that has LocalizableEnumConverter, we need to first convert string back to enum
            if (property.PropertyType.IsEnum() && value is String && typeConverter is LocalizableEnumConverter)
            {
                object enumValue = ((LocalizableEnumConverter)typeConverter).GetEnumValue((string)value);
                if(enumValue != null)
                {
                    property.SetValue(component, enumValue, null);
                    return;
                }
            }
            property.SetValue(component, value, null);
        }

        /// <summary>
        /// The localized category string for the property
        /// </summary>
        public override string Category
        {
            get
            {
                return this.displayCategory;
            }
        }

        /// <summary>
        /// The type of the class containing the property
        /// </summary>
        public override Type ComponentType
        {
            get
            {
                return property.ReflectedType;
            }
        }

        /// <summary>
        /// The localized description of the property
        /// </summary>
        public override string Description
        {
            get
            {
                return this.displayDescription;
            }
        }

        /// <summary>
        /// Whether the property can only be set at design time
        /// </summary>
        public override bool DesignTimeOnly
        {
            get
            {
                bool result = false;

                object[] customAttributes = property.GetCustomAttributes(typeof(DesignOnlyAttribute), true);

                if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
                {
                    result = ((DesignOnlyAttribute)customAttributes[0]).IsDesignOnly;
                }

                return result;
            }
        }

        /// <summary>
        /// The localized name that is to be displayed in object browsers such as PropertyGrid
        /// </summary>
        public override string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Whether the property should be displayed in object browsers such as PropertyGrid
        /// </summary>
        public override bool IsBrowsable
        {
            get
            {
                bool result = true;

                object[] customAttributes = property.GetCustomAttributes(typeof(BrowsableAttribute), true);

                if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
                {
                    result = ((BrowsableAttribute)customAttributes[0]).Browsable;
                }

                return result;
            }
        }

        /// <summary>
        /// Whether the property value should be localized
        /// </summary>
        public override bool IsLocalizable
        {
            get
            {
                bool result = false;

                object[] customAttributes = property.GetCustomAttributes(typeof(LocalizableAttribute), true);

                if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
                {
                    result = ((LocalizableAttribute)customAttributes[0]).IsLocalizable;
                }

                return result;
            }
        }

        /// <summary>
        /// Forces this property descriptor to return True for IsReadOnly
        /// </summary>
        public void ForceReadOnly()
        {
            this.readonlyOverride = true;
        }

        /// <summary>
        /// Whether the property is read-only
        /// </summary>
        public override bool IsReadOnly
        {
            get
            {
                if (this.readonlyOverride)
                {
                    return true;
                }

                // if the property is truly read-only, return true
                if (!property.CanWrite)
                {
                    return true;
                }

                // otherwise, check ReadOnly attribute (default is false)
                bool result = false;

                object[] customAttributes = property.GetCustomAttributes(typeof(ReadOnlyAttribute), true);

                if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
                {
                    result = ((ReadOnlyAttribute)customAttributes[0]).IsReadOnly;
                }

                return result;
            }
        }

        /// <summary>
        /// The unlocalized property name
        /// </summary>
        public override string Name
        {
            get
            {
                return property.Name;
            }
        }

        /// <summary>
        /// The type of the property
        /// </summary>
        public override Type PropertyType
        {
            get
            {
                return property.PropertyType;
            }
        }


        /// <summary>
        /// The ordinal at which the property should be displayed
        /// </summary>
        public int DisplayOrdinal
        {
            get
            {
                return this.displayOrdinal;
            }
        }

        /// <summary>
        /// Returns the TypeConverter to be used for this property. If not overwridden returns the default
        /// for this type.
        /// </summary>
        public override TypeConverter Converter
        {
            get
            {
                if (this.typeConverter == null)
                {
                    return base.Converter;
                }
                else
                {
                    return this.typeConverter;
                }
            }
        }



        #region Private Static Methods
        static private string GetCategoryAttribute(PropertyInfo property)
        {
            string displayCategory = null;
            object[] customAttributes = property.GetCustomAttributes(typeof(CategoryAttribute), true);

            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
            {
                displayCategory = ((CategoryAttribute)customAttributes[0]).Category;
            }

            return displayCategory;
        }

        static private string GetDescriptionAttribute(PropertyInfo property)
        {
            string displayDescription = null;
            object[] customAttributes = property.GetCustomAttributes(typeof(DescriptionAttribute), true);

            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
            {
                displayDescription = ((DescriptionAttribute)customAttributes[0]).Description;
            }

            return displayDescription;
        }
        #endregion
    }
    #endregion

    #region LocalizablePropertyComparer Internal Class
    /// <summary>
    /// Orders properties by ordinal, then by display name
    /// </summary>
    internal class LocalizablePropertyComparer : IComparer
    {
        public LocalizablePropertyComparer() { }

        /// <summary>
        /// Compare properties a and b
        /// </summary>
        /// <param name="a">First property</param>
        /// <param name="b">Second proprety</param>
        /// <returns>less than zero if a is less than b, 0 if a and b are equal, and greater than zero if a is greater than b</returns>
        public int Compare(object a, object b)
        {
            if ((a == null) || (b == null) || !(a is LocalizablePropertyDescriptor) || !(b is LocalizablePropertyDescriptor))
            {
                throw new ArgumentException();
            }

            LocalizablePropertyDescriptor desc_a = (LocalizablePropertyDescriptor)a;
            LocalizablePropertyDescriptor desc_b = (LocalizablePropertyDescriptor)b;

            int result = 0;

            if (desc_a.DisplayOrdinal < desc_b.DisplayOrdinal)
            {
                result = -1;
            }
            else if (desc_b.DisplayOrdinal < desc_a.DisplayOrdinal)
            {
                result = 1;
            }
            else
            {
                result = String.Compare(desc_a.DisplayName, desc_b.DisplayName, StringComparison.CurrentCulture);
            }

            return result;
        }
    }
    #endregion


    #region LocalizableMemberDescriptor Class
    /// <summary>
    /// LocalizableMemberDescriptor is a generic descriptor that LocalizableTypeConverter uses to return
    /// MemberDescriptor information for Types.
    /// </summary>
    public class LocalizableMemberDescriptor : MemberDescriptor
    {
        private Type type;
        private string displayName;
        private string displayCategory;
        private string displayDescription;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resourceManager">The resource manager containing the localized category and name strings</param>
        /// <param name="isDefaultResourceManager"></param>
        public LocalizableMemberDescriptor(Type type, ResourceManager resourceManager, bool isDefaultResourceManager)
            : base(type.Name, null)
        {
            TraceHelper.Assert(type != null, "unexpected null type object");
            TraceHelper.Assert(resourceManager != null, "resourceManager is null, is the resource string name correct?");

            // throw exceptions for null parameters
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            this.type = type;

            // initialize display category - check DisplayCategory, then Category
            this.displayCategory = DisplayKeyHelper.GetValueFromCustomAttribute(type, typeof(DisplayCategoryKeyAttribute), resourceManager, isDefaultResourceManager);
            if (this.displayCategory == null)
            {
                this.displayCategory = DisplayKeyHelper.ConvertNullToEmptyString(GetCategoryAttribute(type));
            }

            // initialize display name - check DisplayNameKey, then type Name
            this.displayName = DisplayKeyHelper.GetValueFromCustomAttribute(type, typeof(DisplayNameKeyAttribute), resourceManager, isDefaultResourceManager);
            if ((displayName == null) || (displayName.Length == 0))
            {
                displayName = type.Name;
            }

            // initialize display description - check DisplayDescriptionKey, then Description
            this.displayDescription = DisplayKeyHelper.GetValueFromCustomAttribute(type, typeof(DisplayDescriptionKeyAttribute), resourceManager, isDefaultResourceManager);
            if (this.displayDescription == null)
            {
                this.displayDescription = DisplayKeyHelper.ConvertNullToEmptyString(GetDescriptionAttribute(type));
            }

        }

        #region Public Properties
        /// <summary>
        /// The localized category string for the property
        /// </summary>
        public override string Category
        {
            get
            {
                return this.displayCategory;
            }
        }

        /// <summary>
        /// The localized description of the property
        /// </summary>
        public override string Description
        {
            get
            {
                return this.displayDescription;
            }
        }

        /// <summary>
        /// Whether the property can only be set at design time
        /// </summary>
        public override bool DesignTimeOnly
        {
            get
            {
                bool result = false;

                object[] customAttributes = this.type.GetCustomAttributes(typeof(DesignOnlyAttribute), true);

                if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
                {
                    result = ((DesignOnlyAttribute)customAttributes[0]).IsDesignOnly;
                }

                return result;
            }
        }

        /// <summary>
        /// The localized name that is to be displayed in object browsers such as PropertyGrid
        /// </summary>
        public override string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Whether the property should be displayed in object browsers such as PropertyGrid
        /// </summary>
        public override bool IsBrowsable
        {
            get
            {
                bool result = true;

                object[] customAttributes = this.type.GetCustomAttributes(typeof(BrowsableAttribute), true);

                if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
                {
                    result = ((BrowsableAttribute)customAttributes[0]).Browsable;
                }

                return result;
            }
        }

        /// <summary>
        /// The unlocalized property name
        /// </summary>
        public override string Name
        {
            get
            {
                return this.type.Name;
            }
        }
        #endregion

        #region Private Static Methods
        static private string GetCategoryAttribute(Type type)
        {
            string displayCategory = null;
            object[] customAttributes = type.GetCustomAttributes(typeof(CategoryAttribute), true);

            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
            {
                displayCategory = ((CategoryAttribute)customAttributes[0]).Category;
            }

            return displayCategory;
        }

        static private string GetDescriptionAttribute(Type type)
        {
            string displayDescription = null;
            object[] customAttributes = type.GetCustomAttributes(typeof(DescriptionAttribute), true);

            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
            {
                displayDescription = ((DescriptionAttribute)customAttributes[0]).Description;
            }

            return displayDescription;
        }
        #endregion

    }
    #endregion


    #region LocalizableTypeConverter Class
    /// <summary>
    /// A type converter to show the properies of a class using localized name, description, and category
    /// </summary>
    public class LocalizableTypeConverter : TypeConverter
    {
        private string defaultResourceName = "LocalizableResources";

        private void GetResourceManager(Type valueType, out ResourceManager resourceManager, out bool isDefaultResources)
        {
            resourceManager = null;
            isDefaultResources = false;
            // get the appropriate resource manager
            try
            {
                object[] customAttributes = valueType.GetCustomAttributes(typeof(LocalizedPropertyResourcesAttribute), true);
                string resourcesName = null;

                if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
                {
                    resourcesName = ((LocalizedPropertyResourcesAttribute)customAttributes[0]).ResourcesName;
                    isDefaultResources = ((LocalizedPropertyResourcesAttribute)customAttributes[0]).UseDefaultKeys;

                }
                else
                {
                    resourcesName = valueType.Namespace + "." + defaultResourceName;
                    isDefaultResources = true;
                }

                resourceManager = new ResourceManager(resourcesName, valueType.Assembly());
            }
            catch (ArgumentNullException)
            {
            }

        }

        private PropertyDescriptorCollection GetPropertiesFromObject(ITypeDescriptorContext context, object value, Attribute[] filter)
        {
            PropertyDescriptorCollection result = GetPropertiesFromType(value.GetType());

            // if the target implements IDynamicProperties give it the opportunity to add properties to
            // the collection
            IDynamicProperties dynamicProperties = value as IDynamicProperties;
            if (dynamicProperties != null)
            {
                dynamicProperties.AddProperties(result, context, value, filter);
            }

            // if the target implements IDynamicReadOnly give it the opportunity to override
            // the IsReadOnly property in these PropertyDescriptors
            IDynamicReadOnly dynamicReadOnlyValue = value as IDynamicReadOnly;
            if (dynamicReadOnlyValue != null)
            {
                // we need to make a list of LocalizablePropertyDescriptors
                // ideally this would be the native storage for this class but without doing major surgery, this works.
                List<LocalizablePropertyDescriptor> localizablePropertyDescriptors = new List<LocalizablePropertyDescriptor>(result.Count);
                foreach (PropertyDescriptor pd in result)
                {
                    // all of the results are LocalizablePropertyDescriptor's so the cast is safe
                    localizablePropertyDescriptors.Add((LocalizablePropertyDescriptor)pd);
                }
                dynamicReadOnlyValue.OverrideReadOnly(localizablePropertyDescriptors, context, value, filter);
            }

            return result;
        }


        private PropertyDescriptorCollection GetPropertiesFromType(Type valueType)
        {
            TraceHelper.Assert(valueType != null, "unexpected null value");

            if (valueType == null)
            {
                throw new ArgumentNullException("value");
            }

            PropertyDescriptorCollection result = null;
            bool isDefaultResources;
            ResourceManager manager;
            GetResourceManager(valueType, out manager, out isDefaultResources);
            if (manager != null)
            {
                // get the properties of the value passed in
                PropertyInfo[] properties = valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                result = this.GetPropertyDescriptorsFromPropertyInfo(properties, manager, isDefaultResources);
            }
            return result;
        }

        private PropertyDescriptorCollection GetPropertyDescriptorsFromPropertyInfo(PropertyInfo[] properties, ResourceManager resourceManager, bool isDefaultResources)
        {
            PropertyDescriptorCollection result = null;
            if (resourceManager != null)
            {
                // get the properties of the value passed in
                ArrayList browsablePropertyDescriptors = new ArrayList();

                // add the browsable descriptors to the collection
                foreach (PropertyInfo property in properties)
                {
                    LocalizablePropertyDescriptor descriptor = new LocalizablePropertyDescriptor(property, resourceManager, isDefaultResources);

                    if (descriptor.IsBrowsable)
                    {
                        browsablePropertyDescriptors.Add(descriptor);
                    }
                }

                // if there are browsable descriptors, put them in the result collection
                int browsableDescriptorCount = browsablePropertyDescriptors.Count;

                if (0 < browsableDescriptorCount)
                {
                    result = new PropertyDescriptorCollection((PropertyDescriptor[])null);

                    browsablePropertyDescriptors.Sort(new LocalizablePropertyComparer());

                    for (int index = 0; index < browsableDescriptorCount; ++index)
                    {
                        result.Insert(index, (PropertyDescriptor)browsablePropertyDescriptors[index]);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Get a collection of PropertyDescriptors describing the properties of the input object
        /// </summary>
        /// <param name="context">Unused, the context of the object</param>
        /// <param name="value">The object whose properties are being described</param>
        /// <param name="filter">Unused, attributes to filter the properties with.</param>
        /// <returns>A collection of PropertyDescriptors</returns>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] filter)
        {
            if (value is Type)
            {
                return GetPropertiesFromType((Type)value);
            }
            else
            {
                return GetPropertiesFromObject(context, value, filter);
            }
        }


        /// <summary>
        /// Get a collection of PropertyDescriptors describing the properties passed in.  The ResourceManager will be retrieved from
        /// the declaring type of the first property in the list of properties.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties(PropertyInfo[] properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }

            PropertyDescriptorCollection result = null;
            if (properties.Length != 0)
            {
                Type valueType = properties[0].DeclaringType;
                bool isDefaultResources;
                ResourceManager manager;
                GetResourceManager(valueType, out manager, out isDefaultResources);
                if (manager != null)
                {
                    // get the properties of the value passed in
                    result = this.GetPropertyDescriptorsFromPropertyInfo(properties, manager, isDefaultResources);
                }
            }
            return result;
        }

        /// <summary>
        /// Whether custom property descriptions supported
        /// </summary>
        /// <param name="context">Unused, the context for which descriptions are to be requested</param>
        /// <returns>Whether custom property descriptions supported</returns>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        /// Retrieves the MemberDescriptor for the type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public LocalizableMemberDescriptor GetTypeMemberDescriptor(Type type)
        {
            TraceHelper.Assert(type != null, "unexpected null value");
            if (type == null)
            {
                throw new ArgumentNullException("value");
            }
            bool isDefaultResources;
            ResourceManager manager;
            GetResourceManager(type, out manager, out isDefaultResources);
            return new LocalizableMemberDescriptor(type, manager, isDefaultResources);
        }
    }
    #endregion

    #region LocalizableEnumConverter Class
    /// <summary>
    /// The LocalizableEnumConverter allows for the values of an Enum to be converted to localized
    /// strings based on the DisplayNameKey attribute applied to the individual items in the Enum.
    /// </summary>
    /// <example>
    ///     public enum ScriptModeOptions
    ///     {
    ///         [DisplayNameKey("CreateOnlyMode")] scriptCreateOnly,
    ///         [DisplayNameKey("DropOnlyMode")] scriptDropOnly
    ///     }
    /// </example>
    public class LocalizableEnumConverter : EnumConverter
    {
        // Maps from strings to enum values.
        // for enum values to strings we just walk through the list.
        // Most enums are small show this should be a small perf hit.
        // Not using HashTable because it doesn't keep alphabetic order of fields in LoadLocalizedNames,
        // which leads to a disordered drop down in propertygrid.
        SortedList localizedEnumFields = new SortedList(StringComparer.CurrentCulture);

        /// <summary>
        /// Load display names for the enum fields
        /// </summary>
        /// <param name="type">The .NET Type for the enum</param>
        /// <param name="manager">The resource manager used to load localized field names</param>
        private void LoadLocalizedNames(Type type, ResourceManager manager)
        {
            // to keep us from constantly going to the resource manager we will load
            // up all the conversions here and then just reference them in each of the Convert functions

            // load localized field names if there is a resource manager for the type, otherwise
            // load the unlocalized, literal field names
            if (manager != null)
            {
                LoadLocalizedFieldNames(type, manager);
            }
            else
            {
                LoadUnlocalizedFieldNames(type);
            }
        }

        /// <summary>
        /// Load localized display names for the enum fields from a resource manager
        /// </summary>
        /// <param name="type">The .NET Type for the enum</param>
        /// <param name="manager">The resource manager used to load localized field names</param>
        private void LoadLocalizedFieldNames(Type type, ResourceManager manager)
        {
            TraceHelper.Assert(type.IsEnum(), "type is not an Enum");
            TraceHelper.Assert(manager != null, "manager is null");

            // we get the FieldInfo for each field and then pull off the DisplayNameKey if it has one
            // and then use that to get the localized value
            foreach (string fieldName in Enum.GetNames(type))
            {
                FieldInfo fi = type.GetField(fieldName);

                var attributesAsObjects = fi.GetCustomAttributes(typeof(DisplayNameKeyAttribute), true);
                DisplayNameKeyAttribute[] attributes = attributesAsObjects.OfType<DisplayNameKeyAttribute>().ToArray();

                if (attributes.Length > 0)
                {
                    string key = manager.GetString(attributes[0].Key);

                    // if this key did not exist in the resource manager then we need to
                    // just use the fieldName as the loc'ed name
                    if (key == null)
                    {
                        key = fieldName;
                    }
                    localizedEnumFields[key] = Enum.Parse(type, fieldName);
                }
                else
                {
                    // if we have no localized field then we just use the name
                    localizedEnumFields[fieldName] = Enum.Parse(type, fieldName);
                }
            }
        }


        /// <summary>
        /// Load the field names for the enum
        /// </summary>
        /// <remarks>
        /// this is called when there are no localized strings for the field names.  In lieu of localized
        /// field names, the method puts the C# enum field names in the field name dictionary.
        /// </remarks>
        /// <param name="type">The .NET Type for the enum</param>
        private void LoadUnlocalizedFieldNames(Type type)
        {
            TraceHelper.Assert(type.IsEnum(), "type is not an Enum");

            foreach (string fieldName in Enum.GetNames(type))
            {
                localizedEnumFields[fieldName] = Enum.Parse(type, fieldName);
            }
        }

        /// <summary>
        /// This constructor is used by our internal PropertyDescriptor when it is created automatically
        /// for any Enum property.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="manager"></param>
        internal LocalizableEnumConverter(System.Type type, ResourceManager manager)
            : base(type.GetType())
        {
            // we get a resource manager from the LocalizableTypeCoverter so we just keep using that one
            LoadLocalizedNames(type, manager);
        }

        /// <summary>
        /// This constructor is the default constructor that would be used if this converter is placed
        /// on an Enum class directly and not via the abstraction through the LocalizedTypeConverter attribute
        /// on the containing class.
        /// </summary>
        /// <param name="type"></param>
        public LocalizableEnumConverter(System.Type type)
            : base(type.GetType())
        {
            // we don't have a resource manager yet so we need to go get one from the attribute.
            ResourceManager manager = null;

            object[] customAttributes = type.GetCustomAttributes(typeof(LocalizedPropertyResourcesAttribute), true);

            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
            {
                string resourcesName = ((LocalizedPropertyResourcesAttribute)customAttributes[0]).ResourcesName;
                manager = new ResourceManager(resourcesName, type.Assembly());
            }

            LoadLocalizedNames(type, manager);
        }

        /// <summary>
        /// used to translate the enum value into the localized string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GetEnumDescription(Enum value)
        {
            string description = string.Empty;

            // we just have to walk over the hashtable and find the right value
            IDictionaryEnumerator IEnum = localizedEnumFields.GetEnumerator();
            while (IEnum.MoveNext())
            {
                if (IEnum.Value.Equals(value))
                {
                    description = (string)IEnum.Key;
                    break;
                }
            }

            return description;

        }

        /// <summary>
        /// Get the enum value based on the string. This uses the hashtable lookup to increase perf.
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        internal object GetEnumValue(string description)
        {
            return localizedEnumFields[description];
        }

        /// <summary>
        /// Does the conversion from Enum to string and the odd string to string. All others are passed on to the base
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (value is Enum && destinationType == typeof(string))
            {
                return this.GetEnumDescription((Enum)value);
            }
            if (value is string && destinationType == typeof(string))
            {
                return value;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        /// <summary>
        /// Does the conversion from string to enum and the odd enum to enum. All others are passed on to the base
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                return this.GetEnumValue((string)value);
            }
            if (value is Enum)
            {
                return value;
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Returns the list of values for the list box in the property grid.
        /// If the IDynamicVisible interface is defined then we call into that to get the manipulated values
        /// array. If not then we just return the list of values.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StandardValuesCollection valuesCollection = null;

            if (context.Instance is IDynamicVisible)
            {
                // we use values here because the enum defining class won't necessarily know what the translated
                // values are and dealing with Enums is easier. The standard conversion calls above will be called
                // for each of the values so the translation will still occur.
                ICollection newValues = ((IDynamicVisible)context.Instance).ConfigureVisibleEnumFields(context, new ArrayList(localizedEnumFields.Values));
                valuesCollection = new StandardValuesCollection(newValues);
            }
            else
            {
                // we use the keys here because these are the pre-translated values and the conversion routines
                // are still called but they basically no-op (just return the string again) for string to string
                // conversion. This results in a much shorter code path
                valuesCollection = new StandardValuesCollection(localizedEnumFields.Keys);
            }

            return valuesCollection;
        }

        // we are obviously using standard values
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        // enums are always a limited list
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
    }

#endregion

#region DynamicValueTypeConverter Class
    /// <summary>
    /// The DynamicValueTypeConverter is used to call into the containing class to allow the
    /// class to generate the dynamic list.
    ///
    /// This TypeConverter is created and returned for any properties that have the DynamicValues
    /// attribute enabled.
    /// </summary>
    public class DynamicValueTypeConverter : StringConverter
    {
        // we simply look to see if the instance we are being called on has the IDynamicValues interface
        // and if it does call into that GetStandardValues implementation.
        // If not just call into the base. This will most likely be a null set.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (context.Instance is IDynamicValues)
            {
                return ((IDynamicValues)context.Instance).GetStandardValues(context);
            }
            else
            {
                return base.GetStandardValues(context);
            }
        }

        // we are obviously supporting standard values
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        // the list is exlusive. If the need every comes up to support non exlusive lists (combo boxes)
        // the DynamicValues attribute could be modified to include a property for that support and then
        // the attribute could be checked here and return the correct value.
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

    }
#endregion
}
