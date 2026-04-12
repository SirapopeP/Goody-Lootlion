using System.Security.Cryptography;
using System.Text;

namespace Lootlion.Infrastructure.Security;

public static class TokenHasher
{
    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
