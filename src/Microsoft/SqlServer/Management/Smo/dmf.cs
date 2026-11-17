// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public class SmoDmfAdapter
    {
        static readonly PropertyInfo SmoSchemaPropertyInfo = typeof (ScriptSchemaObjectBase).GetProperty ("Schema");
        static readonly PropertyInfo SmoNamePropertyInfo = typeof (NamedSmoObject).GetProperty ("Name");

        internal static PropertyInfo[] GetTypeProperties (Type type)
        {
            PropertyInfo pi = null;

            List<PropertyInfo> propertyList = new List<PropertyInfo> ();

            Type pmp = type.GetNestedType ("PropertyMetadataProvider", BindingFlags.NonPublic);
            if (null != pmp)
            {
                Microsoft.SqlServer.Management.Smo.StaticMetadata[] md =
                    (Microsoft.SqlServer.Management.Smo.StaticMetadata[])
                    pmp.GetField ("staticMetadata", BindingFlags.NonPublic | BindingFlags.Static).GetValue (null);

                foreach (Microsoft.SqlServer.Management.Smo.StaticMetadata smd in md)
                {
                    if (smd.Name == "PolicyHealthState")
                    {
                        //TODO: This will be handled through DmfPropertyIgnore attribute
                        //
                        // do not surface PolicyHealthState as a property
                        // of physical facets
                        continue;
                    }

                    pi = type.GetProperty (smd.Name);
                    if (null != pi)
                    {
                        if ((pi.GetCustomAttributes (typeof (DmfIgnorePropertyAttribute), false)).Length == 0)
                        {
                            propertyList.Add (pi);
                        }
                    }
                }
            }

            // Name and Schema are not exposed through ISfcPropertyProvider
            // DMF will handle them as special cases
            //
            if (type.IsSubclassOf (typeof (ScriptSchemaObjectBase)))
            {
                propertyList.Add (SmoSchemaPropertyInfo);
            }

            if (type.IsSubclassOf (typeof (NamedSmoObject)))
            {
                propertyList.Add (SmoNamePropertyInfo);
            }

            return propertyList.ToArray ();
        }

        internal static PropertyInfo[] GetTypeFilterProperties (string skeleton)
        {
            return GetTypeFilterProperties(SqlSmoObject.GetTypeFromUrnSkeleton(new Urn(skeleton)));
        }
        
        /// <summary>
        /// Returns filtering properties supported by the SMO
        /// type represented by the given expression, or null if it's
        /// not a valid SMO type.
        /// Currently we only allow Name on objects that are child of the 
        /// Server object.
        /// </summary>
        internal static PropertyInfo[] GetTypeFilterProperties (Type type)
        {
            if (null == type)
            {
                return null;
            }
            List<PropertyInfo> propertyList = new List<PropertyInfo> ();
            

            PropertyInfo pi = type.GetProperty("Parent", 
                BindingFlags.Public | BindingFlags.Instance );

            // only return Name if parent is Server
            if (type.IsSubclassOf (typeof (NamedSmoObject)) && 
                null != pi && pi.PropertyType == typeof(Server))
            {
                propertyList.Add (SmoNamePropertyInfo);
            }
                
            return propertyList.ToArray ();
        }
    }
}
 




