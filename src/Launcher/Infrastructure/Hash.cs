using System.Security.Cryptography;
using System.Text;

namespace Launcher.Infrastructure;

/// <summary>
/// MD5等の各種のハッシュを算出
/// </summary>
public static class Hash
{
    public static string MD5(string data)
    {
        return MD5(Encoding.Default.GetBytes(data));
    }
    public static string MD5(byte[] data)
    {
        return Convert.ToHexString(
            System.Security.Cryptography.MD5.HashData(data)
            ).ToLower();
    }

    public static string SHA1(string data)
    {
        return SHA1(Encoding.Default.GetBytes(data));
    }
    public static string SHA1(byte[] data)
    {
        return Convert.ToHexString(
            System.Security.Cryptography.SHA1.HashData(data)
            ).ToLower();
    }

    public static string HMACMD5(string pass, string challenge)
    {
        return HMACMD5(
            Encoding.Default.GetBytes(pass),
            Encoding.Default.GetBytes(challenge));
    }
    public static string HMACMD5(byte[] pass, byte[] challenge)
    {
        return Convert.ToHexString(
            System.Security.Cryptography.HMACMD5.HashData(pass, challenge)
            ).ToLower();
    }
}
