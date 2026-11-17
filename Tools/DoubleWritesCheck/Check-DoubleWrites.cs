#:package MSBuild.StructuredLogger@2.3.213
//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
//
// Validates that a build did not produce "double writes" -- i.e. the same
// destination file written from two or more distinct sources during the build.
// Double writes make a build order-dependent and non-deterministic.
//
// This uses the same analyzer that powers the "Double-writes" node in the
// MSBuild Structured Log Viewer (https://msbuildlog.com/).
//
// Usage:
//   dotnet run Check-DoubleWrites.cs -- --severity <error|warning> <binlog> [<binlog> ...]

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Logging.StructuredLogger;

string severity = "error";
List<string> binlogs = new();

for (int argumentIndex = 0; argumentIndex < args.Length; argumentIndex++)
{
    switch (args[argumentIndex])
    {
        case "--severity":
            if (argumentIndex + 1 >= args.Length)
            {
                Console.Error.WriteLine("Missing value for --severity. Expected 'error' or 'warning'.");
                return 2;
            }

            severity = args[++argumentIndex];
            break;

        default:
            binlogs.Add(args[argumentIndex]);
            break;
    }
}

if (binlogs.Count == 0)
{
    Console.Error.WriteLine("No binlog paths were provided.");
    return 2;
}

if (severity is not ("error" or "warning"))
{
    Console.Error.WriteLine($"Invalid --severity '{severity}'. Expected 'error' or 'warning'.");
    return 2;
}

int reportedCount = 0;

foreach (string binlog in binlogs)
{
    if (!File.Exists(binlog))
    {
        Console.Error.WriteLine($"Binlog not found: {binlog}");
        return 2;
    }

    string binlogName = Path.GetFileName(binlog);
    Console.WriteLine($"Analyzing '{binlogName}' for double writes...");

    Build build = BinaryLog.ReadBuild(binlog);
    BuildAnalyzer.AnalyzeBuild(build);

    foreach (KeyValuePair<string, HashSet<string>> doubleWrite in DoubleWritesAnalyzer.GetDoubleWrites(build))
    {
        static string EscapeForVso(string value) =>
            value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A").Replace("]", "%5D");

        string destination = EscapeForVso(doubleWrite.Key);
        string sources = EscapeForVso(string.Join("; ", doubleWrite.Value));
        reportedCount++;

        Console.WriteLine(
            $"##vso[task.logissue type={severity}]Double write in {binlogName}: '{destination}' is written from multiple sources: {sources}");
    }
}

if (reportedCount == 0)
{
    Console.WriteLine("No double writes detected.");
    return 0;
}

Console.WriteLine($"Detected {reportedCount} double write(s).");
return severity == "error" ? 1 : 0;
