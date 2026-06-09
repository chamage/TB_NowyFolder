namespace TB_NowyFolder.Models;

/// <summary>
/// Model żądania weryfikacji podpisu cyfrowego dokumentu.
/// Używany przez POST /api/documents/verify.
/// </summary>
public class VerifyRequest
{
    /// <summary>Treść dokumentu w formacie JSON (skopiowana z pola Payload odpowiedzi /generate).</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Podpis cyfrowy w formacie Base64 (skopiowany z pola Signature odpowiedzi /generate).</summary>
    public string Signature { get; set; } = string.Empty;
}
