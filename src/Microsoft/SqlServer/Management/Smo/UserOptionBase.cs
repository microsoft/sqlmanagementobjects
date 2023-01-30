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
    [SfcElementType("UserOption")]
    public partial class UserOptions : SqlSmoObject, Cmn.IAlterable, IScriptable
    {
        internal UserOptions() : base() { }

        internal UserOptions(Server parentsrv, ObjectKeyBase key, SqlSmoState state) :
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
            internal set
            {
                singletonParent = value;
                SetServerObject(((Server)singletonParent).GetServerObject());
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
                return "UserOption";
            }
        }

        public void Alter()
        {
            overrideValueChecking = false;
            base.AlterImpl();
        }

        bool overrideValueChecking = false;
        public void Alter(bool overrideValueChecking)
        {
            this.overrideValueChecking = overrideValueChecking;
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            ScriptProperties(query, sp);
        }

        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            ScriptProperties(query, sp);
        }

        private void ScriptProperties(StringCollection query, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            // UserOption is a server level object and is supported on Azure servers with version
            // >= 12, however, currently no T-SQL manipulation(i.e sp_configure for On-premise server)
            // is provided on Azure-side. So we need to disable scripting capability for this case.
            if (sp.TargetDatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                throw new UnsupportedEngineTypeException(ExceptionTemplates.ScriptingNotSupportedOnCloud(typeof(UserOptions).Name)).
                    SetHelpContext("ScriptingNotSupportedOnCloud");
            }

            Initialize(true);

            StringBuilder sbStatement = new StringBuilder();

            InitializeKeepDirtyValues();

            object[][] SRV_OPTIONS = new object[][] {
                                                        new object [] {"DisableDefaultConstraintCheck", 1},
                                                        new object [] {"ImplicitTransactions", 2},
                                                        new object [] {"CursorCloseOnCommit", 4},
                                                        new object [] {"AnsiWarnings", 8},
                                                        new object [] {"AnsiPadding", 16},
                                                        new object [] {"AnsiNulls", 32},
                                                        new object [] {"AbortOnArithmeticErrors", 64},
                                                        new object [] {"IgnoreArithmeticErrors", 128},
                                                        new object [] {"QuotedIdentifier", 256},
                                                        new object [] {"NoCount", 512},
                                                        new object [] {"AnsiNullDefaultOn", 1024},
                                                        new object [] {"AnsiNullDefaultOff", 2048},
                                                        new object [] {"ConcatenateNullYieldsNull", 4096},
                                                        new object [] {"NumericRoundAbort", 8192},
                                                        new object [] {"AbortTransactionOnError", 16384},
                                                        new object [] {"", 0}};

            bool anyDirty = false;
            int setOptions = 0;
            for (int i = 0; ((String)SRV_OPTIONS[i][0]).Length > 0; i++)
            {
                Property p = Properties.Get((String)SRV_OPTIONS[i][0]);
                // at this point we should have a value for the property,
                // either from the user instantiating it or from us having called
                // InitializeKeepDirtyValues
                anyDirty |= p.Dirty;
                setOptions += ((bool)p.Value) ? (int)SRV_OPTIONS[i][1] : 0;
            }

            String strOverrideValueChecking = string.Empty;
            if (overrideValueChecking)
            {
                strOverrideValueChecking = " WITH OVERRIDE";
            }
            if (anyDirty || !sp.ScriptForAlter)
            {
                if (this.ServerVersion.Major > 8)
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture, Scripts.SRV_SET_OPTIONS90, setOptions) + strOverrideValueChecking);
                }
                else
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture, Scripts.SRV_SET_OPTIONS80, setOptions) + strOverrideValueChecking);
                }
            }
        }

        internal protected override string GetDBName()
        {
            return string.Empty; //signal that it is database agnostic
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

        internal static string[] GetScriptFields(Type parentType,
                                   Microsoft.SqlServer.Management.Common.ServerVersion version,
                                   Cmn.DatabaseEngineType databaseEngineType,
                                   Cmn.DatabaseEngineEdition databaseEngineEdition,
                                   bool defaultTextMode)
        {
            string[] fields =
            {
                "AnsiNulls",
            };

            List<string> list = GetSupportedScriptFields(typeof(UserOptions.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}





