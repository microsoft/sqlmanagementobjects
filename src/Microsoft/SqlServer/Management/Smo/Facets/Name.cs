// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Name Facet
    /// </summary>
    [CLSCompliantAttribute(false)]
    [EvaluationMode(AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.FacetSR")]
    [Sfc.DisplayNameKey("NameName")]
    [Sfc.DisplayDescriptionKey("NameDesc")]
    public interface INameFacet : Sfc.IDmfFacet
    {
        /// <summary>
        /// Name
        /// </summary>
        [Sfc.DisplayNameKey("NameName")]
        [Sfc.DisplayDescriptionKey("NameDesc")]
        string Name
        {
            get;
        }

    }

    /// <summary>
    /// Wrapper for the INameFacet Interface
    /// </summary>
    internal sealed class NameAdapter : IDmfAdapter, INameFacet, IRefreshable
    {
        private const string cName = "Name";

        Microsoft.SqlServer.Management.Smo.NamedSmoObject wrappedObject = null;

        #region Constructors
        public NameAdapter(Microsoft.SqlServer.Management.Smo.Table obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.Index obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.StoredProcedure obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.Trigger obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.SqlAssembly obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.View obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.UserDefinedFunction obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.Synonym obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.Sequence obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.UserDefinedType obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.Rule obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.Default obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.User obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.AsymmetricKey obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.SymmetricKey obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.Certificate obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.DatabaseRole obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.ApplicationRole obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.Schema obj)
        {
            this.wrappedObject = obj;
        }

        public NameAdapter(Microsoft.SqlServer.Management.Smo.XmlSchemaCollection obj)
        {
            this.wrappedObject = obj;
        }

        #endregion Constructors

        public string Name
        {
            get { return ((NamedSmoObject)wrappedObject).Name; }
        }

        public void Refresh()
        {
            this.wrappedObject.Refresh();
        }

    }


}
