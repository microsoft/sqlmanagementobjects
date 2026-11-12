// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.Data.Tools.Sql.BatchParser
{
    internal sealed class BatchParserException : Exception
    {
        const string ErrorCodeName = "ErrorCode";
        const string BeginName = "Begin";
        const string EndName = "End";
        const string TextName = "Text";
        const string TokenTypeName = "TokenType";

        ErrorCode _errorCode;
        PositionStruct _begin;
        PositionStruct _end;
        string _text;
        LexerTokenType _tokenType;

        public BatchParserException(ErrorCode errorCode, Token token, string message)
            : base(message)
        {
            _errorCode = errorCode;
            _begin = token.Begin;
            _end = token.End;
            _text = token.Text;
            _tokenType = token.TokenType;
        }

        public ErrorCode ErrorCode { get { return _errorCode; } }

        public PositionStruct Begin { get { return _begin; } }

        public PositionStruct End { get { return _end; } }

        public string Text { get { return _text; } }

        public LexerTokenType TokenType { get { return _tokenType; } }

    }
}
