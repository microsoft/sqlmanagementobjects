// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;


namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    /// <summary>
    /// Possible specialized PhysicalFacet types
    /// </summary>
    [Flags]
    public enum PhysicalFacetOptions
    {
        /// No options
        None = 0,
        /// Read-only facet
        ReadOnly = 1,
    }

    [AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PhysicalFacetAttribute : System.Attribute
    {
        bool isReadOnly;

        /// <summary>
        /// Indicates whether the facet is read-only
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return isReadOnly;
            }
        }

        /// <summary>
        /// Creates a PhysicalFacet with no options selected
        /// </summary>
        public PhysicalFacetAttribute() 
        { 
            this.isReadOnly = false; 
        }

        /// <summary>
        /// Creates a PhysicalFacet with the options specified in the constructor
        /// </summary>
        /// <param name="options"></param>
        public PhysicalFacetAttribute(PhysicalFacetOptions options)
        {
            this.isReadOnly = (PhysicalFacetOptions.None != (options & PhysicalFacetOptions.ReadOnly));
        }
    }

    [AttributeUsage (AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DmfIgnorePropertyAttribute : System.Attribute
    {
    }

    /// <summary>
    /// Custom attribute that identifies root facets 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class RootFacetAttribute : System.Attribute
    {
        Type rootType;
        /// <summary>
        /// Returns rootType originally specified in ctor
        /// </summary>
        public Type RootType { get { return this.rootType; } }
        /// <summary>
        /// Ctor--takes type of root as parameter
        /// </summary>
        /// <param name="rootType"></param>
        public RootFacetAttribute(Type rootType) { this.rootType = rootType; }
    }

    /// <summary>
    /// Base Facet interface - indicates inheriting interface is a Facet
    /// </summary>
    public interface IDmfFacet { }
}
