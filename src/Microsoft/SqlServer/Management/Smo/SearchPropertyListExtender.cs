// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class which provides extended properties for a SearchPropertyList object
    /// </summary>
    [CLSCompliant(false)]
    public class SearchPropertyListExtender : SmoObjectExtender<SearchPropertyList>, ISfcValidate
    {
        private StringCollection databaseNamesToSelect = null;
        private StringCollection propertyListNamesToSelect = null;
        private String selectedDatabaseName = String.Empty;
        private String selectedPropertyListName = String.Empty;
        private DataTable searchProperties = null;
        private ValidationState gridValidationState = null;
        private SearchPropertyListValidator searchPropertyListValidator = null;

        public SearchPropertyListExtender() : base() { Initialize(); }

        public SearchPropertyListExtender(SearchPropertyList SearchPropertyList)
            : base(SearchPropertyList)
        {
            Initialize();
        }

        private void Initialize()
        {
            this.searchPropertyListValidator = new SearchPropertyListValidator(this);
            this.searchPropertyListValidator.Initialize(this.Parent.SearchProperties);
            this.GridValidationState = new ValidationState();
            this.SelectedDatabaseName = this.Parent.Parent.Name;
        }


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

        [ExtendedPropertyAttribute()]
        public string SelectedDatabaseName
        {
            get
            {
                return this.selectedDatabaseName;
            }
            set
            {
                if (this.selectedDatabaseName != value)
                {
                    this.selectedDatabaseName = value;
                    InitPropertyListNamesToSelect();
                }
            }
        }

        [ExtendedPropertyAttribute()]
        public StringCollection DatabaseNamesToSelect
        {
            get
            {
                if (this.databaseNamesToSelect == null)
                {
                    this.databaseNamesToSelect = new StringCollection();
                    Database database = this.Parent.Parent;


                    foreach (Database db in database.Parent.Databases)
                    {
                        this.databaseNamesToSelect.Add(db.Name);
                    }
                }
                return this.databaseNamesToSelect;
            }
        }

        [ExtendedPropertyAttribute()]
        public string SelectedPropertyListName
        {
            get
            {
                return this.selectedPropertyListName;
            }
            set
            {
                this.selectedPropertyListName = value;
            }
        }

        [ExtendedPropertyAttribute()]
        public StringCollection PropertyListNamesToSelect
        {
            get
            {
                return this.propertyListNamesToSelect;
            }
        }


        [ExtendedPropertyAttribute()]
        public DataTable SearchProperties
        {
            get
            {
                if (this.searchProperties == null)
                {
                    this.searchProperties = new DataTable();
                    this.searchProperties.Locale = SmoApplication.DefaultCulture;
                    this.searchProperties.Columns.Add("RowId", typeof(int)).AutoIncrement = true;
                    this.searchProperties.Columns.Add("Name");
                    this.searchProperties.Columns.Add("PropertySetGuid");
                    this.searchProperties.Columns.Add("IntID", typeof(SearchPropertyIntIDType));                    
                    this.searchProperties.Columns.Add("Description");
                    this.searchProperties.PrimaryKey = new DataColumn[] { this.searchProperties.Columns[0] };

                    foreach (SearchProperty searchProperty in this.Parent.SearchProperties)
                    {
                        DataRow row = searchProperties.NewRow();
                        row["Name"] = searchProperty.Name;
                        row["PropertySetGuid"] = searchProperty.PropertySetGuid;
                        row["IntID"] = new SearchPropertyIntIDType(searchProperty.IntID.ToString(SmoApplication.DefaultCulture));
                        row["Description"] = searchProperty.Description;
                        this.searchProperties.Rows.Add(row);
                    }

                    this.searchProperties.ColumnChanging += this.searchPropertyListValidator.SearchPropertiesColumnChangingValidationHandler;
                    this.searchProperties.ColumnChanged += this.searchPropertyListValidator.SearchPropertiesColumnChangedValidationHandler;

                    this.searchProperties.RowDeleting += this.searchPropertyListValidator.SearchPropertiesRowDeleteValidationHandler;

                }
                return this.searchProperties;
            }
            set
            {
                this.searchProperties = value;
            }
        }

        [ExtendedPropertyAttribute()]
        public DataTable SortedSearchProperties
        {
            get
            {            
                this.SearchProperties.DefaultView.Sort = this.SortingExpression;
                DataTable sortedTable = this.SearchProperties.DefaultView.ToTable();
                sortedTable.PrimaryKey = new DataColumn[] { sortedTable.Columns[0] };
                sortedTable.ColumnChanging += this.searchPropertyListValidator.SearchPropertiesColumnChangingValidationHandler;
                sortedTable.ColumnChanged += this.searchPropertyListValidator.SearchPropertiesColumnChangedValidationHandler;

                sortedTable.RowDeleting += this.searchPropertyListValidator.SearchPropertiesRowDeleteValidationHandler;

                foreach (DataRow row in sortedTable.Rows)
                {
                    int rowId = Convert.ToInt32(row["RowId"], SmoApplication.DefaultCulture);
                    row.RowError = this.searchPropertyListValidator.GetValidationErrorsMessage(rowId);
                }


                this.SearchProperties.DefaultView.Sort = "";
                return sortedTable;
            }
        }

        private string sortingExpression = "Name ASC";
        [ExtendedPropertyAttribute()]
        public string SortingExpression
        {
            internal get
            {
                return this.sortingExpression;
            }
            set
            {
                this.sortingExpression = value;
            }
        }


        private bool isEmptyList = true;
        [ExtendedPropertyAttribute()]
        public bool IsEmptyList
        {
            get
            {
                return this.isEmptyList;
            }
            set
            {
                this.isEmptyList = value;
            }
        }

        [ExtendedPropertyAttribute()]
        public string DatabaseName
        {
            get
            {
                return this.Parent.Parent.Name;
            }
        }

        [ExtendedPropertyAttribute()]
        public ServerConnection ConnectionContext
        {
            get
            {
                return this.Parent.Parent.GetServerObject().ConnectionContext;
            }
        }

        private void InitPropertyListNamesToSelect()
        {
            this.propertyListNamesToSelect = new StringCollection();
            Database database = this.Parent.Parent.Parent.Databases[this.SelectedDatabaseName];
            SearchPropertyListCollection propertyLists = database.SearchPropertyLists;
            foreach (SearchPropertyList propertyList in propertyLists)
            {
                this.propertyListNamesToSelect.Add(propertyList.Name);
            }

            this.SelectedPropertyListName = string.Empty;

        }


        [ExtendedPropertyAttribute()]
        public ValidationState GridValidationState
        {
            get
            {
                if (gridValidationState == null)
                {
                    gridValidationState = new ValidationState();
                }
                return gridValidationState;
            }
            set
            {
                gridValidationState = value;
            }
        }

        internal DataRow GetRow(int rowId)
        {
            return this.SearchProperties.Select("RowId = " + rowId)[0];
        }

        public void ApplyChanges()
        {
            this.Parent.Alter();
            foreach (string name in this.searchPropertyListValidator.DeletedSearchPropertyNames)
            {
                this.Parent.SearchProperties[name].Drop();
            }

            foreach (int rowId in this.searchPropertyListValidator.UpdatedRows)
            {
                DataRow row = this.GetRow(rowId);
                string name = row["Name"] as string;
                string guid = row["PropertySetGuid"] as string;
                int intId = Convert.ToInt32(row["IntID"].ToString(), SmoApplication.DefaultCulture);
                string description = row["Description"] as string;
                this.Parent.SearchProperties[name].Drop();
                new SearchProperty(this.Parent, name, guid, intId, description).Create();
            }

            foreach (int rowId in this.searchPropertyListValidator.NewRows)
            {
                DataRow row = this.GetRow(rowId);
                string name = row["Name"] as string;
                string guid = row["PropertySetGuid"] as string;
                int intId = Convert.ToInt32(row["IntID"].ToString(), SmoApplication.DefaultCulture);
                string description = row["Description"] as string;
                if (description == null)
                {
                    description = string.Empty;
                }
                new SearchProperty(this.Parent, name, guid, intId, description).Create();
            }
            this.Parent.SearchProperties.Refresh();
        }

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            if (string.IsNullOrEmpty(this.Parent.Name) || this.Parent.Name.Length > SearchPropertyListConstants.MaxSearchPropertyListNameLength)
            {
                return new ValidationState(ExceptionTemplates.SearchPropertyListNameNotValid(SearchPropertyListConstants.MaxSearchPropertyListNameLength), "Name");
            }

            if (string.IsNullOrEmpty(this.Parent.Name.Trim()))
            {
                return new ValidationState(ExceptionTemplates.SearchPropertyListNameAllWhiteSpaces, "Name");
            }

            if (!this.IsEmptyList && string.IsNullOrEmpty(this.SelectedPropertyListName))
            {
                return new ValidationState(ExceptionTemplates.EmptySourceSearchPropertyListName, "SelectedPropertyListName");
            }

            return GridValidationState;
        }

        #endregion


    }


    [TypeConverter(typeof(SearchPropertyIntIDTypeConverter))]
    internal class SearchPropertyIntIDType : IComparable
    {
        private string value;

        public SearchPropertyIntIDType(string value)
        {
            this.value = value;
        }

        int IComparable.CompareTo(object obj)
        {
            return this.ParseInt(this.value) - this.ParseInt((obj as SearchPropertyIntIDType).value);
        }

        public override string ToString()
        {
            return value;
        }

        // used by the IComparable.CompareTO()
        private int ParseInt(string proposedValue)
        {
            int intId = int.MinValue;
            if (!string.IsNullOrEmpty(proposedValue))
            {
                //Validate Format
                try
                {
                    intId = Convert.ToInt32(proposedValue, SmoApplication.DefaultCulture);
                }
                catch (FormatException)
                {
                    //Considering non valid integers as the least possible integers
                    //for the purpose of comparison
                    intId = int.MinValue;
                }
            }
            return intId;
        }
    }

    internal class SearchPropertyIntIDTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                return new SearchPropertyIntIDType((string)value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return value.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
