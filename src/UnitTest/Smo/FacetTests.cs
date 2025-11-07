// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert=NUnit.Framework.Assert;
using NUnit.Framework;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class FacetTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void Facets_have_name_and_description_for_all_properties()
        {
            Assert.Multiple(() =>
            {
                foreach (var facetInfo in PolicyStore.Facets)
                {
                    // avoid capture of the loop variable
                    var fi = facetInfo;
                    Assert.That(fi.Description, Is.Not.Null.And.Not.Empty, "{0}: Description", fi.FacetType);
                    Assert.That(fi.DisplayName, Is.Not.Null.And.Not.Empty, "{0} : DisplayName", fi.FacetType);
                    Assert.That(fi.FacetPropertyDescriptors, Is.Not.Null.And.Not.Empty, "{0}: FacetPropertyDescriptors",
                                fi.FacetType);
                    foreach (var propertyDescriptor in (fi.FacetPropertyDescriptors ?? new PropertyDescriptorCollection(Enumerable.Empty<PropertyDescriptor>().ToArray())).Cast<PropertyDescriptor>())
                    {
                        var pd = propertyDescriptor;
                        Assert.That(pd.Description, Is.Not.Null.And.Not.Empty, 
                            $"{facetInfo.FacetType}.{pd.Name} is missing localized descriptions. Add these lines to LocalizableResources.strings in the appropriate section, maintaining the sorted order{System.Environment.NewLine}{MissingResourceLines(facetInfo, pd.Name)}"
                            );
                    }
                }
            });            
        }

        static string MissingResourceLines(FacetInfo facetInfo, string propertyName)
        {
            return $"{facetInfo.FacetType.Name}_{propertyName}Name=<Display Name>{System.Environment.NewLine}{facetInfo.FacetType.Name}_{propertyName}Desc=<Description>";
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Facets_properties_register_correctly()
        {
            Assert.Multiple(() =>
            {
                foreach (var facetType in FacetRepository.RegisteredFacets.Cast<Type>())
                {
                    Trace.TraceInformation("Facet: {0}", facetType.Name);
                    foreach (var propertyInfo in FacetRepository.GetFacetProperties(facetType))
                    {
                        var actualConfigurable = FacetRepository.IsPropertyConfigurable(facetType, propertyInfo.Name);
                        var expectedConfigurable = facetType.IsFacetPropertyConfigurable(propertyInfo);
                        Assert.That(actualConfigurable, Is.EqualTo(expectedConfigurable), "{0}.{1} IsConfigurable", facetType, propertyInfo.Name);
                    }
                }
            });
        }
    }

    static class FacetTypeExtensions
    {
        public static bool IsReadOnlyFacet(this Type facetType)
        {
            var isReadonly = false;
            var physicalInfo = (PhysicalFacetAttribute)Attribute.GetCustomAttribute(facetType, typeof(PhysicalFacetAttribute));
            if (physicalInfo != null)
            {
                isReadonly = physicalInfo.IsReadOnly;
            }
            return isReadonly;
        }

        public static bool IsDmfIgnoreAttribute(this PropertyInfo pi)
        {
            var isIgnore = false;
            var dmfPropertyAttribute = (DmfIgnorePropertyAttribute)Attribute.GetCustomAttribute(pi, typeof(DmfIgnorePropertyAttribute), true);
            if (dmfPropertyAttribute != null)
            {
                isIgnore = true;
            }
            return isIgnore;
        }

        public static bool IsFacetPropertyConfigurable(this Type facetType, PropertyInfo pi)
        {
            return pi.CanWrite && !(facetType.IsReadOnlyFacet() || pi.IsDmfIgnoreAttribute() || pi.IsReadOnlyAfterCreation());
        }

        public static bool IsReadOnlyAfterCreation(this PropertyInfo pi)
        {
            var isReadOnly = false;
            var sfcPropertyAttribute = (SfcPropertyAttribute)Attribute.GetCustomAttribute(pi, typeof(SfcPropertyAttribute), true);
            if ((sfcPropertyAttribute != null) && sfcPropertyAttribute.ReadOnlyAfterCreation)
            {
                isReadOnly = true;
            }
            return isReadOnly;
        }

    }
}