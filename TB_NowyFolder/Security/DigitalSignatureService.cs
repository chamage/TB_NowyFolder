using System.Security.Cryptography;
using System.Text;

namespace TB_NowyFolder.Security;

public class DigitalSignatureService
{
    private readonly RSA _rsa;

    public DigitalSignatureService()
    {
        _rsa = RSA.Create(2048);
    }

    public string SignData(string payload)
    {
        payload = payload.Replace("\r\n", "\n").Trim();
        var dataBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = _rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signatureBytes);
    }

    public bool VerifySignature(string payload, string signatureBase64)
    {
        try
        {
            payload = payload.Replace("\r\n", "\n").Trim();
            var dataBytes = Encoding.UTF8.GetBytes(payload);
            var signatureBytes = Convert.FromBase64String(signatureBase64);
            return _rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
}
