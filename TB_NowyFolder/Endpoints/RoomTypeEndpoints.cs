using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;

namespace TB_NowyFolder.Endpoints;

public static class RoomTypeEndpoints
{
    public static void MapRoomTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roomtypes")
            .WithTags("Room Types");

        // GET all room types
        group.MapGet("/", async (HotelDbContext db) =>
        {
            return await db.RoomTypes.ToListAsync();
        })
        .WithName("GetAllRoomTypes")
        .Produces<List<RoomType>>(StatusCodes.Status200OK);

        // GET room type by ID
        group.MapGet("/{id}", async (int id, HotelDbContext db) =>
        {
            return await db.RoomTypes.FindAsync(id)
                is RoomType roomType
                    ? Results.Ok(roomType)
                    : Results.NotFound();
        })
        .WithName("GetRoomTypeById")
        .Produces<RoomType>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST create room type
        group.MapPost("/", async (RoomType roomType, HotelDbContext db) =>
        {
            db.RoomTypes.Add(roomType);
            await db.SaveChangesAsync();
            return Results.Created($"/api/roomtypes/{roomType.RoomTypeID}", roomType);
        })
        .WithName("CreateRoomType")
        .Produces<RoomType>(StatusCodes.Status201Created);

        // PUT update room type
        group.MapPut("/{id}", async (int id, RoomType inputRoomType, HotelDbContext db) =>
        {
            var roomType = await db.RoomTypes.FindAsync(id);
            if (roomType is null) return Results.NotFound();

            roomType.TypeName = inputRoomType.TypeName;
            roomType.Description = inputRoomType.Description;
            roomType.Standard = inputRoomType.Standard;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateRoomType")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE room type
        group.MapDelete("/{id}", async (int id, HotelDbContext db) =>
        {
            var roomType = await db.RoomTypes.FindAsync(id);
            if (roomType is null) return Results.NotFound();

            db.RoomTypes.Remove(roomType);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteRoomType")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
