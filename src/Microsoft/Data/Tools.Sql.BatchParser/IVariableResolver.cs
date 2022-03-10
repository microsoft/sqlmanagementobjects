// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.Data.Tools.Sql.BatchParser
{
    internal interface IVariableResolver
    {
        string GetVariable(PositionStruct pos, string name);
        void SetVariable(PositionStruct pos, string name, string value);
    }
}
