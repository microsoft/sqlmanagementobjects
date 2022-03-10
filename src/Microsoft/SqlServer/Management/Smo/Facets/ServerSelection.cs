// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.ComponentModel;

using System.Data;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{

    [CLSCompliantAttribute(false)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [Sfc.DisplayNameKey("IServerSelection_Name")]
    [Sfc.DisplayDescriptionKey("IServerSelection_Desc")]
    public interface IServerSelectionFacet : Sfc.IDmfFacet
    {
        #region Interface Properties

        [Sfc.DisplayNameKey("Server_BuildNumberName")]
        [Sfc.DisplayDescriptionKey("Server_BuildNumberDesc")]
        System.Int32 BuildNumber
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_CollationName")]
        [Sfc.DisplayDescriptionKey("Server_CollationDesc")]
        System.String Collation
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_EditionName")]
        [Sfc.DisplayDescriptionKey("Server_EditionDesc")]
        System.String Edition
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_IsCaseSensitiveName")]
        [Sfc.DisplayDescriptionKey("Server_IsCaseSensitiveDesc")]
        System.Boolean IsCaseSensitive
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_LanguageName")]
        [Sfc.DisplayDescriptionKey("Server_LanguageDesc")]
        System.String Language
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_NamedPipesEnabledName")]
        [Sfc.DisplayDescriptionKey("Server_NamedPipesEnabledDesc")]
        System.Boolean NamedPipesEnabled
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_OSVersionName")]
        [Sfc.DisplayDescriptionKey("Server_OSVersionDesc")]
        System.String OSVersion
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_PlatformName")]
        [Sfc.DisplayDescriptionKey("Server_PlatformDesc")]
        System.String Platform
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_TcpIpProtocolEnabledName")]
        [Sfc.DisplayDescriptionKey("Server_TcpEnabledDesc")]
        System.Boolean TcpEnabled
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_VersionMajorName")]
        [Sfc.DisplayDescriptionKey("Server_VersionMajorDesc")]
        System.Int32 VersionMajor
        {
            get;
        }

        [Sfc.DisplayNameKey("Server_VersionMinorName")]
        [Sfc.DisplayDescriptionKey("Server_VersionMinorDesc")]
        System.Int32 VersionMinor
        {
            get;
        }

        #endregion
    }

    /// <summary>
    /// Server Selection.  This facet has logical properties enabling users to chose properties to select server.
    /// It inherits from the ServerAdapterBase.
    /// </summary>
    public class ServerSelectionAdapter : ServerAdapterBase, IDmfAdapter, IServerSelectionFacet
    {
        #region Constructors
        public ServerSelectionAdapter(Microsoft.SqlServer.Management.Smo.Server obj)
            : base(obj)
        {
        }
        #endregion


        #region IServerSelectionFacet Members

        public int BuildNumber
        {
            get { return this.Server.BuildNumber; }
        }

        public string Edition
        {
            get { return this.Server.Edition; }
        }

        public bool IsCaseSensitive
        {
            get { return this.Server.IsCaseSensitive; }
        }

        public string Language
        {
            get { return this.Server.Language; }
        }

        public string OSVersion
        {
            get { return this.Server.OSVersion; }
        }

        public string Platform
        {
            get { return this.Server.Platform; }
        }

        public int VersionMajor
        {
            get { return this.Server.VersionMajor; }
        }

        public int VersionMinor
        {
            get { return this.Server.VersionMinor; }
        }

        #endregion
    }
}

