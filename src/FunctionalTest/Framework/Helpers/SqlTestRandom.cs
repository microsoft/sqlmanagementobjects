// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    public static class SqlTestRandom
    {
        private static Random _random = new Random();

        /// <summary>
        /// Generates an array of the specified length of random bytes
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            _random.NextBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Use crypto to generate a random set of bytes, then
        /// convert those to a string.
        /// </summary>
        /// <remark>This method was taken from DACFx's StringUtils class</remark>
        public static string GeneratePassword()
        {
            // Create a byte array to hold the random value.
            byte[] randomNumber = new byte[48];
            Random randomChar = new Random(Environment.TickCount);

            RandomNumberGenerator provider = RandomNumberGenerator.Create();
            provider.GetBytes(randomNumber);

            byte[] complexity = { 0x6d, 0x73, 0x46, 0x54, 0x37, 0x5f, 0x26, 0x23, 0x24, 0x21, 0x7e, 0x3c };
            Array.Copy(complexity, 0, randomNumber, randomNumber.GetLength(0) / 2, complexity.GetLength(0));

            StringBuilder sb = new StringBuilder();
            List<char> badChars = new List<char>();
            badChars.Add('\''); // single quote is a bad character
            badChars.Add('-');
            badChars.Add('*');
            badChars.Add('/');
            badChars.Add('\\');
            badChars.Add('\"');
            badChars.Add('[');
            badChars.Add(']');
            badChars.Add(')');
            badChars.Add('(');
            for (int i = 0; i < randomNumber.GetLength(0); i++)
            {
                if (randomNumber[i] == 0) ++randomNumber[i];
                char ch = Convert.ToChar(randomNumber[i]);
                if ((int)ch < 32 ||
                    (int)ch > 126 ||
                    badChars.Contains(ch))
                {
                    ch = (char)((int)'a' + randomChar.Next(0, 125 - (int)'a')); //replacing bad character with 'a' + random
                }
                sb.Append(ch);
            }
            
            return sb.ToString();
        }
    }
}
