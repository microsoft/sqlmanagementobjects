// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class Information : SqlSmoObject
	{
		internal Information(Server parentsrv, ObjectKeyBase key, SqlSmoState state) : 
					base(key, state)
		{
            singletonParent = parentsrv as Server;
			SetServerObject( parentsrv.GetServerObject());
		}

        [SfcObject(SfcObjectRelationship.ParentObject)]
		public Server Parent
		{
			get 
			{
				CheckObjectState();
                return singletonParent as Server;
			}
		}

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        // returns the name of the type in the urn expression
		public static string UrnSuffix
		{
			get 
			{
				return "Information";
			}
		}

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone)]
		public Version Version
		{
			get
			{
				ServerVersion sv = this.ServerVersion;
				return new Version(sv.Major, sv.Minor, sv.BuildNumber);
			}
		}

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Edition EngineEdition
        {
            get
            {
                // from Books Online (SQL Server 2005):
                // SERVERPROPERTY('EngineEdition') returns the following
                //   1 = Personal or Desktop Engine
                //   2 = Standard
                //   3 = Enterprise, Enterprise Evaluation, or Developer
                //   4 = Express
                int result = (Int32) this.Properties.GetValueWithNullReplacement("EngineEdition");
                return (Edition) result;
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Version ResourceVersion
        {
            get
            {
                return new Version(this.ResourceVersionString);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Version BuildClrVersion
        {
            get
            {
                //BuildClrVersionString is of format 'v2.0.50727', hence getting substring leaving first character
                return new Version(this.BuildClrVersionString.Substring(1));
            }
        }
	}
}




