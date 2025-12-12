using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;

namespace TB_NowyFolder.Endpoints;

public static class GuestEndpoints
{
    public static void MapGuestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/guests")
            .WithTags("Guests");

        // GET all guests
        group.MapGet("/", async (HotelDbContext db) =>
        {
            return await db.Guests.ToListAsync();
        })
        .WithName("GetAllGuests")
        .Produces<List<Guest>>(StatusCodes.Status200OK);

        // GET guest by ID
        group.MapGet("/{id}", async (int id, HotelDbContext db) =>
        {
            return await db.Guests.FindAsync(id)
                is Guest guest
                    ? Results.Ok(guest)
                    : Results.NotFound();
        })
        .WithName("GetGuestById")
        .Produces<Guest>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST create guest
        group.MapPost("/", async (Guest guest, HotelDbContext db) =>
        {
            db.Guests.Add(guest);
            await db.SaveChangesAsync();
            return Results.Created($"/api/guests/{guest.GuestID}", guest);
        })
        .WithName("CreateGuest")
        .Produces<Guest>(StatusCodes.Status201Created);

        // PUT update guest
        group.MapPut("/{id}", async (int id, Guest inputGuest, HotelDbContext db) =>
        {
            var guest = await db.Guests.FindAsync(id);
            if (guest is null) return Results.NotFound();

            guest.FirstName = inputGuest.FirstName;
            guest.LastName = inputGuest.LastName;
            guest.Email = inputGuest.Email;
            guest.Phone = inputGuest.Phone;
            guest.TaxID = inputGuest.TaxID;
            guest.Notes = inputGuest.Notes;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateGuest")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE guest
        group.MapDelete("/{id}", async (int id, HotelDbContext db) =>
        {
            var guest = await db.Guests.FindAsync(id);
            if (guest is null) return Results.NotFound();

            db.Guests.Remove(guest);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteGuest")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
