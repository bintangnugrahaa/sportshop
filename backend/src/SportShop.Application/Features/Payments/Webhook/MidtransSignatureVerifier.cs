using System.Security.Cryptography;
using System.Text;

namespace SportShop.Application.Features.Payments.Webhook;

public static class MidtransSignatureVerifier
{
    public static string CalculateSignature(string orderId, string statusCode, string grossAmount, string serverKey)
    {
        var input = $"{orderId}{statusCode}{grossAmount}{serverKey}";
        var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool Verify(string orderId, string statusCode, string grossAmount, string serverKey, string signatureKey)
    {
        if (string.IsNullOrWhiteSpace(signatureKey))
        {
            return false;
        }

        var expectedHex = CalculateSignature(orderId, statusCode, grossAmount, serverKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedHex);
        var providedBytes = Encoding.UTF8.GetBytes(signatureKey.Trim().ToLowerInvariant());

        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
