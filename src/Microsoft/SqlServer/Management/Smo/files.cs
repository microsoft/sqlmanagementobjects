// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// This class incapsulates a database log file
    ///</summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class LogFile : DatabaseFile, Cmn.ICreatable
    {
        internal LogFile(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public LogFile(Database database, string name, string fileName)
            : base()
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = database;
            this.FileName = fileName;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "LogFile";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            var bSuppressDirtyCheck = sp.SuppressDirtyCheck;

            var statement = new StringBuilder();
            statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] ADD LOG FILE ",
                                                            SqlBraket(ParentColl.ParentInstance.InternalName));

            if (ScriptDdl(sp, statement, bSuppressDirtyCheck, true))
            {
                createQuery.Add(statement.ToString());
            }
        }

    }

    ///<summary>
    /// This class incapsulates a database data file
    ///</summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    [SfcElementType("File")]
    public partial class DataFile : DatabaseFile, Cmn.ICreatable
    {
        internal DataFile(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            
        }

        public DataFile(FileGroup fileGroup, string name, string fileName)
            : base()
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = fileGroup;
            this.FileName = fileName;
        }

        private void InitializeDefaults()
        {
            this.IsPrimaryFile = false;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "File";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            var bSuppressDirtyCheck = sp.SuppressDirtyCheck;

            var statement = new StringBuilder();
            var dbName = ParentColl.ParentInstance.ParentColl.ParentInstance.InternalName;
            statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] ADD FILE ",
                                        SqlBraket(dbName));

            // if this returns false, no DDL for the file could be generated
            if (!ScriptDdl(sp, statement, bSuppressDirtyCheck, true))
            {
                return;
            }

            if (dbName.ToLower(SmoApplication.DefaultCulture) != "tempdb")
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, " TO FILEGROUP [{0}]",
                                                SqlBraket(ParentColl.ParentInstance.InternalName));
            }

            createQuery.Add(statement.ToString());
        }

        internal static void Validate_set_IsPrimaryFile(Property prop, object newValue)
        {
            var thisObj = (DataFile)prop.Parent.m_parent;
            // throw if parent already created
            if (thisObj.State != SqlSmoState.Creating)
            {
                throw new PropertyReadOnlyException(prop.Name);
            }

            // we can only change this for the primary file
            if (0 != string.Compare(thisObj.Parent.Name, "PRIMARY", StringComparison.Ordinal) &&
                (bool)newValue)
            {
                throw new SmoException(ExceptionTemplates.CannotChangePrimary);
            }

            // check if other files are set to be primary
            if ((bool)newValue)
            {
                var files = ((DataFileCollection)thisObj.ParentColl).InternalStorage;
                foreach (var f in files)
                {
                    var pPrim = f.Properties.Get(nameof(DataFile.IsPrimaryFile));
                    if (thisObj != f && pPrim.Value != null && (bool)pPrim.Value)
                    {
                        throw new SmoException(ExceptionTemplates.OnlyOnePrimaryFile);
                    }
                }
            }
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "IsPrimaryFile")
            {
                Validate_set_IsPrimaryFile(prop, value);
            }
        }

        /// <summary>
        /// Brings the file offline. CAUTION: A file set to OFFLINE can only be set 
        /// online by restoring the file from a previously existing backup
        /// </summary>
        public void SetOffline()
        {
            try
            {
                CheckObjectState();
                ThrowIfBelowVersion90();

                this.ExecutionManager.ExecuteNonQuery(string.Format(
                                SmoApplication.DefaultCulture,
                                "ALTER DATABASE [{0}] MODIFY FILE (NAME=N'{1}', OFFLINE)",
                                SqlBraket(Parent.Parent.Name), SqlString(this.Name)));

            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetOffline, this, e);
            }
            // CS1058: Warning as Error: A previous catch clause already catches all exceptions. All non-exceptions thrown will be wrapped in a System.Runtime.CompilerServices.RuntimeWrappedException
            //			catch
            //			{
            //				throw new FailedOperationException(ExceptionTemplates.SetOffline, this, null);
            //			}
        }

        internal static string[] GetScriptFields(Type parentType,
            Cmn.ServerVersion version,
            Cmn.DatabaseEngineType databaseEngineType,
            Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            string[] fields =
            {
                "IsPrimaryFile",
            };
            var list = GetSupportedScriptFields(typeof(Database.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }

    ///<summary>
    /// Base class for DataFile and LogFile
    ///</summary>
    public class DatabaseFile : NamedSmoObject, Cmn.IAlterable, Cmn.IDroppable, Cmn.IRenamable, Cmn.IMarkForDrop
    {
        // propagate the base constructors to the instance classes DataFile and LogFile
        internal DatabaseFile(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {

        }


        protected internal DatabaseFile() : base() { }

        public void Drop()
        {
            base.DropImpl();
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            CheckObjectState();
            dropQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}]  REMOVE FILE [{1}]",
                                                            SqlBraket(DatabaseName), SqlBraket(this.Name)));
        }

        internal bool ScriptDdl(ScriptingPreferences sp, StringBuilder ddl,
                            bool bSuppressDirtyCheck, bool scriptCreate)
        {
            return ScriptDdl(sp, ddl, bSuppressDirtyCheck, scriptCreate, "");
        }

        // scripts ddl for file creation eg. something like 
        // (NAME = ..., FILENAME=..., SIZE=... etc)
        // Return values:
        // - ManagedInstance: false if none of the properties was scripted; true otherwise
        // - Non ManagedInstance: false if none of the properties was scripted, or if the FileName is missing; true otherwise
        // if scriptCreate is true, then this function scripts for create, else it is for alter
        internal bool ScriptDdl(ScriptingPreferences sp, StringBuilder ddl, bool bSuppressDirtyCheck,
                                bool scriptCreate, string onlyThisProperty)
        {
            ddl.Append(Globals.LParen);

            var changeCount = 0;
            var isManagedInstance = sp.TargetDatabaseEngineEdition == Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlManagedInstance;

            ddl.AppendFormat(SmoApplication.DefaultCulture, " NAME = N'{0}'", SqlString(this.Name));

            if (null == onlyThisProperty)
            {
                onlyThisProperty = "";
            }

            switch (onlyThisProperty)
            {
                case "FileName":
                    if (!ScriptFileName(sp,ddl, bSuppressDirtyCheck, scriptCreate, ref changeCount))
                    {
                        return false;
                    }

                    break;
                case "Size":
                    ScriptSize(sp, ddl, bSuppressDirtyCheck, ref changeCount);
                    break;
                case "MaxSize":
                    ScriptMaxSize(sp, ddl, bSuppressDirtyCheck, ref changeCount);
                    break;
                case "Growth":
                    ScriptGrowth(sp, ddl, bSuppressDirtyCheck, scriptCreate, ref changeCount);
                    break;
                default:
                    // Note that we're doing the same thing in "case 'FileName'" and "default":
                    // both are trying to script ScriptFileName().
                    // This could be refactored and merged into one statement (tracked with TFS #12860040)
                    //

                    // Don't script FILENAME if scripting for Managed Instances - it's not supported.
                    //
                    if (!isManagedInstance)
                    {
                        if (!ScriptFileName(sp, ddl, bSuppressDirtyCheck, scriptCreate, ref changeCount))
                            throw new PropertyNotSetException("FileName");
                    }

                    ScriptSize(sp, ddl, bSuppressDirtyCheck, ref changeCount);
                    ScriptMaxSize(sp, ddl, bSuppressDirtyCheck, ref changeCount);
                    ScriptGrowth(sp, ddl, bSuppressDirtyCheck, scriptCreate, ref changeCount);
                    break;
            }

            ddl.Append(Globals.RParen);

            // The special case for ManagedInstance is because:
            // 1) Scripting the FileName is a no-op in an Alter (this prop cannot be changed, really)
            // 2) Still, then we are creating the object we cannot rely on the fact that
            //    FileName was scriped (i.e. bumped up 'changeCount'.
            return changeCount > 0 || (scriptCreate && isManagedInstance);
        }

        private string DatabaseName
        {
            get
            {
                if (this is LogFile)
                {
                    return ParentColl.ParentInstance.InternalName;
                }
                else
                {
                    return ParentColl.ParentInstance.ParentColl.ParentInstance.InternalName;
                }
            }
        }

        // this function tries to fit the table size into the sizes that the server
        // is accepting. 
        private void AppendSize(ScriptingPreferences sp, StringBuilder ddl, double size)
        {

            var isTransformed = false;
            if (size > Int32.MaxValue)
            {
                // transform KB into  GB 
                size = size / (01 <<  20);
                isTransformed = true;
            }

            if (0 > size || Int32.MaxValue < size)
            {
                throw new SmoException(ExceptionTemplates.WrongSize);
            }

            if (isTransformed)
            {
                ddl.AppendFormat(SmoApplication.DefaultCulture, "{0}{1} ",
                        Convert.ToInt32(size, SmoApplication.DefaultCulture), "GB");
            }
            else
            {
                ddl.AppendFormat(SmoApplication.DefaultCulture, "{0}KB ",
                        Convert.ToInt32(size, SmoApplication.DefaultCulture));
            }
        }

        private void ScriptSize(ScriptingPreferences sp, StringBuilder ddl,
                                bool bSuppressDirtyCheck, ref int changeCount)
        {
            var pSize = Properties.Get("Size");

            //Size cannot be specified for filestream filegroups.
            if (this.IsFileStreamBasedFile())
            {
                if (pSize.Dirty)
                {
                    throw new SmoException(ExceptionTemplates.InvalidSizeFileStream);
                }

                return;
            }

            if (pSize.Value != null && (bSuppressDirtyCheck || pSize.Dirty))
            {
                ddl.Append(", SIZE = ");
                AppendSize(sp, ddl, (double)pSize.Value);
                changeCount++;
            }
        }

        private void ScriptMaxSize(ScriptingPreferences sp, StringBuilder ddl,
                                bool bSuppressDirtyCheck, ref int changeCount)
        {
            var pMaxSize = Properties.Get("MaxSize");

            if (this.IsFileStreamBasedFile() && sp.TargetServerVersion < SqlServerVersion.Version110)
            {
                // The MaxSize property specification is not valid for FileStream based filegroups
                // in versions less than Denali.
                if (pMaxSize.Dirty)
                {
                    throw new SmoException(ExceptionTemplates.InvalidMaxSizeFileStream);
                }
                    
                return;
            }

            if (pMaxSize.Value != null && (bSuppressDirtyCheck || pMaxSize.Dirty))
            {
                ddl.Append(", MAXSIZE = ");
                if ((double)pMaxSize.Value == 0.0 || (double)pMaxSize.Value == -1.0)
                {
                    ddl.Append("UNLIMITED");
                }
                else
                {
                    AppendSize(sp, ddl, (double)pMaxSize.Value);
                }

                changeCount++;
            }
        }

        private void ScriptGrowth(ScriptingPreferences sp, StringBuilder ddl, bool bSuppressDirtyCheck,
                                    bool scriptCreate, ref int changeCount)
        {
            var pGrowthType = this.Properties.Get("GrowthType");
            var pGrowth = this.Properties.Get("Growth");

            if (this.IsFileStreamBasedFile())
            {
                if (pGrowth.Dirty || pGrowthType.Dirty)
                {
                    throw new SmoException(ExceptionTemplates.InvalidGrowthFileStream);
                }

                return;
            }

            // if the Growth type is None, this means the file is not supposed to grow
            if ((pGrowthType.Value == null || FileGrowthType.None == (FileGrowthType)pGrowthType.Value)
                && (bSuppressDirtyCheck || pGrowthType.Dirty))
            {
                ddl.Append(", FILEGROWTH = 0");
                changeCount++;
                // make sure growth is either set to 0 or not dirty
                if (pGrowth.Value != null &&
                    (bSuppressDirtyCheck || pGrowth.Dirty) &&
                    (double)pGrowth.Value != 0)
                {
                    throw new WrongPropertyValueException(pGrowth);
                }
                return;
            }

            var isGrowthPercent = (pGrowthType.Value != null &&
                                    FileGrowthType.Percent == (FileGrowthType)pGrowthType.Value);


            if (pGrowth.Value != null && (bSuppressDirtyCheck || pGrowth.Dirty || pGrowthType.Dirty))
            {
                ddl.AppendFormat(SmoApplication.DefaultCulture, ", FILEGROWTH = ");
                var growth = (double)pGrowth.Value;
                if (isGrowthPercent)
                {
                    if (1 > growth)
                    {
                        throw new SmoException(ExceptionTemplates.WrongPercentageGrowth);
                    }

                    ddl.AppendFormat(SmoApplication.DefaultCulture, "{0}%", Convert.ToString(pGrowth.Value, SmoApplication.DefaultCulture));
                }
                else
                {
                    AppendSize(sp, ddl, growth);
                }
                changeCount++;
            }
            else if (!scriptCreate && pGrowthType.Dirty)
            {
                // you can't modify the GrowthType without modifying Growth
                throw new SmoException(ExceptionTemplates.MustSpecifyGrowth);
            }
        }

        private bool ScriptFileName(ScriptingPreferences sp,StringBuilder ddl, bool bSuppressDirtyCheck,
                                    bool scriptCreate, ref int changeCount)
        {
            // FileName must not be missing
            var pFileName = Properties.Get("FileName");
            if (pFileName.Value != null && (bSuppressDirtyCheck || pFileName.Dirty))
            {
                var fileName = (string)pFileName.Value;

                ddl.AppendFormat(SmoApplication.DefaultCulture, ", FILENAME = N'{0}' ", SqlString(fileName));

                changeCount++;
            }
            else
            {
                // if filename missing, return false is we are scripting for creation
                if (pFileName.Value == null && scriptCreate)
                {
                    return false;
                }
            }

            return true;
        }

        // returns true if the file belongs to a file stream based filegroup
        // these are either file stream groups or file groups containing memory optimized data
        private bool IsFileStreamBasedFile()
        {
            var df = this as DataFile;
            if (df == null)
            {
                return false;
            }

            var parentfg = df.Parent;

            switch (parentfg.FileGroupType)
            {
                case FileGroupType.RowsFileGroup:
                    return false;

                case FileGroupType.FileStreamDataFileGroup:
                case FileGroupType.MemoryOptimizedDataFileGroup:
                    return true;

                default:
                    throw new SmoException(ExceptionTemplates.UnsupportedFileGroupType);
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        // generates alter script
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            // TODO make sure it does nothing when no property has been modified
            var statement = new StringBuilder();

            // for 7.0 we have to generate multiple alter statements
            // because this is how T-SQL works 
            if (ServerVersion.Major == 7)
            {
                statement.Length = 0;
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] MODIFY FILE ",
                                                                    SqlBraket(DatabaseName));
                if (ScriptDdl(sp, statement, false, false, "FileName"))
                {
                    alterQuery.Add(statement.ToString());
                }

                statement.Length = 0;
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] MODIFY FILE ",
                                                                    SqlBraket(DatabaseName));
                if (ScriptDdl(sp, statement, false, false, "Size"))
                {
                    alterQuery.Add(statement.ToString());
                }

                statement.Length = 0;
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] MODIFY FILE ",
                                                                    SqlBraket(DatabaseName));
                if (ScriptDdl(sp, statement, false, false, "MaxSize"))
                {
                    alterQuery.Add(statement.ToString());
                }

                statement.Length = 0;
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] MODIFY FILE ",
                                                                    SqlBraket(DatabaseName));
                if (ScriptDdl(sp, statement, false, false, "Growth"))
                {
                    alterQuery.Add(statement.ToString());
                }
            }
            else
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] MODIFY FILE ",
                                                                    SqlBraket(DatabaseName));
                if (ScriptDdl(sp, statement, false, false, null))
                {
                    alterQuery.Add(statement.ToString());
                }
            }

        }

        public void Rename(string newname)
        {
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            if (ServerVersion.Major == 7)
            {
                throw new SmoException(ExceptionTemplates.CannotRenameObject(this.GetType().Name, ServerVersion.ToString()));
            }

            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] MODIFY FILE (NAME=N'{1}', NEWNAME=N'{2}')",
                                             SqlBraket(DatabaseName),
                                             SqlString(this.Name), SqlString(newName)));
        }

        public void MarkForDrop(bool dropOnAlter)
        {
                base.MarkForDropImpl(dropOnAlter);
        }

        ///<summary>
        /// Shrinks a dbfile or a log file
        ///</summary>
        public void Shrink(Int32 newSizeInMB, ShrinkMethod shrinkType)
        {
            try
            {
                CheckObjectState();
                var query = new StringCollection();

                var statement = new StringBuilder();
                var urn = this.Urn;
                statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(urn.GetNameForType("Database")));
                query.Add(statement.ToString());

                statement.Remove(0, statement.Length);
                statement.Append("DBCC");
                var filename = this.Name;
                switch (shrinkType)
                {
                    case ShrinkMethod.Default:
                        statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.SHRINKFILE2, SqlString(filename), newSizeInMB);
                        break;
                    case ShrinkMethod.NoTruncate:
                        statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.SHRINKFILE2, SqlString(filename), "NOTRUNCATE");
                        break;
                    case ShrinkMethod.TruncateOnly:
                        statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.SHRINKFILE3, SqlString(filename), newSizeInMB, "TRUNCATEONLY");
                        break;
                    case ShrinkMethod.EmptyFile:
                        statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.SHRINKFILE2, SqlString(filename), "EMPTYFILE");
                        break;
                    default:
                        throw new ArgumentException(ExceptionTemplates.UnknownShrinkType);
                }
                query.Add(statement.ToString());

                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Shrink, this, e);
            }
        }
    }



    ///<summary>
    /// Represents a sql server database filegroup
    ///</summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class FileGroup : NamedSmoObject, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IRenamable, Cmn.IMarkForDrop
    {
        internal FileGroup(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state) 
        {
            m_Files = null;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "FileGroup";
            }
        }

        internal override void ValidateName(string name)
        {
            base.ValidateName(name);
            if (0 == name.Length)
            {
                throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(ExceptionTemplates.FileGroup));
            }
        }

        /// <summary>
        /// Constructor that takes in the filegroup type.
        /// 
        /// This constructor handles the different version issues:
        ///     - Katmai and before, only rows and partitions scheme file group types supported.
        ///     - KJ and Denali, file stream file group type added.
        ///     - Denali+, Memory optimized data file group type added.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="fileGroupType"></param>
        public FileGroup(Database database, string name, FileGroupType fileGroupType)
            : this(database, name)
        {
            this.FileGroupType = fileGroupType;
            this.IsFileStream = fileGroupType == FileGroupType.FileStreamDataFileGroup;
            if (IsSupportedProperty("AutogrowAllFiles"))
            {
                this.AutogrowAllFiles = false;
            }
        }

        private void InitializeDefaults()
        {
            this.FileGroupType = FileGroupType.RowsFileGroup;
            this.IsDefault = false;
            this.IsFileStream = false;
            if (IsSupportedProperty("AutogrowAllFiles"))
            {
                this.AutogrowAllFiles = false;
            }
        }

        /// <summary>
        /// Constructor that takes a Boolean value indicating if the FileGroup is of type FileStream or not.
        /// In case isFileStream is true, FileStreamDataFileGroup type will be used
        /// otherwise RowsFileGroup will be used
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="isFileStream"></param>
        /// <remarks>This constructor is kept for consistency, it's recommended to specify FileGroupType when creating a FileGroup</remarks>
        public FileGroup(Database database, string name, bool isFileStream)
            : this(database, name, (isFileStream) ? FileGroupType.FileStreamDataFileGroup : FileGroupType.RowsFileGroup)
        {
        }

        DataFileCollection m_Files;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(DataFile))]
        public DataFileCollection Files
        {
            get
            {
                CheckObjectState();
                if (null == m_Files)
                {
                    m_Files = new DataFileCollection(this);
                }
                return m_Files;
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();

            if (null != m_Files)
            {
                m_Files.MarkAllDropped();
            }
        }

        public void Rename(string newname)
        {
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            if (ServerVersion.Major == 7)
            {
                throw new SmoException(ExceptionTemplates.CannotRenameObject("FileGroup", ServerVersion.ToString()));
            }

            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] MODIFY FILEGROUP [{1}]  NAME = [{2}]",
                                             SqlBraket(ParentColl.ParentInstance.InternalName), SqlBraket(this.Name), SqlBraket(newName)));
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            var statement = new StringBuilder();
            var dbName = ParentColl.ParentInstance.InternalName;
            var filegroup = this.Name;

            // remember the number of properties modified
            var propMod = 0;

            var p = Properties.Get("ReadOnly");
            if (p.Dirty && p.Value != null)
            {
                var readOnly = (bool)p.Value;
                statement.Length = 0;
                var statementfmt = "declare @readonly bit{5}SELECT @readonly=convert(bit, (status & 0x08)) FROM sysfilegroups WHERE groupname=N'{0}'{5}if(@readonly={1}){5}	ALTER DATABASE [{2}] MODIFY FILEGROUP [{3}] {4}";

                statement.AppendFormat(SmoApplication.DefaultCulture, statementfmt, 
                                        SqlString(filegroup), // {0}
                                        readOnly ? 0 : 1, // {1}
                                        SqlBraket(dbName), // {2}
                                        SqlBraket(filegroup), // {3}
                                        readOnly ? "READONLY" : "READWRITE", // {4}
                                        sp.NewLine // {5}
                                        );

                alterQuery.Add(statement.ToString());
                propMod++;
            }

            if (this.IsSupportedProperty("AutogrowAllFiles", sp))
            {
                p = Properties.Get("AutogrowAllFiles");
                if (p.Dirty && p.Value != null)
                {
                    var autogrowAllFiles = (bool)p.Value;
                    statement.Length = 0;
                    var statementfmt = "declare @autogrow bit\r\nSELECT @autogrow=convert(bit, is_autogrow_all_files) FROM sys.filegroups WHERE name=N'{0}'\r\nif(@autogrow={1})\r\n	ALTER DATABASE [{2}] MODIFY FILEGROUP [{3}] {4}";
                    statement.AppendFormat(SmoApplication.DefaultCulture, statementfmt, SqlString(filegroup), autogrowAllFiles ? 0 : 1,
                                            SqlBraket(dbName), SqlBraket(filegroup), autogrowAllFiles ? "AUTOGROW_ALL_FILES" : "AUTOGROW_SINGLE_FILE");

                    alterQuery.Add(statement.ToString());
                    propMod++;
                }
            }

            p = Properties.Get("IsDefault");
            if (p.Dirty && p.Value != null)
            {
                if ((bool)p.Value)
                {
                    statement.Length = 0;

                    var statementfmt = "declare @isdefault bit{3}SELECT @isdefault=convert(bit, (status & 0x10)) FROM sysfilegroups WHERE groupname=N'{0}'{3}if(@isdefault=0){3}	ALTER DATABASE [{1}] MODIFY FILEGROUP [{2}] DEFAULT";
                    statement.AppendFormat(SmoApplication.DefaultCulture, statementfmt, SqlString(filegroup), SqlBraket(dbName), SqlBraket(filegroup), sp.NewLine);
                    //This property need to be set this database as current context unlike other properties need of use master
                    alterQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(dbName)));
                    alterQuery.Add(statement.ToString());
                    propMod++;
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.CantSetDefaultFalse);
                }
            }

        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            var bSuppressDirtyCheck = sp.SuppressDirtyCheck;

            var statement = new StringBuilder();
            var parentdb = (Database)ParentColl.ParentInstance;
            statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER DATABASE {0} ADD FILEGROUP [{1}]",
                                    parentdb.FormatFullNameForScripting(sp), SqlBraket(this.Name));

            switch (this.FileGroupType)
            {
                case FileGroupType.FileStreamDataFileGroup:
                    statement.AppendFormat(SmoApplication.DefaultCulture, " CONTAINS FILESTREAM ");
                    break;
                        
                case FileGroupType.MemoryOptimizedDataFileGroup:
                    statement.AppendFormat(SmoApplication.DefaultCulture, " CONTAINS MEMORY_OPTIMIZED_DATA ");
                    break;
            }

            createQuery.Add(statement.ToString());
        }

        ///<summary>
        /// Drop the filegroup and all the files in the filegroup.
        ///</summary>
        public void Drop()
        {
            base.DropImpl();
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            // first drop all the contained files
            // we make a roundtrip to the server, but otherwise we would have failed
            // We don't check if there are files to save roundtrips
            for (var i = 0; i < Files.Count; i++)
            {
                var f = Files[i];
                f.ScriptDropInternal(dropQuery, sp);
            }

            dropQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] REMOVE FILEGROUP [{1}]",
                                    SqlBraket(ParentColl.ParentInstance.InternalName), SqlBraket(this.Name)));
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        internal void ScriptDdl(ScriptingPreferences sp, StringBuilder ddl)
        {
            ScriptDdl(sp, ddl, false);
        }

        private void ScriptFileGroupFiles(ScriptingPreferences sp, StringBuilder ddl, bool databaseIsView)
        {
            var firstFileScripted = false;

            // script the primary file first
            foreach (var df in this.Files)
            {
                var isPrimary = df.GetPropValueOptional("IsPrimaryFile", false);
                if (isPrimary)
                {
                    ddl.Append(Globals.newline);
                    FileGroup.GetFileScriptWithCheck(sp, df, ddl, databaseIsView);
                    firstFileScripted= true;
                }
            }


            // script all others afterwards
            foreach (var df in this.Files)
            {
                var isPrimary = df.GetPropValueOptional("IsPrimaryFile", false);
                if (isPrimary)
                {
                    // we should have dealt with this
                    continue;
                }

                if (firstFileScripted)
                {
                    ddl.Append(Globals.comma + Globals.newline);
                }
                else
                {
                    ddl.Append(Globals.newline);
                }
                GetFileScriptWithCheck(sp, df, ddl, databaseIsView);
                firstFileScripted = true;
            }
        }

        private void ScriptPrimaryFileGroup(ScriptingPreferences sp, StringBuilder ddl, bool databaseIsView)
        {
            if (this.Files.Count == 0)
            {
                if (this.State == SqlSmoState.Existing)
                {
                    // if the database already exists and the Files collection
                    // if empty it means we did not have enough access rights 
                    // to read the metadata
                    throw new SmoException(ExceptionTemplates.NotEnoughRights);
                }
                else
                {
                    // the PRIMARY filegroup must have at least one file, if 
                    // it is specified. We can create other empty filegroups, 
                    // but not this one.
                    throw new SmoException(ExceptionTemplates.PrimaryFgMustHaveFiles);
                }
            }

            if (!databaseIsView)
            {
                ddl.Append(" PRIMARY ");
            }

            //Default not allowed for primary filegroup at time of creation of database as it is default automatically.
            if (Properties.Get("IsDefault").Value != null && this.Properties["IsDefault"].Dirty && this.IsDefault 
                && sp.TargetServerVersion >= SqlServerVersion.Version100)
            {
                throw new SmoException(ExceptionTemplates.PrimaryAlreadyDefault);
            }
        }

        /*
         * <filegroup> ::= 
         * {
         * FILEGROUP filegroup_name [ CONTAINS FILESTREAM ] [ DEFAULT ]
         *     <filespec> [ ,...n ]
         * }
         */
        internal void ScriptDdl(ScriptingPreferences sp, StringBuilder ddl, bool databaseIsView)
        {
            var fgName = this.Name;
            var pDefault = Properties.Get("IsDefault");

            if (string.Compare(fgName, "PRIMARY", StringComparison.Ordinal) == 0)
            {
                this.ScriptPrimaryFileGroup(sp, ddl, databaseIsView);
            }
            // check if you are creating a snapshot. If so, we will only script the file specification, otherwise, we will 
            // go into File group files scripting
            else if (!databaseIsView) // creating a database snapshot does not include all the complicated file stream options
            {
                var filegroupTypeScript = string.Empty;
                var filegroupDefaultScript = this.IsDefault ? " DEFAULT" : string.Empty;
                var filegroupNameScript = string.Format(SmoApplication.DefaultCulture, " FILEGROUP [{0}] ",
                                                    SqlBraket(fgName));

                switch (this.FileGroupType)
                {
                    case FileGroupType.FileStreamDataFileGroup:
                        filegroupTypeScript = "CONTAINS FILESTREAM ";
                        break;

                    case FileGroupType.MemoryOptimizedDataFileGroup:
                        filegroupTypeScript = "CONTAINS MEMORY_OPTIMIZED_DATA ";
                        break;
                }

                ddl.Append(filegroupNameScript + filegroupTypeScript + filegroupDefaultScript);
            }

            this.ScriptFileGroupFiles(sp, ddl, databaseIsView);
        }

        internal static void GetFileScriptWithCheck(ScriptingPreferences sp, DatabaseFile df, StringBuilder ddl, bool databaseIsView)
        {
            if (!df.ScriptDdl(sp, ddl, true, true, databaseIsView ? "FileName" : ""))
            {
                throw new SmoException(ExceptionTemplates.NoSqlGen(df.Urn.ToString()));
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            // this is a little tricky, so a comment will help
            // we are propagating the file creation if the file group itself is created, but not when the 
            // database is created
            // we are not propagating the drop when the filegroup is going to be dropped, since 
            // we need to drop all the files before dropping the file group anyway
            return new PropagateInfo[] { 
                new PropagateInfo(Files, (	(this.ParentColl.ParentInstance.State != SqlSmoState.Creating || 
                                        action != PropagateAction.Create) && 
                                        this.State != SqlSmoState.ToBeDropped))
                    };
        }


        public StringCollection CheckFileGroup()
        {
            try
            {
                CheckObjectState();
                var queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.InternalName)));
                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC CHECKFILEGROUP( N'{0}' ) WITH NO_INFOMSGS", SqlString(Name)));

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckFileGroup, this, e);
            }
        }

        public StringCollection CheckFileGroupDataOnly()
        {
            try
            {
                CheckObjectState();
                var queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.InternalName)));
                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC CHECKFILEGROUP( N'{0}', NOINDEX ) WITH NO_INFOMSGS", SqlString(Name)));

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckFileGroup, this, e);
            }
        }

        void AddObjects(String partialUrn, ArrayList list)
        {
            String dbUrn = this.ParentColl.ParentInstance.Urn;
            var term = string.Format(SmoApplication.DefaultCulture, "[@FileGroup='{0}']", Urn.EscapeString(this.Name));

            var dt = this.ExecutionManager.GetEnumeratorData(new Request(dbUrn + partialUrn + term, new string[] { "Urn" }));

            foreach (DataRow dr in dt.Rows)
            {
                list.Add((string)dr[0]);
            }
        }

        public SqlSmoObject[] EnumObjects()
        {
            CheckObjectState();

            var list = new ArrayList();
            // get the tables stored on the filegroup
            AddObjects("/Table", list);

            // get the table indexes stored on this filegroup
            AddObjects("/Table/Index", list);

            // get the view indexes stored on this filegroup
            AddObjects("/View/Index", list);

            // get the statistics stored on the filegroup
            AddObjects("/Table/Statistic", list);

            var retval = new SqlSmoObject[list.Count];
            var idx = 0;
            var srv = GetServerObject();
            foreach (var o in list)
            {
                retval[idx++] = srv.GetSmoObject((Urn)(string)o);
            }

            return retval;
        }
    }
}

