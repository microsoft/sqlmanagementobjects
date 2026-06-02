// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Differencing.Impl;
using Microsoft.SqlServer.Management.Sdk.Differencing.SPI;

namespace Microsoft.SqlServer.Management.Sdk.Differencing
{
    /// <summary>
    /// The factory to obtain an Differencer.
    /// </summary>
    public class DifferencingService
    {
        /// <summary>
        /// Implementation note:
        /// This is a temporary implementation of DifferencingService. The implementation should be 
        /// replaced by MFx "Service" mechanism when it  is due. For now, the only client we are
        /// attempting to support is Smo. This class look up known Smo Providers using reflection.
        /// All provider is loaded from Smo's dll.
        /// </summary>

        private readonly static DifferencingService Singleton = new DifferencingService();

        private const String ASSEMBLY_NAME
            = "Microsoft.SqlServer.Smo";

        private const String PROVIDER_NAME_AVALIABLE_VALUE_0
            = "Microsoft.SqlServer.Management.Smo.SmoAvaliablePropertyProvider";

        private const String PROVIDER_NAME_AVALIABLE_VALUE_1
            = "Microsoft.SqlServer.Management.Smo.OnlineSmoAvailablePropertyProvider";

        private const String PROVIDER_NAME_NODE_ADAPTER_0
            = "Microsoft.SqlServer.Management.Smo.SmoNodeAdapterProvider";

        private const String PROVIDER_NAME_COLLECTION_SORTER_0
            = "Microsoft.SqlServer.Management.Smo.SmoCollectionSortingProvider";

        private const String PROVIDER_NAME_PROP_COMPARER_0
            = "Microsoft.SqlServer.Management.Smo.SmoPropertyComparerProvider";

        /// <summary>
        /// Constructor
        /// </summary>
        private DifferencingService()
        {
        }

        private static void RegisterProvider(ProviderRegistry myRegistry, String assemblyName, String name)
        {
            Provider provider = CreateProviderInstance(assemblyName, name);
            if (provider != null)
            {
                RegisterProvider(myRegistry, provider);
            }
        }

        private static void RegisterProvider(ProviderRegistry myRegistry, Provider provider)
        {
            if (provider is SfcNodeAdapterProvider)
            {
                myRegistry.RegisterProvider(provider as SfcNodeAdapterProvider);
            }
            else if (provider is NodeItemNamesAdapterProvider)
            {
                myRegistry.RegisterProvider(provider as NodeItemNamesAdapterProvider);
            }
            else if (provider is ContainerSortingProvider)
            {
                myRegistry.RegisterProvider(provider as ContainerSortingProvider);
            }
            else if (provider is AvailablePropertyValueProvider)
            {
                myRegistry.RegisterProvider(provider as AvailablePropertyValueProvider);
            }
            else if (provider is PropertyComparerProvider)
            {
                myRegistry.RegisterProvider(provider as PropertyComparerProvider);
            }
            else
            {
                if (SmoEventSource.Log.IsEnabled(EventLevel.Verbose, SmoEventSource.Keywords.Differencing))
                {
                    SmoEventSource.Log.DifferencingTrace($"The type of provider, '{provider.GetType().Name}', is not recognized.");
                }
            }
        }

        private static Provider CreateProviderInstance(String assemblyName, String name)
        {
            try
            {
                Object obj = Microsoft.SqlServer.Management.Sdk.Sfc.ObjectCache.CreateObjectInstance(assemblyName, name);
                if (obj is Provider)
                {
                    return obj as Provider;
                }
                else
                {
                    SmoEventSource.Log.DifferencingTrace($"The type of provider, '{name}', is not recognized.");
                    return null;
                }
            }
            catch (Exception e) when (!Differencer.IsSystemGeneratedException(e))
            {
                SmoEventSource.Log.DifferencingTrace($"Exception loading provider, '{name}'.");
                return null;
            }
        }

        /// <summary>
        /// Singleton method to           obtain the service
        /// </summary>
        public static DifferencingService Service
        {
            get { return Singleton; }
        }

        /// <summary>
        /// Create a Differencer
        /// </summary>
        /// <returns></returns>
        public IDifferencer CreateDifferencer()
        {
            return new Differencer(CreateDefaultRegistry());
        }

        /// <summary>
        /// Create a Differencer, with specified providers
        /// </summary>
        /// <returns></returns>
        public IDifferencer CreateDifferencer(ProviderRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException("register");
            }
            return new Differencer(registry);
        }

        /// <summary>
        /// Create a ProviderRegistry with default provider registered.
        /// </summary>
        /// <returns></returns>
        public ProviderRegistry CreateDefaultRegistry()
        {
            ProviderRegistry myRegistry = new ProviderRegistry();
            RegisterProvider(myRegistry, new MetadataNodeItemNamesProvider());
            RegisterProvider(myRegistry, ASSEMBLY_NAME, PROVIDER_NAME_AVALIABLE_VALUE_0);
            RegisterProvider(myRegistry, ASSEMBLY_NAME, PROVIDER_NAME_AVALIABLE_VALUE_1);
            RegisterProvider(myRegistry, ASSEMBLY_NAME, PROVIDER_NAME_NODE_ADAPTER_0);
            RegisterProvider(myRegistry, ASSEMBLY_NAME, PROVIDER_NAME_COLLECTION_SORTER_0);
            RegisterProvider(myRegistry, ASSEMBLY_NAME, PROVIDER_NAME_PROP_COMPARER_0);
            return myRegistry;
        }
    }
}
