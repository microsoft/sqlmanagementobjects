// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587

namespace Microsoft.SqlServer.Management.Smo
{
    internal class SfcResolverHelper
    {
        /// <summary>
        /// Get the root object of a domain instance. Should work on all types of domains that implement .Parent on objects
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static Database GetDatabase(object obj)
        {
            object cur = obj;
            object database = null;

            // Walk hiearchy down until database is hit
            // This assumes objects have a 'Parent' property, which should be true

            while (database == null)
            {
                Type t = cur.GetType();
                PropertyInfo pi = t.GetProperty("Parent");

                Debug.Assert(pi != null, String.Format("{0} is missing Parent property.", t.FullName));

                cur = pi.GetValue(cur, null);

                Debug.Assert(cur != null, String.Format("{0}.Parent property returned null.", t.FullName));

                if (cur.GetType() == typeof(Database))
                {
                    database = cur;
                }
            }
            return (Database)database;
        }

        /// <summary>
        /// Returns DataType
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static DataType GetDataType(object obj)
        {
            object dataType = null;

            Type t = obj.GetType();
            PropertyInfo pi = t.GetProperty("DataType");
            dataType = pi.GetValue(obj, null);

            if (dataType != null && dataType.GetType() == typeof(DataType))
            {
                return (DataType)dataType;
            }

            return null;
        }

        internal static string GetSchemaName(object obj)
        {
            Type t = obj.GetType();
            PropertyInfo pi = t.GetProperty("Schema");

            Debug.Assert(pi != null, String.Format("{0} is missing Schema property.", t.FullName));

            string name = (string)pi.GetValue(obj, null);

            Debug.Assert(name != null, String.Format("{0}.Schema property returned null.", t.FullName));

            return name;
        }
    }

    /// <summary>
    /// Custom resolver for DataType::UserDefinedDataType
    /// TODO: implement resolvers for other types (note that the URN resolver can be generic for all data types)
    /// </summary>
    public class UserDefinedDataTypeResolver
    {
        public static object Resolve(object instance, params object[] parameters)
        {
            DataType dataType = SfcResolverHelper.GetDataType(instance);
            if (dataType == null || dataType.SqlDataType != SqlDataType.UserDefinedDataType)
            {
                return null;
            }
            Database db = SfcResolverHelper.GetDatabase(instance);
            return db.UserDefinedDataTypes[dataType.Name, dataType.Schema];
        }

        public static object ResolveUrn(object instance, params object[] parameters)
        {
            DataType dataType = SfcResolverHelper.GetDataType(instance);
            if (dataType == null || dataType.SqlDataType != SqlDataType.UserDefinedDataType)
            {
                return null;
            }
            Database db = SfcResolverHelper.GetDatabase(instance);

            return new Urn(db.Urn.ToString() + String.Format(SmoApplication.DefaultCulture,"/UserDefinedDataType[@Name = '{0}' and @Schema = '{1}']",
                        SfcSecureString.EscapeSquote(dataType.Name),
                        SfcSecureString.EscapeSquote(dataType.Schema)));
        }
    }

    /// <summary>
    /// Custom resolver for DataType::UserDefinedType
    /// </summary>
    public class UserDefinedTypeResolver
    {
        public static object Resolve(object instance, params object[] parameters)
        {
            DataType dataType = SfcResolverHelper.GetDataType(instance);
            if (dataType == null || dataType.SqlDataType != SqlDataType.UserDefinedType)
            {
                return null;
            }
            Database db = SfcResolverHelper.GetDatabase(instance);
            return db.UserDefinedTypes[dataType.Name, dataType.Schema];
        }

        public static object ResolveUrn(object instance, params object[] parameters)
        {
            DataType dataType = SfcResolverHelper.GetDataType(instance);
            if (dataType == null || dataType.SqlDataType != SqlDataType.UserDefinedType)
            {
                return null;
            }
            Database db = SfcResolverHelper.GetDatabase(instance);

            return new Urn(db.Urn.ToString() + String.Format(SmoApplication.DefaultCulture,"/UserDefinedType[@Name = '{0}' and @Schema = '{1}']",
                        SfcSecureString.EscapeSquote(dataType.Name),
                        SfcSecureString.EscapeSquote(dataType.Schema)));
        }
    }

    /// <summary>
    /// Custom resolver for DataType::UserDefinedTableType
    /// </summary>
    public class UserDefinedTableTypeResolver
    {
        public static object Resolve(object instance, params object[] parameters)
        {
            DataType dataType = SfcResolverHelper.GetDataType(instance);
            if (dataType == null || dataType.SqlDataType != SqlDataType.UserDefinedTableType)
            {
                return null;
            }
            Database db = SfcResolverHelper.GetDatabase(instance);
            return db.UserDefinedTableTypes[dataType.Name, dataType.Schema];
        }

        public static object ResolveUrn(object instance, params object[] parameters)
        {
            DataType dataType = SfcResolverHelper.GetDataType(instance);
            if (dataType == null || dataType.SqlDataType != SqlDataType.UserDefinedTableType)
            {
                return null;
            }
            Database db = SfcResolverHelper.GetDatabase(instance);

            return new Urn(db.Urn.ToString() + String.Format(SmoApplication.DefaultCulture,"/UserDefinedTableType[@Name = '{0}' and @Schema = '{1}']",
                        SfcSecureString.EscapeSquote(dataType.Name),
                        SfcSecureString.EscapeSquote(dataType.Schema)));
        }
    }

    /// <summary>
    /// Custom resolver for Schema
    /// We need a custom resolver as not all schema based objects live under Database
    /// For example service broker objects have an intermediate object
    /// so we cannot resolve using a fixed template
    /// </summary>
    public class SchemaCustomResolver
    {
        public static object Resolve(object instance, params object[] parameters)
        {
            Database db = SfcResolverHelper.GetDatabase(instance);
            return db.Schemas[SfcResolverHelper.GetSchemaName(instance)];
        }

        public static object ResolveUrn(object instance, params object[] parameters)
        {
            Database db = SfcResolverHelper.GetDatabase(instance);
            return new Urn(db.Urn.ToString() + String.Format(SmoApplication.DefaultCulture,"/Schema[@Name = '{0}']",
                SfcSecureString.EscapeSquote(SfcResolverHelper.GetSchemaName(instance))));
        }
    }
}
