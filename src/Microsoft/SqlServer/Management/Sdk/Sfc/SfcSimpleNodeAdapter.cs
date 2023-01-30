// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Reflection;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// A mechansim that let a client adapters a custom object model into SfcSimpleNode.
    /// 
    /// The adapter provides default implementation by reflection, where possible.
    /// </summary>
    public abstract class SimpleNodeAdapter
    {

        /// <summary>
        /// Indicate if this provider support the specified graph. If it is, 
        /// returns true, false otherwise.
        /// 
        /// The method is called only one on the top most node of each graph.
        /// </summary>
        public abstract bool IsSupported(object reference);

        /// <summary>
        /// Get the Urn of the specified object
        /// </summary>
        public abstract Urn GetUrn(object reference);

        /// <summary>
        /// Get the named Property of the specified object. The default implementation
        /// resolve the property by reflection.
        /// </summary>
        public virtual object GetProperty(object reference, string propertyName)
        {
            try
            {
                PropertyInfo pi = reference.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                object obj = pi.GetValue(reference, null);

                return obj;
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Get the named Child of the specified object. The default implementation
        /// resolve the child by reflection.
        /// </summary>
        public virtual object GetObject(object reference, string childName)
        {
            try
            {
                PropertyInfo pi = reference.GetType().GetProperty(childName, BindingFlags.Public | BindingFlags.Instance);
                object obj = pi.GetValue(reference, null);

                return obj;
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Get the named children enumerable of the specified object. The default implementation
        /// resolve the children by reflection.
        /// </summary>
        public virtual IEnumerable GetEnumerable(object reference, string enumName)
        {
            try
            {
                // code extracted and modified from Serializer.cs
                PropertyInfo pi = reference.GetType().GetProperty(enumName, BindingFlags.Public | BindingFlags.Instance);

                object collection = pi.GetValue(reference, null);
                return collection as IEnumerable;
            }
            catch (TargetInvocationException)
            {
                // return an empty list for failed reflection calls
                return null;
            }
        }

        /// <summary>
        /// Returns true if the specified children matches the adapter criteria and will be
        /// included in the result. The default implementation always return true.
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public virtual bool IsCriteriaMatched(object reference)
        {
            return true;
        }

        #region checked method
        internal object CheckedGetProperty(object reference, string propertyName)
        {
            try
            {
                return GetProperty(reference, propertyName);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                return null;
            }
        }

        internal object CheckedGetObject(object reference, string childName)
        {
            try
            {
                return GetObject(reference, childName);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                return null;
            }
        }

        internal IEnumerable CheckedGetEnumerable(object reference, string enumName)
        {
            try
            {
                return GetEnumerable(reference, enumName);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                return null;
            }
        }

        internal Urn CheckedGetUrn(object reference)
        {
            try
            {
                return GetUrn(reference);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                return null;
            }
        }

        internal bool CheckedIsCriteriaMatched(object reference)
        {
            try
            {
                return IsCriteriaMatched(reference);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                return false;
            }
        }

        internal bool CheckedIsSupported(object reference)
        {
            try
            {
                return IsSupported(reference);
            }
            catch (Exception e)
            {
                if (IsSystemGeneratedException(e))
                {
                    throw e;
                }
                TraceHelper.LogExCatch(e);
                return false;
            }
        }
        #endregion

        /// <summary>
        /// Utility method to properly pass thru exception
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        internal static bool IsSystemGeneratedException(Exception e)
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
            //if (e is ExecutionEngineException) //This type previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.
            //{
            //    return true;
            //}
            return false;
        }
    }

    internal class IAlienObjectAdapter : SimpleNodeAdapter
    {
        public override bool IsSupported(object node)
        {
            if (node is IAlienObject)
            {
                return true;
            }
            return false;
        }

        public override Urn GetUrn(object reference)
        {
            IAlienObject alienObject = reference as IAlienObject;
            return alienObject.GetUrn();
        }

        public override object GetProperty(object instance, string propertyName)
        {
            // code extracted and modified from Serializer.cs
            object propertyVal;
            IAlienObject alienObject = instance as IAlienObject;

            try
            {
                // in Serializer.cs, propertyType is obtained from Metadata
                Type propertyType = alienObject.GetPropertyType(propertyName);
                propertyVal = alienObject.GetPropertyValue(propertyName, propertyType);
            }
            catch (TargetInvocationException tie)
            {
                TraceHelper.LogExCatch(tie);
                propertyVal = null;
            }
            return propertyVal;
        }
    }

    internal class SfcSimpleNodeAdapter : SimpleNodeAdapter
    {
        public override bool IsSupported(object node)
        {
            if (node is SfcInstance)
            {
                return true;
            }
            return false;
        }

        public override Urn GetUrn(object reference)
        {
            // in Serializer.cs, there are condition for adapterHandler, which is removed.
            SfcInstance sfcInstance = reference as SfcInstance;
            return sfcInstance.Urn;
        }

        public override object GetProperty(object instance, string propertyName)
        {
            // code extracted and modified from Serializer.cs

            // in Serializer.cs, there are condition for adapterHandler, which is removed.
            SfcInstance sfcInstance = instance as SfcInstance;
            object propertyVal = ((SfcInstance)instance).Properties[propertyName].Value;

            return propertyVal;
        }
    }
}
