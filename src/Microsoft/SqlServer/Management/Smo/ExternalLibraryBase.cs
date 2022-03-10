// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Server;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ExternalLibrary : ScriptNameObjectBase, Cmn.IAlterable, Cmn.IDroppable,
        Cmn.IDropIfExists, IExtendedProperties, IScriptable
    {
        /// <summary>
        /// The library content (could be a path or actual bits) to install/alter library from.
        /// </summary>
        private string libraryContent;

        /// <summary>
        /// Is the library content a path (versus actual bits).
        /// </summary>
        private ExternalLibraryContentType libraryContentType;

        /// <summary>
        /// State variable for externalLibraryFile property.
        /// </summary>
        private ExternalLibraryFile externalLibraryFile;

        private void Init()
        {
            this.libraryContent = null;
            this.libraryContentType = ExternalLibraryContentType.Path;
            this.externalLibraryFile = null;
        }

        #region Constructors

        internal ExternalLibrary(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
        base(parentColl, key, state)
        {
            if (SqlContext.IsAvailable)
            {
                throw new Exception(ExceptionTemplates.SmoSQLCLRUnAvailable);
            }
            Init();
        }

        #endregion

        #region Properties

        /// <summary>
        /// returns the name of the type in the urn expression.
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "ExternalLibrary";
            }
        }

        /// <summary>
        /// Extended properties of the library.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                CheckObjectState();
                if (null == this.m_ExtendedProperties)
                {
                    this.m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return this.m_ExtendedProperties;
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { new PropagateInfo(this.IsSupportedObject<ExternalLibrary>() ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        /// <summary>
        /// The file associated with the library.
        /// </summary>
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public ExternalLibraryFile ExternalLibraryFile
        {
            get
            {
                CheckObjectState();
                if (null == this.externalLibraryFile)
                {
                    // Note: the "ExternalLibraryFile" object does not really live by itself (i.e. it cannot be scripted
                    //       (create/alter) independely from its parent. So, it makes sense to set its 'state' to be the
                    //       same as its parent, i.e. the ExternaLibrary object.
                    externalLibraryFile = new ExternalLibraryFile(parent: this, key: new ObjectKeyBase(), state: this.State);
                }
                return this.externalLibraryFile;
            }
        }      

        #endregion

        #region Create Methods

        /// <summary>
        /// Create a library using a path or bits with CREATE EXTERNAL LIBRARY FROM {path or binary}.
        /// <param name="libraryContent">The library path or the library binary</param>
        /// <param name="contentType">Is the content type specifying a path or binary</param>
        /// </summary>
        public void Create(string libraryContent, ExternalLibraryContentType contentType)
        {
            try
            {
                if (String.IsNullOrEmpty(libraryContent))
                {
                    throw new FailedOperationException(ExceptionTemplates.Create, this, null,
                                                    ExceptionTemplates.EmptyInputParam("libraryContent", "string"));
                }

                this.libraryContent = libraryContent;
                this.libraryContentType = contentType;
                base.CreateImpl();
            }
            finally
            {
                if (!this.IsDesignMode)
                {
                    this.libraryContent = null;
                    this.libraryContentType = ExternalLibraryContentType.Path;
                }
            }
        }

        /// <summary>
        /// Creates the external library on the instance of SQL Server.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            Property property;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            StringCollection scAddFile = new StringCollection();

            string fullName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_EXTERNAL_LIBRARY, "NOT", SqlString(GetName(sp)));
                sb.Append(sp.NewLine);
            }

            // CREATE EXTERNAL LIBRARY <library_name>
            sb.AppendFormat("CREATE EXTERNAL LIBRARY {0}", fullName);
            sb.Append(sp.NewLine);

            // AUTHORIZATION <owner_name>
            if (sp.IncludeScripts.Owner && (null != (property = this.Properties.Get("Owner")).Value))
            {
                sb.AppendFormat(" AUTHORIZATION [{0}]", SqlBraket(property.Value.ToString()));
                sb.Append(sp.NewLine);
            }

            // If a content is not given, then we get it from the ExternalLibraryFile
            // Note: an existing ExternalLibrary object cannot be scripted out
            //       with a Content = <path>, because that information is not
            //       preserved anywhere on the server.
            if (this.libraryContent == null)
            {
                // ExternalLibraryFile is never null (it is a property on this object),
                // so it is safe to call GetFileText() on it.
                //
                // It is unlikely for GetFileText() not to return a string prefixed with "0x", but
                // we play safe and check. If the "0x" prefix is there, we strip it since
                // GenerateContentString() is going to add it back.
                var encodedContentAsText = this.ExternalLibraryFile.GetFileText();
                this.libraryContent = (encodedContentAsText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    ? encodedContentAsText.Substring(2)
                    : encodedContentAsText;
                this.libraryContentType = ExternalLibraryContentType.Binary;
            }

            // FROM (content = <path> or <binary>)
            sb.AppendFormat(" FROM {0}", GenerateContentString(this.libraryContent, this.libraryContentType));
            sb.Append(sp.NewLine);

            // WITH LANGUAGE
            sb.AppendFormat(" WITH (LANGUAGE = '{0}')", SqlString(this.Properties.Get("ExternalLibraryLanguage").Value.ToString()));

            queries.Add(sb.ToString());
        }
        #endregion

        #region Drop Methods

        /// <summary>
        /// Drop the library.
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
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string fullName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_EXTERNAL_LIBRARY, "", SqlString(GetName(sp)));
                sb.Append(sp.NewLine);
            }

            // DROP EXTERNAL LIBRARY
            sb.AppendFormat("DROP EXTERNAL LIBRARY {0}", fullName);
            sb.Append(sp.NewLine);

            dropQuery.Add(sb.ToString());
        }
        #endregion

        #region Alter Methods

        /// <summary>
        /// Alter the library.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Alter a library using a path or bits with ALTER EXTERNAL LIBRARY SET {path or binary}.
        /// <param name="libraryContent">The library path or the library binary</param>
        /// <param name="contentType">Is the content type specifying a path or binary</param>
        /// </summary>
        public void Alter(string libraryContent, ExternalLibraryContentType contentType)
        {
            try
            {
                if (String.IsNullOrEmpty(libraryContent))
                {
                    throw new FailedOperationException(ExceptionTemplates.Alter, this, null,
                                                    ExceptionTemplates.EmptyInputParam("libraryContent", "string"));
                }

                this.libraryContent = libraryContent;
                this.libraryContentType = contentType;
                base.AlterImpl();
            }
            finally
            {
                if (!this.IsDesignMode)
                {
                    this.libraryContentType = ExternalLibraryContentType.Path;
                    this.libraryContent = null;
                }
            }
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (!IsObjectDirty())
            {
                return;
            }
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            // ALTER EXTERNAL LIBRARY <external_library_name>
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            sb.AppendFormat("ALTER EXTERNAL LIBRARY {0}", FullQualifiedName);
            sb.Append(sp.NewLine);

            // SET (content = <path> or <binary>)
            sb.AppendFormat(" SET {0}", GenerateContentString(this.libraryContent, this.libraryContentType));
            sb.Append(sp.NewLine);

            // WITH LANGUAGE
            sb.AppendFormat(" WITH (LANGUAGE = '{0}')", SqlString(this.Properties.Get("ExternalLibraryLanguage").Value.ToString()));

            alterQuery.Add(sb.ToString());
        }
        #endregion

        #region Helpers

        /// <summary>
        /// Generate an external library content string based on the content type.
        /// <param name="content">The library content</param>
        /// <param name="contentType">The library content type</param>
        /// </summary>
        private static string GenerateContentString(string content, ExternalLibraryContentType contentType)
        {
            string result = "";
            switch (contentType)
            {
                case ExternalLibraryContentType.Path:
                    result = String.Format("(content = '{0}')", SqlString(content));
                    break;
                case ExternalLibraryContentType.Binary:
                    result = String.Format("(content = 0x{0})", content);
                    break;
                default:
                    throw new SmoException(String.Format("Unsupported value for the external library content type: {0}.", contentType));
            }
            return result;
        }

        #endregion

        /// <summary>
        /// Script an external library.
        /// </summary>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting optiions
        /// <param name="scriptingOptions">Scripting options.</param>
        /// </summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Check if the object is dirty.
        /// </summary>
        protected override bool IsObjectDirty()
        {
            return (base.IsObjectDirty() || null != libraryContent);
        }

        /// <summary>
        /// Mark the object as dropped.
        /// </summary>
        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != externalLibraryFile)
            {
                externalLibraryFile.MarkDroppedInternal();
            }
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">DatabaseEngineType of the server</param>
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
            return new string[] {
                                    "ID",
                                    "ExternalLibraryLanguage",
                                    // "Name", - There is no need to explicitly add the 'Name' field, as it is automatically
                                    //           added in Microsoft.SqlServer.Management.Smo.Server.AddNecessaryFields()
                                    "Owner"
                                };
        }
    }
}
