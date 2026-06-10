using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;
using TB_NowyFolder.Security;


namespace TB_NowyFolder.Endpoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/services")
            .WithTags("Services");

        // Wyświetlanie listy usług
        group.MapGet("/", async (HotelDbContext db) =>
        {
            return await db.Services.ToListAsync();
        })
        .AllowAnonymous()
        .WithName("GetAllServices")
        .Produces<List<Service>>(StatusCodes.Status200OK);

        // Pobieranie usługi po ID
        group.MapGet("/{id}", async (int id, HotelDbContext db) =>
        {
            return await db.Services.FindAsync(id)
                is Service service
                    ? Results.Ok(service)
                    : Results.NotFound();
        })
        .AllowAnonymous()
        .WithName("GetServiceById")
        .Produces<Service>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Wyświetlanie dostępnych usług
        group.MapGet("/available", async (HotelDbContext db) =>
        {
            return await db.Services
                .Where(s => s.Availability == "Available")
                .ToListAsync();
        })
        .AllowAnonymous()
        .WithName("GetAvailableServices")
        .Produces<List<Service>>(StatusCodes.Status200OK);

        // Dodawanie usługi
        group.MapPost("/", async (Service service, HotelDbContext db) =>
        {
            db.Services.Add(service);
            await db.SaveChangesAsync();
            return Results.Created($"/api/services/{service.ServiceID}", service);
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly)
        .WithName("CreateService")
        .Produces<Service>(StatusCodes.Status201Created);

        // Edytowanie usługi
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
        .RequireAuthorization(AuthorizationPolicies.AdminOnly)
        .WithName("UpdateService")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // Usuwanie usługi
        group.MapDelete("/{id}", async (int id, HotelDbContext db) =>
        {
            var service = await db.Services.FindAsync(id);
            if (service is null) return Results.NotFound();

            db.Services.Remove(service);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly)
        .WithName("DeleteService")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
