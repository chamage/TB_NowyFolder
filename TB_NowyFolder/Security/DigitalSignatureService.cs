using System.Security.Cryptography;
using System.Text;

namespace TB_NowyFolder.Security;

public class DigitalSignatureService
{
    private readonly RSA _rsa;

    public DigitalSignatureService()
    {
        // Klucz RSA generowany przy starcie, tylko w pamięci - po restarcie podpisy z poprzedniej sesji nie dadzą się zweryfikować.
        // W produkcji klucz powinien być utrwalony (np. Azure Key Vault).
        _rsa = RSA.Create(2048);
    }

    public string SignData(string payload)
    {
        // Normalizacja \r\n -> \n przed podpisaniem, żeby podpis był spójny na różnych systemach.
        payload = payload.Replace("\r\n", "\n").Trim();


        var dataBytes = Encoding.UTF8.GetBytes(payload);

        // Podpisanie danych algorytmem SHA256 z paddingiem PKCS1.
        var signatureBytes = _rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Zwrócenie podpisu Base64, żeby można go było dołączyć do JSONa.
        return Convert.ToBase64String(signatureBytes);
    }

    public bool VerifySignature(string payload, string signatureBase64)
    {
        try
        {

            // Normalizacja \r\n -> \n przed zweryfikowaniem, żeby podpis był spójny na różnych systemach.
            payload = payload.Replace("\r\n", "\n").Trim();


            var dataBytes = Encoding.UTF8.GetBytes(payload);

            // Konwersja Base64 na bajty.
            var signatureBytes = Convert.FromBase64String(signatureBase64);

            // Weryfikacja podpisu algorytmem SHA256 z paddingiem PKCS1.
            return _rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            // Błędny Base64, uszkodzony podpis itp. - traktowane jako nieprawidłowy podpis.
            return false;
        }
    }
}