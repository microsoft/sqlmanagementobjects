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
    /// SMO class for Search Property List
    /// 
    /// </summary>

    [Facets.StateChangeEvent("CREATE_SEARCH_PROPERTY_LIST", "SEARCHPROPERTYLIST", "SEARCH PROPERTY LIST")]
    [Facets.StateChangeEvent("ALTER_SEARCH_PROPERTY_LIST", "SEARCHPROPERTYLIST", "SEARCH PROPERTY LIST")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "SEARCHPROPERTYLIST", "SEARCH PROPERTY LIST")] // For Owner    
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)] // support enforce mode after VSTS: 289570 is unblocked
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class SearchPropertyList : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IAlterable, IScriptable
    {
        //name of the existing source search propety list
        internal string sourceSearchPropertyListName = String.Empty;

        //name of the existing database of which the source stoplist is a part of
        internal string sourceDatabaseName = String.Empty;


        internal SearchPropertyList(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
            m_SearchProperties = null;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "SearchPropertyList";
            }
        }

        /// <summary>
        /// Name of SearchPropertyList
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

        //SearchProperties collection
        SearchPropertyCollection m_SearchProperties;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(SearchProperty))]
        public SearchPropertyCollection SearchProperties
        {
            get
            {
                CheckObjectState();
                if (m_SearchProperties == null)
                {
                    m_SearchProperties = new SearchPropertyCollection(this);
                }
                return m_SearchProperties;
            }
        }


        /// <summary>
        /// Creates a new property list object without any source stoplist
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        /// <summary>
        /// Creates a new property list from an existing source property list in the current database
        /// </summary>
        public void Create(string sourceSearchPropertyListName)
        {
            if (sourceSearchPropertyListName == null || sourceSearchPropertyListName.Length == 0)
            {
                throw new FailedOperationException(ExceptionTemplates.NullOrEmptyParameterSourceSearchPropertyListName);
            }
            this.sourceSearchPropertyListName = sourceSearchPropertyListName;
            try
            {
                this.Create();
            }
            finally
            {
                this.sourceSearchPropertyListName = null;
            }
        }

        /// <summary>
        /// Creates a new property list from an existing source property list in the different database
        /// </summary>
        public void Create(string sourceDatabaseName, string sourceSearchPropertyListName)
        {
            if (sourceDatabaseName == null || sourceDatabaseName.Length == 0)
            {
                throw new FailedOperationException(ExceptionTemplates.NullOrEmptyParameterSourceDatabaseName);
            }
            this.sourceDatabaseName = sourceDatabaseName;
            try
            {
                this.Create(sourceSearchPropertyListName);
            }
            finally
            {
                this.sourceDatabaseName = null;
            }
        }


        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion110(sp.TargetServerVersion);

            var sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, this.Name, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SEARCH_PROPERTY_LIST, "NOT", SqlString(this.Name));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            // CREATE PROPERTY LIST <list_name>
            sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE {0} {1} ", SearchPropertyListConstants.SearchPropertyList, MakeSqlBraket(this.Name));

            //[ FROM { [ database_name. ] source_list_name }]
            if (this.sourceSearchPropertyListName != null && this.sourceSearchPropertyListName != String.Empty)
            {
                sb.Append("FROM ");

                if (this.sourceDatabaseName != String.Empty)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0}.", MakeSqlBraket(this.sourceDatabaseName));
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0} ", MakeSqlBraket(this.sourceSearchPropertyListName));
            }

            // [AUTHORIZATION <owner_name>]
            if (sp.IncludeScripts.Owner)
            {
                Property property = this.Properties.Get("Owner");
                if ((null != property.Value) && (property.Value.ToString().Length > 0))
                {
                    sb.AppendFormat("AUTHORIZATION {0}", MakeSqlBraket(property.Value.ToString()));
                } 
            }

            sb.Append(";");
            sb.Append(sp.NewLine);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());

            // Azure DB SPLs don't support custom search properties
            if (sp.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                // This is required as ScriptImpl() method 
                // doesn't use GetPropagateInfo()              
                foreach (SearchProperty searchProperty in SearchProperties)
                {
                    searchProperty.ScriptCreateInternal(createQuery, sp);
                }
            }

        }

        /// <summary>
        /// Drops the stoplist
        /// </summary>
        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion110(sp.TargetServerVersion);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);


            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, this.Name, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SEARCH_PROPERTY_LIST, String.Empty, SqlString(this.Name));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            // DROP PROPERTY LIST list_name;
            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP {0} {1};", SearchPropertyListConstants.SearchPropertyList, MakeSqlBraket(this.Name));
            sb.Append(sp.NewLine);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            dropQuery.Add(sb.ToString());
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

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion110(sp.TargetServerVersion);

            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptChangeOwner(alterQuery, sp);
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            // Excluding Create action as ScriptImpl() method 
            // doesn't use GetPropagateInfo() 
            // And, ScriptCreate handles Propagation
            var bWithScript = action != PropagateAction.Create;
            return new PropagateInfo[] { new PropagateInfo(SearchProperties, bWithScript) };
        }
    }
}
