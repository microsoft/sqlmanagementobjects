// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;


namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// CPU Collection Class
    /// </summary>
    public sealed class CpuCollection : ICollection, IEnumerable
    {
        private AffinityInfoBase parent;
        NumaCPUCollectionBase<Cpu> cpuCollection;
        internal SortedList cpuCol;
        internal bool setByUser;
        ICollection iCol;
        internal CpuCollection(AffinityInfoBase parent)
        {
            this.parent = parent;
            cpuCollection = new NumaCPUCollectionBase<Cpu>(parent);
            iCol = cpuCollection as ICollection;
            cpuCol = cpuCollection.cpuNumaCol;
            setByUser = false;
        }
        /// <summary>
        /// Copies CPU's in collection to Array starting from index
        /// </summary>
        /// <param name="array">Array in which collection will be copied</param>
        /// <param name="index">Start index</param>
        public void CopyTo(Array array, Int32 index)
        {
            iCol.CopyTo(array, index);
        }

        /// <summary>
        /// Copies CPU's in collection to CPU Array starting from index
        /// </summary>
        /// <param name="array">Array in which collection will be copied</param>
        /// <param name="index">Start index</param>
        public void CopyTo(Cpu[] array, Int32 index)
        {
            iCol.CopyTo(array, index);
        }

        /// <summary>
        /// To get enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return cpuCollection.GetEnumerator();
        }

        /// <summary>
        /// Total number of elements in collection
        /// </summary>
        public Int32 Count
        {
            get
            {
                return iCol.Count;
            }
        }

        /// <summary>
        /// If collection is Synchronized
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return iCol.IsSynchronized;
            }
        }

        /// <summary>
        /// Returns SyncRoot of collection
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return iCol.SyncRoot;
            }
        }

        /// <summary>
        /// Gets a particular Cpu from its Index
        /// </summary>
        /// <param name="index">position of the Cpu in collection</param>
        /// <returns>Cpu object</returns>
        public Cpu this[Int32 index]
        {
            get
            {
                return cpuCollection[index];
            }
        }

        /// <summary>
        /// Gets a particular Cpu from its Index
        /// </summary>
        /// <param name="position">position of the Cpu in collection</param>
        /// <returns>Cpu object</returns>
        public Cpu GetElementAt(int position)
        {
            return this[position];
        }

        /// <summary>
        /// Gets a particulat Cpu from its ID
        /// </summary>
        /// <param name="cpuId">Id of the Cpu</param>
        /// <returns>Cpu object</returns>
        public Cpu GetByID(int cpuId)
        {
            Cpu cpu = null;
            CpuCollectionFromId.TryGetValue(cpuId,out cpu);
            return cpu;
        }

        /*Create internal dictionary for storing CPU Id and corresponding AffinityMask*/
        private Dictionary<int,Cpu> cpuCollectionFromId ;
        private Dictionary<int, Cpu> CpuCollectionFromId
        {
            get
            {
                if (cpuCollectionFromId == null)
                {
                    cpuCollectionFromId = new Dictionary<int, Cpu>();
                    for (int i = 0; i < this.Count; i++)
                    {
                        cpuCollectionFromId.Add(this[i].ID, this[i]);
                    }
                }
                return cpuCollectionFromId;
            }
        }

        private int maxCpuId = -1;
        private int MaxCpuId
        {
            get
            {
                if (maxCpuId == -1)
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        if (this[i].ID > maxCpuId)
                        {
                            maxCpuId = this[i].ID;
                        }
                    }
                }
                return maxCpuId;
            }
        }

        private int minCpuId = int.MaxValue; //Currently int is the data type supported in DMV
        private int MinCpuId
        {
            get
            {
                if (minCpuId == int.MaxValue)
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        if (this[i].ID < minCpuId)
                        {
                            minCpuId = this[i].ID;
                        }
                    }
                }
                return minCpuId;
            }
        }

        /// <summary>
        /// Sets Affinity to All Cpus
        /// </summary>
        public void SetAffinityToAll(bool affinityMask)
        {
            foreach (Cpu c in this)
            {
                c.AffinityMask = affinityMask;
            }
        }

        /// <summary>
        /// Will set Affinity to a Range of CPU's provided.This method will throw an exception if there is a Cpu Id does not exist
        /// </summary>
        /// <param name="startCpuId"></param>
        /// <param name="endCpuId"></param>
        /// <param name="affinityMask">value of affinity</param>
        public void SetAffinityToRange(int startCpuId, int endCpuId, bool affinityMask)
        {
            SetAffinityToRange(startCpuId, endCpuId, affinityMask, false);
        }

        /// <summary>
        /// Will set Affinity to a Range of CPU's provided
        /// </summary>
        /// <param name="startCpuId"></param>
        /// <param name="endCpuId"></param>
        /// <param name="affinityMask">value of affinity</param>
        /// <param name="ignoreMissingIds">Ignore missing Cpu Ids</param>
        public void SetAffinityToRange(int startCpuId, int endCpuId, bool affinityMask, bool ignoreMissingIds)
        {
            if ((startCpuId < MinCpuId) || (startCpuId > MaxCpuId))
            {
                throw new ArgumentOutOfRangeException("startCpuId");
            }
            if ((endCpuId < MinCpuId) || (endCpuId > MaxCpuId))
            {
                throw new ArgumentOutOfRangeException("endCpuId");
            }
            if (startCpuId > endCpuId)
            {
                throw new FailedOperationException(ExceptionTemplates.WrongIndexRangeProvidedCPU(startCpuId, endCpuId));
            }
            for (int i = startCpuId; i <= endCpuId; i++)
            {
                Cpu c;

                if (CpuCollectionFromId.TryGetValue(i,out c))
                {
                    c.AffinityMask = affinityMask;
                }
                else
                {
                    if (!ignoreMissingIds)
                    {
                        throw new FailedOperationException(ExceptionTemplates.HoleInIndexRangeProvidedCPU(i));
                    }
                }
            }
        }

        /// <summary>
        /// Will Return Affitinized CPU's
        /// </summary>
        public IEnumerable AffitinizedCPUs
        {
            get
            {
                for (int i = 0; i < cpuCol.Count; i++)
                {
                    if (this[i].AffinityMask == true)
                    {
                        yield return cpuCol[i];
                    }
                }
            }
        }

        internal StringCollection AddCpuInDdl(StringBuilder sb)
        {
            bool notFirstTime = false;
            int startPoint = 0;
            int endPoint = 0;
            bool isExternalResourcePoolAffinity = this.parent is ExternalResourcePoolAffinityInfo;

            sb.AppendFormat(SmoApplication.DefaultCulture, isExternalResourcePoolAffinity ? "CPU = (" : "CPU = ");

            /* for each cpu of it's affinity mask is true Add it to the query
             * if Affinity Type is AUTO then we have a conflict and thus throw and exception */
            for (int cpuCount = 0; cpuCount <= MaxCpuId; cpuCount++)
            {
                Cpu cpu;
                if (!CpuCollectionFromId.TryGetValue(cpuCount, out cpu) || !cpu.AffinityMask)
                {
                    continue;
                }
                startPoint = cpuCount;
                endPoint = startPoint;

                while (CpuCollectionFromId.TryGetValue(++cpuCount, out cpu) && cpu.AffinityMask)
                {
                    ; // Move CPU counter to next non affitinized CPU or a CPU hole
                }

                endPoint = --cpuCount; //As we excedeed the count by 1 , reducing the count.

                //If this is not the first  time add a comma {","}
                if (notFirstTime)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ",");
                }
                else
                {
                    notFirstTime = true;
                }
                //Decide if we need to add TO in the query or not
                if (startPoint != endPoint)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0} TO {1}", startPoint, endPoint);
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", startPoint);
                }
            }

            // If we moved till the end without finding Any affitinized CPU throw exception
            if (!notFirstTime)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.NoCPUAffinitized);
            }

            if (isExternalResourcePoolAffinity)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, ")");
            }

            StringCollection query = new StringCollection();
            query.Add(sb.ToString());
            return query;
        }             
    }

    /// <summary>
    /// NumaNode collection, class containg collection of all Numas
    /// </summary>
    public sealed class NumaNodeCollection :ICollection
    {
        private AffinityInfoBase parent;
        private NumaCPUCollectionBase<NumaNode> numaCollection;
        internal SortedList numaNodeCol;
        ICollection iCol;
        internal NumaNodeCollection(AffinityInfoBase parent)
        {
            this.parent = parent;
            this.numaCollection = new NumaCPUCollectionBase<NumaNode>(parent);
            iCol = numaCollection as ICollection;
            numaNodeCol = numaCollection.cpuNumaCol;
        }

        /// <summary>
        /// Copies NumaNodes's in collection to Array starting from index
        /// </summary>
        /// <param name="array">Array in which collection will be copied</param>
        /// <param name="index">Start index</param>
        public void CopyTo(Array array, Int32 index)
        {
            iCol.CopyTo(array, index);
        }

        /// <summary>
        /// Copies NumaNode's in collection to NumaNode Array starting from index
        /// </summary>
        /// <param name="array">Array in which collection will be copied</param>
        /// <param name="index">Start index</param>
        public void CopyTo(NumaNode[] array, Int32 index)
        {
            iCol.CopyTo(array, index);
        }

        /// <summary>
        /// To get enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return numaCollection.GetEnumerator();
        }

        /// <summary>
        /// Total number of elements in collection
        /// </summary>
        public Int32 Count
        {
            get
            {
                return iCol.Count;
            }
        }

        /// <summary>
        /// If collection is Synchronized
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return iCol.IsSynchronized;
            }
        }

        /// <summary>
        /// Returns SyncRoot of collection
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return iCol.SyncRoot;
            }
        }

        /// <summary>
        /// Gets a particular NumaNode from the collection on Index
        /// </summary>
        /// <param name="index">index of NumaNode</param>
        /// <returns>NumaNode object</returns>
        public NumaNode this[Int32 index]
        {
            get
            {
               return numaCollection[index];
            }
        }
        
        //Creates internal dictionary of NumaNodeCollection with Id as Key
        private Dictionary<int, NumaNode> numaCollectionFromId;
        private Dictionary<int, NumaNode> NumaCollectionFromId
        {
            get
            {
                if (numaCollectionFromId == null)
                {
                    numaCollectionFromId = new Dictionary<int, NumaNode>();
                    for (int i = 0; i < this.Count; i++)
                    {
                        numaCollectionFromId.Add(this[i].ID, this[i]);
                    }
                }
                return numaCollectionFromId;
            }
        }

        /// <summary>
        /// Gets maximum NumaNode Id
        /// </summary>
        private int maxNumaId = -1;
        private int MaxNumaId
        {
            get
            {
                if (maxNumaId == -1)
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        if (this[i].ID > maxNumaId)
                        {
                            maxNumaId = this[i].ID;
                        }
                    }
                }
                return maxNumaId;
            }
        }

        /// <summary>
        /// Gets maximum NumaNode Id
        /// </summary>
        private int minNumaId = int.MaxValue; //Currently int is the data type supported in DMV
        private int MinNumaId
        {
            get
            {
                if (minNumaId == int.MaxValue)
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        if (this[i].ID < minNumaId)
                        {
                            minNumaId = this[i].ID;
                        }
                    }
                }
                return minNumaId;
            }
        }

        /// <summary>
        /// Gets a particular NumaNode from the collection on Index
        /// </summary>
        /// <param name="position">index of NumaNode</param>
        /// <returns>NumaNode object</returns>
        public NumaNode GetElementAt(int position)
        {
            return this[position];
        }

        /// <summary>
        /// Gets a particular NumaNode from the collection on ID
        /// </summary>
        /// <param name="numanodeId">Id of NumaNode</param>
        /// <returns>NumaNodeObject</returns>
        public NumaNode GetByID(int numanodeId)
        {
            NumaNode nNode= null;
            NumaCollectionFromId.TryGetValue(numanodeId,out nNode);
            return nNode;
        }

        /// <summary>
        /// Will set Affinity to a Range of NumaNodes's provided.This method will throw an exception if a NumaNode does not exist in the Range
        /// </summary>
        /// <param name="startNumaNodeId"></param>
        /// <param name="endNumaNodeId"></param>
        /// <param name="affinityMask">value of affinity</param>
        public void SetAffinityToRange(int startNumaNodeId, int endNumaNodeId, NumaNodeAffinity affinityMask)
        {
            SetAffinityToRange(startNumaNodeId, endNumaNodeId, affinityMask, false);
        }
        /// <summary>
        /// Will set Affinity to a Range of NumaNodes's provided
        /// </summary>
        /// <param name="startNumaNodeId"></param>
        /// <param name="endNumaNodeId"></param>
        /// <param name="affinityMask">value of affinity</param>
        /// <param name="ignoreMissingIds">ignore the missing Id's in NumaNodes</param>
        public void SetAffinityToRange(int startNumaNodeId, int endNumaNodeId, NumaNodeAffinity affinityMask, bool ignoreMissingIds)
        {
            if ((startNumaNodeId < MinNumaId) || (startNumaNodeId > MaxNumaId))
            {
                throw new ArgumentOutOfRangeException("startNumaNodeId");
            }
            if ((endNumaNodeId < MinNumaId) || (endNumaNodeId > MaxNumaId))
            {
                throw new ArgumentOutOfRangeException("endNumaNodeId");
            }
            if (startNumaNodeId > endNumaNodeId)
            {
                throw new FailedOperationException(ExceptionTemplates.WrongIndexRangeProvidedNuma(startNumaNodeId, endNumaNodeId));
            }
            for (int i = startNumaNodeId; i <= endNumaNodeId; i++)
            {
                NumaNode nNode;
                if (NumaCollectionFromId.TryGetValue(i, out nNode))
                {
                    nNode.AffinityMask = affinityMask;
                }
                else
                {
                    if (!ignoreMissingIds)
                    {
                        throw new FailedOperationException(ExceptionTemplates.HoleInIndexRangeProvidedNumaNode(i));
                    }
                }

            }
        }

        //Adding Numa Script to Alter DDL with parenthesis option
        //this is needed to cover the syntax for RG pool affinities
        internal StringCollection AddNumaInDdl(StringBuilder stringBuilder)
        {
            int startPoint = 0;
            int endPoint = 0;
            bool notFirstTime = false;
            int i;
            NumaNode nNode;
            StringBuilder sb = new StringBuilder(stringBuilder.ToString());
            string str = sb.ToString(); //Local variable to keep current state
            bool isResourcePoolAffinity = this.parent is ResourcePoolAffinityInfo || this.parent is ExternalResourcePoolAffinityInfo;

            /* For each numaNode Add numanode to the query only if its fully affintinized
             * If Any numaNode is partially affitinized then break and try adding CPU query */
            for (i = 0; i <= MaxNumaId; i++)
            {
                // Until we find the first Numanode ( w.r.t Id ) 
                if (!NumaCollectionFromId.TryGetValue(i,out nNode))
                {
                    continue;
                }
                // If Fully affitinized
                if (nNode.AffinityMask == NumaNodeAffinity.Full) 
                {
                    startPoint = i;
                    endPoint = startPoint;

                    while (NumaCollectionFromId.TryGetValue(++i, out nNode) && nNode.AffinityMask == NumaNodeAffinity.Full)
                    {
                        ;  // Move the cursor till next non affitinized Numa or a Numanode hole
                    }

                    endPoint = --i; //As we exceeded i by 1 for Full affinitized numanodes , getting back the counter

                    //Donot Add comma {,} for the first time in the query
                    if (notFirstTime)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, ",");
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, isResourcePoolAffinity ? "NUMANODE = (" : "NUMANODE = ");
                        notFirstTime = true;
                    }

                    //Adding TO is the DDL if continuous affitinized numanodes are present else donot add.
                    if (startPoint != endPoint)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0} TO {1}", startPoint, endPoint);
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", startPoint);
                    }
                    
                }
                // if any NumaNode is Partially Affitinized break then we cannot generate NUMA scripts
                else if (0 == nNode.AffinityMask.CompareTo(NumaNodeAffinity.Partial))
                {
                    sb = new StringBuilder(str);
                    break;
                }
            }

            /*if we traversed through whole numa without any Partial numa this means
             * 1)There is nothing to script ,else we would have atleast one partial numa thus throw an exception  
             * 2)We successfully added one or more numa as there is no partial num and we should return */

            if (i == (MaxNumaId+1))
            {
                if (!notFirstTime)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.NoCPUAffinitized);
                }
                else
                {
                    // We will return a Numa script, check if we need to close the parenthesis
                    if (isResourcePoolAffinity)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, ")");
                    }

                    StringCollection query = new StringCollection();
                     query.Add(sb.ToString());
                    return query;
                }
            }

            return null;           
        }

        // returns true if any cpu in any numa node was set by the user
        internal bool IsManuallySet()
        {
            bool result = false;

            foreach (NumaNode nNode in this.numaCollection)
            {
                result = result || nNode.Cpus.setByUser;
            }

            return result;
        }
    }

    /// <summary>
    /// Scheduler collection, a class containg collection of all schedulers in the system and their affinity to the parent resource
    /// pool.
    /// </summary>
    public sealed class SchedulerCollection : ICollection
    {
        private AffinityInfoBase parent;
        private NumaCPUCollectionBase<Scheduler> schedulerCollection;
        internal SortedList schedulerList;
        ICollection iCol;
        internal bool setByUser;
        internal SchedulerCollection(AffinityInfoBase parent)
        {
            this.parent = parent;
            this.schedulerCollection = new NumaCPUCollectionBase<Scheduler>(parent);
            this.schedulerList = this.schedulerCollection.cpuNumaCol;
            iCol = this.schedulerCollection as ICollection;
        }

        /// <summary>
        /// Copies NumaNodes's in collection to Array starting from index
        /// </summary>
        /// <param name="array">Array in which collection will be copied</param>
        /// <param name="index">Start index</param>
        public void CopyTo(Array array, Int32 index)
        {
            iCol.CopyTo(array, index);
        }

        /// <summary>
        /// Copies NumaNode's in collection to NumaNode Array starting from index
        /// </summary>
        /// <param name="array">Array in which collection will be copied</param>
        /// <param name="index">Start index</param>
        public void CopyTo(NumaNode[] array, Int32 index)
        {
            iCol.CopyTo(array, index);
        }

        /// <summary>
        /// To get enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return this.schedulerCollection.GetEnumerator();
        }

        /// <summary>
        /// Total number of elements in collection
        /// </summary>
        public Int32 Count
        {
            get
            {
                return iCol.Count;
            }
        }

        /// <summary>
        /// If collection is Synchronized
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return iCol.IsSynchronized;
            }
        }

        /// <summary>
        /// Returns SyncRoot of collection
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return iCol.SyncRoot;
            }
        }

        /// <summary>
        /// Gets a particular NumaNode from the collection on Index
        /// </summary>
        /// <param name="index">index of NumaNode</param>
        /// <returns>NumaNode object</returns>
        public Scheduler this[Int32 index]
        {
            get
            {
                return this.schedulerCollection[index];
            }
        }

        internal StringCollection AddSchedulerInDdl(StringBuilder sb)
        {
            bool notFirstTime = false;
            int startPoint = 0;
            int endPoint = 0;

            sb.AppendFormat(SmoApplication.DefaultCulture, "SCHEDULER = (");

            /* for each cpu of it's affinity mask is true Add it to the query
             * if Affinity Type is AUTO then we have a conflict and thus throw and exception */
            for (int schedulerCount = 0; schedulerCount <= this.MaxSchedulerId; schedulerCount++)
            {
                Scheduler scheduler;
                if (!this.SchedulerCollectionFromId.TryGetValue(schedulerCount, out scheduler) || !scheduler.AffinityMask)
                {
                    continue;
                }
                startPoint = schedulerCount;
                endPoint = startPoint;

                while (this.SchedulerCollectionFromId.TryGetValue(++schedulerCount, out scheduler) && scheduler.AffinityMask)
                {
                    ; // Move counter to next non affitinized scheduler or a scheduler hole
                }

                endPoint = --schedulerCount; //As we excedeed the count by 1 , reducing the count.

                //If this is not the first  time add a comma {","}
                if (notFirstTime)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ",");
                }
                else
                {
                    notFirstTime = true;
                }
                //Decide if we need to add TO in the query or not
                if (startPoint != endPoint)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0} TO {1}", startPoint, endPoint);
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", startPoint);
                }
            }

            // If we moved till the end without finding Any affitinized CPU throw exception
            if (!notFirstTime)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.NoCPUAffinitized);
            }

            //add the closing prenthesis
            sb.Append(")");

            StringCollection query = new StringCollection();
            query.Add(sb.ToString());
            return query;
        }

        //Creates internal dictionary of NumaNodeCollection with Id as Key
        private Dictionary<int, Scheduler> schedulerCollectionFromId;
        private Dictionary<int, Scheduler> SchedulerCollectionFromId
        {
            get
            {
                if (this.schedulerCollectionFromId == null)
                {
                    this.schedulerCollectionFromId = new Dictionary<int, Scheduler>();
                    for (int i = 0; i < this.Count; i++)
                    {
                        this.schedulerCollectionFromId.Add(this[i].Id, this[i]);
                    }
                }
                return this.schedulerCollectionFromId;
            }
        }

        /// <summary>
        /// Gets maximum Scheduler Id
        /// </summary>
        private int maxSchedulerId = -1;
        private int MaxSchedulerId
        {
            get
            {
                if (this.maxSchedulerId == -1)
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        if (this[i].Id > this.maxSchedulerId)
                        {
                            this.maxSchedulerId = this[i].Id;
                        }
                    }
                }
                return this.maxSchedulerId;
            }
        }

        /// <summary>
        /// Gets minimum scheduler Id
        /// </summary>
        private int minSchedulerId = int.MaxValue; //Currently int is the data type supported in DMV
        private int MinSchedulerId
        {
            get
            {
                if (this.minSchedulerId == int.MaxValue)
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        if (this[i].Id < this.minSchedulerId)
                        {
                            this.minSchedulerId = this[i].Id;
                        }
                    }
                }
                return this.minSchedulerId;
            }
        }

        /// <summary>
        /// Gets a particular Scheduler from the collection on Index
        /// </summary>
        /// <param name="position">index of Schduler</param>
        /// <returns>Scheduler object</returns>
        public Scheduler GetElementAt(int position)
        {
            return this[position];
        }

        /// <summary>
        /// Gets a particular Schduler from the collection on ID
        /// </summary>
        /// <returns>SchedulerObject</returns>
        public Scheduler GetByID(int SchedulernodeId)
        {
            Scheduler nNode = null;
            SchedulerCollectionFromId.TryGetValue(SchedulernodeId, out nNode);
            return nNode;
        }

        /// <summary>
        /// Will set Affinity to a Range of SchdulerNodes's provided.This method will throw an exception if a SchdulerNode does not exist in the Range
        /// </summary>
        /// <param name="startIndex">start index</param>
        /// <param name="endIndex">end index</param>
        /// <param name="affinityMask">value of affinity</param>
        public void SetAffinityToRange(int startIndex, int endIndex, bool affinityMask)
        {
            if ((startIndex < this.MinSchedulerId) || (startIndex > this.MaxSchedulerId))
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if ((endIndex < this.MinSchedulerId) || (endIndex > this.MaxSchedulerId))
            {
                throw new ArgumentOutOfRangeException("endIndex");
            }
            if (startIndex > endIndex)
            {
                throw new FailedOperationException(ExceptionTemplates.WrongIndexRangeProvidedScheduler(startIndex, endIndex));
            }
            for (int i = startIndex; i <= endIndex; i++)
            {
                Scheduler s;
                if (this.SchedulerCollectionFromId.TryGetValue(i, out s))
                {
                    s.AffinityMask = affinityMask;
                    s.Cpu.AffinityMask = affinityMask; //set the corresponing CPU to the same affinity mask
                }
                else
                {
                    //Technically, we should never be here since scheduler always have contiguous ids
                    throw new FailedOperationException("Invalid Scheduler range with holes in it.");
                }
            }
        }
    }
    
    /// <summary>
    /// Internal generic class of NumaNode and CPU collection
    /// </summary>
    /// <typeparam name="T">CPU or NumaNode</typeparam>
    internal class NumaCPUCollectionBase<T> : ICollection
    {
        private AffinityInfoBase parent;
        internal NumaCPUCollectionBase(AffinityInfoBase parent)
        {
            this.parent = parent;
        }

        internal SortedList cpuNumaCol = new SortedList(new NumaCPUComparer());

        void ICollection.CopyTo(Array array, Int32 index)
        {
            if ((index < 0) || (index >= cpuNumaCol.Count))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            foreach (T c in this)
            {
                array.SetValue(c, index++);
            }
        }

        void CopyTo(T[] array, Int32 index)
        {
            if ((index < 0) || (index >= cpuNumaCol.Count))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            foreach (T c in this)
            {
                array.SetValue(c, index++);
            }
        }

        public IEnumerator GetEnumerator()
        {
            if (null == parent.AffinityInfoTable)
            {
                parent.PopulateDataTable();
            }

            return new NumaCPUEnumerator(cpuNumaCol);
        }

        Int32 ICollection.Count
        {
            get
            {
                if (null == parent.AffinityInfoTable)
                {
                    parent.PopulateDataTable();
                }

                return cpuNumaCol.Count;

            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                if (null == parent.AffinityInfoTable)
                {
                    parent.PopulateDataTable();
                }

                return parent.AffinityInfoTable.Rows.IsSynchronized;
            }

        }

        object ICollection.SyncRoot
        {
            get
            {
                if (null == parent.AffinityInfoTable)
                {
                    parent.PopulateDataTable();
                }

                return parent.AffinityInfoTable.Rows.SyncRoot;
            }
        }

        //Returing CPU or NumaNode based on index
        internal T this[Int32 index]
        {
            get
            {
                if (null == parent.AffinityInfoTable)
                {
                    parent.PopulateDataTable();
                }

                return (T)cpuNumaCol[index];
            }
        }

    }

    //Class implementing Icomparer to Compare to Numa or CPU objects
    internal class NumaCPUComparer : IComparer
    {
        int IComparer.Compare(Object object1, Object object2)
        {
            return ((int)object1 - (int)object2);
        }
    }

    ///<summary>
    /// nested enumerator class. It basically uses SortedList enumerations.
    ///</summary>
    internal class NumaCPUEnumerator : IEnumerator
    {
        private int idx;
        private SortedList col;

        internal NumaCPUEnumerator(SortedList col)
        {
            this.idx = -1;
            this.col = col;
        }

        object IEnumerator.Current
        {
            get
            {
                return col[idx];
            }
        }

        bool IEnumerator.MoveNext()
        {
            return ++idx < col.Count;
        }

        void IEnumerator.Reset()
        {
            idx = -1;
        }
    }
}



