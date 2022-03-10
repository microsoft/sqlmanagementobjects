// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// SFC Collection class for Action.
    /// </summary>
    public sealed class ActionCollection : SfcCollatedDictionaryCollection<Action, Action.Key, Event>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        internal ActionCollection(Event parent)
            : base(parent, parent.Parent.Parent.GetComparer())
        {
        }


        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.XEvent.Action"/> with the specified name.
        /// </summary>
        /// <value>name of the action</value>
        /// <returns>Action with the specify name</returns>
        public Action this[string name]
        {
            get { return this[new Action.Key(name)]; }
        }

        /// <summary>
        /// Determines whether the collection contains the Action.
        /// </summary>
        /// <param name="name">The name of the Action.</param>
        /// <returns>
        ///     <c>true</c> if the collection contains Action; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Contains"))
            {
                tm.TraceParameterIn("name", name);
                return Contains(new Action.Key(name));
            }
        }

        /// <summary>
        /// Gets the element factory impl.
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return Action.GetObjectFactory();
        }

        /// <summary>
        /// Adds the obj to the internal shadow collection
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool AddShadow(Action obj)
        {
            try
            {
                return base.AddShadow(obj);
            }
            catch(ArgumentException e)
            {

                throw new XEventException(ExceptionTemplates.ActionNameNotUnique(obj.Name), e);
            }            
        }
    }

}
