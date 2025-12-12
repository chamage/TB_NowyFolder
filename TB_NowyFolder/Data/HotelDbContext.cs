using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Models;

namespace TB_NowyFolder.Data;

public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
    {
    }

    public DbSet<Guest> Guests { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ReservationRoom> ReservationRooms { get; set; }
    public DbSet<ReservationService> ReservationServices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite keys
        modelBuilder.Entity<ReservationRoom>()
            .HasKey(rr => new { rr.ReservationID, rr.RoomID });

        modelBuilder.Entity<ReservationService>()
            .HasKey(rs => new { rs.ReservationID, rs.ServiceID });

        // Seed some initial data
        modelBuilder.Entity<RoomType>().HasData(
            new RoomType { RoomTypeID = 1, TypeName = "Single", Description = "Single room with one bed", Standard = "Standard" },
            new RoomType { RoomTypeID = 2, TypeName = "Double", Description = "Double room with two beds", Standard = "Standard" },
            new RoomType { RoomTypeID = 3, TypeName = "Suite", Description = "Luxury suite with living area", Standard = "Luxury" }
        );

        modelBuilder.Entity<Room>().HasData(
            new Room { RoomID = 1, RoomTypeID = 1, RoomNumber = "101", Capacity = 1, PricePerNight = 100m, Status = "Available" },
            new Room { RoomID = 2, RoomTypeID = 1, RoomNumber = "102", Capacity = 1, PricePerNight = 100m, Status = "Available" },
            new Room { RoomID = 3, RoomTypeID = 2, RoomNumber = "201", Capacity = 2, PricePerNight = 150m, Status = "Available" },
            new Room { RoomID = 4, RoomTypeID = 2, RoomNumber = "202", Capacity = 2, PricePerNight = 150m, Status = "Available" },
            new Room { RoomID = 5, RoomTypeID = 3, RoomNumber = "301", Capacity = 4, PricePerNight = 300m, Status = "Available" }
        );

        modelBuilder.Entity<Service>().HasData(
            new Service { ServiceID = 1, ServiceName = "Breakfast", Description = "Continental breakfast", UnitPrice = 15m, Availability = "Available" },
            new Service { ServiceID = 2, ServiceName = "Room Service", Description = "24/7 room service", UnitPrice = 25m, Availability = "Available" },
            new Service { ServiceID = 3, ServiceName = "Spa Treatment", Description = "Relaxing spa treatment", UnitPrice = 80m, Availability = "Available" },
            new Service { ServiceID = 4, ServiceName = "Airport Transfer", Description = "Transportation to/from airport", UnitPrice = 50m, Availability = "Available" }
        );

        modelBuilder.Entity<Guest>().HasData(
            new Guest { GuestID = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Phone = "123-456-7890" },
            new Guest { GuestID = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Phone = "098-765-4321" }
        );
    }
}
