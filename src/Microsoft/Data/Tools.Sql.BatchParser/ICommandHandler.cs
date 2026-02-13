// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System.IO;

namespace Microsoft.Data.Tools.Sql.BatchParser
{
    internal interface ICommandHandler
    {
        BatchParserAction Go(TextBlock batch, int repeatCount);
        BatchParserAction OnError(Token token, OnErrorAction action);
        BatchParserAction Include(TextBlock filename, out TextReader stream, out string newFilename);
    }
}
