# Hotel Reservation API

Hotel reservation management system built with ASP.NET Core 9.0, Minimal APIs, Entity Framework Core, and JWT.

## Tech Stack

- ASP.NET Core 9.0 (Minimal API + Razor Pages)
- Entity Framework Core 9.0 + SQL Server / LocalDB
- JWT Bearer authentication
- RBAC — Role-Based Access Control (Administrator, Receptionist, Client)
- RSA-SHA256 digital signatures for document generation
- Swagger / OpenAPI

## Authorization

All protected endpoints require a JWT token obtained from `POST /api/auth/token`.

Three roles with different access levels:

| Role | Access |
|---|---|
| `Administrator` | Full access to all endpoints |
| `Receptionist` | Reservations and guests — no access to dictionary management (rooms, room types, services) |
| `Client` | Own reservations only |

Demo accounts:

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin123!` | Administrator |
| `reception` | `Reception123!` | Receptionist |
| `client` | `Client123!` | Client |

## Running the Application

For environment setup and database configuration see [SETUP.md](SETUP.md).

```bash
dotnet run --project TB_NowyFolder
```

Swagger UI: `https://localhost:7029/swagger`

## API Reference

### Auth (`/api/auth`) — public

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/token` | Login — returns a JWT token |
| `POST` | `/api/auth/register` | Register new client account |
| `GET` | `/api/auth/me` | Current user info (requires token) |

### Guests (`/api/guests`) — Staff/Admin only

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/guests` | List all guests |
| `GET` | `/api/guests/{id}` | Get guest by ID |
| `POST` | `/api/guests` | Create guest |
| `PUT` | `/api/guests/{id}` | Update guest |
| `DELETE` | `/api/guests/{id}` | Delete guest |

### Room Types (`/api/roomtypes`) — read: public, write: Admin only

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/roomtypes` | List all room types |
| `GET` | `/api/roomtypes/{id}` | Get room type by ID |
| `POST` | `/api/roomtypes` | Create room type |
| `PUT` | `/api/roomtypes/{id}` | Update room type |
| `DELETE` | `/api/roomtypes/{id}` | Delete room type |

### Rooms (`/api/rooms`) — read: public, write: Admin only

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/rooms` | List all rooms |
| `GET` | `/api/rooms/{id}` | Get room by ID |
| `GET` | `/api/rooms/available` | List available rooms only |
| `POST` | `/api/rooms` | Create room |
| `PUT` | `/api/rooms/{id}` | Update room |
| `DELETE` | `/api/rooms/{id}` | Delete room |

### Services (`/api/services`) — read: public, write: Admin only

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/services` | List all services |
| `GET` | `/api/services/{id}` | Get service by ID |
| `GET` | `/api/services/available` | List available services only |
| `POST` | `/api/services` | Create service |
| `PUT` | `/api/services/{id}` | Update service |
| `DELETE` | `/api/services/{id}` | Delete service |

### Reservations (`/api/reservations`) — requires JWT token

| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/reservations` | Staff/Admin | List all reservations |
| `GET` | `/api/reservations/my` | Authenticated | Client's own reservations |
| `GET` | `/api/reservations/{id}` | Authenticated | Reservation details (clients see own only) |
| `GET` | `/api/reservations/guest/{guestId}` | Staff/Admin | Reservations by guest |
| `POST` | `/api/reservations` | Authenticated | Create reservation |
| `PUT` | `/api/reservations/{id}` | Staff/Admin | Update reservation |
| `DELETE` | `/api/reservations/{id}` | Authenticated | Delete reservation (clients delete own only) |
| `POST` | `/api/reservations/{id}/rooms/{roomId}` | Staff/Admin | Add room to reservation |
| `POST` | `/api/reservations/{id}/services/{serviceId}` | Staff/Admin | Add service to reservation |

### Documents (`/api/documents`) — Admin only

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/documents/generate/{reservationId}` | Generate digitally signed booking receipt (RSA-SHA256) |
| `POST` | `/api/documents/verify` | Verify document signature — returns `{ IsValid: true/false }` |

> **Note:** The RSA key is generated in memory at application startup. After a restart, signatures from previous sessions cannot be verified.

## Seed Data

The database is populated via EF Core migrations:

- 3 room types: Single, Double, Suite
- 5 rooms: 101, 102, 201, 202, 301
- 4 services: Breakfast, Room Service, Spa Treatment, Airport Transfer
- 2 demo guests: John Doe, Jane Smith
- 3 user accounts (passwords stored as PBKDF2 hashes — see demo accounts above)

## Project Structure

```
TB_NowyFolder/
├── Data/
│   └── HotelDbContext.cs        # EF Core context, model configuration, seed data
├── Endpoints/
│   ├── AuthEndpoints.cs         # Login, register, /me
│   ├── GuestEndpoints.cs        # Guest CRUD
│   ├── RoomTypeEndpoints.cs     # Room type CRUD
│   ├── RoomEndpoints.cs         # Room CRUD
│   ├── ServiceEndpoints.cs      # Service CRUD
│   ├── ReservationEndpoints.cs  # Reservation CRUD + sub-endpoints
│   └── DocumentEndpoints.cs     # Digital document generation and verification
├── Models/
│   ├── Guest.cs
│   ├── Room.cs
│   ├── RoomType.cs
│   ├── Service.cs
│   ├── Reservation.cs
│   ├── ReservationRoom.cs       # Join table with composite key
│   ├── ReservationService.cs    # Join table with composite key
│   ├── User.cs
│   └── VerifyRequest.cs         # Request model for document verification
├── Security/
│   ├── ApplicationRoles.cs      # Role name constants
│   ├── AuthorizationPolicies.cs # RBAC policy registration
│   └── DigitalSignatureService.cs  # RSA-SHA256 sign and verify
├── Migrations/                  # EF Core migrations
├── Pages/                       # Razor Pages (frontend)
├── Documentation/               # Project documentation
├── appsettings.json             # Configuration template (no secrets, committed to repo)
├── appsettings.Local.json       # Local secrets — NOT in repo (.gitignore)
└── Program.cs                   # Application startup and DI configuration
```

## EF Core Migrations

```bash
# Add migration after model changes
dotnet ef migrations add MigrationName --project TB_NowyFolder --startup-project TB_NowyFolder

# Apply migrations to database
dotnet ef database update --project TB_NowyFolder --startup-project TB_NowyFolder

# Remove last migration (if not yet applied)
dotnet ef migrations remove --project TB_NowyFolder --startup-project TB_NowyFolder
```
