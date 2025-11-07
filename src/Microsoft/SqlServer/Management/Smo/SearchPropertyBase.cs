// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// SMO class for Search Properties in a Search Property List
    /// 
    /// </summary>
    public partial class SearchProperty : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, IScriptable
    {
        internal SearchProperty(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {

        }


        public SearchProperty(SearchPropertyList parent, string name, string propertySetGuid, int intID, string description)
            : base()
        {
            this.Name = name;
            this.Parent = parent;
            this.PropertySetGuid = new System.Guid(propertySetGuid);
            this.IntID = intID;
            this.Description = description;
        }


        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "SearchProperty";
            }
        }

        /// <summary>
        /// Name of SearchProperty
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// Scripts object with default scripting options
        /// </summary>
        public StringCollection Script()
        {
            return base.ScriptImpl();
        }

        /// <summary>
        /// Scripts object with specific scripting options
        /// </summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return base.ScriptImpl(scriptingOptions);
        }


        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion110(sp.TargetServerVersion);
            // SearchProperty objects can be enumerated on Azure DB but cannot be created
            ThrowIfCloud(sp.TargetDatabaseEngineType);
            var sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, this.Name, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SEARCH_PROPERTY, "NOT", this.Parent.ID, SqlString(this.Name));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            // ALTER PROPERTY LIST list_name
            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER {0} {1}", SearchPropertyListConstants.SearchPropertyList, MakeSqlBraket(this.Parent.Name));
            sb.Append(sp.NewLine);


            // ADD 'property_name' WITH (
            sb.AppendFormat(SmoApplication.DefaultCulture, "ADD {0}{1}WITH (", MakeSqlString(this.Name), sp.NewLine);

            // PROPERTY_SET_GUID = 'property_set_guid'
            sb.AppendFormat(SmoApplication.DefaultCulture, "PROPERTY_SET_GUID = {0}", MakeSqlString(((System.Guid)this.GetPropValue("PropertySetGuid")).ToString()));

            // , PROPERTY_INT_ID = property_int_id
            Object intID = this.GetPropValue("IntID");

            if (intID != null)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, ", PROPERTY_INT_ID = {0} ", intID);
            }

            //, PROPERTY_DESCRIPTION = 'property_description'
            Object descrition = this.GetPropValueOptional("Description");

            if (descrition != null)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, ", PROPERTY_DESCRIPTION = {0} ", MakeSqlString(descrition as string));
            }

            sb.Append(");");
            sb.Append(sp.NewLine);


            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());
        }


        public void Drop()
        {
            base.DropImpl();
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion110(sp.TargetServerVersion);

            // DROP N'property_name'
            dropQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER {0} {1} DROP {2};", SearchPropertyListConstants.SearchPropertyList, MakeSqlBraket(this.Parent.Name), MakeSqlString(this.Name)));
        }

        internal static string[] GetScriptFields(Type parentType,
           Cmn.ServerVersion version,
           Cmn.DatabaseEngineType databaseEngineType,
           Cmn.DatabaseEngineEdition databaseEngineEdition,
           bool defaultTextMode)
        {
            return new string[] {"IntID","Description","PropertySetGuid"};
        }
    }

}

