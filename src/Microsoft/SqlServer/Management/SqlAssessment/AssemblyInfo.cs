// Copyright (c) Microsoft.
// Licensed under the MIT license.

// when looking up resources with user preferences set to the same culture as the 
// neutral resources language (In our case English-US), the ResourceManager will 
// automatically use the resources located in the main assembly, instead of searching 
// for the English satellite
[assembly: System.Resources.NeutralResourcesLanguageAttribute("en-US")]
