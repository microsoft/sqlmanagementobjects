// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Differencing.SPI
{
    /// <summary>
	/// This class is an entry points for differencer to obtains its provider. Domain specific 
    /// provider of the Diff service can be registered into this class.
    /// 
	/// A provider supplies domain specific information or logic so that the differencer can 
    /// handle different graph type and runtime behaviour correctly.
    /// 
    /// A provider must be registered before the compare method is invoked.
    /// </summary>
    public class ProviderRegistry
    {
        private static Object monitor = new Object();

        private List<SfcNodeAdapterProvider> navigators = new List<SfcNodeAdapterProvider>();

        private List<AvailablePropertyValueProvider> props = new List<AvailablePropertyValueProvider>();

        private List<NodeItemNamesAdapterProvider> names = new List<NodeItemNamesAdapterProvider>();

        private List<ContainerSortingProvider> sorters = new List<ContainerSortingProvider>();

        private List<PropertyComparerProvider> propComps = new List<PropertyComparerProvider>();

		/// <summary>
		/// Constructor
		/// </summary>
		public ProviderRegistry()
		{
		}

        /// <summary>
        /// Obtain a list of registered SfcNodeAdapterProvider.
        /// </summary>
        public ICollection<SfcNodeAdapterProvider> SfcNodeAdapterProviders
        {
            get
            {
                lock (monitor)
                {
                    return new List<SfcNodeAdapterProvider>(navigators);
                }
            }
        }

        /// <summary>
        /// Register the specified provider
        /// </summary>
        public bool RegisterProvider(SfcNodeAdapterProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (navigators.Contains(provider))
                {
                    return false;
                }

                navigators.Insert(0, provider);
                return true;
            }
        }

        /// <summary>
        /// Unregister the specified provider
        /// </summary>
        public bool UnregisterProvider(SfcNodeAdapterProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (!navigators.Contains(provider))
                {
                    return false;
                }

                navigators.Remove(provider);
                return true;
            }
        }

        /// <summary>
        /// Obtain a list of registered AvailablePropertyValueProvider.
        /// </summary>
        public ICollection<AvailablePropertyValueProvider> AvailablePropertyValueProviders
        {
            get
            {
                lock (monitor)
                {
                    return new List<AvailablePropertyValueProvider>(props);
                }
            }
        }

        /// <summary>
        /// Register the specified provider
        /// </summary>
        public bool RegisterProvider(AvailablePropertyValueProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (props.Contains(provider))
                {
                    return false;
                }

                props.Insert(0, provider);
                return true;
            }
        }

        /// <summary>
        /// Unregister the specified provider
        /// </summary>
        public bool UnregisterProvider(AvailablePropertyValueProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (!props.Contains(provider))
                {
                    return false;
                }

                props.Remove(provider);
                return true;
            }
        }

        /// <summary>
        /// Obtain a list of registered SfcNodeAdapterProvider.
        /// </summary>
        public ICollection<PropertyComparerProvider> PropertyComparerProviders
        {
            get
            {
                lock (monitor)
                {
                    return new List<PropertyComparerProvider>(propComps);
                }
            }
        }

        /// <summary>
        /// Register the specified provider
        /// </summary>
        public bool RegisterProvider(PropertyComparerProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (propComps.Contains(provider))
                {
                    return false;
                }

                propComps.Insert(0, provider);
                return true;
            }
        }

        /// <summary>
        /// Unregister the specified provider
        /// </summary>
        public bool UnregisterProvider(PropertyComparerProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (!propComps.Contains(provider))
                {
                    return false;
                }

                propComps.Remove(provider);
                return true;
            }
        }

        /// <summary>
        /// Obtain a list of registered NodeItemNamesAdapterProvider
        /// </summary>
        public ICollection<NodeItemNamesAdapterProvider> NodeItemNameAdapterProviders
        {
            get
            {
                lock (monitor)
                {
                    return new List<NodeItemNamesAdapterProvider>(names);
                }
            }
        }

        /// <summary>
        /// Register the specified provider
        /// </summary>
        public bool RegisterProvider(NodeItemNamesAdapterProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (names.Contains(provider))
                {
                    return false;
                }

                names.Insert(0, provider);
                return true;
            }
        }

        /// <summary>
        /// Unregister the specified provider
        /// </summary>
        public bool UnregisterProvider(NodeItemNamesAdapterProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (!names.Contains(provider))
                {
                    return false;
                }

                names.Remove(provider);
                return true;
            }
        }

        /// <summary>
        /// Obtain a list of registered ContainerSortingProvider
        /// </summary>
        public ICollection<ContainerSortingProvider> ContainerSortingProviders
        {
            get
            {
                lock (monitor)
                {
                    return new List<ContainerSortingProvider>(sorters);
                }
            }
        }

        /// <summary>
        /// Register the specified provider
        /// </summary>
        public bool RegisterProvider(ContainerSortingProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (sorters.Contains(provider))
                {
                    return false;
                }

                sorters.Insert(0, provider);
                return true;
            }
        }

        /// <summary>
        /// Unregister the specified provider
        /// </summary>
        public bool UnregisterProvider(ContainerSortingProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            lock (monitor)
            {
                if (!sorters.Contains(provider))
                {
                    return false;
                }

                sorters.Remove(provider);
                return true;
            }
        }
    }
}
