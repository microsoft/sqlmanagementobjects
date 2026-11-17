// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;

using System.ComponentModel;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This class provides information about
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FacetInfo : IDisposable, IComparable
    {
        Type facetType = null;
        LocalizableTypeConverter typeConverter = null;

        // Cache the context used in the most recent call to
        // GetTargetProperty.
        FacetEvaluationContext context = null;
        object lastTarget = null;

        internal FacetInfo()
        {
        }

        ///
        internal FacetInfo(Type facetType)
        {
            this.facetType = facetType;
        }

        ///
        internal FacetInfo(string facetShortName)
            : this(FacetRepository.GetFacetType(facetShortName))
        {
        }

        /// <summary>
        /// Returns a AssemblyQualifiedName of the facet type.
        /// </summary>
        public string Name
        {
            get
            {
                if (this.FacetType != null)
                {
                    return this.FacetType.Name;
                }
                return string.Empty;
            }
        }


        private string displayName = null;
        /// <summary>
        /// Returns a display name of the facet.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (this.displayName == null)
                {
                    this.displayName = String.Empty;
                    if (this.TypeConverter != null)
                    {
                        LocalizableMemberDescriptor memberDescriptor = this.TypeConverter.GetTypeMemberDescriptor(this.facetType);
                        this.displayName = memberDescriptor.DisplayName;
                    }
                }
                return this.displayName;
            }
        }

        /// <summary>
        /// Returns the DisplayName property.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.DisplayName;
        }

        private string description = null;
        /// <summary>
        /// Returns the description of the facet.
        /// </summary>
        public string Description
        {
            get
            {
                if (this.description == null)
                {
                    this.description = String.Empty;

                    if (this.TypeConverter != null)
                    {
                        LocalizableMemberDescriptor memberDescriptor = this.TypeConverter.GetTypeMemberDescriptor(this.facetType);
                        this.description = memberDescriptor.Description;
                    }
                }

                return this.description;
            }
        }

        /// <summary>
        /// Returns the Type of the facet itself.
        /// </summary>
        public Type FacetType
        {
            get { return this.facetType; }
        }

        /// <summary>
        /// Returns the Types that this facet operates on.
        /// </summary>
        public ReadOnlyCollection<Type> TargetTypes
        {
            get
            {
                return new ReadOnlyCollection<Type>(FacetRepository.GetFacetSupportedTypes(this.FacetType));
            }
        }

        /// <summary>
        /// Returns the AutomatedPolicyEvaluationMode of the facet.
        /// </summary>
        public AutomatedPolicyEvaluationMode EvaluationMode
        {
            get
            {
                return FacetRepository.GetFacetEvaluationMode(this.FacetType);
            }
        }

        /// <summary>
        /// Returns static information about the properties descriptors exposed by
        /// this facet.
        /// </summary>
        public PropertyDescriptorCollection FacetPropertyDescriptors
        {
            get
            {
                PropertyDescriptorCollection descriptors = null;
                if (this.TypeConverter != null)
                {
                    descriptors = this.TypeConverter.GetProperties(FacetRepository.GetFacetProperties(this.FacetType));
                }
                return descriptors;
            }
        }
        
        /// <summary>
        /// Returns static information about the properties exposed by
        /// this facet.
        /// </summary>
        public ReadOnlyCollection<System.Reflection.PropertyInfo> FacetProperties
        {
            get
            {
                return new ReadOnlyCollection<System.Reflection.PropertyInfo>(FacetRepository.GetFacetProperties(this.FacetType));
            }
        }

        /// <summary>
        /// Given a property name and a target object, this method
        /// returns the value of that property as seen on the target
        /// by this facet.
        /// </summary>
        public object GetTargetProperty(string propName, object target)
        {
            return GetAdapter (target).GetPropertyValue (propName);
        }

        #region Internal helpers

        private LocalizableTypeConverter TypeConverter
        {
            get
            {
                if (this.facetType != null && this.typeConverter == null)
                {
                    this.typeConverter = TypeDescriptor.GetConverter(this.facetType) as LocalizableTypeConverter;
                }
                return this.typeConverter;
            }
        }

        private FacetEvaluationContext GetAdapter(object target)
        {
            if (target == this.lastTarget)
            {
                return this.context;
            }

            this.context = FacetEvaluationContext.GetFacetEvaluationContext (this.FacetType, target);
            this.lastTarget = target;
            return this.context;
        }

        #endregion

        #region private interface implementation

        void IDisposable.Dispose()
        {
            this.lastTarget = null;
            this.context = null;
            this.typeConverter = null;
            this.displayName = null;
            this.description = null;
        }

        int IComparable.CompareTo(object other)
        {
            return this.DisplayName.CompareTo(((FacetInfo)other).DisplayName);
        }
        #endregion
    }
}
