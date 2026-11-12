// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.Data.Tools.Sql.BatchParser
{
    [Flags]
    internal enum TextRuleFlags
    {
        ReportWhitespace = 1,
        RecognizeDoubleQuotedString = 2,
        RecognizeSingleQuotedString = 4,
        RecognizeLineComment = 8,
        RecognizeBlockComment = 16,
        RecognizeBrace = 32,
    }
}
