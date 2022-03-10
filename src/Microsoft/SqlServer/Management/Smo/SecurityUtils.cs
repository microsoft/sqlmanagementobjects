// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Provides Security domain Utility methods.
    /// </summary>
    internal static class SecurityUtils
    {
        /// <summary>
        /// Scripts Random password for Shiloh and earlier in Login and for all version in ApplicationRoles.
        /// </summary>
        /// <param name="pwdGenScript"></param>
        public static void ScriptRandomPwd(StringBuilder pwdGenScript)
        {
            // this is what we are generating
            //                  /* To avoid disclosure of passwords, the password is generated in script. */
            //                  declare @idx as int
            //                  declare @randomPwd as nvarchar(64)
            //                  declare @rnd as float
            //                  select @idx = 0
            //                  select @randomPwd = ''
            //                  select @rnd = rand((@@CPU_BUSY % 100) +
            //                      ((@@IDLE % 100) * 100) +
            //                      (DATEPART(ss, GETDATE()) * 10000) +
            //                      ((cast(DATEPART(ms, GETDATE()) as int) % 100) * 1000000))
            //                  while @idx < 64
            //                  begin
            //                      select @randomPwd = @randomPwd + char((cast((@rnd * 83) as int) + 43))
            //                      select @idx = @idx + 1
            //                      select @rnd = rand()
            //                  end
            int pwdLength = 64;

            pwdGenScript.Append("/* To avoid disclosure of passwords, the password is generated in script. */");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("declare @idx as int");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.AppendFormat(SmoApplication.DefaultCulture,
                                      "declare @randomPwd as nvarchar({0})", pwdLength);
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("declare @rnd as float");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("select @idx = 0");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("select @randomPwd = N''");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("select @rnd = rand((@@CPU_BUSY % 100) + ((@@IDLE % 100) * 100) + ");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("       (DATEPART(ss, GETDATE()) * 10000) + ((cast(DATEPART(ms, GETDATE()) as int) % 100) * 1000000))");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.AppendFormat(SmoApplication.DefaultCulture,
                                      "while @idx < {0}", pwdLength);
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("begin");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("   select @randomPwd = @randomPwd + char((cast((@rnd * 83) as int) + 43))");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("   select @idx = @idx + 1");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("select @rnd = rand()");
            pwdGenScript.Append(Globals.newline);
            pwdGenScript.Append("end");
            pwdGenScript.Append(Globals.newline);

        }

        ///<summary>
        ///uses a random number generator to generate a cryptographically 
        ///strong password</summary>
        public static string GenerateRandomPassword()
        {
            const int RandomPasswordLength = 32;

            byte[] bytepwd = new byte[RandomPasswordLength];
            Rng.GetNonZeroBytes(bytepwd);
            return Convert.ToBase64String(bytepwd);
        }

        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
        private static RandomNumberGenerator Rng
        {
            get
            {
                return rng;
            }
        }
    }
}
