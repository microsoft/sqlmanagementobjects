using System;
using System.Text;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Globalization;

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
	/// <summary>
	/// Summary description for DataProtection.
	/// </summary>
	//[StrongNameIdentityPermissionAttribute(
	//	 SecurityAction.LinkDemand, 
	//	 PublicKey="0024000004800000940000000602000000240000525341310004000001000100976BDD12F5AC899FF7F6081A3DA4EF2C88BC3B3571D299F67C24C5EBA386BEAA77F494B10DF5EE7C69ACB27A8F9A7801192F4274C6B2C442F12061BFF6A8F2A2490F7338C7DE6A096780A6D15A4B16E5522CB977BE0B8E4341AD32DA46617D2A0ED5962A904DDC50403A09AAAC75937A8ABB78B4CA119FF3F96DAC20C6E6B7DC")]
	internal sealed class DataProtection
	{
		// statics only here
		private DataProtection()
		{
		}

		// takes a string and returns a base64 encoded string containing
		// the protected data
		public static string ProtectData(string data) 
		{
			return ProtectData(data, CryptoNativeMethods.UIForbidden);
		}

		// takes a base64 encoded containing protected data and
		// returns the data as a string
		public static string UnprotectData(string data) 
		{
			byte[] dataIn = Convert.FromBase64String(data);
			byte[] dataOut = UnprotectData(dataIn);
			string strOut = null;

            if ( null != dataOut )
            {
                if (dataOut.Length == 0)
                {
                    strOut = null;
                } 
                else
                {
                    strOut = Encoding.Unicode.GetString(dataOut);
                }
            }
            dataOut = null;
			return strOut;
		}
		// works on byte arrays
		public static byte[] ProtectData(byte[] data) 
		{
			return ProtectData(data, CryptoNativeMethods.UIForbidden);
		}
		// works on byte arrays
		public static byte[] UnprotectData(byte[] data) 
		{
			return UnprotectData(data, CryptoNativeMethods.UIForbidden);
		}

        private sealed class CryptoNativeMethods 
        {
            // p/invoke decls
            [DllImport("crypt32", CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError=true)]
            internal static extern bool CryptProtectData
                (ref DATA_BLOB dataIn, 
                string szDataDescr, 
                IntPtr optionalEntropy, 
                IntPtr pvReserved, 
                IntPtr pPromptStruct,
                int dwFlags, 
                ref DATA_BLOB pDataOut);

            [DllImport("crypt32", CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError=true)]
            internal static extern bool CryptUnprotectData(
                ref DATA_BLOB dataIn, 
                StringBuilder ppszDataDescr,
                IntPtr optionalEntropy, 
                IntPtr pvReserved,
                IntPtr pPromptStruct, 
                int dwFlags,
                ref DATA_BLOB pDataOut);

            [DllImport("kernel32")]
            internal static extern IntPtr LocalFree(IntPtr hMem);
        
            [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
                public struct DATA_BLOB 
            {
                public int cbData;
                public IntPtr pbData;
            }

            public const int UIForbidden = 0x1;
            public const int LocalMachine = 0x4;
        }

#region private stuff
		// exposes flags
		private static string ProtectData(string data, int flags) 
		{
			byte[] dataIn = Encoding.Unicode.GetBytes(data);
			byte[] dataOut = ProtectData(dataIn, flags);
			string strOut = null;
			if ( null != dataOut )
				strOut = Convert.ToBase64String(dataOut);
            dataIn = null;
			return strOut;
		}

		private static byte[] UnprotectData(byte[] data, int dwFlags) 
		{
			byte[] clearText = null;

			// copy data into unmanaged memory
			CryptoNativeMethods.DATA_BLOB din = new CryptoNativeMethods.DATA_BLOB();		

			StringBuilder name = new StringBuilder();

			din.cbData = data.Length;
			din.pbData = Marshal.AllocHGlobal(din.cbData);
			Marshal.Copy(data, 0, din.pbData, din.cbData);
			CryptoNativeMethods.DATA_BLOB dout = new CryptoNativeMethods.DATA_BLOB();
			try 
			{
				bool ret = CryptoNativeMethods.CryptUnprotectData(
					ref din, 
					name, 
					IntPtr.Zero, 
					IntPtr.Zero, 
					IntPtr.Zero, 
					dwFlags, 
					ref dout);
				if ( ret ) 
				{
					clearText = new byte[dout.cbData];
					Marshal.Copy(dout.pbData, clearText, 0, dout.cbData);
					byte[] emptyBytes = new Byte[dout.cbData];
					// zero out the unmanaged buffer
					Marshal.Copy(emptyBytes, 0, dout.pbData, dout.cbData);
					CryptoNativeMethods.LocalFree(dout.pbData);
					dout.pbData = IntPtr.Zero;
				}
				else 
				{
					throw new Exception(SRError.CryptUnprotectDataWin32Error(Marshal.GetLastWin32Error()));
				}
			}
			finally 
			{
				if (din.pbData != IntPtr.Zero)
					Marshal.FreeHGlobal(din.pbData);
				if (dout.pbData != IntPtr.Zero)
				{
					CryptoNativeMethods.LocalFree(dout.pbData);
					dout.pbData = IntPtr.Zero;
				}
			}
			return clearText;
		}

		private static byte[] ProtectData(byte[] data, int dwFlags) 
		{
			string name = "Default"; // description of data, is required on Win2K
			byte[] cipherText = null;
			// copy data into unmanaged memory
			CryptoNativeMethods.DATA_BLOB din = new CryptoNativeMethods.DATA_BLOB();
			din.cbData = data.Length;
			din.pbData = Marshal.AllocHGlobal(din.cbData);
			Marshal.Copy(data, 0, din.pbData, din.cbData);
			CryptoNativeMethods.DATA_BLOB dout = new CryptoNativeMethods.DATA_BLOB();
			try 
			{
				bool ret = CryptoNativeMethods.CryptProtectData(
					ref din,       // data in
					name,          // szDataDescr
					IntPtr.Zero,   // pOptionalEntropy
					IntPtr.Zero,   // pvReserved
					IntPtr.Zero,   // pPromptStruct
					dwFlags,       // dwFlags
					ref dout       // pDataOut
					);
				// zero out the unmanaged buffer buffer
				byte[] emptyBytes = new byte[din.cbData];
				Marshal.Copy(emptyBytes, 0, din.pbData, din.cbData);

				if ( ret ) 
				{
					cipherText = new byte[dout.cbData];
					Marshal.Copy(dout.pbData, cipherText, 0, dout.cbData);
					CryptoNativeMethods.LocalFree(dout.pbData);
					dout.pbData = IntPtr.Zero;
				}
				else 
				{
					throw new Exception(SRError.CryptProtectDataWin32Error(Marshal.GetLastWin32Error()));
				}
			}
			finally 
			{
				if (din.pbData != IntPtr.Zero)
					Marshal.FreeHGlobal(din.pbData);
				if (dout.pbData != IntPtr.Zero)
				{
					CryptoNativeMethods.LocalFree(dout.pbData);
					dout.pbData = IntPtr.Zero;
				}
			}
			return cipherText;
		}
#endregion
	}


}
