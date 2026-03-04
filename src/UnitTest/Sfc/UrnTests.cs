// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.SfcUnitTests
{
    [TestClass]
    public class UrnTests
    {
        private readonly string[] singleQuoteQuotedUrns =
        {
            //Single quotes quoted filter values
            "Server[@Name='MyServer']",
            "Server[@Name='MyServertest''test']",
            "Server[@Name='MyServertest''''test']",
            "Server[@Name='''''MyServertesttest''''']",
            "Server[@Name='MyServertest\"test']",
            "Server[@Name='MyServertest\"\"test']",
            "Server[@Name='''MyServertest''\"\"''test''']",
        };

        
        //Double quotes quoted filter values
        //Note - if adding values to this you should also add similar tests to doubleQuoteQuotedUrns_XPathExpression
        private readonly string[] doubleQuoteQuotedUrns =
        {
            "Server[@Name=\"MyServer\"]",
            "Server[@Name=\"MyServer''test\"]",
            "Server[@Name=\"MyServer''''test\"]",
            "Server[@Name=\"''''MyServertest''''\"]",
            "Server[@Name=\"''''MyServertest\"test''''\"]",
            "Server[@Name=\"''''MyServertest''\"\"''test''''\"]",
        };

        //See Verify_Urn_XPathExpression_ToString_Is_Correct for why this is necessary
        //                     Urn, ExpectedValue, ExpectedException
        private readonly Tuple<string, string, Type>[] doubleQuoteQuotedUrns_XPathExpression = 
        {
            new Tuple<string, string, Type>("Server[@Name=\"MyServer\"]", "Server[@Name='MyServer']", null),
            new Tuple<string, string, Type>("Server[@Name=\"MyServer''test\"]", "Server[@Name='MyServer''test']", null),
            new Tuple<string, string, Type>("Server[@Name=\"MyServer''''test\"]", "Server[@Name='MyServer''''test']", null), 
            new Tuple<string, string, Type>("Server[@Name='''''MyServertest''''']", "Server[@Name='''''MyServertest''''']", null), 
            new Tuple<string, string, Type>("Server[@Name=\"MyServertest\"test\"]", "Server[@Name='MyServertest\"test']", typeof(XPathException)),
            new Tuple<string, string, Type>("Server[@Name=\"MyServertest\"\"test\"]", "Server[@Name='MyServertest\"\"test']", typeof(XPathException)),
            
        };

        /// <summary>
        /// Verify that creating a urn from a string and then calling urn.ToString() returns
        /// the original urn string
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Verify_Urn_ToString_Is_Correct()
        {
            foreach (string urnString in singleQuoteQuotedUrns.Union(doubleQuoteQuotedUrns))
            {
                VerifyUrnStringIsCorrect(urnString);
            }
        }

        /// <summary>
        /// Verify that creating a urn from a string and then calling urn.XPathExpression.ToString() returns
        /// a string with the expected value (usually should be the original urn string but see notes below
        /// for current bug where this isn't the case)
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Verify_Urn_XPathExpression_ToString_Is_Correct()
        {
            foreach (string urnString in singleQuoteQuotedUrns)
            {
                VerifyUrnStringXPathExpressionIsCorrect(urnString);
            }

            //Note - there's currently a bug with the XPathExpression parsing that
            //doesn't correctly handle double quoted strings, it treats them like
            //single quote strings. See TFS#8062030

            //This isn't going to be fixed right now so instead I'm adding these tests to expect the
            //wrong behavior - that way we're still at least testing that functionality is staying the
            //same
            foreach(Tuple<string,string,Type> urnValues in doubleQuoteQuotedUrns_XPathExpression)
            {
                VerifyUrnStringXPathExpressionIsCorrect(urnValues.Item1, urnValues.Item2, urnValues.Item3);
            }
        }

        /// <summary>
        /// Verifies that calling ToString on the Urn object returns the original Urn string passed in
        /// </summary>
        /// <param name="urnString">The original urn string</param>
        private void VerifyUrnStringIsCorrect(string urnString)
        {
            try
            {
                var urn = new Urn(urnString);
                Assert.AreEqual(urnString, urn.ToString(), "Urn ToString() does not match the string passed in");
            }
            catch (Exception e)
            {
                throw new InternalTestFailureException(string.Format("Internal exception when verifying URN {0}", urnString), e);
            }
        }

        /// <summary>
        /// Verifies that calling ToString on the Urn.XPathExpression object returns the original Urn string
        /// passed in
        /// </summary>
        /// <param name="urnString">The original urn string</param>
        /// <param name="expectedValue">The expected value of calling urn.XPathExpression.ToString() (defaults to urnString)</param>
        /// <param name="expectedExceptionType">The type of exception we expecte, NULL if no exception expected (see test methods above for explanation)</param>
        private void VerifyUrnStringXPathExpressionIsCorrect(string urnString, string expectedValue = null, Type expectedExceptionType = null)
        {
            try
            {
                expectedValue = expectedValue ?? urnString;
                var urn = new Urn(urnString);
                Assert.AreEqual(expectedValue, urn.XPathExpression.ToString(), "Urn.XPathExpression ToString() does not match the string passed in");
            }
            catch (Exception e)
            {
                if (expectedExceptionType != null && e.GetType() != expectedExceptionType)
                {
                    throw new InternalTestFailureException(
                        string.Format("Internal exception when verifying URN {0} - {1}", urnString, e.Message), e);
                }
            }
        }
    }
}
