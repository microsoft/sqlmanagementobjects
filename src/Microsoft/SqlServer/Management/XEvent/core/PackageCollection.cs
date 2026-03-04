// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for Package
    /// </summary>
    public sealed class PackageCollection : SfcCollatedDictionaryCollection<Package, Package.Key, BaseXEStore>
    {
        private const string Pattern = @"\[(.+)\]\.(.+)";

        /// <summary>
        /// Initialize a new instance of PackageCollection given the XEStore.
        /// </summary>
        /// <param name="parent"></param>
        internal PackageCollection(BaseXEStore parent)
            : base(parent, parent.GetComparer())
        {
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.Package"/> 
        ///    by module id and name.
        /// </summary>
        /// <param name="moduleID"> module id of the package</param>
        /// <param name="name">name of the package</param>
        /// <returns>package with the specified module id and name</returns>
        public Package this[Guid moduleID, string name]
        {
            get
            {
                return this[new Package.Key(moduleID.ToString(), name)];
            }
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.Package"/> with the specified package GUID.
        /// </summary>
        /// <param name="packageID">GUID of the package</param>
        /// <returns>package with the specified GUID</returns>
        public Package this[Guid packageID]
        {
            get
            {
                foreach (Package pkg in this)
                {
                    if (pkg.ID.Equals(packageID))
                    {
                        return pkg;
                    }
                }
                return null;
            }
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.Package"/> by name.
        /// </summary>
        /// <param name="name">just the package name, without the module id</param>
        /// <returns>Package with the specified name, if the name is unique</returns>
        /// <exception cref="Microsoft.SqlServer.Management.XEvent.XEventException">if the package name is not unique</exception>
        public Package this[string name]
        {
            get
            {
                Package foundPkg = null;                             
                foreach (Package pkg in this)
                {
                    if (this.Parent.GetComparer().Compare(name, pkg.Name) == 0)
                    {
                        if (foundPkg != null) //if there is a duplicate
                        {
                            throw new XEventException(ExceptionTemplates.PackageNameNotUnique(name));
                        }
                        foundPkg = pkg; // set the pkg
                    }
                }

                //return the found package
                return foundPkg;
            }
        }


        /// <summary>
        /// Determines whether the collection contains Package.
        /// </summary>
        /// <param name="moduleID"> module id of the package</param>
        /// <param name="name">name of the package</param>
        /// <returns>
        /// 	<c>true</c> if the collection contains Package; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(Guid moduleID, string name)
        {
            return Contains(new Package.Key(moduleID.ToString(), name));
        }

        /// <summary>
        /// Determines whether the collection contains Package.
        /// </summary>
        /// <param name="name">just the package name, without the module id</param>
        /// <returns>
        /// 	<c>true</c> if the collection contains Package; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            return this.GetPackages(name).Count > 0;
        }

        /// <summary>
        /// Return the instance of object factory for Package.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return Package.GetObjectFactory();
        }

        /// <summary>
        ///  returns list of all packages with the package name matching the given name
        /// </summary>        
        /// <param name="name"></param>
        /// <returns></returns>
        internal ICollection<Package> GetPackages(string name)
        {
            //the helper getter methods in the XEStore.ObjectMetadata, introduced to handle the duplicate names,  split the name and call this method passing the package name part of the name; The expected behavior is to consider all the possible packages if the package name part is empty
            if (string.IsNullOrEmpty(name))
            {
                return this;
            }

            List<Package> pkgs = new List<Package>();
            foreach (Package pkg in this)
            {
                if (this.Parent.GetComparer().Compare(name, pkg.Name) == 0)
                {
                    pkgs.Add(pkg);
                }                                
            }

            return pkgs;
        }
    }

}
