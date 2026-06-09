using System.Text.Json;
using TB_NowyFolder.Security;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;
using Microsoft.EntityFrameworkCore;

namespace TB_NowyFolder.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        // GET /api/documents/generate/{reservationId}
        // Generuje podpisany cyfrowo dokument potwierdzenia rezerwacji (podpis RSA-SHA256).
        // UWAGA: Klucz RSA jest generowany przy starcie aplikacji i nie jest utrwalany.
        // Weryfikacja podpisów z poprzednich sesji nie jest możliwa po restarcie serwera.
        group.MapGet("/generate/{reservationId}", async (int reservationId, HotelDbContext db, DigitalSignatureService signer) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.Guest)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null)
                return Results.NotFound();

            var documentPayload = new
            {
                Type = "BookingReceipt",
                ReservationID = reservation.ReservationID,
                GuestName = $"{reservation.Guest?.FirstName} {reservation.Guest?.LastName}",
                CheckIn = reservation.CheckInDate.ToString("yyyy-MM-dd"),
                CheckOut = reservation.CheckOutDate.ToString("yyyy-MM-dd"),
                Total = reservation.TotalPrice,
                GeneratedAt = DateTime.UtcNow.ToString("O")
            };

            var payloadString = JsonSerializer.Serialize(documentPayload, new JsonSerializerOptions { WriteIndented = true });
            var signature = signer.SignData(payloadString);

            return Results.Ok(new
            {
                Payload = payloadString,
                Signature = signature
            });
        })
        .WithName("GenerateDocument")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/documents/verify
        // Weryfikuje podpis cyfrowy dokumentu. Zwraca {IsValid: true/false}.
        group.MapPost("/verify", (VerifyRequest request, DigitalSignatureService signer) =>
        {
            if (string.IsNullOrWhiteSpace(request.Payload) || string.IsNullOrWhiteSpace(request.Signature))
                return Results.BadRequest("Payload and Signature are required.");

            bool isValid = signer.VerifySignature(request.Payload, request.Signature);

            return Results.Ok(new { IsValid = isValid });
        })
        .WithName("VerifyDocument")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
