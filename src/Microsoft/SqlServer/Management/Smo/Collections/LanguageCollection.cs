// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    public sealed partial class LanguageCollection : SimpleObjectCollectionBase<Language, Server>
    {
        
        /// <summary>
        /// Overrides ItemById to use the LocaleID property as the key.
        /// </summary>
        /// <param name="lcid"></param>
        /// <returns></returns>
        public override Language ItemById(int lcid) => GetItemById(lcid, nameof(Language.LocaleID));
    }
}
