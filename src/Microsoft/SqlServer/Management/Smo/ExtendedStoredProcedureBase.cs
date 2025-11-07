// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ExtendedStoredProcedure : ScriptSchemaObjectBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, IExtendedProperties
    {
        internal ExtendedStoredProcedure(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {
        }

        public void ChangeSchema(string newSchema)
        {
            CheckObjectState();
            ChangeSchema(newSchema, true);
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion90(); //added on yukon
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "ExtendedStoredProcedure";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            //XP (Extended Stored Procedure) scriting is not supported aganist Microsoft Azure SQL Database
            //Tagged with cloud in the xml, only to enable processing 
            // a UI Request to display system XPs 
            // should move this block to some common place if we see more such kind of exceptions -sivasat
            if (sp.TargetDatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                if (sp.ScriptForCreateDrop || !this.IgnoreForScripting)
                {
                    throw new UnsupportedEngineTypeException(ExceptionTemplates.UnsupportedEngineTypeException);
                }
                return;
            }

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // format full object name for scripting
            string sXStoredProcName = FormatFullNameForScripting(sp);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_IF_NOT_EXISTS_XPROCEDURE, "NOT", SqlString(sXStoredProcName));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_addextendedproc {0}, '{1}'", MakeSqlString(sXStoredProcName), (string)Properties["DllLocation"].Value);
            sb.Append(sp.NewLine);

            queries.Add(sb.ToString());

            if (sp.IncludeScripts.Owner)
            {
                ScriptOwner(queries, sp);
            }
        }

        public void Drop()
        {
            base.DropImpl();
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {

            //XP (Extended Stored Procedure) scriting is not supported aganist Microsoft Azure SQL Database
            //Tagged with cloud in the xml, only to enable processing 
            // a UI Request to display system XPs 
            // should move this block to some common place if we see more such kind of exceptions -sivasat
            if (sp.TargetDatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                if (sp.ScriptForCreateDrop || !this.IgnoreForScripting)
                {
                    throw new UnsupportedEngineTypeException(ExceptionTemplates.UnsupportedEngineTypeException);
                }
                return;
            }

            CheckObjectState();
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // format full object name for scripting
            string sXStoredProcName = FormatFullNameForScripting(sp);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_IF_NOT_EXISTS_XPROCEDURE, "", SqlString(sXStoredProcName));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_dropextendedproc {0}", MakeSqlString(sXStoredProcName));

            queries.Add(sb.ToString());
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { 
											new PropagateInfo(ServerVersion.Major < 9 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix )
											};
        }


        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (sp.IncludeScripts.Owner)
            {
                ScriptOwner(alterQuery, sp);
            }
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            string[] fields = {
                    "Owner",
                    "IsSchemaOwned"};
            List<string> list = GetSupportedScriptFields(typeof(ExtendedStoredProcedure.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}


