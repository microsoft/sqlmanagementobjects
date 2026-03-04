// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Method")]
    public partial class SoapPayloadMethod : SoapMethodObject, Cmn.ICreatable, Cmn.IDroppable, Cmn.IAlterable
    {
        public SoapPayloadMethod() : base() { }

        public SoapPayloadMethod(SoapPayload soapPayload, string name)
            : base()
        {
            ValidateName(name);
            this.key = new SoapMethodKey(name, string.Empty);
            this.Parent = soapPayload;
        }

        public SoapPayloadMethod(SoapPayload soapPayload, string name, string methodNamespace)
            : base()
        {
            ValidateName(name);
            this.key = new SoapMethodKey(name, methodNamespace);
            this.Parent = soapPayload;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="soapPayload"></param>
        /// <param name="name"></param>
        /// <param name="database"></param>
        /// <param name="schema"></param>
        /// <param name="sqlMethod"></param>
        public SoapPayloadMethod(SoapPayload soapPayload, string name, string database, string schema, string sqlMethod)
            : base()
        {
            ValidateName(name);
            this.key = new SoapMethodKey(name, string.Empty);
            this.Parent = soapPayload;
            this.SetSqlMethod(database, schema, sqlMethod);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="soapPayload"></param>
        /// <param name="name"></param>
        /// <param name="database"></param>
        /// <param name="schema"></param>
        /// <param name="sqlMethod"></param>
        /// <param name="methodNamespace"></param>
        public SoapPayloadMethod(SoapPayload soapPayload, string name, string database, string schema, string sqlMethod, string methodNamespace)
            : base()
        {
            ValidateName(name);
            this.key = new SoapMethodKey(name, methodNamespace);
            this.Parent = soapPayload;
            this.SetSqlMethod(database, schema, sqlMethod);
        }

        internal SoapPayloadMethod(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Method";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        private void AddEndpointPart(StringBuilder sb, ScriptingPreferences sp)
        {
            Endpoint e = ((SoapPayload)ParentColl.ParentInstance).Parent;

            // format full table name for scripting
            string fullEndpointName = e.FullQualifiedName;

            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER ENDPOINT {0} FOR SOAP", e.FormatFullNameForScripting(sp));
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            AddEndpointPart(sb, sp);
            sb.Append("( ADD ");
            Script(sb, sp);
            sb.Append(Globals.RParen);	// close soap spec
            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // format full table name for scripting
            AddEndpointPart(sb, sp);
            sb.Append("( ");
            Script(sb, sp);
            sb.Append(Globals.RParen);	// close soap spec
            alterQuery.Add(sb.ToString());
        }

        public void Drop()
        {
            base.DropImpl();
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            AddEndpointPart(sb, sp);
            sb.Append("( DROP ");
            Script(sb, sp);
            sb.Append(Globals.RParen);	// close soap spec
            queries.Add(sb.ToString());
        }

        public void SetSqlMethod(System.String database, System.String schema, System.String name)
        {
            if (null == database)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("database"));
            }

            if (null == schema)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("schema"));
            }

            if (null == name)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("name"));
            }

            this.Properties.Get("SqlMethod").SetValue(string.Format(SmoApplication.DefaultCulture,
                                                            "{0}.{1}.{2}",
                                                            MakeSqlBraket(database),
                                                            MakeSqlBraket(schema),
                                                            MakeSqlBraket(name)));
            this.Properties.Get("SqlMethod").SetDirty(true);
        }

        internal void Script(StringBuilder sb, ScriptingPreferences sp)
        {
            if (sp.ScriptForAlter)
            {
                if (this.State == SqlSmoState.Creating)
                {
                    sb.Append("ADD ");
                }
                else if (this.State == SqlSmoState.ToBeDropped)
                {
                    sb.Append("DROP ");
                }
                else
                {
                    sb.Append("ALTER ");
                }
            }

            sb.Append("WEBMETHOD ");

            string sNamespace = this.Namespace;

            if (sNamespace.Length > 0)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "'{0}'.", SqlString(sNamespace));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "'{0}'", SqlString(GetName(sp)));

            if (sp.Behavior == ScriptBehavior.Drop)
            {
                return;
            }

            string sqlMethod = (string)GetPropValue("SqlMethod");
            sb.Append(Globals.LParen);
            sb.AppendFormat(SmoApplication.DefaultCulture, " NAME={0}", MakeSqlString(sqlMethod));

            object schema = GetPropValueOptional("MethodXsdSchemaOption");
            if (null != schema && (!sp.ScriptForAlter || Properties.Get("MethodXsdSchemaOption").Dirty))
            {
                string schemaOption = string.Empty;
                switch ((MethodXsdSchemaOption)schema)
                {
                    case MethodXsdSchemaOption.None: schemaOption = "NONE"; break;
                    case MethodXsdSchemaOption.Standard: schemaOption = "STANDARD"; break;
                    case MethodXsdSchemaOption.Default: schemaOption = "DEFAULT"; break;
                }

                if (schemaOption.Length > 0)
                {
                    sb.Append(Globals.newline);
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", SCHEMA={0}", schemaOption);
                }
            }

            object format = GetPropValueOptional("ResultFormat");
            if (null != format && (!sp.ScriptForAlter || Properties.Get("ResultFormat").Dirty))
            {
                string formatName = string.Empty;
                switch ((ResultFormat)format)
                {
                    case ResultFormat.AllResults: formatName = "ALL_RESULTS"; break;
                    case ResultFormat.RowSets: formatName = "ROWSETS_ONLY"; break;
                }

                if (formatName.Length > 0)
                {
                    sb.Append(Globals.newline);
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", FORMAT={0}", formatName);
                }
            }

            sb.Append(Globals.RParen);	// close method specification
        }


    }
}


