using System.Security.Cryptography;
using System.Text;

namespace TB_NowyFolder.Security;

public class DigitalSignatureService
{
    // Mechanizm RSA do podpisywania i sprawdzania danych
    private readonly RSA _rsa;

    public DigitalSignatureService()
    {
        // Tworzenie kluczy RSA o długości 2048 bitów
        _rsa = RSA.Create(2048);
    }

    public string SignData(string payload)
    {
        // Ujednolicenie tekstu przed podpisaniem
        payload = payload.Replace("\r\n", "\n").Trim();

        // Zamiana tekstu na bajty
        var dataBytes = Encoding.UTF8.GetBytes(payload);

        // Tworzenie podpisu cyfrowego
        var signatureBytes = _rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Zamiana podpisu na tekst Base64
        return Convert.ToBase64String(signatureBytes);
    }

    public bool VerifySignature(string payload, string signatureBase64)
    {
        try
        {
            // Ujednolicenie tekstu przed sprawdzeniem podpisu
            payload = payload.Replace("\r\n", "\n").Trim();

            // Zamiana tekstu na bajty
            var dataBytes = Encoding.UTF8.GetBytes(payload);

            // Odczyt podpisu z Base64
            var signatureBytes = Convert.FromBase64String(signatureBase64);

            // Sprawdzenie czy podpis jest poprawny
            return _rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            // Zwrócenie false jeśli wystąpi błąd
            return false;
        }
    }
}