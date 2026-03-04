// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// The partial definition of the ResumableIndex class.
    /// https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-index-resumable-operations
    /// https://docs.microsoft.com/en-us/sql/t-sql/statements/alter-index-transact-sql 
    /// https://docs.microsoft.com/en-us/sql/t-sql/statements/create-index-transact-sql 
    /// </summary>
    public partial class ResumableIndex : NamedSmoObject
    {
        /// <summary>
        /// Constructs ResumableIndex object.
        /// </summary>
        /// <param name="parentColl">Parent collection.</param>
        /// <param name="key">Object key.</param>
        /// <param name="state">Object state.</param>
        internal
        ResumableIndex(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Aborts the resumable index operation.
        /// </summary>
        public void Abort()
        {
            PauseOrAbort(isAbort: true);
        }

        /// <summary>
        /// Pauses the resumable index operation.
        /// </summary>
        public void Pause()
        {
            PauseOrAbort(isAbort: false);
        }

        /// <summary>
        /// Resumes the resumable index operation.
        /// </summary>
        public void Resume()
        {
            try
            {
                StringCollection queries = new StringCollection();

                ScriptingPreferences sp = new ScriptingPreferences(this);
                sp.ScriptForCreateDrop = true;

                this.GetContextDB().AddUseDb(queries, sp);
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER INDEX {0} {1} {2} RESUME",
                                      MakeSqlBraket(this.Name), Globals.On, Parent.FullQualifiedName);

                // Script the WITH clause for the resumable index if necessary.  If all of these are set to the defaults,
                // then there's no need to script these clauses out.
                // 
                if (ResumableMaxDuration != 0 || LowPriorityAbortAfterWait != AbortAfterWait.None || MaxDOP != 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " {0} (MAXDOP = {1}", Globals.With, MaxDOP);

                    if (ResumableMaxDuration != 0)
                    {
                        sb.Append(Globals.commaspace);
                        sb.AppendFormat(SmoApplication.DefaultCulture, "MAX_DURATION = {0} MINUTES", ResumableMaxDuration);
                    }

                    if (LowPriorityAbortAfterWait != AbortAfterWait.None)
                    {
                        var converter = new AbortAfterWaitConverter();

                        sb.Append(Globals.commaspace);
                        sb.AppendFormat(
                                SmoApplication.DefaultCulture,
                                "WAIT_AT_LOW_PRIORITY (MAX_DURATION = {0} MINUTES, ABORT_AFTER_WAIT = {1})",
                                LowPriorityMaxDuration,
                                converter.ConvertToInvariantString(LowPriorityAbortAfterWait));
                    }

                    sb.Append(Globals.RParen);
                }

                sb.Append(";");
                queries.Add(sb.ToString());
                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ResumeIndexes, this, e);
            }
        }
 
        /// <summary>
        /// Specifies the overall MAX_DURATION for the resumable operation.
        /// </summary>
        private int resumableMaxDuration = 0;

        /// <summary>
        /// Gets or sets the overall MAX_DURATION for the resumable operation.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public int ResumableMaxDuration
        {
            get
            {
                return resumableMaxDuration;
            }

            set
            {
                resumableMaxDuration = value;
            }
        }

        /// <summary>
        /// Specifies the max amount of time in minutes to wait at low priority.
        /// </summary>
        private int lowPriorityMaxDuration = 0;

        /// <summary>
        /// Gets or sets the MAX_DURATION for the WAIT_AT_LOW_PRIORITY option of the
        /// resumable index operation.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public int LowPriorityMaxDuration
        {
            get
            {
                return lowPriorityMaxDuration;
            }

            set
            {
                lowPriorityMaxDuration = value;
            }
        }

        /// <summary>
        /// Specifies the ABORT_AFTER_WAIT action for the WAIT_AT_LOW_PRIORITY option of the
        /// resumable index operation.
        /// </summary>
        private AbortAfterWait lowPriorityAbortAfterWait = AbortAfterWait.None;

        /// <summary>
        /// Gets or sets the ABORT_AFTER_WAIT action for the WAIT_AT_LOW_PRIORITY option of the
        /// resumable index operation.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public AbortAfterWait LowPriorityAbortAfterWait
        {
            get
            {
                return lowPriorityAbortAfterWait;
            }

            set
            {
                lowPriorityAbortAfterWait = value;
            }
        }

        /// <summary>
        /// Returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "ResumableIndex";
            }
        }

        /// <summary>
        /// Scripts and executes a pause/abort on the resumable index.
        /// </summary>
        /// <param name="isAbort"></param>
        private void PauseOrAbort(bool isAbort)
        {
            try
            {
                StringCollection queries = new StringCollection();

                ScriptingPreferences sp = new ScriptingPreferences(this);
                sp.ScriptForCreateDrop = true;

                this.GetContextDB().AddUseDb(queries, sp);

                // Generate the pause/abort script.
                //
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER INDEX {0} ON {1} {2}",
                    MakeSqlBraket(Name), Parent.FullQualifiedName, isAbort ? "ABORT" : "PAUSE");

                queries.Add(sb.ToString());

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                if (isAbort)
                {
                    throw new FailedOperationException(ExceptionTemplates.AbortIndexes, this, e);
                }
                else
                {
                    throw new FailedOperationException(ExceptionTemplates.PauseIndexes, this, e);
                }
            }
        }
    }
}