// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Binding List that supports sorting
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SortableBindingList<T> : BindingList<T>
    {
        private PropertyDescriptor propertyDescriptor;
        private ListSortDirection listSortDirection;
        private bool isSorted;
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public SortableBindingList()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortableBindingList&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="list">An <see cref="T:System.Collections.Generic.IList`1"/> of items to be contained in the <see cref="T:System.ComponentModel.BindingList`1"/>.</param>
        public SortableBindingList(IList<T> list)
            :this()
        {
            this.Items.Clear();
            foreach (T item in list)
            {
                this.Add(item);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the list supports sorting.
        /// </summary>
        /// <value></value>
        /// <returns>true if the list supports sorting; otherwise, false. The default is false.</returns>
        protected override bool SupportsSortingCore
        {
            get 
            { 
                return true; 
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the list is sorted.
        /// </summary>
        /// <value></value>
        /// <returns>true if the list is sorted; otherwise, false. The default is false.</returns>
        protected override bool IsSortedCore
        {
            get 
            { 
                return this.isSorted; 
            }
        }

        /// <summary>
        /// Gets the property descriptor that is used for sorting the list if sorting is implemented in a derived class; otherwise, returns null.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.ComponentModel.PropertyDescriptor"/> used for sorting the list.</returns>
        protected override PropertyDescriptor SortPropertyCore
        {
            get 
            { 
                return this.propertyDescriptor; 
            }
        }

        /// <summary>
        /// Gets the direction the list is sorted.
        /// </summary>
        /// <value></value>
        /// <returns>One of the <see cref="T:System.ComponentModel.ListSortDirection"/> values. The default is <see cref="F:System.ComponentModel.ListSortDirection.Ascending"/>. </returns>
        protected override ListSortDirection SortDirectionCore
        {
            get 
            { 
                return this.listSortDirection; 
            }
        }

        /// <summary>
        /// Sorts the items if overridden in a derived class; otherwise, throws a <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <param name="prop">A <see cref="T:System.ComponentModel.PropertyDescriptor"/> that specifies the property to sort on.</param>
        /// <param name="direction">One of the <see cref="T:System.ComponentModel.ListSortDirection"/>  values.</param>
        /// <exception cref="T:System.NotSupportedException">Method is not overridden in a derived class. </exception>
        protected override void ApplySortCore(PropertyDescriptor prop, 
            ListSortDirection direction)
        {
            List<T> itemsList = this.Items as List<T>;

            this.propertyDescriptor = prop;
            this.listSortDirection = direction;
            this.isSorted = true;

            itemsList.Sort(this.GetComparisonDelegate(prop, direction));
        }

        /// <summary>
        /// Removes any sort applied with <see cref="M:System.ComponentModel.BindingList`1.ApplySortCore(System.ComponentModel.PropertyDescriptor,System.ComponentModel.ListSortDirection)"/> if sorting is implemented in a derived class; otherwise, raises <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">Method is not overridden in a derived class. </exception>
        protected override void RemoveSortCore()
        {
            this.isSorted = false;
            this.propertyDescriptor = base.SortPropertyCore;
            this.listSortDirection = base.SortDirectionCore;
        }


        /// <summary>
        /// Gets the comparison delegate for sorting.
        /// </summary>
        /// <param name="propertyDescriptor">The property descriptor.</param>
        /// <param name="direction">The sortdirection.</param>
        /// <returns></returns>
        protected virtual Comparison<T> GetComparisonDelegate(PropertyDescriptor propertyDescriptor,
            ListSortDirection direction)
        {
            Comparison<T> comparisionDelegate = delegate(T t1, T t2)
            {
                int reverse = (direction == ListSortDirection.Ascending) ? 1 : -1;

                int comparisionValue;
                if (propertyDescriptor.PropertyType == typeof(string))
                {
                    comparisionValue = StringComparer.CurrentCulture.Compare(propertyDescriptor.GetValue(t1),
                        propertyDescriptor.GetValue(t2));
                }
                else
                {
                    comparisionValue = Comparer.Default.Compare(propertyDescriptor.GetValue(t1),
                        propertyDescriptor.GetValue(t2));
                }

                return reverse * comparisionValue;
            };

            return comparisionDelegate;
         }
    }
}
