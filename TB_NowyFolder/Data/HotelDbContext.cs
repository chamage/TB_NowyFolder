using Microsoft.EntityFrameworkCore;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;

namespace TB_NowyFolder.Data;

public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
    {
    }

    // Tabela baz danych
    public DbSet<Guest> Guests { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ReservationRoom> ReservationRooms { get; set; }
    public DbSet<ReservationService> ReservationServices { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ustawienie kluczy złożonych - EF Core nie wywnioskuje ich automatycznie.
        modelBuilder.Entity<ReservationRoom>()
            .HasKey(rr => new { rr.ReservationID, rr.RoomID });

        modelBuilder.Entity<ReservationService>()
            .HasKey(rs => new { rs.ReservationID, rs.ServiceID, rs.ServiceDate });

        // Unikalny indeks na Username - baza odrzuci duplikat przy próbie rejestracji.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Dane seedowe - typy pokoi, pokoje, usługi i goście demo wgrywane przez migracje EF Core.
        modelBuilder.Entity<RoomType>().HasData(
            new RoomType { RoomTypeID = 1, TypeName = "Single", Description = "Single room with one bed", Standard = "Standard" },
            new RoomType { RoomTypeID = 2, TypeName = "Double", Description = "Double room with two beds", Standard = "Standard" },
            new RoomType { RoomTypeID = 3, TypeName = "Suite", Description = "Luxury suite with living area", Standard = "Luxury" }
        );

        // Pokoje demo
        modelBuilder.Entity<Room>().HasData(
            new Room { RoomID = 1, RoomTypeID = 1, RoomNumber = "101", Capacity = 1, PricePerNight = 100m, Status = "Available" },
            new Room { RoomID = 2, RoomTypeID = 1, RoomNumber = "102", Capacity = 1, PricePerNight = 100m, Status = "Available" },
            new Room { RoomID = 3, RoomTypeID = 2, RoomNumber = "201", Capacity = 2, PricePerNight = 150m, Status = "Available" },
            new Room { RoomID = 4, RoomTypeID = 2, RoomNumber = "202", Capacity = 2, PricePerNight = 150m, Status = "Available" },
            new Room { RoomID = 5, RoomTypeID = 3, RoomNumber = "301", Capacity = 4, PricePerNight = 300m, Status = "Available" }
        );

        // Usługi demo
        modelBuilder.Entity<Service>().HasData(
            new Service { ServiceID = 1, ServiceName = "Breakfast", Description = "Continental breakfast", UnitPrice = 15m, Availability = "Available" },
            new Service { ServiceID = 2, ServiceName = "Room Service", Description = "24/7 room service", UnitPrice = 25m, Availability = "Available" },
            new Service { ServiceID = 3, ServiceName = "Spa Treatment", Description = "Relaxing spa treatment", UnitPrice = 80m, Availability = "Available" },
            new Service { ServiceID = 4, ServiceName = "Airport Transfer", Description = "Transportation to/from airport", UnitPrice = 50m, Availability = "Available" }
        );

        // Goście demo
        modelBuilder.Entity<Guest>().HasData(
            new Guest { GuestID = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Phone = "123-456-7890" },
            new Guest { GuestID = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Phone = "098-765-4321" }
        );

        // Konta testowe - hasła jako hasze PBKDF2, nie w postaci jawnej.
        // HasData() nie obsługuje IConfiguration, więc hasze są na stałe w kodzie.
        // W produkcji ten seed należy usunąć.
        //
        //   admin      / hasło: admin123!
        //   reception  / hasło: reception123!
        //   client     / hasło: client123!
        modelBuilder.Entity<User>().HasData(
            new User { UserID = 1, Username = "admin", PasswordHash = "AQAAAAIAAYagAAAAEDIxCxLk7cO67wzbcIxZEhSNWwO3N7OB3apVA/gpSSaDEx9E2cO0kFL8kaMZmlw3qA==", Role = Security.ApplicationRoles.Administrator },
            new User { UserID = 2, Username = "reception", PasswordHash = "AQAAAAIAAYagAAAAEML12Nj+jhhywZ/TBEuyFOCAoQWcbiIiZXnp8fkBYkYBdViiElzI/uHC6vI3OqpAHA==", Role = Security.ApplicationRoles.Receptionist },
            new User { UserID = 3, Username = "client", PasswordHash = "AQAAAAIAAYagAAAAEIXuk4hfcIORPrlAC3EANB5kTeEiXf/QpfoTuRSCfUVNFqzvGgXCYsc8gzDjMyKiPg==", Role = Security.ApplicationRoles.Client, GuestID = 1 }
        );
    }
}
