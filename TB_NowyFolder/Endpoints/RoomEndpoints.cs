using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;
using TB_NowyFolder.Security;


namespace TB_NowyFolder.Endpoints;

public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms")
            .WithTags("Rooms");

        // GET all rooms — public (offer browsing)
        group.MapGet("/", async (HotelDbContext db) =>
        {
            return await db.Rooms.Include(r => r.RoomType).ToListAsync();
        })
        .AllowAnonymous()
        .WithName("GetAllRooms")
        .Produces<List<Room>>(StatusCodes.Status200OK);

        // GET room by ID — public
        group.MapGet("/{id}", async (int id, HotelDbContext db) =>
        {
            var room = await db.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            return room is not null
                ? Results.Ok(room)
                : Results.NotFound();
        })
        .AllowAnonymous()
        .WithName("GetRoomById")
        .Produces<Room>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET available rooms — public
        group.MapGet("/available", async (HotelDbContext db) =>
        {
            return await db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "Available")
                .ToListAsync();
        })
        .AllowAnonymous()
        .WithName("GetAvailableRooms")
        .Produces<List<Room>>(StatusCodes.Status200OK);

        // POST create room — Admin only
        group.MapPost("/", async (Room room, HotelDbContext db) =>
        {
            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return Results.Created($"/api/rooms/{room.RoomID}", room);
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly)
        .WithName("CreateRoom")
        .Produces<Room>(StatusCodes.Status201Created);

        // PUT update room — Admin only
        group.MapPut("/{id}", async (int id, Room inputRoom, HotelDbContext db) =>
        {
            var room = await db.Rooms.FindAsync(id);
            if (room is null) return Results.NotFound();

            room.RoomTypeID = inputRoom.RoomTypeID;
            room.RoomNumber = inputRoom.RoomNumber;
            room.Capacity = inputRoom.Capacity;
            room.PricePerNight = inputRoom.PricePerNight;
            room.Status = inputRoom.Status;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly)
        .WithName("UpdateRoom")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE room — Admin only
        group.MapDelete("/{id}", async (int id, HotelDbContext db) =>
        {
            var room = await db.Rooms.FindAsync(id);
            if (room is null) return Results.NotFound();

            db.Rooms.Remove(room);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly)
        .WithName("DeleteRoom")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
