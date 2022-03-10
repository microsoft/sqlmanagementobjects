// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class Language : NamedSmoObject
    {

        internal Language(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "Language";
            }
        }


        /// <summary>
        /// The Day property returns the text string representing the name of a day in the referenced language.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "day-1")]
        public string Day(DayOfWeek day)
        {
            //the comma separated string from server has Monday on the first position and Sunday last, 
            //while DayOfWeek starts with Sunday which has the value 0; we have to get index 0 
            //for DayOfWeek.Monday and move DayOfWeek.Sunday at the end to 6
            int idx = (int)day - (int)DayOfWeek.Monday;
            if( -1 == idx )
            {
                idx = 6;
            }

            //the string to be split comes from server and is guaranteed to be well formed
            return ((System.String)Properties["Days"].Value).Split(',')[idx];
        }

        /// <summary>
        /// The ShortMonth property returns an abbreviation for the name of a month from an installed 
        ///	Microsoft� SQL Server� 2000 language.
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "month-1")]
        public string ShortMonth(Month month)
        {
            //make a zero based index
            int idx = (int)month - (int)Microsoft.SqlServer.Management.Smo.Month.January;
            
            //the string to be split comes from server and is guaranteed to be well formed
            return ((System.String)Properties["ShortMonths"].Value).Split(',')[idx];
        }

        /// <summary>
        /// The Month property returns the text string representing the name of a month in the referenced language.
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "month-1")]
        public string Month(Month month)
        {
            //make a zero based index
            int idx = (int)month - (int)Microsoft.SqlServer.Management.Smo.Month.January;
            
            //the string to be split comes from server and is guaranteed to be well formed
            return ((System.String)Properties["Months"].Value).Split(',')[idx];
        }
    }

    /// <summary>
    /// This object manages the lcid and name properties for both the user and database SMO objects.
    /// Methods are provided to read and write both the default and fulltext properties.
    /// </summary>
    public class DefaultLanguage 
    {
        private SqlSmoObject smoObj;

        private string parentPropertyName;
        private string lcidPropertyName;
        private string namePropertyName;

        //In case this object is created using Reflection lcid and name property names are not initialized.
        //The following variables will be used at that time because without property names we can't access the 
        //property bag.
        private string name = string.Empty; 
        private int lcid = -1;

        private DefaultLanguage() { }

        internal DefaultLanguage(SqlSmoObject smoObj,
                            string parentPropertyName)
        {
            this.smoObj = smoObj;
            this.parentPropertyName = parentPropertyName;

            this.lcidPropertyName =
                string.Format(CultureInfo.InvariantCulture,
                                "{0}Lcid",
                                this.parentPropertyName);
            this.namePropertyName =
                string.Format(CultureInfo.InvariantCulture,
                                "{0}Name",
                                this.parentPropertyName);
        }

        /// <summary>
        /// Gets or sets the locale id of the language.
        /// </summary>
        public int Lcid
        {
            get
            {
                if (!this.IsProperlyInitialized())
                {
                    return this.lcid;
                }

                object value = this.smoObj.GetPropertyOptional(this.lcidPropertyName).Value;
                return (value == null) ? -1 : (System.Int32)value; //-1 default value for default language.
            }
            set
            {
                this.SetProperty(this.lcidPropertyName, true, value, true);
            }
        }

        /// <summary>
        /// Gets or sets the name of the language.
        /// </summary>
        public string Name
        {
            get
            {
                if (!this.IsProperlyInitialized())
                {
                    return this.name;
                }

                object value = this.smoObj.GetPropertyOptional(this.namePropertyName).Value;
                return (value == null) ? string.Empty : (System.String)value;
            }
            set
            {
                this.SetProperty(this.namePropertyName, false, value, true);
            }
        }

        private void SetProperty(string propertyName, bool isLcid, object value, bool withConsistencyCheck)
        {
            if (!this.IsProperlyInitialized())
            {
                if (isLcid)
                {
                    this.lcid = (System.Int32)value;
                }
                else
                {
                    this.name = value as System.String;
                }
            }
            else
            {
                if (withConsistencyCheck)
                {
                    this.smoObj.Properties.SetValueWithConsistencyCheck(propertyName, value);
                }
                else
                {
                    this.smoObj.Properties.Get(propertyName).SetValue(value);
                    this.smoObj.Properties.Get(propertyName).SetRetrieved(true);
                }
            }
        }        

        internal bool IsProperlyInitialized()
        {
            return this.lcidPropertyName != null
                    && this.namePropertyName != null
                    && this.smoObj != null;
        }

        internal DefaultLanguage Copy(SqlSmoObject smoObj,
                                        string parentPropertyName)
        {
            DefaultLanguage copy = new DefaultLanguage(smoObj, parentPropertyName);            

            copy.SetProperty(copy.namePropertyName, false, this.Name, false);
            copy.SetProperty(copy.lcidPropertyName, true, this.Lcid, false);            

            return copy;
        }

        /// <summary>
        /// Throws exception if both the properties are set simultaneously.
        /// </summary>
        internal void VerifyBothLcidAndNameNotDirty(bool isLanguageValueNoneAllowed)
        {
            if (this.smoObj.IsSupportedProperty(this.lcidPropertyName))
            {
                Property lcidProp = this.smoObj.GetPropertyOptional(this.lcidPropertyName);
                Property nameProp = this.smoObj.GetPropertyOptional(this.namePropertyName);

                //If Language value "None" is allowed, we don't need to do this validation for
                //all values. We skip this validation for invalid values.
                //In User object, we script "None" when SMO consumer provides an invalid value for
                //Name or Lcid of the default language.
                if ((lcidProp.Dirty
                        && (isLanguageValueNoneAllowed || ((int)lcidProp.Value) >= 0)
                        )
                    && (nameProp.Dirty 
                        && (isLanguageValueNoneAllowed || !string.IsNullOrEmpty(nameProp.Value.ToString())))
                    )
                {
                    throw new SmoException(
                        ExceptionTemplates.MutuallyExclusiveProperties(
                        string.Format(CultureInfo.InvariantCulture,
                                        "{0}.Lcid",
                                        this.parentPropertyName),
                        string.Format(CultureInfo.InvariantCulture,
                                        "{0}.Name",
                                        this.parentPropertyName)
                        ));
                }
            }
        }

        /// <summary>
        /// Determines whether the specified objects are considered equal.
        /// </summary>
        /// <param name="obj">The object to compare to the current DataType.</param>
        /// <returns>A Boolean that specifies whether the objects are equal. True if the two objects are equal. False if they are not equal.</returns>
        public override bool Equals(object obj)
        {
            DefaultLanguage defaultLanguage = obj as DefaultLanguage;

            if (defaultLanguage == null)
            {
                return false;
            }

            return defaultLanguage.Lcid == this.Lcid
                    && defaultLanguage.Name == this.Name;
        }

        /// <summary>
        /// This method supports the SQL Server infrastructure and is not intended to be used directly from your code. 
        /// </summary>
        /// <returns>A hash code for the current Object.</returns>
        public override int GetHashCode()
        {
            return this.Lcid;
        }        
    }
}

