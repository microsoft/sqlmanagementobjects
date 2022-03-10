// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.Data.Tools.Sql.BatchParser
{
    internal enum LexerTokenType
    {
        None,
        Text,
        TextVerbatim,
        Whitespace,
        NewLine,
        String,
        Eof,
        Error,
        Comment,

        // batch commands
        Go,
        Reset,
        Ed,
        Execute,
        Quit,
        Exit,
        Include,
        Serverlist,
        Setvar,
        List,
        ErrorCommand,
        Out,
        Perftrace,
        Connect,
        OnError,
        Help,
        Xml,
        ListVar,
    }
}
