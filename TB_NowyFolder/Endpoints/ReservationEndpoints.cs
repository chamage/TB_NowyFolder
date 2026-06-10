using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

using TB_NowyFolder.Data;
using TB_NowyFolder.Models;
using TB_NowyFolder.Security;


namespace TB_NowyFolder.Endpoints;

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        // Każdy endpoint rezerwacji wymaga zalogowania, niektóre dodatkowo roli StaffOrAdmin.
        var group = app.MapGroup("/api/reservations")
            .WithTags("Reservations")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        // GET /api/reservations — lista wszystkich rezerwacji, tylko Staff/Admin
        group.MapGet("/", async (HotelDbContext db) =>
        {
            return await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Room)
                        .ThenInclude(room => room.RoomType)
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .ToListAsync();
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin)
        .WithName("GetAllReservations")
        .Produces<List<Reservation>>(StatusCodes.Status200OK);

        // GET /api/reservations/my — rezerwacje zalogowanego klienta
        // GuestID pobierany z tokenu JWT - klient widzi tylko swoje rezerwacje.
        group.MapGet("/my", async (ClaimsPrincipal user, HotelDbContext db) =>
        {
            var guestIdClaim = user.FindFirst("guestId")?.Value;
            if (string.IsNullOrEmpty(guestIdClaim) || !int.TryParse(guestIdClaim, out var guestId))
                return Results.Forbid();

            var reservations = await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Room)
                        .ThenInclude(room => room.RoomType)
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .Where(r => r.GuestID == guestId)
                .ToListAsync();

            return Results.Ok(reservations);
        })
        .WithName("GetMyReservations")
        .Produces<List<Reservation>>(StatusCodes.Status200OK);

        // Staff/Admin widzi każdą. Klient widzi tylko swoją - guestId z tokenu jest porównywane z GuestID rezerwacji.
        group.MapGet("/{id}", async (int id, ClaimsPrincipal user, HotelDbContext db) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Room)
                        .ThenInclude(room => room.RoomType)
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation is null)
                return Results.NotFound();

            // Ochrona przed IDOR - klient nie może podglądać cudzej rezerwacji.
            if (user.IsInRole(ApplicationRoles.Client))
            {
                var guestIdClaim = user.FindFirst("guestId")?.Value;
                if (!int.TryParse(guestIdClaim, out var guestId) || reservation.GuestID != guestId)
                    return Results.Forbid();
            }

            return Results.Ok(reservation);
        })
        .WithName("GetReservationById")
        .Produces<Reservation>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // GET /api/reservations/guest/{guestId} — rezerwacje konkretnego gościa, tylko Staff/Admin
        group.MapGet("/guest/{guestId}", async (int guestId, HotelDbContext db) =>
        {
            return await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Room)
                        .ThenInclude(room => room.RoomType)
                .Where(r => r.GuestID == guestId)
                .ToListAsync();
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin)
        .WithName("GetReservationsByGuest")
        .Produces<List<Reservation>>(StatusCodes.Status200OK);

        // POST /api/reservations - tworzenie rezerwacji
        // Klient zawsze dostaje swoje GuestID z tokenu - nie może podać cudzego ID w żądaniu.
        group.MapPost("/", async (Reservation reservation, ClaimsPrincipal user, HotelDbContext db) =>
        {
            // Nadpisanie GuestID wartością z tokenu - ochrona przed IDOR.
            if (user.IsInRole(ApplicationRoles.Client))
            {
                var guestIdClaim = user.FindFirst("guestId")?.Value;
                if (!string.IsNullOrEmpty(guestIdClaim) && int.TryParse(guestIdClaim, out var guestId))
                    reservation.GuestID = guestId;
            }

            // Cena wyliczana przez serwer na podstawie cen pokoi i liczby nocy.
            if (reservation.ReservationRooms != null && reservation.ReservationRooms.Any())
            {
                int nights = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
                if (nights < 1) nights = 1;

                reservation.TotalPrice = 0;
                foreach (var rr in reservation.ReservationRooms)
                {
                    var room = await db.Rooms.FindAsync(rr.RoomID);
                    if (room != null)
                    {
                        rr.PricePerNight = room.PricePerNight;
                        reservation.TotalPrice += room.PricePerNight * nights;
                        room.Status = "Occupied";
                    }
                }
            }

            db.Reservations.Add(reservation);
            // Rezerwacja, pokoje i status pokoju zapisywane razem w jednej transakcji EF Core.
            await db.SaveChangesAsync();
            return Results.Created($"/api/reservations/{reservation.ReservationID}", reservation);
        })
        .WithName("CreateReservation")
        .Produces<Reservation>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized);

        // PUT /api/reservations/{id} - tylko Staff/Admin
        // Klient nie może sam edytować rezerwacji, m.in. zmieniać jej GuestID.
        group.MapPut("/{id}", async (int id, Reservation inputReservation, HotelDbContext db) =>
        {
            var reservation = await db.Reservations.FindAsync(id);
            if (reservation is null) return Results.NotFound();

            reservation.GuestID = inputReservation.GuestID;
            reservation.CheckInDate = inputReservation.CheckInDate;
            reservation.CheckOutDate = inputReservation.CheckOutDate;
            reservation.NumberOfGuests = inputReservation.NumberOfGuests;
            reservation.TotalPrice = inputReservation.TotalPrice;
            reservation.ReservationStatus = inputReservation.ReservationStatus;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin)
        .WithName("UpdateReservation")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/reservations/{id}
        // Klient może usunąć tylko swoją rezerwację - analogiczna ochrona jak przy GET.
        group.MapDelete("/{id}", async (int id, ClaimsPrincipal user, HotelDbContext db) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.ReservationRooms)
                .ThenInclude(rr => rr.Room)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation is null) return Results.NotFound();

            // Ten sam check co w GET - klient nie może usunąć cudzej rezerwacji.
            if (user.IsInRole(ApplicationRoles.Client))
            {
                var guestIdClaim = user.FindFirst("guestId")?.Value;
                if (!int.TryParse(guestIdClaim, out var guestId) || reservation.GuestID != guestId)
                    return Results.Forbid();
            }

            // Przy usuwaniu rezerwacji pokoje wracają do statusu Available.
            if (reservation.ReservationRooms != null)
            {
                foreach (var rr in reservation.ReservationRooms)
                {
                    if (rr.Room != null)
                    {
                        rr.Room.Status = "Available";
                    }
                }
            }

            db.Reservations.Remove(reservation);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteReservation")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/reservations/{reservationId}/rooms/{roomId} — dodanie pokoju do rezerwacji, Staff/Admin
        group.MapPost("/{reservationId}/rooms/{roomId}", async (int reservationId, int roomId, HotelDbContext db) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.ReservationRooms)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            var room = await db.Rooms.FindAsync(roomId);

            if (reservation is null || room is null)
                return Results.NotFound();

            if (reservation.ReservationRooms?.Any(rr => rr.RoomID == roomId) == true)
            {
                return Results.Conflict("Room is already added to this reservation.");
            }

            var reservationRoom = new ReservationRoom
            {
                ReservationID = reservationId,
                RoomID = roomId,
                PricePerNight = room.PricePerNight
            };

            int nights = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
            if (nights < 1) nights = 1;

            reservation.TotalPrice += room.PricePerNight * nights;
            room.Status = "Occupied";

            db.ReservationRooms.Add(reservationRoom);
            await db.SaveChangesAsync();
            return Results.Created($"/api/reservations/{reservationId}/rooms/{roomId}", reservationRoom);
        })
        .WithName("AddRoomToReservation")
        .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin)
        .Produces<ReservationRoom>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // POST /{reservationId}/services/{serviceId} - Staff/Admin
        // Jeśli ta sama usługa na ten sam dzień już istnieje, ilość jest sumowana zamiast tworzyć nowy wpis.
        group.MapPost("/{reservationId}/services/{serviceId}", async (int reservationId, int serviceId, ReservationService input, HotelDbContext db) =>
        {
            var reservation = await db.Reservations.FindAsync(reservationId);
            var service = await db.Services.FindAsync(serviceId);

            if (reservation is null || service is null)
                return Results.NotFound();

            var existingForSameDay = await db.ReservationServices.FirstOrDefaultAsync(rs =>
                rs.ReservationID == reservationId &&
                rs.ServiceID == serviceId &&
                rs.ServiceDate == input.ServiceDate);

            if (existingForSameDay is not null)
            {
                existingForSameDay.Quantity += input.Quantity;
                reservation.TotalPrice += service.UnitPrice * input.Quantity;
                await db.SaveChangesAsync();
                return Results.Ok(existingForSameDay);
            }

            var reservationService = new ReservationService
            {
                ReservationID = reservationId,
                ServiceID = serviceId,
                Quantity = input.Quantity,
                ServiceDate = input.ServiceDate
            };

            reservation.TotalPrice += service.UnitPrice * input.Quantity;

            db.ReservationServices.Add(reservationService);
            await db.SaveChangesAsync();
            return Results.Created($"/api/reservations/{reservationId}/services/{serviceId}", reservationService);
        })
        .WithName("AddServiceToReservation")
        .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin)
        .Produces<ReservationService>(StatusCodes.Status201Created)
        .Produces<ReservationService>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}
