// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo.Mail
{
    [SfcElementType("Mail")]
	public partial class SqlMail : SqlSmoObject, IScriptable
	{
		internal SqlMail(Server parentsrv, ObjectKeyBase key, SqlSmoState state) : 
			base(key, state)
		{
            singletonParent = parentsrv as Server;
			
			// Mail does not live in a collection, but directly under the Database
			SetServerObject( parentsrv.GetServerObject());
			m_comparer = parentsrv.Databases["msdb"].StringComparer;
		}

		protected internal override string CollationDatabaseInServer => "msdb";

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

		internal protected override string GetDBName()
		{
			return "msdb";
		}

		// returns the name of the type in the urn expression
		public static string UrnSuffix
		{
			get 
			{
				return "Mail";
			}
		}

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
		{
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);
			foreach (ConfigurationValue configValue in ConfigurationValues)
			{
				configValue.ScriptCreateInternal(queries,sp);
			}

			foreach (MailAccount mailAccount in Accounts)
			{
                mailAccount.ScriptCreateInternal(queries, sp);				
			}

			foreach (MailProfile mailProfile in Profiles)
			{
                mailProfile.ScriptCreateInternal(queries, sp);
			}
		}

		/// <summary>
		/// Generate object creation script using default scripting options
		/// </summary>
		/// <returns></returns>
		public StringCollection Script()
		{
			return ScriptImpl();
		}

		/// <summary>
		/// Script object with specific scripting options
		/// </summary>
		/// <param name="scriptingOptions"></param>
		/// <returns></returns>
		public StringCollection Script(ScriptingOptions scriptingOptions)
		{
			return ScriptImpl(scriptingOptions);
		}

		MailProfileCollection mailProfiles;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(MailProfile))]
		public MailProfileCollection Profiles
		{
			get
			{
				ThrowIfBelowVersion90();
				if (mailProfiles == null)
				{
					mailProfiles = new MailProfileCollection(this);
				}
				return mailProfiles;
			}
		}

		MailAccountCollection mailAccounts;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(MailAccount))]
		public MailAccountCollection Accounts
		{
			get
			{
				ThrowIfBelowVersion90();
                if (mailAccounts == null)
				{
					mailAccounts = new MailAccountCollection(this);
				}
				return mailAccounts;
			}
		}

		ConfigurationValueCollection configuratonValues;
        [SfcObject( SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ConfigurationValue))]
		public ConfigurationValueCollection ConfigurationValues
		{
			get
			{
				ThrowIfBelowVersion90();
				if (configuratonValues == null)
				{
					configuratonValues = new ConfigurationValueCollection(this);
				}
				return configuratonValues;
			}
		}
	}
}


