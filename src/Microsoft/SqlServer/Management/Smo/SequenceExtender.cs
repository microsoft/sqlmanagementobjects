// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents sql server SequenceExtender object
    ///</summary>
    [CLSCompliant(false)]
    public class SequenceExtender : SmoObjectExtender<Sequence>, ISfcValidate
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SequenceExtender() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sequence"></param>
        public SequenceExtender(Sequence sequence) : base(sequence) { }

        /// <summary>
        /// State
        /// </summary>
        [ExtendedPropertyAttribute()]
        public SqlSmoState State 
        { 
            get 
            { 
                return this.Parent.State; 
            } 
        }

        ///<summary>
        /// Current SMO Object
        ///</summary>
        [ExtendedPropertyAttribute()]
        public SqlSmoObject CurrentObject 
        { 
            get 
            { 
                return this.Parent; 
            } 
        }

        ///<summary>
        /// Name
        ///</summary>
        [ExtendedPropertyAttribute()]
        public string Name
        {
            get
            {
                return this.Parent.Name;
            }
            set
            {
                this.Parent.Name = value;
            }
        }

        ///<summary>
        /// Schema
        ///</summary>
        [ExtendedPropertyAttribute()]
        public string Schema
        {
            get
            {
                return this.Parent.Schema;
            }
            set
            {
                this.Parent.Schema = value;
            }
        }

        ///<summary>
        /// DatabaseName
        ///</summary>
        [ExtendedPropertyAttribute()]
        public string DatabaseName
        {
            get
            {
                return this.Parent.Parent.Name;
            }
        }

        ///<summary>
        /// ConnectionContext
        ///</summary>

        [ExtendedPropertyAttribute()]
        public ServerConnection ConnectionContext
        {
            get
            {
                return this.Parent.Parent.GetServerObject().ConnectionContext;
            }
        }

       

        StringCollection datatypeNames;

        ///<summary>
        /// Sequence Supported System data types
        ///</summary>
        [ExtendedPropertyAttribute()]
        public StringCollection DatatypeNames
        {
            get
            {
                if (this.datatypeNames == null)
                {
                    this.datatypeNames = new StringCollection();
                    Database db = this.Parent.Parent;
                    if (db != null)
                    {
                        this.datatypeNames.Add("tinyint");
                        this.datatypeNames.Add("smallint");
                        this.datatypeNames.Add("int");
                        this.datatypeNames.Add("bigint");
                        this.datatypeNames.Add("decimal");
                        this.datatypeNames.Add("numeric");
                        Server server = db.GetServerObject();
                        server.SetDefaultInitFields(typeof(UserDefinedDataType), "NumericScale");
                        server.SetDefaultInitFields(typeof(UserDefinedDataType), "SystemType");

                        foreach (UserDefinedDataType uddt in db.UserDefinedDataTypes)
                        {
                            string sType = uddt.SystemType;
                            string schema = uddt.Schema;
                            string name = uddt.Name;
                            switch (sType.ToLower(SmoApplication.DefaultCulture))
                            {
                                case "tinyint":
                                case "smallint":
                                case "int":
                                case "bigint":
                                    this.datatypeNames.Add(UddtFullName(schema, name));
                                    break;
                                case "decimal":
                                case "numeric":

                                    int scale = uddt.NumericScale;
                                    if (scale == 0)
                                    {
                                        this.datatypeNames.Add(UddtFullName(schema, name));
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }                                              
                    }
                }
                return this.datatypeNames;
            }
        }

        
        string selectedDatatypeName = String.Empty;

        ///<summary>
        /// SelectedDatatypeName
        ///</summary>
        [ExtendedPropertyAttribute()]
        public string SelectedDatatypeName
        {
            get
            {
                if(String.IsNullOrEmpty(selectedDatatypeName))
                {
                    //bigint is the default value used by the engine if nothing is specified
                    return "bigint";
                }
                else
                {
                    return this.selectedDatatypeName;
                }
            }
            set
            {
                this.selectedDatatypeName = value;
                
                switch (this.selectedDatatypeName.ToLower(SmoApplication.DefaultCulture))
                {
                    case "tinyint":
                    case "smallint":
                    case "int":
                    case "bigint":                   
                        SqlDataType sqlDataType = DataType.SqlToEnum(this.selectedDatatypeName);
                        this.Parent.DataType = new DataType(sqlDataType);                        
                        break;
                    case "decimal":
                    case "numeric":
                        SqlDataType sqlDecimalDataType = DataType.SqlToEnum(this.selectedDatatypeName);
                        this.Parent.DataType = new DataType(sqlDecimalDataType);
                        break;
                    default:
                        UserDefinedDataType uddt = GetUDDT(this.SelectedDatatypeName);
                        if (uddt != null)
                        {
                            this.Parent.DataType = new DataType(uddt);
                        }                                             
                        break;
                }
            }
        }

        private UserDefinedDataType GetUDDT(string datatypename)
        {
            Database db = this.Parent.Parent;
            foreach (UserDefinedDataType uddt in db.UserDefinedDataTypes)
            {
                if (0 == string.Compare(UddtFullName(uddt.Schema, uddt.Name), this.selectedDatatypeName, StringComparison.OrdinalIgnoreCase))
                {
                    return uddt;
                }
            }
            return null;

        }

        private static string UddtFullName(string schema, string name)
        {
            string fullName = String.Empty;
            if (!String.IsNullOrEmpty(schema))
            {
                fullName = String.Format(SmoApplication.DefaultCulture, "{0}.{1}", schema, name);
            }
            else
            {
                fullName = String.Format(SmoApplication.DefaultCulture, "{0}", name);
            }
            return fullName;
        }

        int defaultPrecision = 18;

        ///<summary>
        /// DefaultPrecision
        ///</summary>
        [ExtendedPropertyAttribute()]
        public int DefaultPrecision
        {
            get
            {
                switch (this.SelectedDatatypeName.ToLower(SmoApplication.DefaultCulture))
                {
                    case "tinyint":
                        defaultPrecision = 3;
                        break;
                    case "smallint":
                        defaultPrecision = 5;
                        break;
                    case "int":
                        defaultPrecision = 10;
                        break;
                    case "bigint":
                        defaultPrecision = 19;
                        break;
                    case "decimal":
                    case "numeric":
                        defaultPrecision = 18;
                        break;
                   
                    default:
                        UserDefinedDataType uddt = GetUDDT(this.SelectedDatatypeName);
                        if (uddt != null)
                        {
                            defaultPrecision = uddt.NumericPrecision;
                        }                       
                        break;
                }
                return defaultPrecision;
            }

        }


        string selectedNumericPrecision = String.Empty;

        ///<summary>
        /// SelectedNumericPrecision
        ///</summary>
        [ExtendedPropertyAttribute()]
        public string SelectedNumericPrecision
        {
            get
            {
                return this.selectedNumericPrecision;
            }
            set
            {
                this.selectedNumericPrecision = value;
                this.Parent.DataType.NumericPrecision = Convert.ToInt32(this.selectedNumericPrecision,SmoApplication.DefaultCulture);
            }
        }

        bool hasMinimumValue = false;

        ///<summary>
        /// HasMinimumValue
        ///</summary>
        [ExtendedPropertyAttribute()]
        public bool HasMinimumValue
        {
            get
            {
                if ((this.Parent.State == SqlSmoState.Existing) && !String.IsNullOrEmpty(this.Parent.MinValue.ToString()))
                {
                    this.hasMinimumValue = true;
                }
                return this.hasMinimumValue;
            }
            set
            {
                this.hasMinimumValue = value;
                if (!this.hasMinimumValue)
                {
                    this.Parent.MinValue = String.Empty;
                }
            }
        }

        bool hasMaximumValue = false;

        ///<summary>
        /// HasMaximumValue
        ///</summary>
        [ExtendedPropertyAttribute()]
        public bool HasMaximumValue
        {
            get
            {
                if ((this.Parent.State == SqlSmoState.Existing) && !String.IsNullOrEmpty(this.Parent.MaxValue.ToString()))
                {
                    this.hasMaximumValue = true;
                }
                return this.hasMaximumValue;
            }
            set
            {
                this.hasMaximumValue = value;
                if (!this.hasMaximumValue)
                {
                    this.Parent.MaxValue = String.Empty;
                }
            }
        }       

        string sequenceDatatypeName = String.Empty;

        ///<summary>
        /// SequenceDatatypeName
        ///</summary>
        [ExtendedPropertyAttribute()]
        public string SequenceDatatypeName
        {
            get
            {
                if (this.Parent.State == SqlSmoState.Existing)
                {                    
                    sequenceDatatypeName = UddtFullName(this.Parent.DataType.Schema, this.Parent.DataType.Name);
                }
                return sequenceDatatypeName;
            }
           
        }

        string sequenceNumericPrecision = String.Empty;

        ///<summary>
        /// SequenceNumericPrecision
        ///</summary>
        [ExtendedPropertyAttribute()]
        public string SequenceNumericPrecision
        {
            get
            {
                if (this.Parent.State == SqlSmoState.Existing)
                {
                    sequenceNumericPrecision = Convert.ToString(this.Parent.DataType.NumericPrecision, SmoApplication.DefaultCulture);
                    
                }
                return sequenceNumericPrecision;
            }

        }

        bool hasRestartValue = false;

        ///<summary>
        /// HasRestartValue
        ///</summary>
        [ExtendedPropertyAttribute()]
        public bool HasRestartValue
        {
            get
            {
                return this.hasRestartValue;
            }
            set
            {
                this.hasRestartValue = value;                
            }
        }

        Object originalStartValue = String.Empty;

        ///<summary>
        /// OriginalStartValue
        ///</summary>
        [ExtendedPropertyAttribute()]
        public Object OriginalStartValue
        {
            get
            {
                return this.originalStartValue;
            }
            set
            {
                this.originalStartValue = value;
            }
        }

        private event EventHandler permissionPageOnRunNow;

        ///<summary>
        /// PermissionPageOnRunNow
        ///</summary>
        [ExtendedPropertyAttribute()]
        public object PermissionPageOnRunNow
        {
            get { return this.permissionPageOnRunNow; }
            set { this.permissionPageOnRunNow = (EventHandler)value; }
        }

        private object permissionPageDataContainer;

        ///<summary>
        /// PermissionPageDataContainer
        ///</summary>
        [ExtendedPropertyAttribute()]
        public object PermissionPageDataContainer
        {
            get { return this.permissionPageDataContainer; }
            set { this.permissionPageDataContainer = value; }
        }

        private event EventHandler extendedPropertyPageOnRunNow;

        ///<summary>
        /// ExtendedPropertyPageOnRunNow
        ///</summary>
        
        [ExtendedPropertyAttribute()]        
        public object ExtendedPropertyPageOnRunNow
        {
            get { return this.extendedPropertyPageOnRunNow; }
            set { this.extendedPropertyPageOnRunNow = (EventHandler)value; }
        }

        private event EventHandler extendedPropertyPageCommitCellEdits;

        ///<summary>
        /// ExtendedPropertyPageCommitCellEdits
        ///</summary>
        [ExtendedPropertyAttribute()]        
        public object ExtendedPropertyPageCommitCellEdits
        {
            get { return this.extendedPropertyPageCommitCellEdits; }
            set { this.extendedPropertyPageCommitCellEdits = (EventHandler)value; }
        }

        private object extendedPropertyPageDataContainer;

        ///<summary>
        /// ExtendedPropertyPageDataContainer
        ///</summary>
        [ExtendedPropertyAttribute()]
        public object ExtendedPropertyPageDataContainer
        {
            get { return this.extendedPropertyPageDataContainer; }
            set { this.extendedPropertyPageDataContainer = value; }
        }


        private bool extendedPropertyPageIsDirty = false;

        ///<summary>
        /// ExtendedPropertyPageIsDirty
        ///</summary>
        [ExtendedPropertyAttribute()]
        public bool ExtendedPropertyPageIsDirty
        {
            get { return this.extendedPropertyPageIsDirty; }
            set { this.extendedPropertyPageIsDirty = value; }
        }

       

        #region ISfcValidate Members
       /// <summary>
       ///  Validate the values
       /// </summary>
       /// <param name="methodName"></param>
       /// <param name="arguments"></param>
       /// <returns></returns>

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            if (methodName.Equals("Create")
               && string.IsNullOrEmpty(this.Parent.Name))
            {
                return new ValidationState(ExceptionTemplates.EnterSequenceName, "Name");
            }
            
            if(this.HasMinimumValue && string.IsNullOrEmpty(this.Parent.MinValue.ToString()))
            {
                return new ValidationState(ExceptionTemplates.EnterMinValue, "MinValue");
            }
            
            if(this.HasMaximumValue && string.IsNullOrEmpty(this.Parent.MaxValue.ToString()))
            {
                return new ValidationState(ExceptionTemplates.EnterMaxValue, "MaxValue");
            }

            if (methodName.Equals("Alter") && string.IsNullOrEmpty(this.Parent.IncrementValue.ToString()))
            {
                return new ValidationState(ExceptionTemplates.EnterIncrementValue, "IncrementValue");
            }           
         
            return this.Parent.Validate(methodName, arguments);           
        }

        #endregion
    }
}
