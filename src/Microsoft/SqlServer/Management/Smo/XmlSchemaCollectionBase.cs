// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class XmlSchemaCollection : ScriptSchemaObjectBase,
        Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IAlterable,
        IScriptable, IExtendedProperties
    {
        internal XmlSchemaCollection(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public XmlSchemaCollection(Database database, string name, string schema, string xmlSchemaDocument)
            : base()
        {
            ValidateName(name);
            this.key = new SchemaObjectKey(name, schema);
            this.Parent = database;
            this.Properties["Text"].Value = xmlSchemaDocument;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "XmlSchemaCollection";
            }
        }

        /// <summary>
        /// Drops the namespace.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string sFullName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, sFullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_XML_COLLECTION,
                    "NOT", SqlString(sFullName));
                sb.Append(sp.NewLine);
            }
            string text = (string)GetPropValue("Text");
            sb.AppendFormat(SmoApplication.DefaultCulture,
                        "CREATE XML SCHEMA COLLECTION {0} AS {1}",
                        sFullName, MakeSqlString(text));

            queries.Add(sb.ToString());
        }

        /// <summary>
        /// Drops the namespace.
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

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_XML_COLLECTION,
                                    "", SqlString(FormatFullNameForScripting(sp)));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP XML SCHEMA COLLECTION  {0}", FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }

        /// <summary>
        /// Save changes made to the object to the database.
        /// Note that only extended properties can be modified.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            // nothing happens here, we only alter ExtendedProperties
        }

        #region IScriptable
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }
        #endregion




        /// <summary>
        /// Collection of extended properties for this object.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] {
                                           new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix ),
            };
        }

        private string xmlSchemaDocument = string.Empty;


        /// <summary>
        /// Allows the addition of new schema documents to the xml schema collection.
        /// </summary>
        /// <param name="xmlSchemaDocument"></param>
        public void AddSchemaDocument(System.String xmlSchemaDocument)
        {
            try
            {
                if (null == xmlSchemaDocument)
                {
                    throw new ArgumentNullException("xmlSchemaDocument");
                }

                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                    "ALTER XML SCHEMA COLLECTION {0} ADD N'{1}'",
                                    this.FullQualifiedName, SqlString(xmlSchemaDocument)));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddSchemaDocument, this, e);
            }
        }

        /// <summary>
        /// Enumerates the namespaces within this collection.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumNamespaces()
        {
            try
            {
                Request req = new Request(this.Urn.Value + "/Namespace");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumNamespaces, this, e);
            }
        }

        /// <summary>
        /// Enumerates all types contained in this xml schema collection.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumTypes()
        {
            try
            {
                Request req = new Request(this.Urn.Value + "/Namespace/Type");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumTypes, this, e);
            }
        }

        /// <summary>
        /// Enumerates all types contained in this xml schema collection, limited
        /// to the specified namespace.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumTypes(string xmlNamespace)
        {
            try
            {
                if (null == xmlNamespace)
                {
                    throw new ArgumentNullException("xmlNamespace");
                }

                Request req = new Request(this.Urn.Value +
                            string.Format(SmoApplication.DefaultCulture,
                                            "/Namespace[@Name='{0}']/Type",
                                            Urn.EscapeString(xmlNamespace)));
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumTypes, this, e);
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
                                            "ID"};
            List<string> list = GetSupportedScriptFields(typeof(XmlSchemaCollection.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            list.Add("Text");
            return list.ToArray();
        }
    }

}



