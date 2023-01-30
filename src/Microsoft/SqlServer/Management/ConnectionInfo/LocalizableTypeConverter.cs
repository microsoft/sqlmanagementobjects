// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Resources;


/*
 *
 *                                              !!!! IMPORTANT - READ ME !!!!!
 *
 * This file contains copies of the types from $\Sql\ssms\smo\ManagementSDK\SFC\src\LocalizableTypeConverter.cs. This is because we want
 * to use these types to handle converting enum values from ConnectionEnums to localized strings, but since SMO has a reference dependency
 * on ConnectionInfo we can't do that unless we move the enums out or move the Type Converter types here. Given that both are public
 * interfaces though this is not possible at this time without significant work and risk of breaks - and so the easiest (albeit verbose)
 * option is to copy the needed types here for the time being.
 *
 * The types were renamed (prefixed with Common) so to avoid name collisions with the original types and reduce chance of errors.
 */
namespace Microsoft.SqlServer.Management.Common
{

    #region CommonLocalizedPropertyResources Attribute
    /// <summary>
    /// The name of the resources containing localized property category, name, and description strings
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class CommonLocalizedPropertyResourcesAttribute : System.Attribute
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
        public CommonLocalizedPropertyResourcesAttribute(string resourcesName)
        {
            if (string.IsNullOrEmpty(resourcesName))
            {
                throw new ArgumentNullException("resourcesName");
            }

            this.resourcesName = resourcesName;
            this.useDefaultKeys = false;
        }

        /// <summary>
        /// Constructor
        ///  </summary>
        /// <param name="resourcesName">the name of the resource (e.g. Microsoft.SqlServer.Foo.BarStrings)</param>
        /// <param name="useDefaultKeys"></param>
        public CommonLocalizedPropertyResourcesAttribute(string resourcesName, bool useDefaultKeys)
        {
            this.resourcesName = resourcesName;
            this.useDefaultKeys = useDefaultKeys;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resourceType">The type of the resources (e.g. Microsoft.SqlServer.Foo.BarStrings)</param>
        public CommonLocalizedPropertyResourcesAttribute(Type resourceType)
        {
            if (resourceType == null)
            {
                throw new ArgumentNullException("resourceType");
            }

            this.resourcesName = resourceType.FullName;
        }
    }
    #endregion

    #region ICommonDisplayKey Interface
    internal interface ICommonDisplayKey
    {
        string Key { get; }
        string GetDefaultKey(PropertyInfo property);
        string GetDefaultKey(Type type);
        string GetDefaultKey(FieldInfo field);
    }

    internal static class CommonDisplayKeyHelper
    {
        // Some dummy objects so that we can get the DefaultKey() values from the class
        private static CommonDisplayNameKeyAttribute displayNameKey = new CommonDisplayNameKeyAttribute(" ");
        private static CommonDisplayDescriptionKeyAttribute displayDiscKey = new CommonDisplayDescriptionKeyAttribute(" ");
        private static CommonDisplayCategoryKeyAttribute displayCatKey = new CommonDisplayCategoryKeyAttribute(" ");

        /// <summary>
        /// A factory method for getting an instance of the type that implements IDisplayKey
        /// </summary>
        /// <param name="keyAttribute"></param>
        /// <returns></returns>
        static private ICommonDisplayKey GetDisplayKey(Type keyAttribute)
        {
            ICommonDisplayKey key = null;
            if (keyAttribute.Equals(typeof(CommonDisplayNameKeyAttribute)))
            {
                key = displayNameKey;
            }
            else if (keyAttribute.Equals(typeof(CommonDisplayDescriptionKeyAttribute)))
            {
                key = displayDiscKey;
            }
            else if (keyAttribute.Equals(typeof(CommonDisplayCategoryKeyAttribute)))
            {
                key = displayCatKey;
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
        /// Retrives the key from the resource manager
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
        /// Retrives the first key value from the customAttribute and retrives the value from the resource manager
        /// </summary>
        /// <param name="customAttributes"></param>
        /// <param name="resourceManager"></param>
        /// <returns></returns>
        static private string GetCustomDisplayValue(object[] customAttributes, ResourceManager resourceManager)
        {
            string result = null;
            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)) && (resourceManager != null))
            {
                string key = ((ICommonDisplayKey)customAttributes[0]).Key;
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
    public class CommonDisplayCategoryKeyAttribute : System.Attribute, ICommonDisplayKey
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
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, property);
        }

        /// <summary>
        /// The key used to look up a localized type category in a default resource file
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetDefaultKey(Type type)
        {
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, type);
        }

        /// <summary>
        /// The key used to look up a localized field category in a default resource file
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public string GetDefaultKey(FieldInfo field)
        {
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, field);
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key used to look up the localized property category</param>
        public CommonDisplayCategoryKeyAttribute(string key)
        {
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
    public class CommonDisplayNameKeyAttribute : System.Attribute, ICommonDisplayKey
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
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, property);
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetDefaultKey(Type type)
        {
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, type);
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public string GetDefaultKey(FieldInfo field)
        {
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, field);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key used to look up the localized property name</param>
        public CommonDisplayNameKeyAttribute(string key)
        {
            this.key = key;
        }
    }
    #endregion

    #region DisplayDescriptionKey Attribute
    /// <summary>
    /// The key used to look up the localized description
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface)]
    public class CommonDisplayDescriptionKeyAttribute : System.Attribute, ICommonDisplayKey
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
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, property);
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetDefaultKey(Type type)
        {
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, type);
        }

        /// <summary>
        /// The key used to look up a localized property category in a default resource file
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public string GetDefaultKey(FieldInfo field)
        {
            return CommonDisplayKeyHelper.ConstructDefaultKey(postfix, delim, field);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key used to look up the localized property description</param>
        public CommonDisplayDescriptionKeyAttribute(string key)
        {
            this.key = key;
        }
    }
    #endregion

    #region ICommonDynamicVisible Interface
    /// <summary>
    /// The ICommonDynamicVisible interface should be implemented by any class that wants to limit
    /// the visibility of certain Enum values based on state of the class at that moment.
    ///
    /// If a class contains a property which exposes an Enum and that class implements the
    /// ICommonDynamicVisible interface then it will be called for each property that is of an Enum type.
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
    public interface ICommonDynamicVisible
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


    #region CommonLocalizableEnumConverter Class
    /// <summary>
    /// The CommonLocalizableEnumConverter allows for the values of an Enum to be converted to localized
    /// strings based on the DisplayNameKey attribute applied to the individual items in the Enum.
    /// </summary>
    /// <example>
    ///     public enum ScriptModeOptions
    ///     {
    ///         [DisplayNameKey("CreateOnlyMode")] scriptCreateOnly,
    ///         [DisplayNameKey("DropOnlyMode")] scriptDropOnly
    ///     }
    /// </example>
    public class CommonLocalizableEnumConverter : EnumConverter
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
            // we get the FieldInfo for each field and then pull off the DisplayNameKey if it has one
            // and then use that to get the localized value
            foreach (string fieldName in Enum.GetNames(type))
            {
                FieldInfo fi = type.GetField(fieldName);

                var attributesAsObjects = fi.GetCustomAttributes(typeof(CommonDisplayNameKeyAttribute), true);
                CommonDisplayNameKeyAttribute[] attributes = attributesAsObjects.OfType<CommonDisplayNameKeyAttribute>().ToArray();

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
        internal CommonLocalizableEnumConverter(System.Type type, ResourceManager manager)
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
        public CommonLocalizableEnumConverter(System.Type type)
            : base(type.GetType())
        {
            // we don't have a resource manager yet so we need to go get one from the attribute.
            ResourceManager manager = null;

            object[] customAttributes = type.GetCustomAttributes(typeof(CommonLocalizedPropertyResourcesAttribute), true);

            if ((customAttributes != null) && (0 < customAttributes.GetLength(0)))
            {
                string resourcesName = ((CommonLocalizedPropertyResourcesAttribute)customAttributes[0]).ResourcesName;
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
        /// If the ICommonDynamicVisible interface is defined then we call into that to get the manipulated values
        /// array. If not then we just return the list of values.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StandardValuesCollection valuesCollection = null;

            if (context.Instance is ICommonDynamicVisible)
            {
                // we use values here because the enum defining class won't necessarily know what the translated
                // values are and dealing with Enums is easier. The standard conversion calls above will be called
                // for each of the values so the translation will still occur.
                ICollection newValues = ((ICommonDynamicVisible)context.Instance).ConfigureVisibleEnumFields(context, new ArrayList(localizedEnumFields.Values));
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

}
