// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class UserDefinedMessage : MessageObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IAlterable, IScriptable
    {
        internal UserDefinedMessage(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Creates a new UserDefinedMessage object.
        /// </summary>
        public UserDefinedMessage () :
            base()
        {
        }

        /// <summary>
        /// Creates a new UserDefinedMessage object; with the Server and message id
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        public UserDefinedMessage(Server server, System.Int32 id) : 
            base()
        {
            this.key = new MessageObjectKey(id, MessageCollectionBase<UserDefinedMessage>.GetDefaultLanguage());
            this.Parent = server;
        }

        /// <summary>
        /// Creates a new UserDefinedMessage object; with the Server, message id, 
        /// and language specified.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        /// <param name="language"></param>
        public UserDefinedMessage(Server server, System.Int32 id, System.String language) :
            base()
        {
            this.key = new MessageObjectKey(id, language);
            this.Parent = server;
        }
        
        /// <summary>
        /// Creates a new UserDefinedMessage object; with the Server, 
        /// message id, and language, severity and message text specified.
        /// Note: this is the minimal constructor which allows inline construction.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        public UserDefinedMessage(Server server, System.Int32 id, System.String language, 
                                    System.Int32 severity, System.String message)
        {
            this.key = new MessageObjectKey(id, language);
            this.Parent = server;
            this.Severity = severity;
            this.Text = message;
        }

        /// <summary>
        /// Creates a new UserDefinedMessage object; with the Server, message id, 
        /// and language, severity, message text, and logging specified. 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        /// <param name="isLogged"></param>
        public UserDefinedMessage(Server server, System.Int32 id, System.String language, 
            System.Int32 severity, System.String message, System.Boolean isLogged)    
        {
            this.key = new MessageObjectKey(id, language);
            this.Parent = server;
            this.Severity = severity;
            this.Text = message;
            this.IsLogged = isLogged;
        }

        /// <summary>
        /// Creates a new UserDefinedMessage object; with the Server, message id, 
        /// and language id, severity and message text specified.
        /// Note: this is the minimal constructor which allows inline construction 
        /// (language type differs from above).
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        public UserDefinedMessage(Server server, System.Int32 id, System.Int32 language, 
            System.Int32 severity, System.String message)    
        {
            Language lang = server.Languages.ItemById(language);

            if (null == lang)
            {
                throw new PropertyNotSetException("language");
            }

            this.key = new MessageObjectKey(id, lang.Name);
            this.Parent = server;
            this.Severity = severity;
            this.Text = message;
        }

        /// <summary>
        /// Creates a new UserDefinedMessage object; with the Server, message id, 
        /// and language id, severity, message text, and logging specified. 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        /// <param name="isLogged"></param>
        public UserDefinedMessage(Server server, System.Int32 id, System.Int32 language, 
            System.Int32 severity, System.String message, System.Boolean isLogged)
        {
            Language lang = server.Languages.ItemById(language);

            if (null == lang)
            {
                throw new PropertyNotSetException("language");
            }

            this.key = new MessageObjectKey(id, lang.Name);
            this.Parent = server;
            this.Severity = severity;
            this.Text = message;
            this.IsLogged = isLogged;
        }

        /// <summary>
        /// Message ID
        /// </summary>
        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public Int32 ID
        {
            get
            {
                return ((MessageObjectKey)key).ID;
            }
            set
            {
                ((MessageObjectKey)key).ID = value;
                UpdateObjectState();
            }
        }

        /// <summary>
        /// Language
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        public string Language
        {
            get
            {
                return ((MessageObjectKey)key).Language;
            }
            set
            {
                ValidateLanguage(value);
                ((MessageObjectKey)key).Language = value;
                UpdateObjectState();
            }
        }

        internal void ValidateState()
        {
            if (this.State != SqlSmoState.Pending)
            {
                // try to find if the object is already a member of the 
                // parent collection, and in this case we'll need to throw
                if (this.key != null && !key.Writable )
                {
                    throw new FailedOperationException(ExceptionTemplates.OperationOnlyInPendingState);
                }
            }
        }

        internal void ValidateLanguage(string language)
        {
            if (null == language)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Language"));
            }

            ValidateState();
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                CheckObjectState();
                return base.ParentColl.ParentInstance as Server;
            }
            set
            {
                SetParentImpl(value);
            }
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "UserDefinedMessage";
            }
        }
        
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            createQuery.Append("EXEC master.dbo.sp_addmessage ");

            createQuery.AppendFormat(SmoApplication.DefaultCulture,
                            "@msgnum={0}", ((MessageObjectKey)this.key).ID);

            string language = string.Empty;
            Property propLangId = Properties.Get("LanguageID");
            if (null != propLangId.Value)
            {
                LanguageCollection lcol = ((Server)ParentColl.ParentInstance).Languages;
                foreach (Language lang in lcol)
                {
                    if (lang.LangID == (Int32)propLangId.Value)
                    {
                        language = lang.Name;
                    }
                }
            }
            // notice that Language takes precedence
            if (null != ((MessageObjectKey)this.key).Language)
            {
                language = ((MessageObjectKey)this.key).Language;
            }

            if (0 == language.Length)
            {
                throw new PropertyNotSetException("Language");
            }

            createQuery.AppendFormat(SmoApplication.DefaultCulture, ", @lang=N'{0}'", SqlString(language));

            int count = 2;
            GetParameter(createQuery, sp, "Severity", "@severity={0}", ref count, true);
            GetStringParameter(createQuery, sp, "Text", "@msgtext=N'{0}'", ref count, true);
            GetBoolParameter(createQuery, sp, "IsLogged", "@with_log={0}", ref count, true);
            queries.Add(createQuery.ToString());

        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            sb.Append("EXEC master.dbo.sp_addmessage ");

            sb.AppendFormat(SmoApplication.DefaultCulture, " @msgnum={0}, @lang=N'{1}'",
                                    ((MessageObjectKey)this.key).ID, ((MessageObjectKey)this.key).Language);

            int count = 2;
            Property prop = Properties["Severity"];
            if (null != prop.Value)
            {
                if (count++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, "@severity={0}", prop.Value.ToString());
            }

            prop = Properties["Text"];
            if (null != prop.Value)
            {
                if (count++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, "@msgtext=N'{0}'",
                                        SqlString(prop.Value.ToString()));
            }

            GetBoolParameter(sb, sp, "IsLogged", "@with_log={0}", ref  count, true);
            sb.AppendFormat(SmoApplication.DefaultCulture,", @replace='REPLACE'");

            StringCollection propColl = new StringCollection();
            propColl.Add("Severity");
            propColl.Add("Text");
            propColl.Add("IsLogged");
            if (this.Properties.ArePropertiesDirty(propColl))
            {
                alterQuery.Add(sb.ToString());
            }
        }
        
        public void Drop()
        {
            base.DropImpl();
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            string language = ((MessageObjectKey)this.key).Language;
            if (language == "us_english")
            {
                language = "all";
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC master.dbo.sp_dropmessage @msgnum={0}, @lang=N'{1}'",
                                    ((MessageObjectKey)this.key).ID, language);
            queries.Add(sb.ToString());
        }
        
        protected override void PostDrop()
        {
            var language = ((MessageObjectKey)key).Language;
            // if we have just dropped the english message, make sure the other messages 
            // are removed from the collection
            if( language == "us_english" )
            {
                Parent.Refresh();
            }
        }
        
        public StringCollection Script()
        {
            return ScriptImpl();
        }
        
        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            string[] fields = {   
                                        "ID",
                                        "Language"};
            List<string> list = GetSupportedScriptFields(typeof(UserDefinedMessage.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}


