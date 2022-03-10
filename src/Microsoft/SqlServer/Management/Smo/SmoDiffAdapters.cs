// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Differencing.SPI;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    internal class SmoAvaliablePropertyProvider : AvailablePropertyValueProvider
    {
        /// <summary>
        /// Indicate if this provider support the specified graph. If it is, 
        /// returns true, false otherwise.
        /// 
        /// The method is called only one on the top most node of each graph.
        /// </summary>
        public override bool IsGraphSupported(ISfcSimpleNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException();
            }

            if (node.ObjectReference is SqlSmoObject)
            {
                SqlSmoObject sqlSmo = (SqlSmoObject)node.ObjectReference;

                if (sqlSmo.IsDesignMode)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether a property value is available. If it is not, the comparison 
        /// is not performed; else, we consider it in the comparison.
        /// </summary>
        public override bool IsValueAvailable(ISfcSimpleNode node, string propName)
        {
            if (node == null || string.IsNullOrEmpty(propName))
            {
                throw new ArgumentNullException();
            }

            if (node.ObjectReference is SqlSmoObject)
            {
                // to determine the Null-ness, we must go thru the IAlienObject
                // path and catch TargetInvocationException. The regular
                // ((SqlSmoObject)node.ObjectReference) will put default value in
                IAlienObject alienObject = node.ObjectReference as IAlienObject;
                try
                {
                    // in Serializer.cs, propertyType is obtained from Metadata
                    Type propType = alienObject.GetPropertyType(propName);
                    object value = alienObject.GetPropertyValue(propName, propType);
                    if (value == null)
                    {
                        return false;
                    }
                }
                catch (TargetInvocationException)
                {
                    return false;
                }

            }
            return true;
        }
    }

    /// <summary>
    /// This provider is added to support dac deployment on Sql Server 2005.
    /// There are certain properties that do not exist on certain versions of sql server, 
    /// for eg. IsReadOnly property of a stored proc parameter does not exist on 2005.
    /// We want to skip the comparison of a property if it does not exist on live server while detecting a drift.
    /// </summary>
    internal class OnlineSmoAvailablePropertyProvider : AvailablePropertyValueProvider
    {
        /// <summary>
        /// Answers whether the graph is supported.
        /// Only supports ONLINE SMO graphs.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override bool IsGraphSupported(ISfcSimpleNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            bool isSmoGraphOnline = (node.ObjectReference is SqlSmoObject &&
                                     !((SqlSmoObject)node.ObjectReference).IsDesignMode);

            return isSmoGraphOnline;
        }

        /// <summary>
        /// Answers if a property is available for a given node.
        /// If false is returned, the property is ignored during differencing.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public override bool IsValueAvailable(ISfcSimpleNode node, string propName)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node"); 
            }
			if (propName == null)
			{
				throw new ArgumentNullException("propName");
			}
            if (propName == string.Empty)
            {
                throw new ArgumentException("propName");
            }

            Debug.Assert(this.IsGraphSupported(node), "SmoDiffAdapter", "Only supported graphs should be used");

            if (node.ObjectReference is SqlSmoObject)
            {
                try
                {
                    ((SqlSmoObject)node.ObjectReference).Properties.GetPropertyObject(propName);
                }
                catch (Exception e)
                {
                    if (OnlineSmoAvailablePropertyProvider.IsSystemGeneratedException(e))
                    {
                        throw;
                    }

                    //Ignore only "UnknownPropertyException" to avoid comparing the properties that
                    //might not be avaible if the online server being compared is a down level version.

                    return !(e is UnknownPropertyException);
                }
            }
            return true;
        }

        private static bool IsSystemGeneratedException(Exception e)
        {
            // see, http://msdn.microsoft.com/en-us/library/ms229007.aspx
            if (e is OutOfMemoryException)
            {
                return true;
            }
            if (e is StackOverflowException)
            {
                return true;
            }
            if (e is System.Runtime.InteropServices.COMException || e is System.Runtime.InteropServices.SEHException)
            {
                return true;
            }
            //if (e is ExecutionEngineException) //'System.ExecutionEngineException' is obsolete: 'This type previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.
            //{
            //    return true;
            //}
            return false;
        }
    }

    internal class SmoNodeAdapterProvider : SfcNodeAdapterProvider
    {
        /// <summary>
        /// Indicate if this provider support the specified graph. If it is, 
        /// returns true, false otherwise.
        /// 
        /// The method is called only one on the top most node of each graph.
        /// </summary>
        public override bool IsGraphSupported(Object obj)
        {
            if (obj is SqlSmoObject)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// The method is called only one on the top most node of the graphs
        /// to be compared.
        /// </summary>
        public override ISfcSimpleNode GetGraphAdapter(Object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return SfcSimpleNodeFactory.Factory.GetSimpleNode(obj, new SmoSimpleNodeAdapter(obj as SqlSmoObject));
        }
    }

    internal class SmoSimpleNodeAdapter : SimpleNodeAdapter
    {
        public SmoSimpleNodeAdapter(SqlSmoObject obj)
        {
            if (obj != null)
            {
                var server = obj.GetServerObject();
                server.SetDefaultInitFields(typeof(Column), true);
                server.SetDefaultInitFields(typeof(DatabaseRole), true);
                server.SetDefaultInitFields(typeof(Index), true);
                server.SetDefaultInitFields(typeof(Schema), true);
                server.SetDefaultInitFields(typeof(StoredProcedure), true);
                server.SetDefaultInitFields(typeof(Trigger), true);
                server.SetDefaultInitFields(typeof(Table), nameof(Table.IsSystemObject));
                server.SetDefaultInitFields(typeof(User), true);
                server.SetDefaultInitFields(typeof(UserDefinedFunction), true);
                server.SetDefaultInitFields(typeof(View), true);
            }
        }

        public override bool IsSupported(object reference)
        {
            return reference is SqlSmoObject;
        }

        public override Urn GetUrn(object reference)
        {
            SqlSmoObject smo = (SqlSmoObject)reference;
            return smo.Urn;
        }

        public override object GetProperty(object reference, string propertyName)
        {
            SqlSmoObject smo = (SqlSmoObject)reference;
            return smo.Properties[propertyName].Value;
        }

        public override bool IsCriteriaMatched(Object reference)
        {
            if (reference is SqlSmoObject == false)
            {
                return base.IsCriteriaMatched(reference);
            }

            SqlSmoObject smoObject = (SqlSmoObject)reference;

            // VSTS #331708 -- Ported IsSystemObject check from Serializer.cs 
            // (we will not diff any smo object where IsSystemObject returns true)
            if (smoObject.Properties.Contains("IsSystemObject"))
            {
                // note that "smoObject.IsSystemObjectInternal()" does not
                // return useful value. Must use smoObject.Properties["IsSystemObject"]
                bool system = (bool)smoObject.Properties["IsSystemObject"].Value;
                if (system)
                {
                    return false;
                }
            }

            if (smoObject is DatabaseRole)
            {
                // VSTS #328229 -- Diff Integration: Role and Schema changes detected by
                // Differencer.
                DatabaseRole role = (DatabaseRole)smoObject;
                if (role.IsFixedRole)
                {
                    return false;
                }
                else if (System.StringComparer.Ordinal.Compare("public", role.Name) == 0)
                {
                    // public is not a fixed role, but is a system object
                    return false;
                }
            }
            else if (smoObject is User)
            {
                // VSTS #346124 -- System created user and schema is detected as Drift
                if (IsDesignModeSystemUser((User)smoObject))
                {
                    return false;
                }
            }
            else if (smoObject is Schema)
            {
                // (same as above) VSTS #346124 -- System created user and schema is detected as Drift
                if (IsDesignModeSystemSchema((Schema)smoObject))
                {
                    return false;
                }
            }
            return true;
        }

        // See, sql\mpu\shared\SMO\Enumerator\sql\src\xml\Schema.xml#Properties\Property[@Name='IsSystemObject'] for the list
        private static readonly IList<String> SYSTEM_SCHEMA_NAMES = new List<String>(new String[] { "dbo", "guest", "INFORMATION_SCHEMA", "sys", "db_owner", "db_accessadmin", "db_securityadmin", "db_ddladmin", "db_backupoperator", "db_datareader", "db_datawriter", "db_denydatareader", "db_denydatawriter" });
        private bool IsDesignModeSystemSchema(Schema schema)
        {
            // it needs to keep in-sync with the file above, and not version safe.
            // it should move into the Schema object itself, when it becomes possible
            if (SYSTEM_SCHEMA_NAMES.Contains(schema.Name))
            {
                if (schema.IsDesignMode) // most expensive, check last
                {
                    return true;
                }
            }
            return false;
        }

        // See, sql\mpu\shared\SMO\Enumerator\sql\src\xml\User.xml#Properties\Property[@Name='IsSystemObject'] for the list
        private static readonly IList<String> SYSTEM_USER_NAMES = new List<String>(new String[] { "dbo", "guest", "INFORMATION_SCHEMA", "sys", });
        private bool IsDesignModeSystemUser(User user)
        {
            // this check is pretty fragile and not version safe.
            // the original check (for online mode) is "u.uid = 1 OR u.uid = 16382 OR u.uid = 16383"
            // by id is not guaranteed to be available
            if (SYSTEM_USER_NAMES.Contains(user.Name))
            {
                if (user.IsDesignMode) // most expensive, check last
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class SmoPropertyComparerProvider : PropertyComparerProvider
    {

        public override bool AreGraphsSupported(ISfcSimpleNode left, ISfcSimpleNode right)
        {
            if (left == null)
            {
                throw new ArgumentNullException("left");
            }
            if (right == null)
            {
                throw new ArgumentNullException("right");
            }
            Diagnostics.TraceHelper.Assert(left.ObjectReference != null, "Expect non-null left.ObjectReference");
            Diagnostics.TraceHelper.Assert(right.ObjectReference != null, "Expect non-null right.ObjectReference");

            if (left.ObjectReference is SqlSmoObject && right.ObjectReference is SqlSmoObject)
            {
                return true;
            }
            return false;
        }

        public override bool Compare(ISfcSimpleNode left, ISfcSimpleNode right, String propName)
        {
            if (left == null)
            {
                throw new ArgumentNullException("left");
            }
            if (right == null)
            {
                throw new ArgumentNullException("right");
            }
            if (propName == null)
            {
                throw new ArgumentNullException("propName");
            }

            Diagnostics.TraceHelper.Assert(left.ObjectReference != null, "Expect non-null left.ObjectReference");
            Diagnostics.TraceHelper.Assert(right.ObjectReference != null, "Expect non-null right.ObjectReference");

            Object leftValue = left.Properties[propName];
            Object rightValue = right.Properties[propName];
            if (left.ObjectReference is Column && right.ObjectReference is Column)
            {
                Column leftCol = (Column)left.ObjectReference;
                Column rightCol = (Column)right.ObjectReference;
                if ("DataType".Equals(propName))
                {
                    if (leftCol.IsDesignMode != rightCol.IsDesignMode)
                    {
                        return CompareDataTypeWorkaround(leftCol, rightCol);
                    }
                }
            }

            return CompareObjects(leftValue, rightValue);
        }

        private static bool CompareDataTypeWorkaround(Column leftCol, Column rightCol)
        {
            Diagnostics.TraceHelper.Assert(leftCol != null, "Expect non-null leftCol");
            Diagnostics.TraceHelper.Assert(rightCol != null, "Expect non-null rightCol");

            DataType designMode = null;
            DataType connectedMode = null;
            if (leftCol.IsDesignMode)
            {
                designMode = leftCol.DataType;
                connectedMode = rightCol.DataType;
            }
            else
            {
                designMode = rightCol.DataType;
                connectedMode = leftCol.DataType;
            }

            if (System.StringComparer.Ordinal.Compare(designMode.Name, connectedMode.Name) != 0)
            {
                return false;
            }

            if (designMode.SqlDataType != connectedMode.SqlDataType)
            {
                return false;
            }

            if (designMode.Schema != connectedMode.Schema)
            {
                return false;
            }

            if (designMode.NumericPrecision != 0 && (designMode.NumericPrecision != connectedMode.NumericPrecision))
            {
                return false;
            }

            if (designMode.MaximumLength != 0 && (designMode.MaximumLength != connectedMode.MaximumLength))
            {
                return false;
            }

            if (designMode.NumericScale != 0 && (designMode.NumericScale != connectedMode.NumericScale))
            {
                return false;
            }

            return true;
        }

        private static bool CompareObjects(Object left, Object right)
        {
            if (left == null && right == null)
            {
                return true;
            }
            if (left == null)
            {
                return false;
            }
            if (right == null)
            {
                return false;
            }

            bool result = left.Equals(right);
            return result;
        }
    }

    internal class SmoCollectionSortingProvider : ContainerSortingProvider
	{

		private static System.StringComparer DEFAULT_COMPARER = System.StringComparer.Ordinal;

		/// <summary>
		/// Indicate if this provider support the specified graph. If it is, 
		/// returns true, false otherwise.
		/// 
		/// The method is called only one on the top most node of each graph.
		/// </summary>
		public override bool AreGraphsSupported(ISfcSimpleNode source, ISfcSimpleNode target)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}

			if (source.ObjectReference is SqlSmoObject && target.ObjectReference is SqlSmoObject)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Obtain a Comparer that compares the children of a pair of comparable lists.
		/// 
		/// The caller should determine if the lists are comparable using AreListCompareable()
		/// method. It throws ArgumentException if the specified lists are not comparable.
		/// </summary>
		public override IComparer<ISfcSimpleNode> GetComparer(ISfcSimpleList source, ISfcSimpleList target)
		{
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

			if (!AreListsComparable(source, target))
			{
				//@TODO tyip-5/27/2009 need localization
				throw new ArgumentException("The specified lists are not comparable.");
			}

			IEnumerator<ISfcSimpleNode> Enumlist1 = source.GetEnumerator();
			IEnumerator<ISfcSimpleNode> Enumlist2 = target.GetEnumerator();
			IComparer comparer = DEFAULT_COMPARER;

			if (Enumlist1.MoveNext() && Enumlist2.MoveNext())
			{
				ISfcSimpleNode node1 = Enumlist1.Current;
				ISfcSimpleNode node2 = Enumlist2.Current;

				if (node1 != null && node1.ObjectReference is SqlSmoObject
					&& node2 != null && node2.ObjectReference is SqlSmoObject)
				{
					SqlSmoObject obj1 = (SqlSmoObject)node1.ObjectReference;
					SqlSmoObject obj2 = (SqlSmoObject)node2.ObjectReference;

					string collation1 = this.GetParentCollation(obj1);
					string collation2 = this.GetParentCollation(obj2);

					if (string.IsNullOrEmpty(collation1) || string.IsNullOrEmpty(collation2) ||
						System.StringComparer.Ordinal.Compare(collation1, collation2) == 0)
					{
						if (!obj1.IsDesignMode && !string.IsNullOrEmpty(collation1))
						{
							comparer = (IComparer)obj1.GetComparerFromCollation(collation1);
						}
						else if (!obj2.IsDesignMode && !string.IsNullOrEmpty(collation2))
						{
							comparer = (IComparer)obj2.GetComparerFromCollation(collation2);
						}
						else
						{
                            comparer = NetCoreHelpers.InvariantCulture.GetStringComparer(ignoreCase: false);
						}
					}
				}
			}

			return new SmoCollectionCompararer(comparer);
		}

		/// <summary>
		/// It will return true if SMO can provide a comparer to compare both the input list.
		/// 
		/// This method return false for these cases:
		/// 1. When one of the list is empty
		/// 2. Type of the objects in either of the list is not SqlSmoObject
		/// </summary>
		private bool AreListsComparable(ISfcSimpleList source, ISfcSimpleList target)
		{
			IEnumerator<ISfcSimpleNode> Enumlist1 = source.GetEnumerator();
			IEnumerator<ISfcSimpleNode> Enumlist2 = target.GetEnumerator();
			bool result = false;

			if (Enumlist1.MoveNext() && Enumlist2.MoveNext())
			{
				ISfcSimpleNode node1 = Enumlist1.Current;
				ISfcSimpleNode node2 = Enumlist2.Current;

				if (node1 != null && node1.ObjectReference is SqlSmoObject
					&& node2 != null && node2.ObjectReference is SqlSmoObject)
				{
					result = true;
				}
			}
			return result;
		}

		private string GetParentCollation(SqlSmoObject obj)
		{
			object cur = obj;
			object dbOrSrv = null;
			string collation = string.Empty;

			// Walk hiearchy down until database or server is hit
			// This assumes objects have a 'Parent' property, which should be true

			while (dbOrSrv == null)
			{
				Type t = cur.GetType();
				PropertyInfo pi = t.GetProperty("Parent");
				cur = pi.GetValue(cur, null);

				if (cur.GetType() == typeof(Database))
				{
					dbOrSrv = cur;
					collation = ((Database)dbOrSrv).Properties.GetPropertyObject("Collation", true).Value as string;
				}
				else if (cur.GetType() == typeof(Server))
				{
					dbOrSrv = cur;
					collation = ((Server)dbOrSrv).Properties.GetPropertyObject("Collation", true).Value as string;
				}
			}
			return collation;
		}
	}

    internal class SmoCollectionCompararer : IComparer<ISfcSimpleNode>
	{
		private IComparer comparer;
		internal SmoCollectionCompararer(IComparer comparer)
		{
            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

			this.comparer = comparer;
		}
		public int Compare(ISfcSimpleNode left, ISfcSimpleNode right)
		{
            if (left == null && right == null)
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }

			SqlSmoObject leftRef = left.ObjectReference as SqlSmoObject;
			SqlSmoObject rightRef = right.ObjectReference as SqlSmoObject;
			
			ObjectKeyBase leftKey = leftRef.key;
			ObjectKeyBase rightKey = rightRef.key;

			IComparer keyComparer = leftKey.GetComparer(comparer);
			return keyComparer.Compare(leftKey, rightKey);
		}
	}
}

