using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;

namespace TB_NowyFolder.Endpoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/services")
            .WithTags("Services");

        // GET all services
        group.MapGet("/", async (HotelDbContext db) =>
        {
            return await db.Services.ToListAsync();
        })
        .WithName("GetAllServices")
        .Produces<List<Service>>(StatusCodes.Status200OK);

        // GET service by ID
        group.MapGet("/{id}", async (int id, HotelDbContext db) =>
        {
            return await db.Services.FindAsync(id)
                is Service service
                    ? Results.Ok(service)
                    : Results.NotFound();
        })
        .WithName("GetServiceById")
        .Produces<Service>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET available services
        group.MapGet("/available", async (HotelDbContext db) =>
        {
            return await db.Services
                .Where(s => s.Availability == "Available")
                .ToListAsync();
        })
        .WithName("GetAvailableServices")
        .Produces<List<Service>>(StatusCodes.Status200OK);

        // POST create service
        group.MapPost("/", async (Service service, HotelDbContext db) =>
        {
            db.Services.Add(service);
            await db.SaveChangesAsync();
            return Results.Created($"/api/services/{service.ServiceID}", service);
        })
        .WithName("CreateService")
        .Produces<Service>(StatusCodes.Status201Created);

        // PUT update service
        group.MapPut("/{id}", async (int id, Service inputService, HotelDbContext db) =>
        {
            var service = await db.Services.FindAsync(id);
            if (service is null) return Results.NotFound();

            service.ServiceName = inputService.ServiceName;
            service.Description = inputService.Description;
            service.UnitPrice = inputService.UnitPrice;
            service.Availability = inputService.Availability;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateService")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE service
        group.MapDelete("/{id}", async (int id, HotelDbContext db) =>
        {
            var service = await db.Services.FindAsync(id);
            if (service is null) return Results.NotFound();

            db.Services.Remove(service);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteService")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
