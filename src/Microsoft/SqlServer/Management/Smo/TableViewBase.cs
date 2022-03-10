// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public class TableViewBase : TableViewTableTypeBase, IExtendedProperties, IScriptable
    {
        internal TableViewBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            m_Triggers = null;
            m_Statistics = null;
        }

        protected internal TableViewBase() : base()
        {}
        
        private TriggerCollection m_Triggers;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Trigger), SfcObjectFlags.Design)]
        public TriggerCollection Triggers
        {
            get 
            {
                if(this is View )
                {
                    ThrowIfBelowVersion80();
                }
                CheckObjectState();

                if( null == m_Triggers )
                {
                    m_Triggers = new TriggerCollection(this);
                }
                return m_Triggers;
            }
        }

        private StatisticCollection m_Statistics;
        [SfcObject( SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Statistic))]
        public StatisticCollection Statistics
        {
            get 
            {
                if(this is View )
                {
                    ThrowIfBelowVersion80();
                }
                CheckObjectState();
                
                if( null == m_Statistics )
                {
                    m_Statistics = new StatisticCollection(this);
                }
                return m_Statistics;
            }
        }
        
        /// <summary>
        /// forces data distribution statistics update for a referenced index or all 
        /// indexes defined on a SQL Server table.
        /// </summary>
        public void UpdateStatistics()
        {
            CheckObjectState(true);
            UpdateStatistics( StatisticsTarget.All, StatisticsScanType.Default, 0, true );
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index or all 
        /// indexes defined on a SQL Server table.
        /// </summary>
        public  void UpdateStatistics(StatisticsTarget affectType,StatisticsScanType scanType)
        {
            CheckObjectState(true);
            UpdateStatistics( affectType, scanType, 0, true );
        }
        
        /// <summary>
        /// forces data distribution statistics update for a referenced index or all 
        /// indexes defined on a SQL Server table.
        /// </summary>
        public  void UpdateStatistics(StatisticsTarget affectType,StatisticsScanType scanType,
            int sampleValue)
        {
            CheckObjectState(true);
            UpdateStatistics( affectType, scanType, sampleValue, true );
        }
        
        /// <summary>
        /// forces data distribution statistics update for a referenced index or all 
        /// indexes defined on a SQL Server table.
        /// </summary>
        public  void UpdateStatistics(StatisticsTarget affectType,StatisticsScanType scanType,
            int sampleValue, bool recompute )
        {
            CheckObjectState(true);
            this.ExecutionManager.ExecuteNonQuery(Statistic.UpdateStatistics(GetDatabaseName(), FormatFullNameForScripting(new ScriptingPreferences()), "", 
                scanType, affectType, !recompute, sampleValue));
        }

        // Index fragmentation support
        public DataTable EnumFragmentation()
        {
            return EnumFragmentation(FragmentationOption.Fast);
        }

        // Index fragmentation support
        public DataTable EnumFragmentation(FragmentationOption fragmentationOption)
        {
            try
            {
                if(this is View && ServerVersion.Major < 8)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.ViewFragInfoNotInV7).SetHelpContext("ViewFragInfoNotInV7");
                }

                CheckObjectState();

                string urn = string.Format(SmoApplication.DefaultCulture, "{0}/Index/{1}", this.Urn.Value, GetFragOptionString(fragmentationOption));
                Request req = new Request(urn);
                
                req.ParentPropertiesRequests = new PropertiesRequest[1];
                PropertiesRequest parentProps = new PropertiesRequest();
                parentProps.Fields = new String[] {"Name", "ID"};
                parentProps.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };
                req.ParentPropertiesRequests[0] = parentProps;

                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumFragmentation, this, e);
            }
        }

        // Index fragmentation support
        public DataTable EnumFragmentation(FragmentationOption fragmentationOption, int partitionNumber)
        {
            try
            {
                if(this is View && ServerVersion.Major < 8)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.ViewFragInfoNotInV7).SetHelpContext("ViewFragInfoNotInV7");
                }

                CheckObjectState();
                //Yukon only
                if(ServerVersion.Major < 9)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.InvalidParamForVersion("EnumFragmentation", "partitionNumber", GetSqlServerVersionName())).SetHelpContext("InvalidParamForVersion");
                }

                string urn = string.Format(SmoApplication.DefaultCulture, 
                    "{0}/Index/{1}[@PartitionNumber={2}]", this.Urn.Value, GetFragOptionString(fragmentationOption), partitionNumber);
                Request req = new Request(urn);

                req.ParentPropertiesRequests = new PropertiesRequest[1];
                PropertiesRequest parentProps = new PropertiesRequest();
                parentProps.Fields = new String[] {"Name", "ID"};
                parentProps.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };
                req.ParentPropertiesRequests[0] = parentProps;

                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumFragmentation, this, e);
            }
        }
        public override void Refresh()
        {
            base.Refresh();
            m_bFullTextIndexInitialized = false;
            this.keysForPermissionWithGrantOption = null;
        }

        public void ReCompileReferences()
        {
            ReCompile(this.Name, this.Schema);
        } 


        private String GetDatabaseName()
        {
            return MakeSqlBraket(ParentColl.ParentInstance.InternalName);
        }

        private List<string> keysForPermissionWithGrantOption;

        /// <summary>
        /// Returns a collection of unique keys for permissions which are assigned on this table or view 
        /// with grant option. This collection of keys is cached and only refreshed 
        /// when this.Refresh() is being called.
        /// </summary>
        /// <returns></returns>
        internal List<string> GetKeysForPermissionWithGrantOptionFromCache()
        {
            if (this.keysForPermissionWithGrantOption == null)
            {
                this.keysForPermissionWithGrantOption = new List<string>();

                foreach(UserPermission permission in this.Permissions)
                {
                    if (permission.PermissionState == PermissionState.GrantWithGrant)
                    {
                        keysForPermissionWithGrantOption.Add(
                            TableViewBase.GetKeyToMatchColumnPermissions(
                                permission.ObjectClass.ToString(),
                                permission.Grantee,
                                permission.GranteeType.ToString(),
                                permission.Grantor,
                                permission.GrantorType.ToString(),
                                permission.Code.ToString()));
                    }
                }
            }

            return this.keysForPermissionWithGrantOption;
        }

        /// <summary>
        /// Returns a key which is used to match table or view permissions 
        /// with the permission on their columns. 
        /// </summary>
        /// <param name="permissionClass"></param>
        /// <param name="grantee"></param>
        /// <param name="granteeType"></param>
        /// <param name="grantor"></param>
        /// <param name="grantorType"></param>
        /// <param name="permissionName"></param>
        /// <returns></returns>
        internal static string GetKeyToMatchColumnPermissions(
            string permissionClass,
            string grantee,
            string granteeType,
            string grantor,
            string grantorType,
            string permissionName)
        {
            StringBuilder key = new StringBuilder();
            key.Append(permissionClass);
            key.Append("_");
            key.Append(grantee);
            key.Append("_");
            key.Append(granteeType);
            key.Append("_");
            key.Append(grantor);
            key.Append("_");
            key.Append(grantorType);
            key.Append("_");
            key.Append(permissionName.ToUpperInvariant()); //Enumerator returns permission codes in small casing while ObjectPermissionSet returns in Capitals.

            return key.ToString();
        }

        /// <summary>
        /// Overrides the permission scripting.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // add the object-level permissions
            AddScriptPermissions(query, PermissionWorker.PermissionEnumKind.Object, sp);

            // iterate through all the columns and add the column-level permissions
            foreach (Column c in this.Columns)
            {
                c.AddScriptPermissions(query, PermissionWorker.PermissionEnumKind.Column, sp);
            }
        }


        #region FulltextIndex
        internal FullTextIndex m_FullTextIndex = null;
        internal bool m_bFullTextIndexInitialized = false;
        [SfcObject( SfcObjectRelationship.ChildObject, SfcObjectCardinality.ZeroToOne)]
        public FullTextIndex FullTextIndex
        {
            get
            {
                // FullTextIndexes are not supported on Views in 80
                // Duplicate code in FullTextIndexBase.ValidateParent
                // logic should be kept in sync
                if(this is View )
                {
                    ThrowIfBelowVersion90();
                }
                CheckObjectState();
                ThrowIfCloudAndVersionBelow12("FullTextIndex");
                if (this is View)
                {
                    this.ThrowIfNotSupported(typeof(View));
                }
                else
                {
                    this.ThrowIfNotSupported(typeof(Table));
                }
                // If we are in creating state the full text information does not exist on the server
                // since full text indexes on a table can be created only after the table has been created.
                // also see vsts#192973,86724 and 149814.
                if( !m_bFullTextIndexInitialized && this.State != SqlSmoState.Creating && !this.IsDesignMode)
                {
                    
                    m_FullTextIndex = InitializeFullTextIndex();
                    m_bFullTextIndexInitialized = true;
                }
                return m_FullTextIndex;
            }
        }

        //Unlike other collection objects, FullTextIndex is not geting removed itself from parent instance
        //after drop method. So, following method will remove it from parent instance. Drop method in FullTextIndex
        //will be the only caller for this method.
        internal void DropFullTextIndexRef()
        {
            m_FullTextIndex = null;
        }

        private FullTextIndex InitializeFullTextIndex()
        {
            //create request for full text index
            Request req = new Request(this.Urn + "/" + FullTextIndex.UrnSuffix);
            req.Fields = new String[] { "UniqueIndexName" };
            DataTable dt = this.ExecutionManager.GetEnumeratorData(req);

            //if it doesn't have full text index return 
            if( 1 != dt.Rows.Count )
            {
                return null;
            }
            string fullTextIndexName = dt.Rows[0][0] as string;
            if( null == fullTextIndexName )
            {
                return null;
            }
            return new FullTextIndex(this, new SimpleObjectKey(fullTextIndexName), SqlSmoState.Existing);
        }
        
        internal FullTextIndex InitializeFullTextIndexNoEnum()
        {
            if( null == m_FullTextIndex )
            {
                m_FullTextIndex = new FullTextIndex(this, new SimpleObjectKey(this.Name), SqlSmoState.Existing);
                m_bFullTextIndexInitialized = true;
            }

            return m_FullTextIndex;
        }
#endregion        
    }
}
