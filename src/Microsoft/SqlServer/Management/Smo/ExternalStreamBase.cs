// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    [SfcIgnore]
    public partial class ExternalStream : NamedSmoObject, ICreatable, IDroppable, IScriptable
    {
        // This is need because External Stream is a collection of Database Object.
        //
        internal ExternalStream(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        private bool HasLocationOption()
        {
            return !String.IsNullOrEmpty(GetPropertyOptional(nameof(Location)).Value as string);
        }

        private bool HasFileFormatOption()
        {
            return !String.IsNullOrEmpty(GetPropertyOptional(nameof(FileFormatName)).Value as string);
        }

        private bool HasDataSourceOption()
        {
            return !String.IsNullOrEmpty(GetPropertyOptional(nameof(DataSourceName)).Value as string);
        }

        private bool HasInputOptionsOption()
        {
            return !String.IsNullOrEmpty(GetPropertyOptional(nameof(InputOptions)).Value as string);
        }

        private bool HasOutputOptionsOption()
        {
            return !String.IsNullOrEmpty(GetPropertyOptional(nameof(OutputOptions)).Value as string);
        }

        // If the object is scriptable on its own, not directly included as part of the script for its parent,
        // its UrnSuffix is referenced in the scriptableTypes HashSet in ScriptMaker.cs
        //
        public static string UrnSuffix
        {
            get
            {
                return nameof(ExternalStream);
            }
        }

        //  Since the object is a collection and has a custom
        //  set of necessary fields for population
        //  It is referenced in the GetFieldNames in SmoCollectionBase.cs
        //
        public static StringCollection RequiredFields
        {
            get 
            {
                StringCollection col = new StringCollection();
                col.Add(nameof(DataSourceName));
                return col;
            }
        }

        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            if (sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                string dataSourceDef = this.HasDataSourceOption() ? $"{Globals.newline}DATA_SOURCE = { MakeSqlBraket(GetPropertyOptional(nameof(DataSourceName)).Value as string) }" : string.Empty;
                string locationDef = this.HasLocationOption() ? $"{Globals.newline}, LOCATION = { QuoteString(GetPropertyOptional(nameof(Location)).Value as string) }" : string.Empty;
                string fileFormatDef = this.HasFileFormatOption() ? $"{Globals.newline}, FILE_FORMAT = { MakeSqlBraket(GetPropertyOptional(nameof(FileFormatName)).Value as string) }" : string.Empty;
                string inputOptionsDef = this.HasInputOptionsOption() ? $"{Globals.newline}, INPUT_OPTIONS = { QuoteString(GetPropertyOptional(nameof(InputOptions)).Value as string) }" : string.Empty;
                string outputOptionsDef = this.HasOutputOptionsOption() ? $"{Globals.newline}, OUTPUT_OPTIONS = { QuoteString(GetPropertyOptional(nameof(OutputOptions)).Value as string) }" : string.Empty;

                if (this.HasDataSourceOption())
                {

                    sb.Append(
                    $"CREATE EXTERNAL STREAM {this.FormatFullNameForScripting(sp)} " +
                    $"{Globals.newline} { Globals.With }" +
                    $"{Globals.newline} { Globals.LParen }" +
                    $"{dataSourceDef}" +
                    $"{locationDef} " +
                    $"{fileFormatDef}" +
                    $"{inputOptionsDef} " +
                    $"{outputOptionsDef} " +
                    $"{Globals.RParen}"
                    );
                    query.Add(sb.ToString());
                }
                else
                {
                    throw new PropertyNotSetException(nameof(DataSourceName));
                }
            }
            else
            {
                throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
            }
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            if (sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDatabaseEdge)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.Append($"DROP EXTERNAL STREAM {this.FormatFullNameForScripting(sp)}");
                dropQuery.Add(sb.ToString());
            }
            else 
            {
                throw new UnsupportedEngineEditionException(ExceptionTemplates.UnsupportedEngineEditionException);
            }
        }

        public void Create()
        {
           this.CreateImpl();
        }

        public void Drop()
        {
            this.DropImpl();
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }
    }
}
