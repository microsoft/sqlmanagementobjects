// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// This class basic building block of the scripts stored
    /// </summary>
    internal class ScriptFragment
    {
        private StringCollection script;
        private Exception ex;

        public ScriptFragment(StringCollection script)
        {
            if (script == null)
            {
                throw new ArgumentNullException("script");
            }
            this.script = script;
        }

        public ScriptFragment(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }
            this.ex = ex;
        }

        /// <summary>
        /// If error has been stored throw error otherwise script
        /// </summary>
        public StringCollection Script
        {
            get
            {
                if (ex == null)
                {
                    return script;
                }
                else
                {
                    throw ex;
                }
            }
        }
    }

    internal delegate void ScriptGenerator(StringCollection stringCollection, ScriptingPreferences sp);

    /// <summary>
    /// This class is collection of scriptfragments 
    /// </summary>
    abstract internal class ScriptContainer
    {
        /// <summary>
        /// Create DDL
        /// </summary>
        public ScriptFragment CreateScript { get; protected set; }

        /// <summary>
        /// Drop DDL
        /// </summary>
        public ScriptFragment DropScript { get; protected set; }

        /// <summary>
        /// USE [Database]
        /// </summary>
        public string DatabaseContext { get; protected set; }
    }

    /// <summary>
    /// Container corresponding to SqlSmoObject
    /// </summary>
    internal class ObjectScriptContainer : ScriptContainer
    {
        public ObjectScriptContainer(SqlSmoObject obj, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
        {
            Initialize(obj, sp, retryEvent);
        }

        /// <summary>
        /// Method to initialize the DDL fragments and to be overridden by base class for any extra initialization if necessary
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sp"></param>
        /// <param name="retryEvent"></param>
        protected virtual void Initialize(SqlSmoObject obj, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
        {
            // for drop or drop and create          
            if (sp.Behavior == ScriptBehavior.Drop || sp.Behavior == ScriptBehavior.DropAndCreate)
            {
                this.DropScript = GenerateScript(sp, new ScriptGenerator(obj.ScriptDropInternal), retryEvent, obj.Urn);
            }

            //for create or drop and create
            if (sp.Behavior == ScriptBehavior.Create || sp.Behavior == ScriptBehavior.DropAndCreate)
            {
                this.CreateScript = GenerateScript(sp, new ScriptGenerator((x, y) => { obj.ScriptCreateInternal(x, y, true); }), retryEvent, obj.Urn);
            }

            //for database context
            if (sp.IncludeScripts.DatabaseContext)
            {
                this.DatabaseContext = ScriptMaker.ScriptDatabaseContext(obj, false);
            }
        }

        protected ScriptFragment GenerateScript(ScriptingPreferences sp, ScriptGenerator scriptGenerator, RetryRequestedEventHandler retryEvent, Urn urn)
        {
            try
            {
                try
                {
                    StringCollection strcol = new StringCollection();
                    scriptGenerator(strcol, sp);
                    return new ScriptFragment(strcol);
                }
                catch (Exception e)
                {
                    if (e is OutOfMemoryException || retryEvent == null)
                    {
                        throw;
                    }

                    RetryRequestedEventArgs retryEventArgs = new RetryRequestedEventArgs(urn, (ScriptingPreferences)sp.Clone());
                    retryEvent(this, retryEventArgs);
                    if (retryEventArgs.ShouldRetry == true)
                    {
                        StringCollection strcol = new StringCollection();
                        scriptGenerator(strcol, retryEventArgs.ScriptingPreferences);
                        ScriptMaker.SurroundWithRetryTexts(strcol, retryEventArgs);
                        return new ScriptFragment(strcol);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ThrowException(sp, ex))
                {
                    throw;
                }
                return new ScriptFragment(ex);
            }
        }

        protected static bool ThrowException(ScriptingPreferences sp, Exception ex)
        {
            return (ex is OutOfMemoryException) || (!sp.ContinueOnScriptingError);
        }
    }

    /// <summary>
    /// Container corresponding to types whose IDs are required in scripting process mainly while ordering
    /// for table and view
    /// </summary>
    internal class IdBasedObjectScriptContainer : ObjectScriptContainer
    {
        public int ID { get; set; }

        public IdBasedObjectScriptContainer(SqlSmoObject obj, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
            : base(obj, sp, retryEvent)
        {

        }

        protected override void Initialize(SqlSmoObject obj, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
        {
            this.ID = (int)obj.Properties.GetValueWithNullReplacement("ID");
            base.Initialize(obj, sp, retryEvent);
        }
    }

    /// <summary>
    /// Table 's container
    /// </summary>
    internal class TableScriptContainer : IdBasedObjectScriptContainer
    {
        /// <summary>
        /// Insert Data scripts
        /// </summary>
        public IEnumerable<string> DataScript { get; private set; }

        /// <summary>
        /// rule and default binding script
        /// </summary>
        public ScriptFragment BindingsScript { get; protected set; }

        public TableScriptContainer(Table table, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
            : base(table, sp, retryEvent)
        {
        }

        protected override void Initialize(SqlSmoObject obj, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
        {
            if (sp.IncludeScripts.Data)
            {
                //script data except for drop
                ScriptData(obj, sp,retryEvent);

                if (!sp.IncludeScripts.Ddl)
                {
                    //script database context as base's initialization is not going to be called
                    if (sp.IncludeScripts.DatabaseContext)
                    {
                        this.DatabaseContext = ScriptMaker.ScriptDatabaseContext(obj, false);
                    }

                    // script drop data  
                    ScriptDropData(obj, sp);
                }
                else
                {
                    base.Initialize(obj, sp, retryEvent);
                }

                // script out the rule and default bindings
                if (sp.IncludeScripts.Ddl && sp.OldOptions.Bindings)
                {
                    this.BindingsScript = GenerateScript(sp, new ScriptGenerator(((Table)obj).ScriptBindings), retryEvent, obj.Urn);
                }
            }
            else
            {
                base.Initialize(obj, sp, retryEvent);
            }
        }

        private void ScriptData(SqlSmoObject obj, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
        {
            if (sp.Behavior != ScriptBehavior.Drop)
            {
                try
                {
                    try
                    {
                        this.DataScript = new DataScriptCollection(new DataEnumerator((Table)obj, sp));
                    }
                    catch (Exception e)
                    {
                        if (e is OutOfMemoryException || retryEvent == null)
                        {
                            throw;
                        }

                        RetryRequestedEventArgs retryEventArgs = new RetryRequestedEventArgs(obj.Urn, (ScriptingPreferences)sp.Clone());
                        retryEvent(this, retryEventArgs);
                        if (retryEventArgs.ShouldRetry == true)
                        {
                            this.DataScript = ScriptMaker.SurroundWithRetryTexts(
                                new DataScriptCollection(new DataEnumerator((Table)obj, retryEventArgs.ScriptingPreferences)),
                                retryEventArgs);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ThrowException(sp, ex))
                    {
                        throw;
                    }
                }
            }
        }

        private void ScriptDropData(SqlSmoObject obj, ScriptingPreferences sp)
        {
            if (sp.Behavior != ScriptBehavior.Create)
            {
                try
                {
                    StringCollection strcol = ((Table)obj).ScriptDropData(sp);
                    this.DropScript = new ScriptFragment(strcol);
                }
                catch (Exception ex)
                {
                    this.DropScript = new ScriptFragment(ex);
                    if (ThrowException(sp, ex))
                    {
                        throw;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Container for index
    /// </summary>
    internal class IndexScriptContainer : ObjectScriptContainer
    {
        private bool isMemoryOptimized = false;

        //all properties required for ordering like clustered,xml,indexkeytype etc.
        public IndexType IndexType { get; private set; }

        public IndexScriptContainer(Index index, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
            : base(index, sp, retryEvent)
        {
        }

        public bool IsMemoryOptimizedIndex
        {
            get
            {
                return this.isMemoryOptimized;
            }
        }
        
        protected override void Initialize(SqlSmoObject obj, ScriptingPreferences sp, RetryRequestedEventHandler retryEvent)
        {
            Index index = (Index)obj;
            //initialize the property used while ordering
            IndexType = index.InferredIndexType;
            
            if (index.IsSupportedProperty("IsMemoryOptimized", sp) && index.GetPropValueOptional("IsMemoryOptimized", false))
            {
                this.isMemoryOptimized = true;
            }

            base.Initialize(obj, sp, retryEvent);
        }
    }
}