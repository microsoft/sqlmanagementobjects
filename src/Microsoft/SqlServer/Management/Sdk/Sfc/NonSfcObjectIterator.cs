// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// Internal IEnumerable utility class that walks over the
    /// given table and creates objects based on the string in
    /// the first column of each row.
    internal class NonSfcObjectIterator : IEnumerable, IEnumerator
    {
        IAlienRoot _nonSfcRoot = null;
        List<string> _nonSfcQueryResults = null; // list of SMO Urns from query results
        int _curRow = -1;

        public NonSfcObjectIterator(IAlienRoot nonSfcRoot, SfcObjectQueryMode activeQueriesMode, SfcQueryExpression query, string[] fields, OrderBy[] orderByFields)
        {
            _nonSfcRoot = nonSfcRoot;
            _nonSfcQueryResults = _nonSfcRoot.SfcHelper_GetSmoObjectQuery(query.ToUrn().ToString(), fields, orderByFields );
        }

        private Object CreateNonSfcObjectFromString(string str)
        {
            return _nonSfcRoot.SfcHelper_GetSmoObject(str);
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return this;
        }

        Object IEnumerator.Current
        {
            get
            {
                // Use the delegate function to create an object
                // from the given string in the first column of
                // the current row.
                return CreateNonSfcObjectFromString(_nonSfcQueryResults[_curRow]);
            }
        }

        bool IEnumerator.MoveNext ()
        {
            _curRow++;
            return _curRow < _nonSfcQueryResults.Count;
        }

        void IEnumerator.Reset ()
        {
            _curRow = -1;
        }
    }
}

