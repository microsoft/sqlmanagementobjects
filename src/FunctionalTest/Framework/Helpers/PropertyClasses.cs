// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Holds information about a column
    /// </summary>
    public class ColumnProperties
    {        
        public ColumnProperties(string name) : this(name, DataType.Int)
        {
            
        }

        public ColumnProperties(string name, DataType dataType)
        {
            Name = name;
            Type = dataType.SqlDataType;
            SmoDataType = dataType;
        }
        public readonly string Name;
        public readonly SqlDataType Type;
        public readonly DataType SmoDataType;
        public bool Nullable = true;
        public bool Identity = false;
        public override string ToString()
        {
            return $"{Name}-{SmoDataType}-";
        }
    }

    /// <summary>
    /// Small helper class to encapsulate the various properties used to create an index
    /// </summary>
    public class IndexProperties
    {
        public IndexType IndexType = IndexType.ClusteredIndex;
        /// <summary>
        /// The Key Type of the index, default None
        /// </summary>
        public IndexKeyType KeyType = IndexKeyType.None;
        /// <summary>
        /// The columns to include in this index
        /// </summary>
        public Column[] Columns = null;
        /// <summary>
        /// Whether the index is clustered or not
        /// </summary>
        public bool IsClustered = true;
        /// <summary>
        /// Whether the index is unique or not
        /// </summary>
        public bool IsUnique = false;

        /// <summary>
        /// Whether the index is online or not
        /// </summary>
        public bool OnlineIndexOperation = false;

        /// <summary>
        /// The name of the index, cannot be empty.
        /// </summary>
        public string Name = String.Empty;

        /// <summary>
        /// Additional column names to include while creating this index.
        /// </summary>
        public string[] ColumnNames = null;

        /// <summary>
        /// Whether we should create the index in a resumable fashion or not.
        /// </summary>
        public bool Resumable = false;
    }

    /// <summary>
    /// Class defining table properties used when creating tables through the 'DatabaseObjectHelper' class.
    /// </summary>
    public class TableProperties
    {
        #region Properties

        /// <summary>
        /// Gets or sets a property determining if the table should be created as a node table.
        /// </summary>
        public bool IsNode { get; set; }

        /// <summary>
        /// Gets or sets a property determining if the table should be created as an edge table.
        /// </summary>
        public bool IsEdge { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// This method applies the set properties of this object to the table object.
        /// </summary>
        /// <param name="table">The table to set the properties on.</param>
        public void ApplyProperties(Table table)
        {
            if (table == null)
            {
                throw new ArgumentException("table");
            }

            if (IsEdge)
            {
                table.IsEdge = true;
            }

            if (IsNode)
            {
                table.IsNode = true;
            }
        }

        #endregion
    }
}
