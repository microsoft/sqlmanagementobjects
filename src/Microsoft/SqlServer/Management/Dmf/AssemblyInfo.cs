// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if APTCA_ENABLED
[assembly: System.Security.AllowPartiallyTrustedCallers]
// make code access security compatible with Netfx 2.0
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)] 
#endif
