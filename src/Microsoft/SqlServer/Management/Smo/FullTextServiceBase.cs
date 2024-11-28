// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class FullTextService : ScriptNameObjectBase, Cmn.IAlterable, IScriptable
    {
        internal FullTextService(Server parentsrv, ObjectKeyBase key, SqlSmoState state) :
            base(key, state)
        {
            singletonParent = parentsrv as Server;
            SetServerObject( parentsrv.GetServerObject());
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as Server;
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "FullTextService";
            }
        }


        /// <summary>
        /// Name of FullTextService
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


        public void Cleanup()
        {
            try
            {
                if (ServerVersion.Major >= 7 && ServerVersion.Major <= 8)
                {
                    StringCollection statements = new StringCollection();
                    statements.Add("EXEC master.dbo.sp_fulltext_service @action=N'clean_up'");
                    this.ExecutionManager.ExecuteNonQuery(statements);
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Cleanup, this, e);
            }
        }

        public void UpdateLanguageResources()
        {
            try
            {
                if (ServerVersion.Major >= 9)
                {
                    StringCollection statements = new StringCollection();
                    statements.Add("EXEC master.dbo.sp_fulltext_service @action=N'update_languages'");
                    this.ExecutionManager.ExecuteNonQuery(statements);
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.UpdateLanguageResources, this, e);
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ScriptService(queries, sp);
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            if (IsObjectDirty())
            {
                ScriptService(queries, sp);
            }
        }

        private void ScriptService(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            Property property;

            // AllowUnsignedBinaries
            if (ServerVersion.Major >= 9)
            {
                if (null != (property = this.Properties.Get("AllowUnsignedBinaries")).Value)
                {
                    // Script Alter if dirty or Create if target version 9 or above
                    if ((sp.ScriptForAlter && property.Dirty) ||
                        (!sp.ScriptForAlter && sp.TargetServerVersion >= SqlServerVersion.Version90))
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC master.dbo.sp_fulltext_service @action=N'verify_signature', @value={0}", (bool)property.Value ? 0 : 1);
                        statement.Append(sp.NewLine);

                        queries.Add(statement.ToString());
                        statement.Length = 0;
                    }
                }

                // LoadOSResourcesEnabled
                if (null != (property = this.Properties.Get("LoadOSResourcesEnabled")).Value)
                {
                    // Script Alter if dirty or Create if target version 9 or above
                    if ((sp.ScriptForAlter && property.Dirty) ||
                        (!sp.ScriptForAlter && sp.TargetServerVersion >= SqlServerVersion.Version90))
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC master.dbo.sp_fulltext_service @action=N'load_os_resources', @value={0}", (bool)property.Value ? 1 : 0);
                        statement.Append(sp.NewLine);

                        queries.Add(statement.ToString());
                        statement.Length = 0;
                    }
                }
            }

            // ConnectTimeout
            if (ServerVersion.Major >= 7 && ServerVersion.Major <= 8)
            {
                if (null != (property = this.Properties.Get("ConnectTimeout")).Value)
                {
                    // Script Alter if dirty or Create if target version 8
                    if ((sp.ScriptForAlter && property.Dirty) ||
                        (!sp.ScriptForAlter &&
                            (sp.TargetServerVersion <= SqlServerVersion.Version80)))
                    {
                        TimeSpan ts = (TimeSpan)property.Value;


                        // if we're getting a zero timespan in scripting mode, this means that
                        // we could not retrieve the value, and we should not script anything
                        if (ts.TotalSeconds != 0 || sp.ScriptForAlter)
                        {
                            int minSeconds = 1, maxSeconds = 32767;
                            if (ts.TotalSeconds < minSeconds || ts.TotalSeconds > maxSeconds)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.InvalidPropertyNumberRange("ConnectTimeout", minSeconds.ToString(SmoApplication.DefaultCulture), maxSeconds.ToString(SmoApplication.DefaultCulture)));
                            }

                            statement.AppendFormat(SmoApplication.DefaultCulture,
                                    "EXEC master.dbo.sp_fulltext_service @action=N'connect_timeout', @value={0}",
                                    Convert.ToInt32(ts.TotalSeconds));
                            statement.Append(sp.NewLine);

                            queries.Add(statement.ToString());
                            statement.Length = 0;
                        }
                    }
                }
            }

            // DataTimeout
            if (ServerVersion.Major == 8)
            {
                if (null != (property = this.Properties.Get("DataTimeout")).Value)
                {
                    // Script for Alter if dirty or Create if target version is 8
                    if ((sp.ScriptForAlter && property.Dirty) ||
                        (!sp.ScriptForAlter && sp.TargetServerVersion == SqlServerVersion.Version80))
                    {
                        TimeSpan ts = (TimeSpan)property.Value;

                        // if we're getting a zero timespan in scripting mode, this means that
                        // we could not retrieve the value, and we should not script anything
                        if (ts.TotalSeconds != 0 || sp.ScriptForAlter)
                        {
                            int minSeconds = 1, maxSeconds = 32767;
                            if (ts.TotalSeconds < minSeconds || ts.TotalSeconds > maxSeconds)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.InvalidPropertyNumberRange("DataTimeout",
                                                                    minSeconds.ToString(SmoApplication.DefaultCulture), maxSeconds.ToString(SmoApplication.DefaultCulture)));
                            }

                            statement.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC master.dbo.sp_fulltext_service @action=N'data_timeout', @value={0}", ts.TotalSeconds);
                            statement.Append(sp.NewLine);

                            queries.Add(statement.ToString());
                            statement.Length = 0;
                        }
                    }
                }
            }

            // ResourceUsage
            if (null != (property = this.Properties.Get("ResourceUsage")).Value)
            {
                // Script for Alter if dirty or Create any target version
                if (((sp.ScriptForAlter && property.Dirty) || (!sp.ScriptForAlter)) && (int)property.Value > 0)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                        "EXEC master.dbo.sp_fulltext_service @action=N'resource_usage', @value={0}", (int)property.Value);
                    statement.Append(sp.NewLine);

                    queries.Add(statement.ToString());
                    statement.Length = 0;
                }
            }

            //Catalog Upgrade Option
            if (ServerVersion.Major >= 10 && sp.TargetServerVersion >= SqlServerVersion.Version100)
            {
                property = this.Properties.Get("CatalogUpgradeOption");

                if (null != property.Value)
                {
                    int upgradeOption = (int)property.Value;
                    if (!Enum.IsDefined(typeof(FullTextCatalogUpgradeOption), upgradeOption))
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("CatalogUpgradeOption"));
                    }

                    // Script for Alter if dirty or Create if target version is greater than 10
                    if ((!sp.ScriptForAlter) || property.Dirty)
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC master.dbo.sp_fulltext_service @action=N'upgrade_option', @value={0}", upgradeOption);
                        statement.Append(sp.NewLine);

                        queries.Add(statement.ToString());
                        statement.Length = 0;
                    }
                }
            }
        }

        public DataTable EnumLanguages()
        {
            try
            {
                ThrowIfBelowVersion90();
                Request req = new Request(this.Urn + "/Language");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumLanguages, this, e);
            }
        }

        //  -----------------------------------------------------
        //  Semantic Platform Language Model support
        public DataTable EnumSemanticLanguages()
        {
            try
            {
                ThrowIfBelowVersion110();
                Request req = new Request(this.Urn + "/SemanticLanguage");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumSemanticLanguages, this, e);
            }
        }   //--------------------------------------------------

    }

}



