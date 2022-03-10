// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Syntactical representation of the XPATH expression
    /// </summary>
    [ComVisible(false)]
    [Serializable]
	public class XPathExpression
	{
        XPathExpressionBlock[] xPathBlocks;

		internal XPathExpression()
		{
            this.xPathBlocks = new XPathExpressionBlock[0];
		}

		/// <summary>
		/// Constructs XPathExpression from a string
		/// </summary>
		public XPathExpression(string strXPathExpression)
		{
			this.Compile(strXPathExpression);
		}

		/// <summary>
		/// Constructs the XPathExpression from a list of
		/// XPathExpressionBlocks.
		/// </summary>
        public XPathExpression(IList<XPathExpressionBlock> blocks)
        {
            this.xPathBlocks = new XPathExpressionBlock[blocks.Count];
            blocks.CopyTo(this.xPathBlocks, 0);
        }
        
		/// <summary>
		/// compile the XPATH in the syntactical tree
		/// </summary>
		/// <param name="strXPathExpression"></param>
		internal void Compile(String strXPathExpression)
		{
			XPathHandler ph = new XPathHandler();
			
			XPathScanner sc = new XPathScanner();
			sc.SetParseString(strXPathExpression);

			AstNode ast = ph.Run(sc);
			Load(ast);
		}

		/// <summary>
		/// load from the .Net parser output
		/// </summary>
		/// <param name="ast"></param>
		private void Load(AstNode ast)
		{
            List<XPathExpressionBlock> blocks = new List<XPathExpressionBlock>();

			while(ast != null)
			{
				XPathExpressionBlock bl = new XPathExpressionBlock();

                while ( ast.TypeOfAst == AstNode.QueryType.Filter )
				{
                    // It is possible to have multiple filters
                    // like "a/b[@z='1'][@y='1']". This is equivalent
                    // to "a/b[@z='1' and @y='1']".
                    var filter = FilterTranslate.decode(ast);
				    if (bl.Filter == null)
				    {
				        bl.Filter = filter;
				    }
				    else
				    {
				        bl.Filter = new FilterNodeOperator(FilterNodeOperator.Type.And, bl.Filter, filter);
				    }

				    ast = ((Filter)ast).Input;
				}

				if( ast.TypeOfAst != AstNode.QueryType.Axis )
                {
                    throw new InvalidQueryExpressionEnumeratorException(SfcStrings.InvalidNode);
                }

                bl.Name = ((Axis)ast).Name;
				ast = ((Axis)ast).Input;

                blocks.Add(bl);
			}
            blocks.Reverse();
            
            this.xPathBlocks = blocks.ToArray();
		}

		/// <summary>
		/// get the tree for the level given by the index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public virtual XPathExpressionBlock this[int index] 
		{ 
			get { return this.xPathBlocks[index]; }
		}

		/// <summary>
		/// number of levels
		/// </summary>
		/// <value></value>
		public int Length
		{
            get { return this.xPathBlocks.Length; }
		}

		/// <summary>
		/// Returns the string representation of this expression.
		/// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append("/");
                }
                sb.Append(this[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets XPathExpression has code by combining individual block hash codes.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hashCode = -1;
            for (int i = 0; i < this.Length; i++)
            {
                hashCode ^= this[i].GetHashCode();
            }
            return hashCode;
        }

		/// <summary>
        /// Returns the expression stripped of all filters.
		/// </summary>
        public string ExpressionSkeleton
        {
            get
            {
                return BlockExpressionSkeleton(this.Length - 1);
            }
        }

		/// <summary>
        /// Returns the expression stripped of all filters, up to and
        /// including the given block index. For an expression
        /// "A/B/C", passing an index of 0 will get you "A", passing 1
        /// will get you "A/B", and passing 2 or higher will get you
        /// "A/B/C".
		/// </summary>
        public string BlockExpressionSkeleton(int blockIndex)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.Length && i <= blockIndex; i++)
            {
                if (i > 0)
                {
                    sb.Append("/");
                }
                sb.Append(this[i].Name);
            }
            return sb.ToString();
        }
        
		/// <summary>
		/// compare to expressions
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="x2"></param>
		/// <param name="compInfoList"></param>
		/// <param name="cultureInfo"></param>
		/// <returns></returns>
		internal static bool Compare(XPathExpression x1, XPathExpression x2, CompareOptions[] compInfoList, CultureInfo cultureInfo)
		{
			if( null == x1 && null == x2 )
            {
                return true;
            }

            if ( null == x1 || null == x2 )
            {
                return false;
            }

            if ( x1.Length != x2.Length )
            {
                return false;
            }

            for (int i = x1.Length - 1; i >= 0; i--)
			{
				if( 0 != String.Compare(x1[i].Name, x2[i].Name, StringComparison.Ordinal))
                {
                    return false;
                }

                CompareOptions compInfo = i < compInfoList.Length ? compInfoList[i] : CompareOptions.None;
                if (!FilterNode.Compare(x1[i].Filter, x2[i].Filter, compInfo, cultureInfo))
                {
                    return false;
                }
			}
			return true;
		}

		/// <summary>
		/// get attribute for the given level from the filter
		/// </summary>
		/// <param name="attributeName">attribute name</param>
		/// <param name="type">level name</param>
		/// <returns></returns>
		public string GetAttribute(String attributeName, String type)
		{
			for(int i = 0; i < Length; i++)
            {
                if ( this[i].Name == type )
                {
                    return this[i].GetAttributeFromFilter(attributeName);
                }
            }

            return null;
		}
	}
}	
