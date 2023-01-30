// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// XEStore is the root for all metadata classes and runtime classes.
    /// </summary>
    public abstract partial class BaseXEStore
    {
        private ObjectMetadata objectInfoSet;

        /// <summary>
        /// Gets <see cref="Microsoft.SqlServer.Management.XEvent.BaseXEStore.ObjectMetadata"/> for the Store.
        /// </summary>
        public ObjectMetadata ObjectInfoSet
        {
            get
            {
                if (this.objectInfoSet == null)
                {
                    this.objectInfoSet = new ObjectMetadata(this);
                }

                return this.objectInfoSet;
            }
        }

        /// <summary>
        /// Provides helpers methods over the metadata hierarchy objects        
        /// </summary>
        public class ObjectMetadata
        {
            private BaseXEStore store;

            /// <summary>
            /// Initializes a new instance of the <see cref="ObjectMetadata"/> class.
            /// </summary>
            /// <param name="store">A store - source of metadata.</param>
            public ObjectMetadata(BaseXEStore store)
            {
                this.store = store;
            }

            /// <summary>
            ///  Returns a collection of all the <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given name.
            /// </summary>
            /// <typeparam name="T">Type of object to enumerate.</typeparam>
            /// <param name="name">format: package_name.object_name; NB: the first part of the name is optional</param>
            /// <exception cref="System.ArgumentNullException">if the name provided is null.</exception>
            /// <returns>A collection of all the <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given name.</returns>
            public ICollection<T> GetAll<T>(string name) where T : SfcInstance, IXEObjectInfo
            {
                string[] parts = this.ValidateAndSplitName(name);
                return this.GetAll<T>(parts[0], parts[1]);
            }

            /// <summary>
            /// Returns a collection of the <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given package name, object name.
            /// </summary>
            /// <typeparam name="T">Type of object to enumerate.</typeparam>
            /// <param name="pkgName">Package name.</param>
            /// <param name="objName">Object name.</param>
            /// <returns>A collection of the <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given package name, object name.</returns>
            public ICollection<T> GetAll<T>(string pkgName, string objName) where T : SfcInstance, IXEObjectInfo
            {
                List<T> list = new List<T>();
                foreach (Package pkg in this.store.Packages.GetPackages(pkgName))
                {
                    T t = pkg.GetChildCollection<T>()[objName];
                    if (t != null)
                    {
                        list.Add(t);
                    }
                }

                return list;
            }

            /// <summary>
            ///  Returns <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given name, if it exists and unique.
            /// </summary>
            /// <typeparam name="T">Type of object to get.</typeparam>
            /// <param name="name">format: [module_guid].package_name.object_name; NB: the first two parts of the name are optional</param>
            /// <exception cref="Microsoft.SqlServer.Management.XEvent.XEventException">if the object does not exist, or if the object name is not unique</exception>
            /// <returns><see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given name.</returns>
            public T Get<T>(string name) where T : SfcInstance, IXEObjectInfo
            {
                string[] parts = this.ValidateAndSplitName(name);

                // if the name begins with the module id
                if (name.StartsWith("[", StringComparison.Ordinal))
                {
                    return this.Get<T>(new Guid(parts[0].Substring(1, parts[0].Length - 2)), parts[1]);
                }

                // name = (packageName, objectName)
                return this.Get<T>(parts[0], parts[1]);
            }

            /// <summary>
            ///  Returns <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given package name and object name, if it exists and unique.
            /// </summary>
            /// <typeparam name="T">Type of object to get.</typeparam>
            /// <param name="pkgName">Package name.</param>
            /// <param name="objName">Object name.</param>
            /// <exception cref="Microsoft.SqlServer.Management.XEvent.XEventException">if the object does not exist, or if the object name is not unique</exception>
            /// <returns><see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given package name and object name.</returns>
            internal T Get<T>(string pkgName, string objName) where T : SfcInstance, IXEObjectInfo
            {
                ICollection<T> objs = this.GetAll<T>(pkgName, objName);

                IEnumerator<T> objEnum = objs.GetEnumerator();

                if (!objEnum.MoveNext()) 
                {
                    // the object does not exist
                    // check if the package does not exist.
                    if (!this.store.Packages.Contains(pkgName))
                    {
                        throw new XEventException(ExceptionTemplates.PackageNotExist(pkgName));
                    }

                    throw new XEventException(ExceptionTemplates.ObjectNotExist(pkgName + "." + objName));
                }

                if (objs.Count > 1) 
                {
                    // the object name is not unique
                    throw new XEventException(ExceptionTemplates.ObjectNameNotUnique(pkgName + "." + objName));
                }

                return objEnum.Current;
            }

            /// <summary>
            ///  Returns <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given module id and name, if it exists and unique.
            /// </summary>
            /// <typeparam name="T">Type of object to get.</typeparam>
            /// <param name="moduleID">Module ID.</param>
            /// <param name="name">Format: package_name.object_name; both parts must be specified.</param>
            /// <exception cref="Microsoft.SqlServer.Management.XEvent.XEventException">if the object does not exist, or if the object name is not unique.</exception>
            /// <returns><see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given module id and name.</returns>
            internal T Get<T>(Guid moduleID, string name) where T : SfcInstance, IXEObjectInfo
            {
                string[] parts = this.ValidateAndSplitName(name);
                return this.Get<T>(moduleID, parts[0], parts[1]);
            }

            /// <summary>
            ///  Returns <see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given module id, package name and object name, if it exists and unique.
            /// </summary>
            /// <typeparam name="T">Type of object to get.</typeparam>
            /// <param name="moduleID">Module ID.</param>
            /// <param name="pkgName">Package name.</param>
            /// <param name="objName">Object name.</param>
            /// <exception cref="Microsoft.SqlServer.Management.XEvent.XEventException">if the object does not exist, or if the object name is not unique.</exception>
            /// <returns><see cref="Microsoft.SqlServer.Management.XEvent.IXEObjectInfo"/> matching the given module id, package name and object name.</returns>
            internal T Get<T>(Guid moduleID, string pkgName, string objName) where T : SfcInstance, IXEObjectInfo
            {
                Package pkg = this.store.Packages[moduleID, pkgName];

                // check if the package does not exist.
                if (pkg == null)
                {
                    throw new XEventException(
                        ExceptionTemplates.PackageNotExist(string.Format(CultureInfo.InstalledUICulture, "[{0}].{1}", moduleID, pkgName)));
                }

                T t = pkg.GetChildCollection<T>()[objName];

                // check if the obj does not exist.
                if (t == null)
                {
                    throw new XEventException(
                        ExceptionTemplates.ObjectNotExist(string.Format(CultureInfo.InstalledUICulture, "[{0}].{1}.{2}", moduleID, pkgName, objName)));
                }

                return t;
            }

            private string[] ValidateAndSplitName(string name)
            {
                using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("SplitName"))
                {
                    // check if the name is null since Split is called on it.
                    if (null == name)
                    {
                        tm.TraceError("the object name is null.");
                        throw new ArgumentNullException("name");
                    }

                    // split the name and get package name and object name;
                    string packageName = null;
                    string objectName = null;

                    string[] parts = name.Split(new char[] { '.' }, 2);

                    switch (parts.Length)
                    {
                        case 1: // the name is specified in form of just objectName                            
                            packageName = string.Empty;
                            objectName = parts[0];
                            break;
                        case 2: // the name is specified in form of packageName.objectName                            
                            packageName = parts[0];
                            objectName = parts[1];
                            break;
                        default:
                            tm.Assert(false, "unexpected number of parts");
                            break;
                    }

                    return new string[] { packageName, objectName };
                }
            }
        }
    }
}
