// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class PhysicalPartitionCollection
    {
        /// <summary>
        /// Copy the content of given PhysicalPartition array to this collection object.
        /// Staring onward partitionNumberStart.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="partitionNumberStart"></param>
        public void CopyTo(PhysicalPartition[] array, Int32 partitionNumberStart)
        {
            if ((Int32.MaxValue < partitionNumberStart) || (partitionNumberStart < 1))
            {
                throw new SmoException(ExceptionTemplates.PartitionNumberStartOutOfRange(Int32.MaxValue));
            }
            ((ICollection)this).CopyTo(array, partitionNumberStart - 1);
        }

        /// <summary>
        /// Copy all content of the given physical partition array in current collection
        /// </summary>
        /// <param name="array"></param>
        public void CopyTo(PhysicalPartition[] array)
        {
            ((ICollection)this).CopyTo(array, 0);
        }

        /// <summary>
        /// Copy the content of the physical partition array for given range of the 
        /// partition numbers in current collection
        /// </summary>
        /// <param name="array"></param>
        /// <param name="partitionNumberStart"></param>
        /// <param name="partitionNumberEnd"></param>
        public void CopyTo(PhysicalPartition[] array, Int32 partitionNumberStart, Int32 partitionNumberEnd)
        {
            if (partitionNumberStart > partitionNumberEnd)
            {
                throw new SmoException(ExceptionTemplates.CannotCopyPartition(partitionNumberStart, partitionNumberEnd));
            }
            PhysicalPartition[] tempArray = new PhysicalPartition[InternalStorage.Count - partitionNumberStart + 1];
            ((ICollection)this).CopyTo(tempArray, partitionNumberStart - 1);
            for (int i = 0; i < partitionNumberEnd - partitionNumberStart + 1; i++)
            {
                array[i] = tempArray[i];
            }
        }

        private Database Database
        {
            get
            {
                if (this.Parent is Table)
                {
                    Table tbl = this.Parent as Table;
                    Debug.Assert(tbl.Parent is Database);
                    return tbl.Parent as Database;
                }
                else if (this.Parent is Index)
                {
                    Index indx = this.Parent as Index;
                    if (indx.Parent is Table)
                    {
                        Table tbl = indx.Parent as Table;
                        Debug.Assert(tbl.Parent is Database);
                        return tbl.Parent as Database;
                    }
                    else if (indx.Parent is View)
                    {
                        View vw = indx.Parent as View;
                        Debug.Assert(vw.Parent is Database);
                        return vw.Parent as Database;
                    }
                    else
                    {
                        Debug.Assert(false);
                        return null;
                    }
                }
                else
                {
                    //if you are getting this exception that implies you have extended the use of PhysicalPartition class
                    //beyond Table and Index classes. You need to modify the code accordingly here.
                    Debug.Assert(false);
                    return null;
                }
            }
        }

        internal void Reset()
        {
            this.Refresh();
        }

        private bool IsAppropriateForCompression()
        {
            if (this.Parent is UserDefinedTableType)
            {
                return false;
            }

            Index index = this.Parent as Index;

            if (index != null &&
                (index.IsMemoryOptimizedIndex || 
                index.HasXmlColumn(true) || 
                (index.ServerVersion.Major < 11 && index.HasSpatialColumn(true))))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
  
        /// <summary>
        /// Rebuild specific to partition will call this method after the rebuild method
        /// </summary>
        /// <param name="partitionNumber"></param>
        internal void Reset(int partitionNumber)
        {
            Diagnostics.TraceHelper.Assert(partitionNumber > 0);
            this[partitionNumber - 1].Refresh();
        }

        internal bool IsDataCompressionStateDirty(int partitionNumber)
        {
            Diagnostics.TraceHelper.Assert(partitionNumber > 0);
            if (!IsAppropriateForCompression())
            {
                return false;
            }
            return this[partitionNumber - 1].IsDirty("DataCompression");
        }

        internal string GetCompressionCode(int partitionNumber)
        {
            Diagnostics.TraceHelper.Assert(partitionNumber > 0);
            switch (this[partitionNumber - 1].DataCompression)
            {
                case DataCompressionType.None: return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = NONE ");
                case DataCompressionType.Row: return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = ROW ");
                case DataCompressionType.Page: return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = PAGE ");
                case DataCompressionType.ColumnStore: return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = COLUMNSTORE ");
                case DataCompressionType.ColumnStoreArchive: return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = COLUMNSTORE_ARCHIVE ");
                default: Debug.Assert(false, "Missing implementation for a new data compression type");
                    break;
            }
            return string.Empty;
        }

        internal bool IsCollectionDirty()
        {
            foreach (PhysicalPartition pp in this)
            {
                if (pp.IsDirty("DataCompression"))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool IsCompressionCodeRequired(bool isOnAlter)
        {
            if (!IsAppropriateForCompression())
            {
                return false;
            }

            if (0 == Count)
            {
                return false;
            }

            foreach (PhysicalPartition p in this)
            {
                if (DataCompressionType.None != p.DataCompression)
                {
                    return true;
                }
            }

            return isOnAlter; // If it's asked by alter method then have to generate in any case

        }

        internal bool IsXmlCompressionStateDirty(int partitionNumber)
         {
            Diagnostics.TraceHelper.Assert(partitionNumber > 0);
            return this[partitionNumber - 1].IsDirty("XmlCompression");
        }

        internal string GetXmlCompressionCode(int partitionNumber)
        {
            Diagnostics.TraceHelper.Assert(partitionNumber > 0);
            switch (this[partitionNumber - 1].XmlCompression)
            {
                case XmlCompressionType.Invalid: return string.Empty;
                case XmlCompressionType.Off: return "XML_COMPRESSION = OFF ";
                case XmlCompressionType.On: return "XML_COMPRESSION = ON ";
                default:
                    Debug.Assert(false, "Missing implementation for a new xml compression type");
                    break;
            }
            return string.Empty;
        }

        internal bool IsXmlCollectionDirty()
        {
            foreach (PhysicalPartition pp in this)
            {
                if (pp.IsDirty(nameof(pp.XmlCompression)))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool IsXmlCompressionCodeRequired(bool isOnAlter)
        {
            
            foreach (PhysicalPartition p in this)
            {
                if(!p.IsSupportedProperty(nameof(p.XmlCompression)))
                {
                    return false;
                }
                // If it's asked by alter method then have to generate in any case.
                // This behavior is added for xml compression to keep the behavior identical
                // with data_compression.
                if (isOnAlter)
                {
                    return true;
                }
                var prop = (p.State == SqlSmoState.Creating) ? 
                    p.Properties.Get(nameof(p.XmlCompression)) : 
                    p.Properties[nameof(p.XmlCompression)];
                if (null != prop.Value && (prop.Dirty || (XmlCompressionType)prop.Value == XmlCompressionType.On))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This method take the comma separated list of string (1,2,3,7,8,10,11,12) and
        /// reformat to engine acceptable short form (1 TO 3, 7, 8, 10 TO 12)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string ReformatCommaString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            char[] separators = { ',' };
            string[] elements = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            if (elements.Length == 0)
            {
                return string.Empty;
            }
            sb.Append(elements[0]);
            int firstValue = Int32.Parse(elements[0]);
            int prevValue = firstValue;
            int nextValue = firstValue;
            int maxCount = elements.Length;
            for (int i = 1; i < maxCount; i++)
            {
                nextValue = Int32.Parse(elements[i]);
                if (nextValue == prevValue + 1)
                {
                    prevValue = nextValue;
                    continue;
                }
                if (firstValue == prevValue)
                {
                    sb.Append(string.Format(", {0}", nextValue));
                }
                else if (firstValue == prevValue - 1)
                {
                    sb.Append(string.Format(", {0}, {1}", prevValue, nextValue));
                }
                else
                {
                    sb.Append(string.Format(" TO {0}, {1}", prevValue, nextValue));
                }
                firstValue = prevValue = nextValue;
            }
            if (firstValue != nextValue)
            {
                if (firstValue == prevValue)
                {
                    //don't need to do anything;
                }
                else if (firstValue == prevValue - 1)
                {
                    sb.Append(string.Format(", {0}", prevValue));
                }
                else
                {
                    sb.Append(string.Format(" TO {0}", prevValue));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// This method check the condition whether we can generate the code like
        /// DATA_COMPRESSION = ROW (without mentioning the partition information).
        /// Typically if all partitions are compressed with same type then we
        /// generate such code. In case if object is in Creating state then we can't
        /// be sure number of partition is equal to object in this collection so,
        /// we go ahead and generate the descriptive one. Exception is given to
        /// objects which has no partition scheme assigned. 
        ///                        PARTITION SCHEME
        ///                   Assigned    Not-Assigned
        ///               -----------------------------
        ///    Existing   |             |             |
        /// S             |      Y      |      Y      |
        /// T             |             |             |
        /// A             -----------------------------
        /// T             |             |             |
        /// E  Creating   |      N      |      Y      |
        ///               |             |             |
        ///               -----------------------------
        /// </summary>
        /// <returns></returns>
        private bool IsNonDescriptiveScriptAllowed()
        {
            if (Parent.State == SqlSmoState.Existing || !Parent.IsSupportedProperty("PartitionScheme"))
            {
                //For existing object I'm assuming collection is fully populated.
                //So, let count should decide the game
                return true;
            }
            Diagnostics.TraceHelper.Assert(Parent.State == SqlSmoState.Creating);

            //We don't know whether Table or index is parent.
            Table tbl = this.Parent as Table;
            if (null != tbl)
            {
                //We are in section where table is yet in creating state.
                //So, unless somebody change partition scheme can't be there
                //without dirty flag
                if (!tbl.IsDirty("PartitionScheme"))
                {
                    ValidatePhysicalPartitionObject(tbl.Name);
                    //Not dirty so table is not going to be partitioned.
                    //So, better to generate non descriptive script
                    return true;
                }
                //May be user had set some partition scheme but change his/her mind
                //later and assigned to null or empty.
                if (string.IsNullOrEmpty(tbl.PartitionScheme))
                {
                    ValidatePhysicalPartitionObject(tbl.Name);
                    return true;
                }
                //If you are here that means, your collection has table class as parent
                //and it has some partition scheme assigned.
                return false;
            }

            //Description for the following block code is similar to earlier
            Index indx = this.Parent as Index;
            if (null != indx)
            {
                if (!indx.IsDirty("PartitionScheme"))
                {
                    ValidatePhysicalPartitionObject(indx.Name);
                    return true;
                }
                if (string.IsNullOrEmpty(indx.PartitionScheme))
                {
                    ValidatePhysicalPartitionObject(indx.Name);
                    return true;
                }
            }

            //Scope for putting further intelligence here to verify the Count with PartionFunction 
            //boundaries values. If partition scheme and function exist on server and count matches 
            //with the partition number implied by the same then we can return true. During such
            //implementation one need to remove the default false return statment from the table block
            //and trap PartitionScheme name in variable which has scope at method level
            return false;
        }

        /// <summary>
        /// This method get called when there is no partition scheme has been assigned to the parent class. If in 
        /// such case we find user has assigned more than one object in the collection or an object has partition 
        /// number value greater than 1 then we through exception.
        /// </summary>
        private void ValidatePhysicalPartitionObject(string objectName)
        {
            if ((this.Count > 1) || ((this.Count == 1) && (this[0].PartitionNumber > 1)))
            {
                throw new FailedOperationException(ExceptionTemplates.PartitionSchemeNotAssignedError(objectName), this, null);
            }
        }

        /// <summary>
        /// This method generates the DATA_COMPRESSION clause for objects with 1 or more partitions.
        /// </summary>
        /// <param name="isOnAlter">True for ALTER statement, false for CREATE</param>
        /// <param name="isOnTable">True for ALTER/CREATE TABLE statement, false for INDEX
        /// Needed because columnstores have different default compression than rowstores</param>
        internal string GetCompressionCode(bool isOnAlter, bool isOnTable, ScriptingPreferences sp)
        {
            //If you are getting this assert failure that implies you are not checking at very early stage whether
            //caller has supressed the option to generate the DataCompression related script.
            Diagnostics.TraceHelper.Assert(sp.Storage.DataCompression);

            int rowCompressionCount = 0;
            int pageCompressionCount = 0;
            int colstoreCompressionCount = 0;
            int archivalCompressionCount = 0;
            int noneCompressionCount = 0;
            string rowCompressedList = string.Empty;
            string pageCompressedList = string.Empty;
            string noneCompressedList = string.Empty;
            string colstoreCompressedList = string.Empty;
            string archivalCompressedList = string.Empty;
            string retString = string.Empty;
            string commaString = Globals.commaspace;

            foreach (PhysicalPartition p in this)
            {
                switch (p.DataCompression)
                {
                    case DataCompressionType.None:
                        noneCompressedList += string.Format(SmoApplication.DefaultCulture, "{0}{1}", p.PartitionNumber, Globals.commaspace);
                        ++noneCompressionCount;
                        break;
                    case DataCompressionType.Row:
                        rowCompressedList += string.Format(SmoApplication.DefaultCulture, "{0}{1}", p.PartitionNumber, Globals.commaspace);
                        ++rowCompressionCount;
                        break;
                    case DataCompressionType.Page:
                        pageCompressedList += string.Format(SmoApplication.DefaultCulture, "{0}{1}", p.PartitionNumber, Globals.commaspace);
                        ++pageCompressionCount;
                        break;
                    case DataCompressionType.ColumnStore:
                        // Once a CCI is created, the base table will have the indexes compression type (COLUMNSTORE or COLUMNSTORE_ARCHIVE)
                        // However, these are not valid compression types when creating a table, though they can be used in alter table statements
                        // (if the table has a cci). So for create table statements, use NONE compression instead.
                        if (isOnTable && !isOnAlter)
                        {
                            noneCompressedList += string.Format(SmoApplication.DefaultCulture, "{0}{1}", p.PartitionNumber, Globals.commaspace);
                            ++noneCompressionCount;
                        }
                        else
                        {
                            colstoreCompressedList += string.Format(SmoApplication.DefaultCulture, "{0}{1}", p.PartitionNumber, Globals.commaspace);
                            ++colstoreCompressionCount;
                        }
                        break;
                    case DataCompressionType.ColumnStoreArchive:
                        // See comment in previous case
                        if (isOnTable && !isOnAlter)
                        {
                            noneCompressedList += string.Format(SmoApplication.DefaultCulture, "{0}{1}", p.PartitionNumber, Globals.commaspace);
                            ++noneCompressionCount;
                        }
                        else
                        {
                            archivalCompressedList += string.Format(SmoApplication.DefaultCulture, "{0}{1}", p.PartitionNumber, Globals.commaspace);
                            ++archivalCompressionCount;
                        }
                        break;
                    default:
                        Debug.Assert(false, "Missing implementation for a new data compression type");
                        break;
                }
            }

            if (!string.IsNullOrEmpty(rowCompressedList))
            {
                rowCompressedList = rowCompressedList.Trim(commaString.ToCharArray());
            }

            if (!string.IsNullOrEmpty(pageCompressedList))
            {
                pageCompressedList = pageCompressedList.Trim(commaString.ToCharArray());
            }

            if (!string.IsNullOrEmpty(noneCompressedList))
            {
                noneCompressedList = noneCompressedList.Trim(commaString.ToCharArray());
            }

            if (!string.IsNullOrEmpty(colstoreCompressedList))
            {
                colstoreCompressedList = colstoreCompressedList.Trim(commaString.ToCharArray());
            }

            if (!string.IsNullOrEmpty(archivalCompressedList))
            {
                archivalCompressedList = archivalCompressedList.Trim(commaString.ToCharArray());
            }

            rowCompressedList = ReformatCommaString(rowCompressedList);
            pageCompressedList = ReformatCommaString(pageCompressedList);
            noneCompressedList = ReformatCommaString(noneCompressedList);
            colstoreCompressedList = ReformatCommaString(colstoreCompressedList);
            archivalCompressedList = ReformatCommaString(archivalCompressedList);

            if (isOnAlter || pageCompressionCount > 0)
            {
                commaString = string.Format(SmoApplication.DefaultCulture, "{0}{1}", Globals.comma, sp.NewLine);
            }

            if (colstoreCompressionCount > 0)
            {
                if ((colstoreCompressionCount == this.Count))
                {
                    return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = COLUMNSTORE");
                }
                retString = string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = COLUMNSTORE ON PARTITIONS ({0})", colstoreCompressedList);
            }

            if (archivalCompressionCount > 0)
            {
                if ((archivalCompressionCount == this.Count) && IsNonDescriptiveScriptAllowed())
                {
                    return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = COLUMNSTORE_ARCHIVE");
                }
                if (!string.IsNullOrEmpty(retString))
                {
                    retString += commaString;
                }
                retString += string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = COLUMNSTORE_ARCHIVE ON PARTITIONS ({0})", archivalCompressedList);
            }

            if (rowCompressionCount > 0)
            {
                if ((rowCompressionCount == this.Count) && IsNonDescriptiveScriptAllowed())
                {
                    return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = ROW");
                }
                Diagnostics.TraceHelper.Assert(string.IsNullOrEmpty(retString));
                retString = string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = ROW ON PARTITIONS ({0})", rowCompressedList);
            }

            if (pageCompressionCount > 0)
            {
                if ((pageCompressionCount == this.Count) && IsNonDescriptiveScriptAllowed())
                {
                    return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = PAGE");
                }
                if (!string.IsNullOrEmpty(retString))
                {
                    retString += commaString;
                }
                retString += string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = PAGE ON PARTITIONS ({0})", pageCompressedList);
            }

            if (isOnAlter && (noneCompressionCount > 0))
            {
                if (noneCompressionCount == this.Count)
                {
                    return string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = NONE");
                }
                if (!string.IsNullOrEmpty(retString))
                {
                    retString += commaString;
                }
                retString += string.Format(SmoApplication.DefaultCulture, "DATA_COMPRESSION = NONE ON PARTITIONS ({0})", noneCompressedList);
            }

            return retString;
        }

        /// <summary>
        /// This method generates the XML_COMPRESSION clause for objects with 1 or more partitions.
        /// </summary>
        /// <param name="isOnAlter">True for ALTER statement, false for CREATE</param>
        /// <param name="isOnTable">True for ALTER/CREATE TABLE statement, false for INDEX
        internal string GetXmlCompressionCode(bool isOnAlter, bool isOnTable, ScriptingPreferences sp)
        {
            // If you are getting this assert failure that implies you are not checking at very early stage whether
            // caller has suppressed the option to generate the XmlCompression related script.
            Diagnostics.TraceHelper.Assert(sp.Storage.XmlCompression);

            int offXmlCompressionCount = 0;
            int onXmlCompressionCount = 0;
			string offXmlCompressedList = string.Empty;
            string onXmlCompressedList = string.Empty;
            string retString = string.Empty;
            string commaString = Globals.commaspace;

            foreach (PhysicalPartition p in this)
            {
                switch (p.XmlCompression)
                {
                    case XmlCompressionType.Invalid: continue;
                    case XmlCompressionType.Off:
						offXmlCompressedList += FormattableString.Invariant($"{p.PartitionNumber}{Globals.commaspace}");
						++offXmlCompressionCount;
                        break;
                    case XmlCompressionType.On:
                        onXmlCompressedList += FormattableString.Invariant($"{p.PartitionNumber}{Globals.commaspace}");
                        ++onXmlCompressionCount;
                        break;
                    default:
                        Debug.Assert(false, "Missing implementation for a new xml compression type");
                        break;
                }
            }

            if (!string.IsNullOrEmpty(offXmlCompressedList))
            {
                offXmlCompressedList = offXmlCompressedList.Trim(commaString.ToCharArray());
            }

            if (!string.IsNullOrEmpty(onXmlCompressedList))
            {
                onXmlCompressedList = onXmlCompressedList.Trim(commaString.ToCharArray());
            }

            offXmlCompressedList = ReformatCommaString(offXmlCompressedList);
            onXmlCompressedList = ReformatCommaString(onXmlCompressedList);

            if (isOnAlter || onXmlCompressionCount > 0)
            {
                commaString = $"{SmoApplication.DefaultCulture}{Globals.comma}{sp.NewLine}";
            }

            if (onXmlCompressionCount > 0)
            {
                if (onXmlCompressionCount == this.Count && IsNonDescriptiveScriptAllowed())
                {
                    return string.Format(SmoApplication.DefaultCulture, "XML_COMPRESSION = ON");
                }

                if (!string.IsNullOrEmpty(retString))
                {
                    retString += commaString;
                }
                retString += $" XML_COMPRESSION = ON ON PARTITIONS ({onXmlCompressedList})";
            }

            if (isOnAlter && (offXmlCompressionCount > 0))
            {
                if (offXmlCompressionCount == this.Count)
                {
                    return "XML_COMPRESSION = OFF";
                }

                if (!string.IsNullOrEmpty(retString))
                {
                    retString += commaString;
                }
                retString += $" XML_COMPRESSION = OFF ON PARTITIONS ({offXmlCompressedList})";
            }

            return retString;
        }
    }
}
