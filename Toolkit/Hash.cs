using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Toolkit
{
	/// <summary>
	/// MD5その他諸々のハッシュを算出
	/// </summary>
	public static class Hash
	{
		public static string MD5(string data) {
			return MD5(Encoding.Default.GetBytes(data));
		}
		public static string MD5(byte[] data) {
			return BitConverter.ToString(
				new MD5CryptoServiceProvider().ComputeHash(data)
				).Replace("-", "").ToLower();
		}

		public static string SHA1(string data) {
			return SHA1(Encoding.Default.GetBytes(data));
		}
		public static string SHA1(byte[] data) {
			return BitConverter.ToString(
				new SHA1CryptoServiceProvider().ComputeHash(data)
				).Replace("-", "").ToLower();
		}

		public static string HMACMD5(string pass, string challenge) {
			return HMACMD5(
				Encoding.Default.GetBytes(pass),
				Encoding.Default.GetBytes(challenge));
		}
		public static string HMACMD5(byte[] pass, byte[] challenge) {
			return BitConverter.ToString(
				new HMACMD5(pass).ComputeHash(challenge) // なんか逆のような気がするのだが。。
				).Replace("-", "").ToLower();
		}
	}
}
