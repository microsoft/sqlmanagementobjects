// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.Reflection;
using System.Resources;

[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguageAttribute("en-US")]
#if APTCA_ENABLED
[assembly: System.Security.AllowPartiallyTrustedCallers]
// make code access security compatible with Netfx 2.0
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)] 
#endif
