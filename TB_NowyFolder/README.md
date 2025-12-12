# Hotel Reservation API

A complete hotel reservation management system built with ASP.NET Core 9.0, Entity Framework Core, and Minimal APIs.

## Features

- **Guest Management**: Create, read, update, and delete guest information
- **Room Type Management**: Manage different types of rooms (Single, Double, Suite)
- **Room Management**: Track individual rooms with availability status
- **Service Management**: Manage hotel services (Breakfast, Spa, etc.)
- **Reservation Management**: Create and manage reservations with rooms and services

## Database

The application uses SQL Server LocalDB with the following connection string:
```
Server=(localdb)\\mssqllocaldb;Database=HotelReservationDB;Trusted_Connection=True;MultipleActiveResultSets=true
```

### Initial Data

The database is seeded with sample data:
- 2 Guests (John Doe, Jane Smith)
- 3 Room Types (Single, Double, Suite)
- 5 Rooms (101, 102, 201, 202, 301)
- 4 Services (Breakfast, Room Service, Spa Treatment, Airport Transfer)

## Running the Application

1. Make sure SQL Server LocalDB is installed
2. Run the application:
   ```bash
   dotnet run
   ```
3. Navigate to: `https://localhost:5001/swagger` (or the HTTPS port shown in console)

## API Endpoints

### Guests (`/api/guests`)
- `GET /api/guests` - Get all guests
- `GET /api/guests/{id}` - Get guest by ID
- `POST /api/guests` - Create new guest
- `PUT /api/guests/{id}` - Update guest
- `DELETE /api/guests/{id}` - Delete guest

### Room Types (`/api/roomtypes`)
- `GET /api/roomtypes` - Get all room types
- `GET /api/roomtypes/{id}` - Get room type by ID
- `POST /api/roomtypes` - Create new room type
- `PUT /api/roomtypes/{id}` - Update room type
- `DELETE /api/roomtypes/{id}` - Delete room type

### Rooms (`/api/rooms`)
- `GET /api/rooms` - Get all rooms
- `GET /api/rooms/{id}` - Get room by ID
- `GET /api/rooms/available` - Get available rooms only
- `POST /api/rooms` - Create new room
- `PUT /api/rooms/{id}` - Update room
- `DELETE /api/rooms/{id}` - Delete room

### Services (`/api/services`)
- `GET /api/services` - Get all services
- `GET /api/services/{id}` - Get service by ID
- `GET /api/services/available` - Get available services only
- `POST /api/services` - Create new service
- `PUT /api/services/{id}` - Update service
- `DELETE /api/services/{id}` - Delete service

### Reservations (`/api/reservations`)
- `GET /api/reservations` - Get all reservations
- `GET /api/reservations/{id}` - Get reservation by ID
- `GET /api/reservations/guest/{guestId}` - Get reservations by guest
- `POST /api/reservations` - Create new reservation
- `PUT /api/reservations/{id}` - Update reservation
- `DELETE /api/reservations/{id}` - Delete reservation
- `POST /api/reservations/{reservationId}/rooms/{roomId}` - Add room to reservation
- `POST /api/reservations/{reservationId}/services/{serviceId}` - Add service to reservation

## Testing in Swagger

1. Start the application
2. Open Swagger UI at `/swagger`
3. Try these example operations:

### Example 1: Create a Reservation

1. **Create a new guest** (if needed):
   ```json
   POST /api/guests
   {
     "firstName": "Alice",
     "lastName": "Johnson",
     "email": "alice@example.com",
     "phone": "555-1234"
   }
   ```

2. **Create a reservation**:
   ```json
   POST /api/reservations
   {
     "guestID": 1,
     "checkInDate": "2025-01-15",
     "checkOutDate": "2025-01-20",
     "numberOfGuests": 2,
     "totalPrice": 750.00,
     "reservationStatus": "Confirmed"
   }
   ```

3. **Add a room to the reservation**:
   ```
   POST /api/reservations/1/rooms/1
   ```

4. **Add a service to the reservation**:
   ```json
   POST /api/reservations/1/services/1
   {
     "quantity": 2,
     "serviceDate": "2025-01-16"
   }
   ```

### Example 2: View Available Rooms

```
GET /api/rooms/available
```

### Example 3: View Guest's Reservations

```
GET /api/reservations/guest/1
```

## Database Migrations

To update the database schema:

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## Technologies Used

- ASP.NET Core 9.0
- Entity Framework Core 9.0
- SQL Server LocalDB
- Swagger/OpenAPI
- Minimal APIs

## Project Structure

```
TB_NowyFolder/
??? Data/
?   ??? HotelDbContext.cs       # Database context
??? Endpoints/
?   ??? GuestEndpoints.cs        # Guest API endpoints
?   ??? RoomTypeEndpoints.cs     # Room type API endpoints
?   ??? RoomEndpoints.cs         # Room API endpoints
?   ??? ServiceEndpoints.cs      # Service API endpoints
?   ??? ReservationEndpoints.cs  # Reservation API endpoints
??? Models/
?   ??? Guest.cs
?   ??? Room.cs
?   ??? RoomType.cs
?   ??? Service.cs
?   ??? Reservation.cs
?   ??? ReservationRoom.cs
?   ??? ReservationService.cs
??? Migrations/                  # EF Core migrations
??? Pages/                       # Razor Pages
??? appsettings.json            # Configuration
??? Program.cs                   # Application startup
```
