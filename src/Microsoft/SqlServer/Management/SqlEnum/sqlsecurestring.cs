// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#region Using directives

using System;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

#endregion

namespace Microsoft.SqlServer.Management.Smo.Internal
{
    /// <summary>
    /// String class replacement that keeps its data encrypted in memory
    /// </summary>
    internal sealed class SqlSecureString : IComparable, IComparable<SqlSecureString>, IDisposable
        , ICloneable
    {
        private SecureString			data;
        private int						length;
        private readonly static SqlSecureString	empty = new SqlSecureString();

        static SqlSecureString()
        {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SqlSecureString()
        {
            this.data		= new SecureString();
            this.length		= 0;
        }

        /// <summary>
        /// Constructor for strings
        /// </summary>
        /// <param name="str">The string to encrypt</param>
        public SqlSecureString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException();
            }

            this.data = new SecureString();
            this.length		= 0;

            foreach (char ch in str.ToCharArray())
            {
                data.AppendChar(ch);
                ++this.length;
            }
        }

        /// <summary>
        /// Constructpr for SecureStrings
        /// </summary>
        /// <param name="secureString">The SecureString to copy</param>
        public SqlSecureString(SecureString secureString)
        {
            if (secureString == null)
            {
                throw new ArgumentNullException();
            }

            this.data		= secureString.Copy();
            this.length		= secureString.Length;
        }

        /// <summary>
        /// Constructor for unmanaged BSTRs.  The BSTR is not freed in this call.
        /// </summary>
        /// <param name="bstr">The unmanaged BSTR to copy</param>
        /// <param name="length">The maximum number of characters to copy from the BSTR</param>
        public SqlSecureString(IntPtr bstr, int length)
        {
            if (bstr == IntPtr.Zero)
            {
                throw new ArgumentNullException();
            }

            string str = Marshal.PtrToStringAuto(bstr, length);
            this.data	= new SecureString();
            this.length = 0;

            foreach (char ch in str.ToCharArray())
            {
                data.AppendChar(ch);
                ++this.length;
            }
        }

        /// <summary>
        /// Deletes the value and frees the resources of this object
        /// </summary>
        public void Dispose()
        {
            // if this hasn't already been disposed and this isn't the static
            // empty secure string, dispose the data
            if ((this.data != null) && (((object) this) != ((object) empty)))
            {
                this.data.Dispose();
                this.data = null;
            }
        }

        /// <summary>
        /// Gets the decrypted character at a specified character position in this instance
        /// </summary>
        /// <param name="index">The index of the character</param>
        /// <returns>The decrypted character</returns>
        public char this[int index]
        {
            get
            {
                return this.ToString()[index];
            }
        }

        /// <summary>
        /// Represents the empty string
        /// </summary>
        public static SqlSecureString Empty
        {
            get
            {
                return SqlSecureString.empty;
            }
        }

        /// <summary>
        /// Gets the number of characters in the instance
        /// </summary>
        public int Length
        {
            get
            {
                return this.length;
            }
        }

        /// <summary>
        /// Returns a copy of this string
        /// </summary>
        public object Clone()
        {
            return this.Copy();
        }

        /// <summary>
        /// Compares two SqlSecureString objects
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public static int Compare(SqlSecureString strA, SqlSecureString strB)
        {
            return String.Compare((string) strA, (string) strB);
        }

        /// <summary>
        /// Compares two SqlSecureString objects, ignoring or honoring their case
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <param name="ignoreCase">Whether to perform a case-insensitive comparison (true indicates a case-insensitive comparison.)</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.Compare(System.String,System.String,System.Boolean)")]
        public static int Compare(SqlSecureString strA, SqlSecureString strB, bool ignoreCase)
        {
            return String.Compare((string) strA, (string) strB, ignoreCase);
        }

        /// <summary>
        /// Compares two SqlSecureString objects.  A parameter specifies whether the comparison uses the current
        /// or invariant culture, honors or ignores case, and uses word or ordinal sort rules.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <param name="comparisonType">One of the System.StringComparison values</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public static int Compare(SqlSecureString strA, SqlSecureString strB, StringComparison comparisonType)
        {
            return String.Compare((string) strA, (string) strB, comparisonType);
        }

        /// <summary>
        /// Compares two SqlSecureString objects, ignoring or honoring case, and using culture-specific
        /// information to influence the comparison
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <param name="ignoreCase">Whether to perform a case-insensitive comparison (true indicates a case-insensitive comparison.)</param>
        /// <param name="cultureInfo">The CultureInfo object that supplies culture-specific comparison information</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public static int Compare(
            SqlSecureString strA, 
            SqlSecureString strB, 
            bool ignoreCase, 
            CultureInfo cultureInfo)
        {
            return String.Compare((string) strA, (string) strB, ignoreCase, cultureInfo);
        }

        /// <summary>
        /// Compares two SqlSecureString objects
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="indexA">The position of the substring within strA</param>
        /// <param name="strB">The second string</param>
        /// <param name="indexB">The position of the substring within strB</param>
        /// <param name="length">The maximum number of characters in the substrings to compare</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public static int Compare(
            SqlSecureString strA,
            int indexA,
            SqlSecureString strB,
            int indexB,
            int length)
        {
            return String.Compare((string) strA, indexA, (string) strB, indexB, length);
        }

        /// <summary>
        /// Compares two SqlSecureString objects
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="indexA">The position of the substring within strA</param>
        /// <param name="strB">The second string</param>
        /// <param name="indexB">The position of the substring within strB</param>
        /// <param name="length">The maximum number of characters in the substrings to compare</param>
        /// <param name="ignoreCase">Whether to perform a case-insensitive comparison (true indicates a case-insensitive comparison.)</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.Compare(System.String,System.Int32,System.String,System.Int32,System.Int32,System.Boolean)")]
        public static int Compare(
            SqlSecureString strA,
            int indexA,
            SqlSecureString strB,
            int indexB,
            int length,
            bool ignoreCase)
        {
            return String.Compare((string) strA, indexA, (string) strB, indexB, length, ignoreCase);
        }

        /// <summary>
        /// Compares two SqlSecureString objects
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="indexA">The position of the substring within strA</param>
        /// <param name="strB">The second string</param>
        /// <param name="indexB">The position of the substring within strB</param>
        /// <param name="length">The maximum number of characters in the substrings to compare</param>
        /// <param name="comparisonType">One of the System.StringComparison values</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public static int Compare(
            SqlSecureString strA,
            int indexA,
            SqlSecureString strB,
            int indexB,
            int length,
            StringComparison comparisonType)
        {
            return String.Compare((string) strA, indexA, (string) strB, indexB, length, comparisonType);
        }

        /// <summary>
        /// Compares two SqlSecureString objects
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="indexA">The position of the substring within strA</param>
        /// <param name="strB">The second string</param>
        /// <param name="indexB">The position of the substring within strB</param>
        /// <param name="length">The maximum number of characters in the substrings to compare</param>
        /// <param name="ignoreCase">Whether to perform a case-insensitive comparison (true indicates a case-insensitive comparison.)</param>
        /// <param name="cultureInfo">The CultureInfo object that supplies culture-specific comparison information</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public static int Compare(
            SqlSecureString strA,
            int indexA,
            SqlSecureString strB,
            int indexB,
            int length,
            bool ignoreCase, 
            CultureInfo cultureInfo)
        {
            return String.Compare((string) strA, indexA, (string) strB, indexB, length, ignoreCase, cultureInfo);
        }

        /// <summary>
        /// Performs a binary comparison of two strings by evaluating the numeric values
        /// of the corresponding char objects in each string.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public static int CompareOrdinal(SqlSecureString strA, SqlSecureString strB)
        {
            return String.CompareOrdinal((string) strA, (string) strB);
        }

        /// <summary>
        /// Performs a binary comparison of two strings by evaluating the numeric values
        /// of the corresponding char objects in each string.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="indexA">The position of the substring within strA</param>
        /// <param name="strB">The second string</param>
        /// <param name="indexB">The position of the substring within strB</param>
        /// <param name="length">The maximum number of characters in the substrings to compare</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public static int CompareOrdinal(
            SqlSecureString strA,
            int indexA,
            SqlSecureString strB,
            int indexB,
            int length)
        {
            return String.CompareOrdinal((string) strA, indexA, (string) strB, indexB, length);
        }

        /// <summary>
        /// Compares this instance with a specified object.
        /// </summary>
        /// <param name="obj">An object that evaluates to a string</param>
        /// <returns>
        /// less than zero if this is less than obj, 
        /// zero if this equals obj, 
        /// greater than zero if this is greater than obj
        /// </returns>
        public int CompareTo(object obj)
        {
            int result = 1;

            if (obj != null)
            {
                result = this.ToString().CompareTo(obj.ToString());
            }

            return result;
        }

        /// <summary>
        /// Compares this instance with a specified string object.
        /// </summary>
        /// <param name="other">Another SqlSecureString</param>
        /// <returns>
        /// less than zero if strA is less than strB, 
        /// zero if strA equals strB, 
        /// greater than zero if strA is greater than strB
        /// </returns>
        public int CompareTo(SqlSecureString other)
        {
            int result = 1;

            if (((object) other) != null)
            {
                result = this.ToString().CompareTo(other.ToString());
            }

            return result;
        }

        /// <summary>
        /// Concatenates the string representations of the values of one or more objects.
        /// </summary>
        /// <param name="obj">The object to convert to a SqlSecureString</param>
        /// <returns>A SqlSecureString containing the string representation of obj</returns>
        public static SqlSecureString Concat(object obj)
        {
            SqlSecureString result;

            if (obj != null)
            {
                result = new SqlSecureString(obj.ToString());
            }
            else
            {
                result = new SqlSecureString();
            }
            
            return result;
        }

        /// <summary>
        /// Concatenates the string representations of the values of one or more objects.
        /// </summary>
        /// <param name="args">The objects to concatenate</param>
        /// <returns>A SqlSecureString containing the concatenation of the string representations of the args</returns>
        public static SqlSecureString Concat(params object[] args)
        {
            return new SqlSecureString(String.Concat(args));
        }

        /// <summary>
        /// Returns a value indicating whether the specified string occurs within this instance
        /// </summary>
        /// <param name="value">The string to seek</param>
        /// <returns>true if value occurs within this instance, or if value is the empty string; otherwise, false</returns>
        public bool Contains(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            return this.ToString().Contains(value);
        }

        /// <summary>
        /// Returns a value indicating whether the specified string occurs within this instance
        /// </summary>
        /// <param name="value">The string to seek</param>
        /// <returns>true if value occurs within this instance, or if value is the empty string; otherwise, false</returns>
        public bool Contains(SqlSecureString value)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            return this.ToString().Contains(value.ToString());
        }

        /// <summary>
        /// Create a copy of the SqlSecureString
        /// </summary>
        /// <returns>The copy of the SqlSecureString</returns>
        public SqlSecureString Copy()
        {
            return new SqlSecureString(this.data);	
        }

        /// <summary>
        /// Determines whether the end of the SqlSecureString matches the specified string
        /// </summary>
        /// <param name="value">The string to compare</param>
        /// <returns>true if value matches the end of the string; otherwise, false</returns>
        public bool EndsWith(SqlSecureString value)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            return this.ToString().EndsWith(value.ToString());
        }

        /// <summary>
        /// Determines whether the end of the SqlSecureString matches the specified string
        /// </summary>
        /// <param name="value">The string to compare</param>
        /// <param name="ignoreCase">Whether to perform a case-insensitive comparison (true indicates a case-insensitive comparison.)</param>
        /// <param name="cultureInfo">The CultureInfo object that supplies culture-specific comparison information</param>
        /// <returns>true if value matches the end of the string; otherwise, false</returns>
        public bool EndsWith(SqlSecureString value, bool ignoreCase, CultureInfo cultureInfo)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            return this.ToString().EndsWith(value.ToString(), ignoreCase, cultureInfo);
        }

        /// <summary>
        /// Determines whether this instance of SqlSecureString and a specified object
        /// have the same string representation
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>true if the object's string representation matches this string; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            bool result = false;

            if (obj != null)
            {
                result = this.ToString().Equals(obj);
            }

            return result;
        }

        /// <summary>
        /// Determines whether this instance and another SqlSecureString have the same binary value
        /// </summary>
        /// <param name="other">The SqlSecureString to compare</param>
        /// <returns>true if the values are the same; otherwise, false</returns>
        public bool Equals(SqlSecureString other)
        {
            return (this == other);
        }

        /// <summary>
        /// Determines whether this instance and another SqlSecureString have the same value
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>true if the values are the same; otherwise, false</returns>
        public static bool Equals(SqlSecureString strA, SqlSecureString strB)
        {
            return (strA == strB);
        }

        /// <summary>
        /// Determines whether this instance and another SqlSecureString have the same binary value
        /// </summary>
        /// <param name="other">The SqlSecureString to compare</param>
        /// <param name="comparisonType">One of the System.StringComparison values</param>
        /// <returns>true if the values are the same; otherwise, false</returns>
        public bool Equals(SqlSecureString other, StringComparison comparisonType)
        {
            return this.ToString().Equals((string) other, comparisonType);
        }

        /// <summary>
        /// Determines whether this instance and another SqlSecureString have the same value
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <param name="comparisonType">One of the System.StringComparison values</param>
        /// <returns>true if the values are the same; otherwise, false</returns>
        public static bool Equals(SqlSecureString strA, SqlSecureString strB, StringComparison comparisonType)
        {
            return String.Equals((string) strA, (string) strB, comparisonType);
        }

        /// <summary>
        /// Replaces each format item in the specified string with the text equivalent
        /// of a corresponding object's value
        /// </summary>
        /// <param name="format">A string containing zero or more format items</param>
        /// <param name="arguments">An object array containing zero or more objects to format</param>
        /// <returns>
        /// A copy of format in which the format items have been replace by the string
        /// equivalent of the corresponding instances of object in arguments
        /// </returns>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])")]
        public static SqlSecureString Format(string format, params object[] arguments)
        {
            return new SqlSecureString(String.Format(format, arguments));
        }

        /// <summary>
        /// Replaces each format item in the specified string with the text equivalent
        /// of a corresponding object's value
        /// </summary>
        /// <param name="formatProvider">An IFormatProvider that supplies culture-specific formatting information</param>
        /// <param name="format">A string containing zero or more format items</param>
        /// <param name="arguments">An object array containing zero or more objects to format</param>
        /// <returns>
        /// A copy of format in which the format items have been replace by the string
        /// equivalent of the corresponding instances of object in arguments
        /// </returns>
        public static SqlSecureString Format(IFormatProvider formatProvider, string format, params object[] arguments)
        {
            return new SqlSecureString(String.Format(formatProvider, format, arguments));
        }

        /// <summary>
        /// Returns the hash code for the instance
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Reports the index of the first occurance of the specified character in this instance.
        /// </summary>
        /// <param name="value">The character to seek</param>
        /// <returns>
        /// The index position of value if that character is found, or -1 if it is not.
        /// </returns>
        public int IndexOf(char value)
        {
            return this.ToString().IndexOf(value);
        }

        /// <summary>
        /// Reports the index of the first occurance of the specified string in this instance.
        /// </summary>
        /// <param name="value">The stringharacter to seek</param>
        /// <returns>
        /// The index position of value if that string is found, or -1 if it is not.
        /// If value is Empty, the return value is 0.
        /// </returns>
        public int IndexOf(string value)
        {
            return this.ToString().IndexOf(value);
        }

        /// <summary>
        /// Reports the index of the first occurance of the specified character in this instance.
        /// The search starts at a specified character position.
        /// </summary>
        /// <param name="value">The character to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <returns>
        /// The index position of value if that character is found, or -1 if it is not.
        /// </returns>
        public int IndexOf(char value, int startIndex)
        {
            return this.ToString().IndexOf(value, startIndex);
        }

        /// <summary>
        /// Reports the index of the first occurance of the specified string in this instance.
        /// The search starts at a specified character position.
        /// </summary>
        /// <param name="value">The string to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <returns>
        /// The index position of value if that string is found, or -1 if it is not.
        /// If value is Empty, the return value is startIndex.
        /// </returns>
        public int IndexOf(string value, int startIndex)
        {
            return this.ToString().IndexOf(value, startIndex);
        }

        /// <summary>
        /// Reports the index of the first occurance of the specified character in this instance.
        /// The search starts at a specified character position and examines a specified
        /// number of character positions.
        /// </summary>
        /// <param name="value">The character to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <param name="count">The number of character positions to examine</param>
        /// <returns>
        /// The index position of value if that character is found, or -1 if it is not.
        /// </returns>
        public int IndexOf(char value, int startIndex, int count)
        {
            return this.ToString().IndexOf(value, startIndex, count);
        }

        /// <summary>
        /// Reports the index of the first occurance of the specified string in this instance.
        /// The search starts at a specified character position and examines a specified
        /// number of character positions.
        /// </summary>
        /// <param name="value">The string to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <param name="count">The number of character positions to examine</param>
        /// <returns>
        /// The index position of value if that string is found, or -1 if it is not.
        /// If value is Empty, the return value is startIndex.
        /// </returns>
        public int IndexOf(string value, int startIndex, int count)
        {
            return this.ToString().IndexOf(value, startIndex, count);
        }

        /// <summary>
        /// Reports the index of the first occurance in this instance of any character
        /// in a specified array of characters.
        /// </summary>
        /// <param name="anyOf">The character array containing one or more characters to seek</param>
        /// <returns>
        /// The index position of the first occurance in this instance where any character of anyOf
        /// was found; otherwise, -1 if no character of anyOf was found.
        /// </returns>
        public int IndexOfAny(char[] anyOf)
        {
            return this.ToString().IndexOfAny(anyOf);
        }

        /// <summary>
        /// Reports the index of the first occurance in this instance of any character
        /// in a specified array of characters.  The search starts at a specified 
        /// character position.
        /// </summary>
        /// <param name="anyOf">The character array containing one or more characters to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <returns>
        /// The index position of the first occurance in this instance where any character of anyOf
        /// was found; otherwise, -1 if no character of anyOf was found.
        /// </returns>
        public int IndexOfAny(char[] anyOf, int startIndex)
        {
            return this.ToString().IndexOfAny(anyOf, startIndex);
        }

        /// <summary>
        /// Reports the index of the first occurance in this instance of any character
        /// in a specified array of characters.  The search starts at a specified 
        /// character position and examines a specified number of character positions.
        /// </summary>
        /// <param name="anyOf">The character array containing one or more characters to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <param name="count">The number of character positions to examine</param>
        /// <returns>
        /// The index position of the first occurance in this instance where any character of anyOf
        /// was found; otherwise, -1 if no character of anyOf was found.
        /// </returns>
        public int IndexOfAny(char[] anyOf, int startIndex, int count)
        {
            return this.ToString().IndexOfAny(anyOf, startIndex, count);
        }

        /// <summary>
        /// Inserts a specified instance of SqlSecureString at a specified index position
        /// in this instance
        /// </summary>
        /// <param name="startIndex">The index position of the insertion</param>
        /// <param name="value">The SqlSecureString to insert</param>
        /// <returns>
        /// The new SqlSecureString equivalent to this instance but with
        /// value inserted at position startIndex.
        /// </returns>
        public SqlSecureString Insert(int startIndex, SqlSecureString value)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            return new SqlSecureString((this.ToString()).Insert(startIndex, value.ToString()));
        }

        /// <summary>
        /// Inserts a specified instance of String at a specified index position
        /// in this instance
        /// </summary>
        /// <param name="startIndex">The index position of the insertion</param>
        /// <param name="value">The String to insert</param>
        /// <returns>
        /// The new SqlSecureString equivalent to this instance but with
        /// value inserted at position startIndex.
        /// </returns>
        public SqlSecureString Insert(int startIndex, String value)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            return new SqlSecureString((this.ToString()).Insert(startIndex, value));
        }

        /// <summary>
        /// Concatenates a specified separator string between each element of a
        /// specified string array, yielding a single contatenated string.
        /// </summary>
        /// <param name="separator">The object whose string representation will be used to separate the values</param>
        /// <param name="value">The array of objects whose string representations will be joined</param>
        /// <returns>A SqlSecureString consisting of the elements of value interspersed with the separator string</returns>
        public static SqlSecureString Join(object separator, object[] value)
        {
            return SqlSecureString.Join(separator, value, 0, value.Length);
        }

        /// <summary>
        /// Concatenates a specified separator string between each element of a
        /// specified string array, yielding a single contatenated string.
        /// </summary>
        /// <param name="separator">The object whose string representation will be used to separate the values</param>
        /// <param name="value">The array of objects whose string representations will be joined</param>
        /// <param name="startIndex">The first array element in value to use</param>
        /// <param name="count">The number of elements of value to use</param>
        /// <returns></returns>
        public static SqlSecureString Join(object separator, object[] value, int startIndex, int count)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            if ((startIndex < 0) || (value.Length < (startIndex + count)))
            {
                throw new ArgumentOutOfRangeException();
            }

            StringBuilder builder = new StringBuilder();

            if (0 < count)
            {
                string	separatorString		= (separator != null) ? separator.ToString() : String.Empty;
                bool	separatorIsNotEmpty = (separatorString.Length != 0);
                int		elementIndex		= startIndex;

                // add the first element to the builder
                if (value[elementIndex] != null)
                {
                    builder.Append(value[elementIndex].ToString());
                }

                ++elementIndex;

                // add all the subsequent elements to the builder
                while ((elementIndex - startIndex) < count)
                {
                    if (value[elementIndex] != null)
                    {
                        if (separatorIsNotEmpty)
                        {
                            builder.Append(separatorString);
                        }

                        builder.Append(value[elementIndex].ToString());
                    }

                    ++elementIndex;
                }
            }

            // form the result
            return new SqlSecureString(builder.ToString());
        }

        /// <summary>
        /// Reports the index of the last occurance of the specified character in this instance.
        /// </summary>
        /// <param name="value">The character to seek</param>
        /// <returns>
        /// The index position of value if that character is found, or -1 if it is not.
        /// </returns>
        public int LastIndexOf(char value)
        {
            return this.ToString().LastIndexOf(value);
        }

        /// <summary>
        /// Reports the index of the last occurance of the specified string in this instance.
        /// </summary>
        /// <param name="value">The stringharacter to seek</param>
        /// <returns>
        /// The index position of value if that string is found, or -1 if it is not.
        /// If value is Empty, the return value is 0.
        /// </returns>
        public int LastIndexOf(string value)
        {
            return this.ToString().LastIndexOf(value);
        }

        /// <summary>
        /// Reports the index of the last occurance of the specified character in this instance.
        /// The search starts at a specified character position.
        /// </summary>
        /// <param name="value">The character to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <returns>
        /// The index position of value if that character is found, or -1 if it is not.
        /// </returns>
        public int LastIndexOf(char value, int startIndex)
        {
            return this.ToString().LastIndexOf(value, startIndex);
        }

        /// <summary>
        /// Reports the index of the last occurance of the specified string in this instance.
        /// The search starts at a specified character position.
        /// </summary>
        /// <param name="value">The string to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <returns>
        /// The index position of value if that string is found, or -1 if it is not.
        /// If value is Empty, the return value is startIndex.
        /// </returns>
        public int LastIndexOf(string value, int startIndex)
        {
            return this.ToString().LastIndexOf(value, startIndex);
        }

        /// <summary>
        /// Reports the index of the last occurance of the specified character in this instance.
        /// The search starts at a specified character position and examines a specified
        /// number of character positions.
        /// </summary>
        /// <param name="value">The character to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <param name="count">The number of character positions to examine</param>
        /// <returns>
        /// The index position of value if that character is found, or -1 if it is not.
        /// </returns>
        public int LastIndexOf(char value, int startIndex, int count)
        {
            return this.ToString().LastIndexOf(value, startIndex, count);
        }

        /// <summary>
        /// Reports the index of the last occurance of the specified string in this instance.
        /// The search starts at a specified character position and examines a specified
        /// number of character positions.
        /// </summary>
        /// <param name="value">The string to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <param name="count">The number of character positions to examine</param>
        /// <returns>
        /// The index position of value if that string is found, or -1 if it is not.
        /// If value is Empty, the return value is startIndex.
        /// </returns>
        public int LastIndexOf(string value, int startIndex, int count)
        {
            return this.ToString().LastIndexOf(value, startIndex, count);
        }

        /// <summary>
        /// Reports the index of the last occurance in this instance of any character
        /// in a specified array of characters.
        /// </summary>
        /// <param name="anyOf">The character array containing one or more characters to seek</param>
        /// <returns>
        /// The index position of the first occurance in this instance where any character of anyOf
        /// was found; otherwise, -1 if no character of anyOf was found.
        /// </returns>
        public int LastIndexOfAny(char[] anyOf)
        {
            return this.ToString().LastIndexOfAny(anyOf);
        }

        /// <summary>
        /// Reports the index of the last occurance in this instance of any character
        /// in a specified array of characters.  The search starts at a specified 
        /// character position.
        /// </summary>
        /// <param name="anyOf">The character array containing one or more characters to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <returns>
        /// The index position of the first occurance in this instance where any character of anyOf
        /// was found; otherwise, -1 if no character of anyOf was found.
        /// </returns>
        public int LastIndexOfAny(char[] anyOf, int startIndex)
        {
            return this.ToString().LastIndexOfAny(anyOf, startIndex);
        }

        /// <summary>
        /// Reports the index of the last occurance in this instance of any character
        /// in a specified array of characters.  The search starts at a specified 
        /// character position and examines a specified number of character positions.
        /// </summary>
        /// <param name="anyOf">The character array containing one or more characters to seek</param>
        /// <param name="startIndex">The search starting position</param>
        /// <param name="count">The number of character positions to examine</param>
        /// <returns>
        /// The index position of the first occurance in this instance where any character of anyOf
        /// was found; otherwise, -1 if no character of anyOf was found.
        /// </returns>
        public int LastIndexOfAny(char[] anyOf, int startIndex, int count)
        {
            return this.ToString().LastIndexOfAny(anyOf, startIndex, count);
        }

        /// <summary>
        /// Determines whether the decrypted strings have the same value.  This is a
        /// binary comparison of the characters of the strings.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>True if the strings are equal; otherwise, false</returns>
        public static bool operator ==(SqlSecureString strA, SqlSecureString strB)
        {
            bool result		= false;
            bool aIsNull	= (((object) strA) == null);
            bool bIsNull	= (((object) strB) == null);

            if (aIsNull && bIsNull)
            {
                result = true;
            }
            else if (!aIsNull && !bIsNull)
            {
                result = (0 == String.CompareOrdinal(strA.ToString(), strB.ToString()));
            }

            return result;
        }

        /// <summary>
        /// Determines whether the decrypted strings have the same value.  This is a
        /// binary comparison of the characters of the strings.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>True if the strings are equal; otherwise, false</returns>
        public static bool operator ==(SqlSecureString strA, object strB)
        {
            bool result  = false;
            bool aIsNull = (((object)strA) == null);
            bool bIsNull = (strB == null);

            if (aIsNull && bIsNull)
            {
                result = true;
            }
            else if (!aIsNull && !bIsNull)
            {
                result = (0 == String.CompareOrdinal(strA.ToString(), strB.ToString()));
            }

            return result;
        }

        /// <summary>
        /// Determines whether the decrypted strings have the same value.  This is a
        /// binary comparison of the characters of the strings.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>True if the strings are equal; otherwise, false</returns>
        public static bool operator ==(object strA, SqlSecureString strB)
        {
            bool result  = false;
            bool aIsNull = (strA == null);
            bool bIsNull = (((object)strB) == null);

            if (aIsNull && bIsNull)
            {
                result = true;
            }
            else if (!aIsNull && !bIsNull)
            {
                result = (0 == String.CompareOrdinal(strA.ToString(), strB.ToString()));
            }

            return result;
        }

        /// <summary>
        /// Determines whether the decrypted strings have different values.  This is a
        /// binary comparison of the characters of the strings.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>True if the strings are different; otherwise, false</returns>
        public static bool operator !=(SqlSecureString strA, SqlSecureString strB)
        {
            return !(strA == strB);
        }

        /// <summary>
        /// Determines whether the decrypted strings have different values.  This is a
        /// binary comparison of the characters of the strings.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>True if the strings are different; otherwise, false</returns>
        public static bool operator !=(SqlSecureString strA, object strB)
        {
            return !(strA == strB);
        }

        /// <summary>
        /// Determines whether the decrypted strings have different values.  This is a
        /// binary comparison of the characters of the strings.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>True if the strings are different; otherwise, false</returns>
        public static bool operator !=(object strA, SqlSecureString strB)
        {
            return !(strA == strB);
        }

        /// <summary>
        /// Concatenates two SqlSecureStrings.
        /// </summary>
        /// <param name="strA">The first string</param>
        /// <param name="strB">The second string</param>
        /// <returns>The concatenation of the two strings</returns>
        public static SqlSecureString operator +(SqlSecureString strA, SqlSecureString strB)
        {
            return SqlSecureString.Concat(strA, strB);
        }

        /// <summary>
        /// Right-aligns the charactes in this instance, padding with spaces on the left
        /// for a specified total length.
        /// </summary>
        /// <param name="totalWidth">The number of characters in the resulting string.</param>
        /// <returns>
        /// A new SqlSecureString that is equivalent to this instance, but right-aligned and
        /// padded on the left with as many spaces as needed to create a length of totalWidth.
        /// -or- If totalWidth is less than the length of this instance, a new SqlSecureString
        /// that is identical to this instance.
        /// </returns>
        public SqlSecureString PadLeft(int totalWidth)
        {
            return new SqlSecureString(this.ToString().PadLeft(totalWidth));
        }

        /// <summary>
        /// Right-aligns the charactes in this instance, padding with a specified character 
        /// on the left for a specified total length.
        /// </summary>
        /// <param name="totalWidth">The number of characters in the resulting string.</param>
        /// <param name="paddingChar">The padding character</param>
        /// <returns>
        /// A new SqlSecureString that is equivalent to this instance, but right-aligned and
        /// padded on the left with as many padding characters as needed to create a length 
        /// of totalWidth. -or- If totalWidth is less than the length of this instance, a new 
        /// SqlSecureString that is identical to this instance.
        /// </returns>
        public SqlSecureString PadLeft(int totalWidth, char paddingChar)
        {
            return new SqlSecureString(this.ToString().PadLeft(totalWidth, paddingChar));
        }

        /// <summary>
        /// Left-aligns the charactes in this instance, padding with spaces on the right
        /// for a specified total length.
        /// </summary>
        /// <param name="totalWidth">The number of characters in the resulting string.</param>
        /// <returns>
        /// A new SqlSecureString that is equivalent to this instance, but left-aligned and
        /// padded on the right with as many spaces as needed to create a length of totalWidth.
        /// -or- If totalWidth is less than the length of this instance, a new SqlSecureString
        /// that is identical to this instance.
        /// </returns>
        public SqlSecureString PadRight(int totalWidth)
        {
            return new SqlSecureString(this.ToString().PadRight(totalWidth));
        }

        /// <summary>
        /// Left-aligns the charactes in this instance, padding with a specified character 
        /// on the right for a specified total length.
        /// </summary>
        /// <param name="totalWidth">The number of characters in the resulting string.</param>
        /// <param name="paddingChar">The padding character</param>
        /// <returns>
        /// A new SqlSecureString that is equivalent to this instance, but left-aligned and
        /// padded on the right with as many padding characters as needed to create a length 
        /// of totalWidth. -or- If totalWidth is less than the length of this instance, a new 
        /// SqlSecureString that is identical to this instance.
        /// </returns>
        public SqlSecureString PadRight(int totalWidth, char paddingChar)
        {
            return new SqlSecureString(this.ToString().PadRight(totalWidth, paddingChar));
        }

        /// <summary>
        /// Deletes characters from this instance beginning at a specified position.
        /// </summary>
        /// <param name="startIndex">The position to start deleting characters</param>
        /// <returns>A SqlSecureString that is equivalent to this instance less the removed characters</returns>
        public SqlSecureString Remove(int startIndex)
        {
            return new SqlSecureString(this.ToString().Remove(startIndex));
        }

        /// <summary>
        /// Deletes a specified number of characters from this instance beginning at a specified position.
        /// </summary>
        /// <param name="startIndex">The position to start deleting characters</param>
        /// <param name="count">The number of characters to delete</param>
        /// <returns>A SqlSecureString that is equivalent to this instance less the removed characters</returns>
        public SqlSecureString Remove(int startIndex, int count)
        {
            return new SqlSecureString(this.ToString().Remove(startIndex, count));
        }

        /// <summary>
        /// Replaces all occurances of a specified character with another specified character
        /// </summary>
        /// <param name="oldChar">The character to be replaced</param>
        /// <param name="newChar">The character to replace all occurrences of oldChar</param>
        /// <returns>
        /// The SqlSecureString equivalent of this instance but with all instances
        /// of oldChar replace with newChar.
        /// </returns>
        public SqlSecureString Replace(char oldChar, char newChar)
        {
            return new SqlSecureString(this.ToString().Replace(oldChar, newChar));
        }

        /// <summary>
        /// Replaces all occurances of a specified string with another specified string
        /// </summary>
        /// <param name="oldValue">The string to be replaced</param>
        /// <param name="newValue">The string to replace all occurrences of oldValue</param>
        /// <returns>
        /// The SqlSecureString equivalent of this instance but with all instances
        /// of oldValue replaced with newValue.
        /// </returns>
        public SqlSecureString Replace(SqlSecureString oldValue, SqlSecureString newValue)
        {
            return new SqlSecureString(this.ToString().Replace(oldValue.ToString(), newValue.ToString()));
        }
        
        /// <summary>
        /// Returns a SqlSecureString array containing the substrings of this instance
        /// that are delimited by a specific character array
        /// </summary>
        /// <param name="separator">The array of delimiter characters, an empty array, or null</param>
        /// <returns>
        /// An array consisting for a single element that contains a copy of this instance, if this
        /// instance contains none of the characters in separator.
        /// -or- An array of substrings, if this instance is delimited by one or more characters in separator.
        /// -or- An array of the substrings of this instance that are delimited by white space
        /// characters, if those characters occur in this instance and separator is null or contains
        /// no delimiter characters.
        /// 
        /// An empty SqlSecureString is returned for any substring in which two delimiters are adjacent,
        /// or if a delimiter is found at the beginning or end of this instance.  Delimiter
        /// characters are not included in the substrings.
        /// </returns>
        public SqlSecureString[] Split(char[] separator)
        {
            return SqlSecureString.StringArrayToSqlSecureStringArray(
                this.ToString().Split(separator));
        }

        /// <summary>
        /// Returns a SqlSecureString array containing the substrings of this instance
        /// that are delimited by a specific character array
        /// </summary>
        /// <param name="separator">The array of delimiter characters, an empty array, or null</param>
        /// <param name="count">The maximum number of substrings to return</param>
        /// <returns>
        /// An array consisting for a single element that contains a copy of this instance, if this
        /// instance contains none of the characters in separator.
        /// -or- An array of substrings, if this instance is delimited by one or more characters in separator.
        /// -or- An array of the substrings of this instance that are delimited by white space
        /// characters, if those characters occur in this instance and separator is null or contains
        /// no delimiter characters.
        /// 
        /// An empty SqlSecureString is returned for any substring in which two delimiters are adjacent,
        /// or if a delimiter is found at the beginning or end of this instance.  Delimiter
        /// characters are not included in the substrings.
        /// </returns>
        public SqlSecureString[] Split(char[] separator, int count)
        {
            return SqlSecureString.StringArrayToSqlSecureStringArray(
                this.ToString().Split(separator, count));
        }

        /// <summary>
        /// Returns a SqlSecureString array containing the substrings of this instance
        /// that are delimited by a specific character array
        /// </summary>
        /// <param name="separator">The array of delimiter characters, an empty array, or null</param>
        /// <param name="options">
        /// RemoveEmptyEntries to omit empty elements from the array returned;
        /// None to include empty array elements in the array returned
        /// </param>
        /// <returns>
        /// An array consisting for a single element that contains a copy of this instance, if this
        /// instance contains none of the characters in separator.
        /// -or- An array of substrings, if this instance is delimited by one or more characters in separator.
        /// -or- An array of the substrings of this instance that are delimited by white space
        /// characters, if those characters occur in this instance and separator is null or contains
        /// no delimiter characters.
        /// 
        /// If options is None, an empty SqlSecureString is returned for any substring in which two delimiters 
        /// are adjacent, or if a delimiter is found at the beginning or end of this instance.  Delimiter
        /// characters are not included in the substrings.
        /// </returns>
        public SqlSecureString[] Split(char[] separator, StringSplitOptions options)
        {
            return SqlSecureString.StringArrayToSqlSecureStringArray(
                this.ToString().Split(separator, options));
        }

        /// <summary>
        /// Returns a SqlSecureString array containing the substrings of this instance
        /// that are delimited by a specific character array
        /// </summary>
        /// <param name="separator">The array of delimiter characters, an empty array, or null</param>
        /// <param name="count">The maximum number of substrings to return</param>
        /// <param name="options">
        /// RemoveEmptyEntries to omit empty elements from the array returned;
        /// None to include empty array elements in the array returned
        /// </param>
        /// <returns>
        /// An array consisting for a single element that contains a copy of this instance, if this
        /// instance contains none of the characters in separator.
        /// -or- An array of substrings, if this instance is delimited by one or more characters in separator.
        /// -or- An array of the substrings of this instance that are delimited by white space
        /// characters, if those characters occur in this instance and separator is null or contains
        /// no delimiter characters.
        /// 
        /// If options is None, an empty SqlSecureString is returned for any substring in which two delimiters 
        /// are adjacent, or if a delimiter is found at the beginning or end of this instance.  Delimiter
        /// characters are not included in the substrings.
        /// </returns>
        public SqlSecureString[] Split(char[] separator, int count, StringSplitOptions options)
        {
            return SqlSecureString.StringArrayToSqlSecureStringArray(
                this.ToString().Split(separator, count, options));
        }

        /// <summary>
        /// Returns a SqlSecureString array containing the substrings of this instance
        /// that are delimited by elements of a specified string array
        /// </summary>
        /// <param name="separator">
        /// The array of strings that delimit the substrings of this instance, 
        /// an empty array, or null
        /// </param>
        /// <param name="options">
        /// RemoveEmptyEntries to omit empty elements from the array returned;
        /// None to include empty array elements in the array returned
        /// </param>
        /// <returns>
        /// An array consisting for a single element that contains a copy of this instance, if this
        /// instance contains none of the strings in separator.
        /// -or- An array of substrings, if this instance is delimited by one or more of the strings in separator.
        /// -or- An array of the substrings of this instance that are delimited by white space
        /// characters, if those characters occur in this instance and separator is null or contains
        /// no delimiter strings.
        /// -or- An empty array, if options is RemoveEmptyEntries and the length of this instance is zero.
        /// 
        /// If options is None, an empty SqlSecureString is returned for any substring in which two delimiters 
        /// are adjacent, or if a delimiter is found at the beginning or end of this instance.  Delimiter
        /// characters are not included in the substrings.
        /// </returns>
        public SqlSecureString[] Split(string[] separator, StringSplitOptions options)
        {
            return SqlSecureString.StringArrayToSqlSecureStringArray(
                this.ToString().Split(separator, options));
        }

        /// <summary>
        /// Returns a SqlSecureString array containing the substrings of this instance
        /// that are delimited by elements of a specified string array
        /// </summary>
        /// <param name="separator">
        /// The array of strings that delimit the substrings of this instance, 
        /// an empty array, or null
        /// </param>
        /// <param name="count">The maximum number of substrings to return</param>
        /// <param name="options">
        /// RemoveEmptyEntries to omit empty elements from the array returned;
        /// None to include empty array elements in the array returned
        /// </param>
        /// <returns>
        /// An array consisting for a single element that contains a copy of this instance, if this
        /// instance contains none of the strings in separator.
        /// -or- An array of substrings, if this instance is delimited by one or more of the strings in separator.
        /// -or- An array of the substrings of this instance that are delimited by white space
        /// characters, if those characters occur in this instance and separator is null or contains
        /// no delimiter strings.
        /// -or- An empty array, if options is RemoveEmptyEntries and the length of this instance is zero.
        /// 
        /// If options is None, an empty SqlSecureString is returned for any substring in which two delimiters 
        /// are adjacent, or if a delimiter is found at the beginning or end of this instance.  Delimiter
        /// characters are not included in the substrings.
        /// </returns>
        public SqlSecureString[] Split(string[] separator, int count, StringSplitOptions options)
        {
            return SqlSecureString.StringArrayToSqlSecureStringArray(
                this.ToString().Split(separator, count, options));
        }

        /// <summary>
        /// Determines whether the beginning of this instance matches the specified string.
        /// </summary>
        /// <param name="value">The string to compare</param>
        /// <returns>True if value matches the beginning of this string; otherwise, false</returns>
        public bool StartsWith(SqlSecureString value)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            return this.ToString().StartsWith(value.ToString());
        }

        /// <summary>
        /// Determines whether the beginning of this instance matches the specified string.
        /// </summary>
        /// <param name="value">The string to compare</param>
        /// <param name="ignoreCase">true to ignore case when comparing this instance and value; false to honor case</param>
        /// <param name="culture">Cultural information that determines how this instance and value are compared</param>
        /// <returns>True if value matches the beginning of this string; otherwise, false</returns>
        public bool StartsWith(SqlSecureString value, bool ignoreCase, CultureInfo culture)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }

            return this.ToString().StartsWith(value.ToString(), ignoreCase, culture);
        }

        /// <summary>
        /// Convert an array of strings to an array of SqlSecureStrings
        /// </summary>
        /// <param name="array">The array of strings to convert</param>
        /// <returns>The equivalent array of SqlSecureStrings</returns>
        public static SqlSecureString[] StringArrayToSqlSecureStringArray(string[] array)
        {
            SqlSecureString[] result = null;

            if (array != null)
            {
                result = new SqlSecureString[array.Length];

                for (int i = 0; i < array.Length; ++i)
                {
                    if (array[i] != null)
                    {
                        result[i] = new SqlSecureString(array[i]);
                    }
                    else
                    {
                        result[i] = null;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves a substring from this instance.
        /// </summary>
        /// <param name="startIndex">The character position of the beginning of the substring in this instance</param>
        /// <returns>
        /// The SqlSecureString equivalent to the substring that begins at startIndex, -or-
        /// an empty SqlSecureString if startIndex is equal to the length of this instance.
        /// </returns>
        public SqlSecureString Substring(int startIndex)
        {
            return new SqlSecureString(this.ToString().Substring(startIndex));
        }

        /// <summary>
        /// Retrieves a substring from this instance.
        /// </summary>
        /// <param name="startIndex">The character position of the beginning of the substring in this instance</param>
        /// <param name="length">The number of characters in the substring</param>
        /// <returns>
        /// The SqlSecureString equivalent to the substring of length length that begins at startIndex, -or-
        /// an empty SqlSecureString if startIndex is equal to the length of this instance.
        /// </returns>
        public SqlSecureString Substring(int startIndex, int length)
        {
            return new SqlSecureString(this.ToString().Substring(startIndex, length));
        }

        /// <summary>
        /// Returns a native BSTR containing the decrypted data in this instance.  Callers must
        /// call Marshal.ZeroFreeBSTR() to clear and release the BSTR memory.
        /// </summary>
        /// <returns>A pointer to a native BSTR containing the decrypted string</returns>
        public IntPtr ToBstr()
        {
            return Marshal.SecureStringToBSTR(this.data);
        }

        /// <summary>
        /// Returns a copy of this string converted to lowercase.
        /// </summary>
        /// <returns>The SqlSecureString equivalent to this instance in lowercase.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        public SqlSecureString ToLower()
        {
            return new SqlSecureString(this.ToString().ToLower());
        }

        /// <summary>
        /// Returns a copy of this string converted to lowercase using the rules of
        /// a particular culture.
        /// </summary>
        /// <param name="culture">A CultureInfo object that supplies the culture-specific casing rules</param>
        /// <returns>The SqlSecureString equivalent to this instance in lowercase.</returns>
        public SqlSecureString ToLower(System.Globalization.CultureInfo culture)
        {
            return new SqlSecureString(this.ToString().ToLower(culture));
        }

        /// <summary>
        /// Returns a copy of this string converted to lowercase using the rules of
        /// the invariant culture.
        /// </summary>
        /// <returns>The SqlSecureString equivalent to this instance in lowercase.</returns>
        public SqlSecureString ToLowerInvariant()
        {
            return new SqlSecureString(this.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Returns a System.Security.SecureString copy of this instance's data.
        /// </summary>
        /// <returns>The SecureString equivalent to this instance.</returns>
        public SecureString ToSecureString()
        {
            return this.data.Copy();
        }

        /// <summary>
        /// Returns a System.String containing the decrypted text of this instance.
        /// Callers should set the string object receiving the plaintext secret to
        /// null as soon as possible after they are done using the data.
        /// </summary>
        /// <returns>The plaintext secret stored in this instance</returns>
        public override string ToString()
        {
            string result = String.Empty;

            if (this.Length != 0)
            {
                // Allow partially trusted/low-privilege callers.  The point of this class
                // is to carry out operations on SecureStrings that require elevated privileges
                // without granting blanket UnmanagedCode permission to client assemblies.
                // Decrypting SqlSecureStrings created by client assemblies is not dangerous, 
                // so no special permissions are demanded of clients to do so.
#if !NETCOREAPP
                new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode).Assert();
#endif
                // Decrypt the SecureString
                IntPtr ps = Marshal.SecureStringToBSTR(this.data);
                result = Marshal.PtrToStringBSTR(ps);
                // zero and free the bstr
                Marshal.ZeroFreeBSTR(ps);
            }

            return result;
        }

        /// <summary>
        /// Returns a copy of this string converted to uppercase.
        /// </summary>
        /// <returns>The SqlSecureString equivalent to this instance in uppercase.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToUpper")]
        public SqlSecureString ToUpper()
        {
            return new SqlSecureString(this.ToString().ToUpper());
        }

        /// <summary>
        /// Returns a copy of this string converted to uppercase using the rules of
        /// a particular culture.
        /// </summary>
        /// <param name="culture">A CultureInfo object that supplies the culture-specific casing rules</param>
        /// <returns>The SqlSecureString equivalent to this instance in uppercase.</returns>
        public SqlSecureString ToUpper(CultureInfo culture)
        {
            return new SqlSecureString(this.ToString().ToUpper(culture));
        }

        /// <summary>
        /// Returns a copy of this string converted to uppercase using the rules of
        /// the invariant culture.
        /// </summary>
        /// <returns>The SqlSecureString equivalent to this instance in uppercase.</returns>
        public SqlSecureString ToUpperInvariant()
        {
            return new SqlSecureString(this.ToString().ToUpperInvariant());
        }

        /// <summary>
        /// Returns a copy of this string with all whitespace characters removed from
        /// the beginning and end of this instance.
        /// </summary>
        /// <returns>
        /// The SqlSecureString equivalent to this instance with whitespace removed 
        /// from the beginning and the end.
        /// </returns>
        public SqlSecureString Trim()
        {
            return new SqlSecureString(this.ToString().Trim());
        }

        /// <summary>
        /// Returns a copy of this string with all occurrences of a set of characters 
        /// specified in an arry removed from the beginning and end of this instance.
        /// </summary>
        /// <returns>
        /// The SqlSecureString equivalent to this instance with the specified characters
        /// removed from the beginning and the end.
        /// </returns>
        public SqlSecureString Trim(char[] trimChars)
        {
            return new SqlSecureString(this.ToString().Trim(trimChars));
        }

        /// <summary>
        /// Returns a copy of this string with all occurrences of a set of characters 
        /// specified in an arry removed from the end of this instance.
        /// </summary>
        /// <returns>
        /// The SqlSecureString equivalent to this instance with the specified characters
        /// removed from the end.
        /// </returns>
        public SqlSecureString TrimEnd(char[] trimChars)
        {
            return new SqlSecureString(this.ToString().TrimEnd(trimChars));
        }

        /// <summary>
        /// Returns a copy of this string with all occurrences of a set of characters 
        /// specified in an arry removed from the beginning of this instance.
        /// </summary>
        /// <returns>
        /// The SqlSecureString equivalent to this instance with the specified characters
        /// removed from the beginning.
        /// </returns>
        public SqlSecureString TrimStart(char[] trimChars)
        {
            return new SqlSecureString(this.ToString().TrimStart(trimChars));
        }

        /// <summary>
        /// Implicitly convert a SqlSecureString to a String containing
        /// the decrypted secred stored in the SqlSecureString.  Callers
        /// should set the returned string to null as soon as possible after
        /// the secret data is no longer being used.
        /// </summary>
        /// <param name="sqlSecureString">The SqlSecureString to decrypt</param>
        /// <returns>The String containing the plaintext secret</returns>
        public static explicit operator String(SqlSecureString sqlSecureString)
        {
            String result = null;
            
            if (sqlSecureString != null)
            {
                result = sqlSecureString.ToString();
            }

            return result;
        }

        /// <summary>
        /// Implicitly convert a String to a SqlSecureString.  The secret in the
        /// string is encrypted in the process.  The input string should be set
        /// to null as soon as possible after this operation.
        /// </summary>
        /// <param name="str">The plaintext secret to encrypt</param>
        /// <returns>The SqlSecureString containing the encrypted secret</returns>
        public static implicit operator SqlSecureString(String str)
        {
            SqlSecureString result = null;

            if (str != null)
            {
                result = new SqlSecureString(str);
            }

            return result;
        }

        /// <summary>
        /// Implicitly convert a SqlSecureString to a System.Security.SecureString.
        /// </summary>
        /// <param name="sqlSecureString">The SqlSecureString to convert</param>
        /// <returns>A SecureString containing the encrypted secret</returns>
        public static implicit operator SecureString(SqlSecureString sqlSecureString)
        {
            SecureString result = null;

            if (sqlSecureString != null)
            {
                result = sqlSecureString.data.Copy();
            }

            return result;
        }

        /// <summary>
        /// Implicitly convert a System.Security.SecureString to a SqlSecureString.
        /// </summary>
        /// <param name="secureString">The SecureString to convert</param>
        /// <returns></returns>
        public static implicit operator SqlSecureString(SecureString secureString)
        {
            SqlSecureString result = null;

            if (secureString != null)
            {
                result = new SqlSecureString(secureString);
            }

            return result;
        }

        /// <summary>
        /// Implicitly convert a SqlSecureString to a SqlString containing
        /// the decrypted secred stored in the SqlSecureString.  Callers
        /// should set the returned string to null as soon as possible after
        /// the secret data is no longer being used.
        /// </summary>
        /// <param name="sqlSecureString">The SqlSecureString to decrypt</param>
        /// <returns>The SqlString containing the plaintext secret</returns>
        public static explicit operator SqlString(SqlSecureString sqlSecureString)
        {
            SqlString result = null;

            if (sqlSecureString != null)
            {
                result = new SqlString(sqlSecureString.ToString());
            }

            return result;
        }

        /// <summary>
        /// Implicitly convert a SqlString to a SqlSecureString.  The secret in the
        /// string is encrypted in the process.  The input string should be set
        /// to null as soon as possible after this operation.
        /// </summary>
        /// <param name="str">The plaintext secret to encrypt</param>
        /// <returns>The SqlSecureString containing the encrypted secret</returns>
        public static implicit operator SqlSecureString(SqlString str)
        {
            SqlSecureString result = null;

            if (!str.IsNull)
            {
                result = new SqlSecureString(str.ToString());
            }

            return result;
        }


    }
}
