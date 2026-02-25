using System.Security.Cryptography;

namespace Stoat.Services.Utils;

public static class APIKeyGenerator
{
    public static string GenerateApiKey(int length = 32)
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] randomBytes = new byte[length];
            rng.GetBytes(randomBytes);

            string apiKey = BitConverter.ToString(randomBytes).Replace("-", "");

            return apiKey;
        }
    }
}