// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    ///    Summary description for SQLToolsOutputDebugStringWriter.
    /// </summary>

    internal class SQLToolsOutputDebugStringListener : TraceListener
    {
        private string myName = "SQLToolsOutputDebugStringListener";

        public SQLToolsOutputDebugStringListener()
        {
            //not much to do
        }

        public SQLToolsOutputDebugStringListener(string name)
        : base(name)
        {
            myName = name;
        }

        //overriden members
        public override void Write(string message)
        {
            #if !NETSTANDARD2_0
            if (Debugger.IsLogging()) 
            {
                Debugger.Log(0, null, message);
            } 
            else
            {
                if (message != null)
                {
                    OutputDebugString(message);
                } 
                else
                {
                    OutputDebugString(String.Empty);
                }
            }
            #endif
        }
        
        public override void WriteLine(string message)
        {
            Write(message + "\r\n");
        }

        #if !NETSTANDARD2_0
        [DllImport("kernel32.dll", CharSet=System.Runtime.InteropServices.CharSet.Auto)]
        #else
        [DllImport("kernel32.dll", CharSet=System.Runtime.InteropServices.CharSet.Ansi)]
#endif
        internal static extern void OutputDebugString(String message);
    };

}
