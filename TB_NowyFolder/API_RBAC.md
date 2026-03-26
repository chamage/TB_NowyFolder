# REST API + RBAC — dokumentacja projektu

## 1. Główne zasoby API

- `guests`
- `reservations`
- `rooms`
- `roomtypes`
- `services`
- `auth` (wydawanie tokenów JWT)

## 2. Role (RBAC)

- `Client` — zalogowany klient hotelu
- `Receptionist` — recepcjonista
- `Administrator` — administrator systemu
- Gość niezalogowany (`Anonymous`) — tylko przegląd oferty

## 3. Endpointy REST API

### Auth
- `POST /api/auth/token` — logowanie i pobranie tokenu JWT

### Guests
- `GET /api/guests`
- `GET /api/guests/{id}`
- `POST /api/guests`
- `PUT /api/guests/{id}`
- `DELETE /api/guests/{id}`

### RoomTypes
- `GET /api/roomtypes`
- `GET /api/roomtypes/{id}`
- `POST /api/roomtypes`
- `PUT /api/roomtypes/{id}`
- `DELETE /api/roomtypes/{id}`

### Rooms
- `GET /api/rooms`
- `GET /api/rooms/{id}`
- `GET /api/rooms/available`
- `POST /api/rooms`
- `PUT /api/rooms/{id}`
- `DELETE /api/rooms/{id}`

### Services
- `GET /api/services`
- `GET /api/services/{id}`
- `GET /api/services/available`
- `POST /api/services`
- `PUT /api/services/{id}`
- `DELETE /api/services/{id}`

### Reservations
- `GET /api/reservations`
- `GET /api/reservations/{id}`
- `GET /api/reservations/guest/{guestId}`
- `POST /api/reservations`
- `PUT /api/reservations/{id}`
- `DELETE /api/reservations/{id}`
- `POST /api/reservations/{reservationId}/rooms/{roomId}`
- `POST /api/reservations/{reservationId}/services/{serviceId}`

## 4. Tabela ról i uprawnień

| Operacja API | Anonymous | Client | Receptionist | Administrator |
|---|---:|---:|---:|---:|
| `POST /api/auth/token` | ✅ | ✅ | ✅ | ✅ |
| `GET /api/rooms*` | ✅ | ✅ | ✅ | ✅ |
| `GET /api/roomtypes*` | ✅ | ✅ | ✅ | ✅ |
| `GET /api/services*` | ✅ | ✅ | ✅ | ✅ |
| `POST/PUT/DELETE /api/rooms*` | ❌ | ❌ | ❌ | ✅ |
| `POST/PUT/DELETE /api/roomtypes*` | ❌ | ❌ | ❌ | ✅ |
| `POST/PUT/DELETE /api/services*` | ❌ | ❌ | ❌ | ✅ |
| `GET/POST/PUT/DELETE /api/guests*` | ❌ | ❌ | ✅ | ✅ |
| `GET /api/reservations*` | ❌ | ❌ | ✅ | ✅ |
| `POST/PUT/DELETE /api/reservations*` | ❌ | ✅ | ✅ | ✅ |
| `POST /api/reservations/{id}/rooms/{roomId}` | ❌ | ✅ | ✅ | ✅ |
| `POST /api/reservations/{id}/services/{serviceId}` | ❌ | ✅ | ✅ | ✅ |

## 5. Krótki opis żądania i odpowiedzi

### `POST /api/auth/token`
- Request: `{ "username": "admin", "password": "admin123!" }`
- Response `200 OK`: `{ "accessToken": "...", "tokenType": "Bearer", "expiresAt": "...", "role": "Administrator" }`
- Response `401 Unauthorized`: błędne dane logowania

### Przykład endpointu CRUD: `POST /api/rooms`
- Wymaga roli `Administrator`
- Request: JSON obiektu `Room`
- Response `201 Created`: utworzony pokój

### Przykład endpointu odczytu publicznego: `GET /api/rooms/available`
- Dostęp bez logowania
- Response `200 OK`: lista dostępnych pokoi

### Przykład operacji rezerwacji: `POST /api/reservations`
- Wymaga roli `Client`, `Receptionist` lub `Administrator`
- Request: JSON obiektu `Reservation`
- Response `201 Created`: utworzona rezerwacja

## 6. Konta testowe (demo)

- `admin / admin123!` → `Administrator`
- `reception / reception123!` → `Receptionist`
- `client / client123!` → `Client`

Token należy przekazać w nagłówku:
`Authorization: Bearer <token>`
