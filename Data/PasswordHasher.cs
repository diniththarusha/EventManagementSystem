using System.Security.Cryptography;
using System.Text;

namespace EventManagementSystem.Data;

public static class PasswordHasher
{
    public static string Hash(string plainText)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string plainText, string hash)
        => Hash(plainText).Equals(hash, StringComparison.OrdinalIgnoreCase);
}
