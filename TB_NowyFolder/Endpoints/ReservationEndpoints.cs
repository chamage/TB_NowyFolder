using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;

namespace TB_NowyFolder.Endpoints;

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservations")
            .WithTags("Reservations");

        // GET all reservations
        group.MapGet("/", async (HotelDbContext db) =>
        {
            return await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .ToListAsync();
        })
        .WithName("GetAllReservations")
        .Produces<List<Reservation>>(StatusCodes.Status200OK);

        // GET reservation by ID
        group.MapGet("/{id}", async (int id, HotelDbContext db) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Room)
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            return reservation is not null
                ? Results.Ok(reservation)
                : Results.NotFound();
        })
        .WithName("GetReservationById")
        .Produces<Reservation>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET reservations by guest
        group.MapGet("/guest/{guestId}", async (int guestId, HotelDbContext db) =>
        {
            return await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Room)
                .Where(r => r.GuestID == guestId)
                .ToListAsync();
        })
        .WithName("GetReservationsByGuest")
        .Produces<List<Reservation>>(StatusCodes.Status200OK);

        // POST create reservation
        group.MapPost("/", async (Reservation reservation, HotelDbContext db) =>
        {
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();
            return Results.Created($"/api/reservations/{reservation.ReservationID}", reservation);
        })
        .WithName("CreateReservation")
        .Produces<Reservation>(StatusCodes.Status201Created);

        // PUT update reservation
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
        .WithName("UpdateReservation")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE reservation
        group.MapDelete("/{id}", async (int id, HotelDbContext db) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.ReservationRooms)
                .ThenInclude(rr => rr.Room)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation is null) return Results.NotFound();

            // Set rooms back to Available
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
        .Produces(StatusCodes.Status404NotFound);

        // POST add room to reservation
        group.MapPost("/{reservationId}/rooms/{roomId}", async (int reservationId, int roomId, HotelDbContext db) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.ReservationRooms) // Include existing rooms to check duplicates
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);
                
            var room = await db.Rooms.FindAsync(roomId);

            if (reservation is null || room is null)
                return Results.NotFound();
            
            // Check if room is already added
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

            // Calculate nights for pricing
            int nights = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
            if (nights < 1) nights = 1;

            // Update total price
            reservation.TotalPrice += room.PricePerNight * nights;
            
            // Mark room as Occupied so it doesn't show in Available list
            room.Status = "Occupied";

            db.ReservationRooms.Add(reservationRoom);
            await db.SaveChangesAsync();
            return Results.Created($"/api/reservations/{reservationId}/rooms/{roomId}", reservationRoom);
        })
        .WithName("AddRoomToReservation")
        .Produces<ReservationRoom>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound);

        // POST add service to reservation
        group.MapPost("/{reservationId}/services/{serviceId}", async (int reservationId, int serviceId, ReservationService input, HotelDbContext db) =>
        {
            var reservation = await db.Reservations.FindAsync(reservationId);
            var service = await db.Services.FindAsync(serviceId);

            if (reservation is null || service is null)
                return Results.NotFound();

            var reservationService = new ReservationService
            {
                ReservationID = reservationId,
                ServiceID = serviceId,
                Quantity = input.Quantity,
                ServiceDate = input.ServiceDate
            };

            // Update total price
            reservation.TotalPrice += service.UnitPrice * input.Quantity;

            db.ReservationServices.Add(reservationService);
            await db.SaveChangesAsync();
            return Results.Created($"/api/reservations/{reservationId}/services/{serviceId}", reservationService);
        })
        .WithName("AddServiceToReservation")
        .Produces<ReservationService>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound);
    }
}
