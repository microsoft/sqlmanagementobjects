// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
#if !NETSTANDARD2_0 && !NETCOREAPP
using System.Runtime.Remoting.Metadata.W3cXsd2001;
#endif

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// 
    /// </summary>
    public class SfcTsqlProcFormatter
    {
        /// <summary>
        /// 
        /// </summary>
        public struct SprocArg
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            /// <param name="property"></param>
            /// <param name="required"></param>
            /// <param name="output"></param>
            public SprocArg(string name, string property, bool required, bool output)
            {
                this.argName = name;
                TraceHelper.Assert(!String.IsNullOrEmpty(property));
                this.property = property;
                this.required = required;
                this.output = output;
            }

            /// <summary>
            /// This constructor is used when a parameter is not a
            /// property on an object but will be provided during the
            /// call to GenerateScript as a RuntimeArg.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="required"></param>
            public SprocArg(string name, bool required)
            {
                this.argName = name;
                this.property = String.Empty;
                this.required = required;
                this.output = false;
            }

            /// <summary>
            /// This constructor is used when a parameter is not a
            /// property on an object but will be provided during the
            /// call to GenerateScript as a RuntimeArg.
            /// Output can be specified here
            /// </summary>
            /// <param name="name"></param>
            /// <param name="required"></param>
            /// <param name="output"></param>
            public SprocArg(string name, bool required, bool output)
            {
                this.argName = name;
                this.property = String.Empty;
                this.required = required;
                this.output = output;
            }

            /// 
            public string argName;
            /// 
            public string property;
            /// 
            public bool required;
            /// 
            public bool output;
        }

        /// <summary>
        /// This struct is used for arguments that are not based on a
        /// SfcProperty inside of the object, but on some arbitrary
        /// value.
        /// </summary>
        public struct RuntimeArg
        {
            /// <summary>
            /// </summary>
            /// <param name="type"></param>
            /// <param name="value"></param>
            public RuntimeArg(Type type, Object value)
            {
                this.type = type;
                this.value = value;
            }

            ///
            public Type type;
            ///
            public Object value;
        }

        List<SprocArg> arguments;
        string procedure;
        
        /// <summary>
        /// 
        /// </summary>
        public string Procedure
        {
            get
            {
                return this.procedure;
            }
            set
            {
                this.procedure = value;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public List<SprocArg> Arguments
        {
            get
            {
                return this.arguments;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SfcTsqlProcFormatter()
        {
            this.arguments = new List<SprocArg>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sfcObject"></param>
        /// <returns></returns>
        public string GenerateScript(SfcInstance sfcObject)
        {
            return GenerateScript(sfcObject, null);
        }

        public string GenerateScript(SfcInstance sfcObject, IEnumerable<RuntimeArg> runtimeArgs)
        {
            return GenerateScript(sfcObject, runtimeArgs, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sfcObject"></param>
        /// <param name="runtimeArgs"></param>
        /// <param name="declareArguments"></param>
        /// <returns></returns>
        public string GenerateScript(SfcInstance sfcObject, IEnumerable<RuntimeArg> runtimeArgs, bool declareArguments)
        {
            IEnumerator<RuntimeArg> runtimeArgsEnum = null;
            if (null != runtimeArgs)
            {
                runtimeArgsEnum = runtimeArgs.GetEnumerator();
                runtimeArgsEnum.Reset();
                runtimeArgsEnum.MoveNext();
            }

            string outputSelect = String.Empty;

            StringBuilder sb = new StringBuilder();

            bool hasOutput = false;

            // Check for OUTPUT args
            //
            // Only supports 1 output parametr
            // Don't set parametr value from the property
            //
            foreach (SprocArg arg in this.Arguments)
            {
                if (arg.output)
                {
                    Type type;

                    if (hasOutput)
                    {
                        TraceHelper.Assert(false, "More than one OUTPUT parameters specified!");
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(arg.property))
                        {
                            // If it's not a property it must be the first in the RuntimeArg collection
                            //  get arg type, but don't move to the next one

                            RuntimeArg runtime = runtimeArgsEnum.Current;
                            type = runtime.type;
                        }
                        else
                        {
                            SfcProperty prop = sfcObject.Properties[arg.property];
                            type = prop.Type;
                        }

                        if (declareArguments)
                        {
                            sb.AppendFormat("Declare @{0} ", arg.argName);

                            if (type == typeof(int))
                            {
                                sb.Append("int");
                            }
                            else if (type == typeof(long))
                            {
                                sb.Append("bigint");
                            }
                            else
                            {
                                TraceHelper.Assert(false, "Unexpected OUTPUT parameter type!");
                            }

                            sb.AppendLine();
                        }

                        outputSelect = "Select @" + arg.argName;

                        hasOutput = true;
                    }
                }
            }

            sb.Append("EXEC ");
            sb.Append(this.Procedure);

            //"$ISSUE - VSTS number:171932
            //START temporary solution
            bool addAllProperties = true;
            foreach (SprocArg arg in this.Arguments)
            {
                if (String.IsNullOrEmpty(arg.property)) //If the property is null, then this won't have to affect the decision of whether to include all properties or not because it's not a property.
                {
                    continue;
                }

                SfcProperty prop = sfcObject.Properties[arg.property];
                if (prop.Dirty)
                {
                    addAllProperties = false;
                    break;
                }
            }
            //END temporary solution

            int count = 0;
            foreach (SprocArg arg in this.Arguments)
            {
                Type type = null;
                Object value = null;

                if (String.IsNullOrEmpty(arg.property))
                {
                    TraceHelper.Assert(!(null == runtimeArgsEnum), String.Format("No runtimeArgsEnum but there is no property with the name '!{0}", arg.argName));
                    RuntimeArg runtime = runtimeArgsEnum.Current;
                    runtimeArgsEnum.MoveNext();
                    type = runtime.type;
                    value = runtime.value;
                }
                else
                {
                    SfcProperty prop = sfcObject.Properties[arg.property];
                    if (arg.required)
                    {
                        if (null == prop)
                        {
                            throw new SfcPropertyNotSetException(arg.property);
                        }
                    }
                    else if (!arg.output && !prop.Dirty && !addAllProperties)
                    {
                        continue;
                    }

                    type = prop.Type;
                    value = prop.Value;
                }

                if (count > 0)
                {
                    sb.Append(",");
                }

                if (arg.output)
                {
                    sb.AppendFormat(" @{0}=@{0} OUTPUT", arg.argName);
                }
                else
                {
                    sb.AppendFormat(" @{0}=", arg.argName);

                    //TODO: when the object model is throwing exception in case of null reference, then enable this,
                    // but this this is creating in-consistency between Object Model and scripting.
                    if (type == typeof(String))
                    {
                        if (value == null)
                        {
                            value = string.Empty;
                        }

                        sb.Append(MakeSqlString((String)value));
                    }
                    else if (type == typeof(DateTime))
                    {
                        // Handles SQL's date, datetime and datetime2 types since we make .NET map to DateTime for all three
                        if (value == null)
                        {
                            value = DateTime.MinValue;
                        }

                        DateTime date = (DateTime)value;
                        // Produces "2008-04-10T06:30:00" sortable ISO 8601 format (common regardless of culture)
                        sb.Append(MakeSqlString(date.ToString("s", CultureInfo.InvariantCulture)));
                    }
                    else if (type == typeof(DateTimeOffset))
                    {
                        if (value == null)
                        {
                            value = DateTimeOffset.MinValue;
                        }

                        DateTimeOffset dateOffset = (DateTimeOffset)value;
                        // Produces "2008-04-10T06:30:00.0000000-07:00" round-trip ISO 8601 format preserving timezone (common regardless of culture)
                        sb.Append(MakeSqlString(dateOffset.ToString("o", CultureInfo.InvariantCulture)));
                    }
                    else if (type == typeof(TimeSpan))
                    {
                        if (value == null)
                        {
                            value = TimeSpan.MinValue;
                        }

                        TimeSpan time = (TimeSpan)value;
                        // Produces "hh:mi:ss[.fffffff]" format (fractional seconds only present if there is any in the value, and always uses 7 digits when present)
                        sb.Append(MakeSqlString(time.ToString()));
                    }
                    else if ((type == typeof(Guid)) ||
                             (type == typeof(SfcQueryExpression)))
                    {
                        if (value == null)
                        {
                            if (type == typeof(Guid))
                            {
                                value = Guid.Empty;
                            }
                            else
                            {
                                value = new SfcQueryExpression("");
                            }
                        }
                        sb.Append(MakeSqlString(value.ToString()));
                    }
                    else if (type.IsEnum())
                    {
                        if (value == null)
                        {
                            value = 0;
                        }

                        sb.Append(Convert.ToInt32(value));
                    }
                    else if (type == typeof(Byte[]))
                    {
                        sb.Append("0x");
                        sb.Append((ConvertToHexBinary((Byte[])value)).ToString());
                    }
                    else
                    {
                        if (value == null)
                        {
                            value = "null";
                        }

                        sb.Append(value);
                    }
                }
                count++;
            }

            if (hasOutput)
            {
                sb.AppendLine();
                sb.AppendLine(outputSelect);
            }

            return sb.ToString();
        }

        private string ConvertToHexBinary(Byte[] byteValue)
        {
            #if !NETSTANDARD2_0 && !NETCOREAPP
            return new SoapHexBinary(byteValue).ToString();
#else
            StringBuilder sb = new StringBuilder(100);
            sb.Length = 0;
            for (int i = 0; i < byteValue.Length; i++)
            {
                String s = byteValue[i].ToString("X", CultureInfo.InvariantCulture);
                if (s.Length == 1)
                {
                    sb.Append('0');
                }
                sb.Append(s);
            }
            return sb.ToString();
#endif
        }


        public static String EscapeString(String value, char charToEscape)
        {
            if (null == value)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                sb.Append(c);
                if (charToEscape == c)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static String SqlString(String value)
        {
            return EscapeString(value, '\'');
        }

        public static String MakeSqlString(String value)
        {
            return String.Format(CultureInfo.InvariantCulture, "N'{0}'", EscapeString(value, '\''));
        }

        public static String SqlBracket(String value)
        {
            return EscapeString(value, ']');
        }

        public static String MakeSqlBracket(String value)
        {
            return string.Format(CultureInfo.InvariantCulture, "[{0}]", EscapeString(value, ']'));
        }

        public static String SqlStringBracket(String value)
        {
            return SqlBracket(SqlString(value));
        }
    }

}
