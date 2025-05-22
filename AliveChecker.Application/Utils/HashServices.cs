using System.Security.Cryptography;
using System.Text;

namespace AliveChecker.Application.Utils;

public interface IHashService
{
    string ToHexString(string input);
    string ToBase64String(string input);
}

public class HashService : IHashService
{
    public string ToHexString(string input)
    {
        return input.CalculateSha256Hash().ToHex();
    }

    public string ToBase64String(string input)
    {
        return input.CalculateSha256Hash().ToBase64String();
    }
}

internal static class HashServiceExtensions
{
    internal static string ToHex(this byte[] hashBytes)
    {
        var hexString = new StringBuilder();

        foreach (var t in hashBytes)
        {
            var hex = (t & 0xff).ToString("x2");
            if (hex.Length == 1)
                hexString.Append('0');
            hexString.Append(hex);
        }

        return hexString.ToString();
    }

    internal static string ToBase64String(this byte[] hashBytes) => Convert.ToBase64String(hashBytes);
    // internal string ToBase64String(this byte[] hashBytes) => Base64UrlEncoder.Encode(hashBytes);

    internal static byte[] CalculateSha256Hash(this string input) => SHA256.HashData(Encoding.UTF8.GetBytes(input));

}