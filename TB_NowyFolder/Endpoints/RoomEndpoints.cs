using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;

namespace TB_NowyFolder.Endpoints;

public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms")
            .WithTags("Rooms");

        // GET all rooms
        group.MapGet("/", async (HotelDbContext db) =>
        {
            return await db.Rooms.Include(r => r.RoomType).ToListAsync();
        })
        .WithName("GetAllRooms")
        .Produces<List<Room>>(StatusCodes.Status200OK);

        // GET room by ID
        group.MapGet("/{id}", async (int id, HotelDbContext db) =>
        {
            var room = await db.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            return room is not null
                ? Results.Ok(room)
                : Results.NotFound();
        })
        .WithName("GetRoomById")
        .Produces<Room>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET available rooms
        group.MapGet("/available", async (HotelDbContext db) =>
        {
            return await db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "Available")
                .ToListAsync();
        })
        .WithName("GetAvailableRooms")
        .Produces<List<Room>>(StatusCodes.Status200OK);

        // POST create room
        group.MapPost("/", async (Room room, HotelDbContext db) =>
        {
            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return Results.Created($"/api/rooms/{room.RoomID}", room);
        })
        .WithName("CreateRoom")
        .Produces<Room>(StatusCodes.Status201Created);

        // PUT update room
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
        .WithName("UpdateRoom")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE room
        group.MapDelete("/{id}", async (int id, HotelDbContext db) =>
        {
            var room = await db.Rooms.FindAsync(id);
            if (room is null) return Results.NotFound();

            db.Rooms.Remove(room);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteRoom")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
