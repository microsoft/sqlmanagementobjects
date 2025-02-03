﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.Data.Tools.Sql.BatchParser
{
    internal sealed class Token
    {
        internal Token(LexerTokenType tokenType, PositionStruct begin, PositionStruct end, string text, string filename)
        {
            TokenType = tokenType;
            Begin = begin;
            End = end;
            Text = text;
            Filename = filename;
        }

        public string Filename { get; private set; }

        public PositionStruct Begin { get; private set; }

        public PositionStruct End { get; private set; }

        public string Text { get; private set; }

        public LexerTokenType TokenType { get; private set; }
    }
}
