// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Affinity type of Cpu Auto or Manual
    /// </summary>
    public enum AffinityType
    {
        Auto,
        Manual
    };

    /// <summary>
    ///AffinityInfoBase 
    /// </summary>
    public abstract class AffinityInfoBase : Cmn.IAlterable, IScriptable
    {
        /// <summary>
        /// this property will return AffinityInfoTable which is populated from the query directly
        /// </summary>
        internal DataTable table = null;
        internal DataTable AffinityInfoTable
        {
            get
            {
                if (null == table)
                {
                    PopulateDataTable();
                }
                return table;
            }
        }

        /// <summary>
        /// Getting NumaCollection Instance
        /// </summary>
        private NumaNodeCollection numaCol;
        public NumaNodeCollection NumaNodes
        {
            get
            {
                if (null == table)
                {
                    PopulateDataTable();
                }
                if (null == numaCol)
                {
                    numaCol = new NumaNodeCollection(this);
                }
                return numaCol;
            }
        }

        AffinityType affinityType;
        /// <summary>
        /// Sets or gets AffinityType 
        /// </summary>
        public AffinityType AffinityType
        {
            get
            {
                if (null == table)
                {
                    PopulateDataTable();
                }
                return affinityType;
            }
            set
            {
                if (null == table)
                {
                    PopulateDataTable();
                }
                affinityType = value;
            }
        }

        internal abstract SqlSmoObject SmoParent
        {
            get;
        }

        //Will internally populate the DataTable 
        internal abstract void PopulateDataTable();

        /// <summary>
        /// Refresh the AffinityInfoBase class along with CPU collection and Numa Collection
        /// </summary>
        public virtual void Refresh()
        {
            table = null;
            numaCol = null;
        }

        //This will create DDL script for alter
        internal abstract StringCollection DoAlter(ScriptingPreferences sp);

        public ExecutionManager ExecutionManager
        {
            get
            {
                return this.SmoParent.ExecutionManager;
            }
        }

        /// <summary>
        /// Will Generate Alter Statment for Server to Set CPU and Numa Configuration 
        /// </summary>
        /// <param name="forcedAuto">If Set to true will generate script to set CPU's to Auto , provided AffinityType is set to AUTO</param>
        public void Alter()
        {
            StringCollection sc = new StringCollection();
            ScriptingPreferences sp = GetCurrentServerScriptingPreferences();

            try
            {
                sc = DoAlter(sp);
                if (sc != null)
                {
                    this.ExecutionManager.ExecuteNonQuery(sc);
                }
                if (!this.ExecutionManager.Recording)
                {
                    this.Refresh();
                }
            }

            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Alter, this, e);
            }
        }

        /// <summary>
        /// Will give script for Cpu And Numa Configuration
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return Script(GetCurrentServerScriptingPreferences());
        }

        internal StringCollection Script(ScriptingPreferences sp)
        {
            return DoAlter(sp);
        }

        /// <summary>
        /// Will give script for Cpu And Numa Configuration
        /// </summary>
        /// <param name="so">Scripting Optios</param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions so)
        {
            return DoAlter(so.GetScriptingPreferences());
        }

        private ScriptingPreferences GetCurrentServerScriptingPreferences()
        {
            ScriptingPreferences sp = new ScriptingPreferences();
            sp.SetTargetServerInfo(this.SmoParent);
            return sp;
        }
    }
}

