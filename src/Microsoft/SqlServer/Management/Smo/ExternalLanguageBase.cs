// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ExternalLanguage : NamedSmoObject, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable,
        Cmn.IDropIfExists, IExtendedProperties, IScriptable
    {
        internal ExternalLanguage(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            m_ExternalLanguageFiles = null;
        }

        /// <summary>
        /// Set of platforms to be removed for this language.
        /// </summary>
        private HashSet<ExternalLanguageFilePlatform> toBeRemovedPlatforms = new HashSet<ExternalLanguageFilePlatform>();

        #region Properties

        /// <summary>
        /// returns the name of the type in the urn expression.
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return nameof(ExternalLanguage);
            }
        }

        /// <summary>
        /// Extended properties of the language.
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
            return new PropagateInfo[] { new PropagateInfo(this.IsSupportedObject<ExternalLanguage>() ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        /// <summary>
        /// A collection of external language files specified for the external language.
        /// </summary>
        ExternalLanguageFileCollection m_ExternalLanguageFiles;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExternalLanguageFile))]
        public ExternalLanguageFileCollection ExternalLanguageFiles
        {
            get
            {
                CheckObjectState();
                if (null == m_ExternalLanguageFiles)
                {
                    m_ExternalLanguageFiles = new ExternalLanguageFileCollection(this);
                }
                return m_ExternalLanguageFiles;
            }
        }

        /// <summary>
        /// Adding a new file spec via language content bits to an external language.
        /// </summary>
        /// <param name="fileName">The .dll or .so file for external language</param>
        /// <param name="contentFromBinary">The language content as binary</param>
        /// <param name="platform">In hybrid OS platform (e.g. Linux/Windows)</param>
        public void AddFile(string fileName, byte[] contentFromBinary, ExternalLanguageFilePlatform platform = ExternalLanguageFilePlatform.Default)
        {
            AddFile(fileName, platform, contentFromBinary, contentFromFile: null);
        }

        /// <summary>
        /// Adding a new file spec via language file path to an external language.
        /// </summary>
        /// <param name="fileName">The .dll or .so file for external language</param>
        /// <param name="contentFromFile">The language content as a file path</param>
        /// <param name="platform">In hybrid OS platform (e.g. Linux/Windows)</param>
        public void AddFile(string fileName, string contentFromFile, ExternalLanguageFilePlatform platform = ExternalLanguageFilePlatform.Default)
        {
            AddFile(fileName, platform, contentFromBinary: null, contentFromFile);
        }

        /// <summary>
        /// Removing a file spec from external language by providing its platform.
        /// </summary>
        /// <param name="platform">An OS platform (e.g. Linux/Windows)</param>
        public void RemoveFile(ExternalLanguageFilePlatform platform)
        {
            toBeRemovedPlatforms.Add(platform);
        }

        #endregion

        #region Create Methods

        /// <summary>
        /// Creates the external language on the instance of SQL Server.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        /// <summary>
        /// Tasks to be performed after object creation.
        /// </summary>
        protected override void PostCreate()
        {
            base.PostCreate();
            this.ExternalLanguageFiles.Clear();
            this.ExternalLanguageFiles.Refresh();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            if (this.ExternalLanguageFiles.Count == 0)
            {
                throw new SmoException(ExceptionTemplates.MissingExternalLanguageFileSpec);
            }

            Property property;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            StringCollection scAddFile = new StringCollection();

            string fullName = FormatFullNameForScripting(sp);

            // Need to generate commentary headers
            //
            if (sp.IncludeScripts.Header) 
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            StringBuilder createStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // CREATE EXTERNAL LANGUAGE <language_name>
            //
            createStmt.AppendFormat("CREATE EXTERNAL LANGUAGE {0}", fullName);
            createStmt.Append(sp.NewLine);

            // AUTHORIZATION <owner_name>
            //
            if (sp.IncludeScripts.Owner && (null != (property = this.Properties.Get(nameof(Owner))).Value))
            {
                createStmt.AppendFormat(" AUTHORIZATION [{0}]", SqlBraket(property.Value.ToString()));
                createStmt.Append(sp.NewLine);
            }

            // FROM (content = <path> or <binary>)
            //
            createStmt.Append(" FROM ");

            var fileSpecs = new List<string>();
            foreach (ExternalLanguageFile elf in this.ExternalLanguageFiles)
            {
                string fileName = elf.GetPropValueOptional(nameof(elf.FileName), string.Empty);
                var platform = elf.GetPropValueOptional(nameof(elf.Platform), ExternalLanguageFilePlatform.Default);

                Property pContentFromFile = elf.Properties.Get(nameof(elf.ContentFromFile));
                Property pContentFromBinary = elf.Properties.Get(nameof(elf.ContentFromBinary));
                
                // We cannot script out a language with the file path since
                // we have only extracted and stored its content in the DB.
                //
                string contentFromFile = null;
                string contentFromBinary = null;

                // If we are in creating mode, we read the current property values. Otherwise (e.g. for scripting), we read the stored values
                //
                if (this.State == SqlSmoState.Creating)
                {
                    contentFromFile = (string)pContentFromFile.Value;
                    contentFromBinary = ConvertBinaryToString((byte[])pContentFromBinary.Value);
                }
                else
                {
                    contentFromBinary = elf.GetFileText();

                    if(fileName == null)
                    {
                        fileName = elf.GetFileName();
                    }
                }

                // For each ExternalLanguageFile, at least one of contentFromFile or contentFromBinary must be specified 
                //
                if (contentFromFile == null && contentFromBinary == null)
                {
                    throw new PropertyNotSetException("Content");
                }

                // FileName is mandatory for each ExternalLanguageFile
                //
                if (fileName == null)
                {
                    throw new PropertyNotSetException("FileName");
                }

                fileSpecs.Add(GenerateContentAndFileNameString(contentFromFile, contentFromBinary, fileName, platform));
            }

            createStmt.Append(string.Join($", {sp.NewLine}", fileSpecs.ToArray()));

            // If we need to generate existence check
            //
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, IncludeExistsExternalLanguage(exists: false, SqlString(GetName(sp))));
                sb.Append(sp.NewLine);

                // IF NOT EXISTS Create External Language throws syntax error. It needs to be wrapped up into EXEC statement
                //
                sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC sys.sp_executesql N'{0}'", SqlString(createStmt.ToString()));
                sb.Append(sp.NewLine);
                queries.Add(sb.ToString());
            }
            else 
            {
                queries.Add(createStmt.ToString());
            }
        }

        #endregion

        #region Drop Methods

        /// <summary>
        /// Drop the language.
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
                sb.AppendFormat(SmoApplication.DefaultCulture, IncludeExistsExternalLanguage(exists: true, GetName(sp)));
                sb.Append(sp.NewLine);
            }

            // DROP EXTERNAL LANGUAGE
            sb.AppendFormat(" DROP EXTERNAL LANGUAGE {0} ", fullName);
            sb.Append(sp.NewLine);

            dropQuery.Add(sb.ToString());
        }

        #endregion

        #region Alter Methods

        /// <summary>
        /// Alter the language.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Alters the object by adding a new file spec, updating a current one, or removing 
        /// a file spec based on platform. The batch delimeters (e.g. GO) is added by the base class 
        /// during the execution time.
        /// </summary>
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            // Handling removal
            //
            foreach(var platform in toBeRemovedPlatforms)
            {
                // ALTER EXTERNAL LANGUAGE <external_language_name>
                //
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.AppendFormat("ALTER EXTERNAL LANGUAGE {0}", FullQualifiedName);
                sb.Append(sp.NewLine);

                // | Remove (content = <path> or <binary>)
                //
                sb.AppendFormat(" REMOVE PLATFORM {0}", GetPlatformDescription(platform));
                sb.Append(sp.NewLine);
                alterQuery.Add(sb.ToString());
            }

            // Handling adding new fileSpec or updating existing ones
            //
            foreach (ExternalLanguageFile elf in this.ExternalLanguageFiles)
            {
                // Do not alter this file if it's state is Existing and none of its properties has been changed
                //
                if (elf.State == SqlSmoState.Existing && !elf.InternalIsObjectDirty)
                {
                    continue;
                }

                // ALTER EXTERNAL LANGUAGE <external_language_name>
                //
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.AppendFormat("ALTER EXTERNAL LANGUAGE {0}", FullQualifiedName);
                sb.Append(sp.NewLine);

                Property pContentFromFile = elf.Properties.Get(nameof(elf.ContentFromFile));
                Property pContentFromBinary = elf.Properties.Get(nameof(elf.ContentFromBinary));
                
                string fileName = elf.GetPropValueOptional(nameof(elf.FileName), string.Empty);
                var platform = elf.GetPropValueOptional(nameof(elf.Platform), ExternalLanguageFilePlatform.Default);

                string contentFromFile = null;
                string contentFromBinary = null;

                if (pContentFromFile.Dirty && pContentFromFile.Value != null && pContentFromBinary.Dirty && pContentFromBinary.Value != null)
                {
                    throw new SmoException(ExceptionTemplates.BadPropertiesCombination);
                }

                if(pContentFromFile.Dirty && pContentFromFile.Value != null)
                {
                    contentFromFile = (string)pContentFromFile.Value;
                }
                else if(pContentFromBinary.Dirty && pContentFromBinary.Value != null)
                {
                    contentFromBinary = ConvertBinaryToString((byte[])pContentFromBinary.Value);
                }

                if(elf.State == SqlSmoState.Existing)
                {
                    // SET (content = <path> or <binary>)
                    //
                    sb.AppendFormat(" SET {0}", GenerateContentAndFileNameString(contentFromFile, contentFromBinary, fileName, platform));
                }
                else if(elf.State == SqlSmoState.Creating)
                {
                    // | ADD (content = <path> or <binary>)
                    //
                    sb.AppendFormat(" ADD {0}", GenerateContentAndFileNameString(contentFromFile, contentFromBinary, fileName, platform));
                }

                sb.Append(sp.NewLine);
                alterQuery.Add(sb.ToString());
            }
        }

        /// <summary>
        /// Perform actions after Altering the language.
        /// </summary>
        protected override void PostAlter()
        {
            base.PostAlter();
            this.ExternalLanguageFiles.Clear();
            this.ExternalLanguageFiles.Refresh();

            // Clearing the set of dropped file specs via platform
            //
            toBeRemovedPlatforms.Clear();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Adding a new file spec to an external language.
        /// </summary>
        /// <param name="fileName">The .dll or .so file for external language</param>
        /// <param name="platform">In hybrid OS platform (e.g. Linux/Windows)</param>
        /// <param name="contentFromBinary">The language content as binary</param>
        /// <param name="contentFromFile">The language content as a file path</param>
        /// <remarks>One of contentFromBinary or contentFromFile must be specified. </remarks>
        private void AddFile(string fileName, ExternalLanguageFilePlatform platform, byte[] contentFromBinary = null, string contentFromFile = null)
        {
            // An arbitrary name for ExternalLanguageFile. The name is not actually stored.
            //
            var elf = new ExternalLanguageFile(this, fileName)
            {
                FileName = fileName,
                Platform = platform
            };
            
            // Only update a property if the value was provided (i.e. do not make the prop dirty if it's not updated)
            //
            if (contentFromBinary != null)
            {
                elf.ContentFromBinary = contentFromBinary;
            }
            if (contentFromFile != null)
            {
                elf.ContentFromFile = contentFromFile;
            }

            this.ExternalLanguageFiles.Add(elf);
        }

        /// <summary>
        /// Generate an external language content string based on the content type.
        /// </summary>
        /// <param name="contentFromFile">The language content as a file path</param>
        /// <param name="contentFromBinary">The language content as binary</param>
        /// <param name="fileName">The .dll or .so file for external language</param>
        /// <param name="platform">In hybrid OS platform (e.g. Linux/Windows)</param>
        /// <returns>The file spec portion of the external language scripting</returns>
        private static string GenerateContentAndFileNameString(string contentFromFile, string contentFromBinary, string fileName, ExternalLanguageFilePlatform platform)
        {
            StringBuilder fileSpec = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (string.IsNullOrEmpty(contentFromFile) && contentFromBinary == null)
            {
                throw new SmoException("A file spec needs either a path to the language file or language content bits");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new SmoException("In a file spec, file name must be specified");
            }

            if (!string.IsNullOrEmpty(contentFromFile))
            {
                fileSpec.AppendFormat($"(content = '{SqlString(contentFromFile)}', file_name = '{SqlString(fileName)}'");
            }
            else if (contentFromBinary != null)
            {
                fileSpec.AppendFormat($"(content = {contentFromBinary}, file_name = '{SqlString(fileName)}'");
            }
            if (platform != ExternalLanguageFilePlatform.Default)
            {
                fileSpec.AppendFormat($", platform = {GetPlatformDescription(platform)})");
            }
            else
            {
                fileSpec.AppendFormat($")");
            }

            return fileSpec.ToString();
        }

        /// <summary>
        /// Returns a script to check existence or not existence of an external language.
        /// </summary>
        /// <param name="exists">check existence or not existence</param>
        /// <param name="name">Name of the external language</param>
        private static string IncludeExistsExternalLanguage(bool exists, string name)
        {
            return $"IF {(exists ? "" : "NOT")} EXISTS (SELECT * from sys.external_languages langs WHERE langs.language = '{SqlString(name)}')";
        }

        /// <summary>
        /// Returns text equivalent of an external language platform value
        /// </summary>
        /// <param name="platform">The platform language was created for</param>
        private static string GetPlatformDescription(ExternalLanguageFilePlatform platform)
        {
            TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(ExternalLanguageFilePlatformConverter));
            return typeConverter.ConvertToInvariantString(platform);
        }

        /// <summary>
        /// Returns corresponding string for the byte array.
        /// </summary>
        private static string ConvertBinaryToString(byte[] fileBytes)
        {
            if (fileBytes == null)
            {
                return null;
            }

            StringBuilder results = new StringBuilder(fileBytes.Length + 2);
            results.Append("0x");
            foreach (byte b in fileBytes)
            {
                // Format the string as two uppercase hexadecimal characters.
                results.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }

            return results.ToString();
        }

        #endregion

        /// <summary>
        /// Script an external language.
        /// </summary>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting options
        /// <param name="scriptingOptions">Scripting options.</param>
        /// </summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Mark the object as dropped.
        /// </summary>
        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();

            if (null != m_ExternalLanguageFiles)
            {
                m_ExternalLanguageFiles.MarkAllDropped();
            }
        }

        /// <summary>
        /// Refreshes the property bag and remove ExternalLanguageFile if it is in drop state
        /// </summary>
        public override void Refresh()
        {
            this.ExternalLanguageFiles.Refresh();
            base.Refresh();
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
        /// <returns>The fields that are needed to script this object</returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            return new string[] {
                nameof(ID),
                nameof(Owner),
                nameof(IsSystemObject)
            };
        }
    }
}
