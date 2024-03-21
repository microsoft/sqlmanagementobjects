// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.ComponentModel;

using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Identity Facet
    /// </summary>
    [CLSCompliantAttribute(false)]
    [StateChangeEvent("CREATE_TABLE", "TABLE")]
    [StateChangeEvent("ALTER_TABLE", "TABLE")]
    [StateChangeEvent("RENAME", "TABLE")]
    [StateChangeEvent("CREATE_VIEW", "VIEW")]
    [StateChangeEvent("ALTER_VIEW", "VIEW")]
    [StateChangeEvent("RENAME", "VIEW")]
    [StateChangeEvent("CREATE_FUNCTION", "FUNCTION")]
    [StateChangeEvent("ALTER_FUNCTION", "FUNCTION")]
    [StateChangeEvent("RENAME", "FUNCTION")]
    [StateChangeEvent("CREATE_PROCEDURE", "PROCEDURE")]
    [StateChangeEvent("ALTER_PROCEDURE", "PROCEDURE")]
    [StateChangeEvent("RENAME", "PROCEDURE")]
    [StateChangeEvent("CREATE_SYNONYM", "SYNONYM")]
    [StateChangeEvent("CREATE_SEQUENCE", "SEQUENCE")]
    [StateChangeEvent("ALTER_SEQUENCE", "SEQUENCE")]
    [StateChangeEvent("RENAME", "SEQUENCE")]
    [StateChangeEvent("CREATE_TYPE", "TYPE")]
    [StateChangeEvent("RENAME", "TYPE")]
    [StateChangeEvent("CREATE_XML_SCHEMA_COLLECTION", "XMLSCHEMACOLLECTION")]
    [StateChangeEvent("ALTER_XML_SCHEMA_COLLECTION", "XMLSCHEMACOLLECTION")]
    [StateChangeEvent("RENAME", "XMLSCHEMACOLLECTION")]
    [StateChangeEvent("ALTER_SCHEMA", "TABLE")]
    [StateChangeEvent("ALTER_SCHEMA", "VIEW")]
    [StateChangeEvent("ALTER_SCHEMA", "FUNCTION")]
    [StateChangeEvent("ALTER_SCHEMA", "PROCEDURE")]
    [StateChangeEvent("ALTER_SCHEMA", "SYNONYM")]
    [StateChangeEvent("ALTER_SCHEMA", "SEQUENCE")]
    [StateChangeEvent("ALTER_SCHEMA", "TYPE")]
    [StateChangeEvent("ALTER_SCHEMA", "XMLSCHEMACOLLECTION")]
    [EvaluationMode(AutomatedPolicyEvaluationMode.CheckOnChanges | AutomatedPolicyEvaluationMode.CheckOnSchedule | AutomatedPolicyEvaluationMode.Enforce)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.FacetSR")]
    [Sfc.DisplayNameKey("MultipartNameName")]
    [Sfc.DisplayDescriptionKey("MultipartNameDesc")]
    public interface IMultipartNameFacet : Sfc.IDmfFacet
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

        /// <summary>
        /// Schema
        /// </summary>
        [Sfc.DisplayNameKey("SchemaName")]
        [Sfc.DisplayDescriptionKey("SchemaDesc")]
        string Schema
        {
            get;
        }
    }

    /// <summary>
    /// Wrapper for the IMultipartNameFacet Interface
    /// </summary>
    internal sealed class MultipartNameAdapter : IDmfAdapter, IMultipartNameFacet, IRefreshable
    {
        private const string cName = "Name";
        private const string cSchema = "Schema";

        ScriptSchemaObjectBase wrappedObject = null;

        string schema = string.Empty;

        #region Constructors
        public MultipartNameAdapter(Microsoft.SqlServer.Management.Smo.Table obj)
        {
            this.wrappedObject = obj;
        }

        public MultipartNameAdapter(Microsoft.SqlServer.Management.Smo.View obj)
        {
            this.wrappedObject = obj;
        }
        public MultipartNameAdapter(Microsoft.SqlServer.Management.Smo.UserDefinedFunction obj)
        {
            this.wrappedObject = obj;
        }
        public MultipartNameAdapter(Microsoft.SqlServer.Management.Smo.StoredProcedure obj)
        {
            this.wrappedObject = obj;
        }
        public MultipartNameAdapter(Microsoft.SqlServer.Management.Smo.Synonym obj)
        {
            this.wrappedObject = obj;
        }
        public MultipartNameAdapter(Microsoft.SqlServer.Management.Smo.Sequence obj)
        {
            this.wrappedObject = obj;
        }
        public MultipartNameAdapter(Microsoft.SqlServer.Management.Smo.UserDefinedType obj)
        {
            this.wrappedObject = obj;
        }
        public MultipartNameAdapter(Microsoft.SqlServer.Management.Smo.XmlSchemaCollection obj)
        {
            this.wrappedObject = obj;
        }

        #endregion Constructors

        public string Name
        {
            get { return ((NamedSmoObject)wrappedObject).Name; }
        }

        public string Schema
        {
            get { return ((ScriptSchemaObjectBase)this.wrappedObject).Schema; }
        }

        public void Refresh()
        {
            this.wrappedObject.Refresh();
        }

    }


}
