# Dokumentacja REST API – System Hotelowy

## Autorzy

 - Piotr Skocz, 
 - Pawel Lejkowski

## Spis treści

1. [Uwierzytelnianie (Auth)](#1-uwierzytelnianie-auth)
2. [Goście (Guests)](#2-goście-guests)
3. [Pokoje (Rooms)](#3-pokoje-rooms)
4. [Typy pokojów (RoomTypes)](#4-typy-pokojów-roomtypes)
5. [Usługi (Services)](#5-usługi-services)
6. [Rezerwacje (Reservations)](#6-rezerwacje-reservations)
7. [Role i uprawnienia](#7-role-i-uprawnienia)
8. [Modele danych](#8-modele-danych)

---

## 1. Uwierzytelnianie (Auth)

Bazowy URL: `/api/auth`

| Metoda | Ścieżka | Nazwa | Autoryzacja | Opis |
|--------|---------|-------|-------------|------|
| `POST` | `/api/auth/token` | CreateAccessToken | Brak (anonimowy) | Logowanie – zwraca token JWT |

### `POST /api/auth/token`

**Żądanie (body JSON):**

```json
{
  "username": "string",
  "password": "string"
}
```

**Odpowiedź `200 OK`:**

```json
{
  "accessToken": "eyJhbGci...",
  "tokenType": "Bearer",
  "expiresAt": "2026-03-27T16:10:00Z",
  "role": "Administrator",
  "username": "admin"
}
```

**Odpowiedź `401 Unauthorized`** – nieprawidłowe dane logowania.

**Konta demo:**

| Login | Hasło | Rola | GuestId |
|-------|-------|------|---------|
| `admin` | `admin123!` | Administrator | – |
| `reception` | `reception123!` | Receptionist | – |
| `client` | `client123!` | Client | 1 |

---

## 2. Goście (Guests)

Bazowy URL: `/api/guests`  
Polityka autoryzacji dla całej grupy: **GuestManagement** (Receptionist, Administrator)

| Metoda | Ścieżka | Nazwa | Opis |
|--------|---------|-------|------|
| `GET` | `/api/guests` | GetAllGuests | Pobranie listy wszystkich gości |
| `GET` | `/api/guests/{id}` | GetGuestById | Pobranie gościa po ID |
| `POST` | `/api/guests` | CreateGuest | Utworzenie nowego gościa |
| `PUT` | `/api/guests/{id}` | UpdateGuest | Aktualizacja danych gościa |
| `DELETE` | `/api/guests/{id}` | DeleteGuest | Usunięcie gościa |

### `GET /api/guests`

- **Odpowiedź `200 OK`:** `List<Guest>`

### `GET /api/guests/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `200 OK`:** `Guest`
- **Odpowiedź `404 Not Found`:** gość nie został znaleziony

### `POST /api/guests`

- **Żądanie (body JSON):** obiekt `Guest`
- **Odpowiedź `201 Created`:** utworzony obiekt `Guest`

### `PUT /api/guests/{id}`

- **Parametr ścieżki:** `id` (int)
- **Żądanie (body JSON):** obiekt `Guest`
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** gość nie został znaleziony

### `DELETE /api/guests/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** gość nie został znaleziony

---

## 3. Pokoje (Rooms)

Bazowy URL: `/api/rooms`

| Metoda | Ścieżka | Nazwa | Autoryzacja | Opis |
|--------|---------|-------|-------------|------|
| `GET` | `/api/rooms` | GetAllRooms | Anonimowy | Pobranie listy wszystkich pokojów |
| `GET` | `/api/rooms/{id}` | GetRoomById | Anonimowy | Pobranie pokoju po ID |
| `GET` | `/api/rooms/available` | GetAvailableRooms | Anonimowy | Pobranie dostępnych pokojów |
| `POST` | `/api/rooms` | CreateRoom | RoomManagement | Utworzenie nowego pokoju |
| `PUT` | `/api/rooms/{id}` | UpdateRoom | RoomManagement | Aktualizacja pokoju |
| `DELETE` | `/api/rooms/{id}` | DeleteRoom | RoomManagement | Usunięcie pokoju |

### `GET /api/rooms`

- **Odpowiedź `200 OK`:** `List<Room>`

### `GET /api/rooms/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `200 OK`:** `Room`
- **Odpowiedź `404 Not Found`:** pokój nie został znaleziony

### `GET /api/rooms/available`

- **Odpowiedź `200 OK`:** `List<Room>` (filtr: `Status == "Available"`)

### `POST /api/rooms`

- **Żądanie (body JSON):** obiekt `Room`
- **Odpowiedź `201 Created`:** utworzony obiekt `Room`

### `PUT /api/rooms/{id}`

- **Parametr ścieżki:** `id` (int)
- **Żądanie (body JSON):** obiekt `Room`
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** pokój nie został znaleziony

### `DELETE /api/rooms/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** pokój nie został znaleziony

---

## 4. Typy pokojów (RoomTypes)

Bazowy URL: `/api/roomtypes`

| Metoda | Ścieżka | Nazwa | Autoryzacja | Opis |
|--------|---------|-------|-------------|------|
| `GET` | `/api/roomtypes` | GetAllRoomTypes | Anonimowy | Pobranie listy typów pokojów |
| `GET` | `/api/roomtypes/{id}` | GetRoomTypeById | Anonimowy | Pobranie typu pokoju po ID |
| `POST` | `/api/roomtypes` | CreateRoomType | RoomTypeManagement | Utworzenie nowego typu pokoju |
| `PUT` | `/api/roomtypes/{id}` | UpdateRoomType | RoomTypeManagement | Aktualizacja typu pokoju |
| `DELETE` | `/api/roomtypes/{id}` | DeleteRoomType | RoomTypeManagement | Usunięcie typu pokoju |

### `GET /api/roomtypes`

- **Odpowiedź `200 OK`:** `List<RoomType>`

### `GET /api/roomtypes/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `200 OK`:** `RoomType`
- **Odpowiedź `404 Not Found`:** typ pokoju nie został znaleziony

### `POST /api/roomtypes`

- **Żądanie (body JSON):** obiekt `RoomType`
- **Odpowiedź `201 Created`:** utworzony obiekt `RoomType`

### `PUT /api/roomtypes/{id}`

- **Parametr ścieżki:** `id` (int)
- **Żądanie (body JSON):** obiekt `RoomType`
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** typ pokoju nie został znaleziony

### `DELETE /api/roomtypes/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** typ pokoju nie został znaleziony

---

## 5. Usługi (Services)

Bazowy URL: `/api/services`

| Metoda | Ścieżka | Nazwa | Autoryzacja | Opis |
|--------|---------|-------|-------------|------|
| `GET` | `/api/services` | GetAllServices | Anonimowy | Pobranie listy wszystkich usług |
| `GET` | `/api/services/{id}` | GetServiceById | Anonimowy | Pobranie usługi po ID |
| `GET` | `/api/services/available` | GetAvailableServices | Anonimowy | Pobranie dostępnych usług |
| `POST` | `/api/services` | CreateService | ServiceManagement | Utworzenie nowej usługi |
| `PUT` | `/api/services/{id}` | UpdateService | ServiceManagement | Aktualizacja usługi |
| `DELETE` | `/api/services/{id}` | DeleteService | ServiceManagement | Usunięcie usługi |

### `GET /api/services`

- **Odpowiedź `200 OK`:** `List<Service>`

### `GET /api/services/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `200 OK`:** `Service`
- **Odpowiedź `404 Not Found`:** usługa nie została znaleziona

### `GET /api/services/available`

- **Odpowiedź `200 OK`:** `List<Service>` (filtr: `Availability == "Available"`)

### `POST /api/services`

- **Żądanie (body JSON):** obiekt `Service`
- **Odpowiedź `201 Created`:** utworzony obiekt `Service`

### `PUT /api/services/{id}`

- **Parametr ścieżki:** `id` (int)
- **Żądanie (body JSON):** obiekt `Service`
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** usługa nie została znaleziona

### `DELETE /api/services/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** usługa nie została znaleziona

---

## 6. Rezerwacje (Reservations)

Bazowy URL: `/api/reservations`

| Metoda | Ścieżka | Nazwa | Autoryzacja | Opis |
|--------|---------|-------|-------------|------|
| `GET` | `/api/reservations` | GetAllReservations | ReservationRead | Pobranie wszystkich rezerwacji |
| `GET` | `/api/reservations/my` | GetMyReservations | ReservationCreateOrUpdate | Pobranie rezerwacji zalogowanego klienta |
| `GET` | `/api/reservations/{id}` | GetReservationById | ReservationRead | Pobranie rezerwacji po ID |
| `GET` | `/api/reservations/guest/{guestId}` | GetReservationsByGuest | ReservationRead | Pobranie rezerwacji danego gościa |
| `POST` | `/api/reservations` | CreateReservation | ReservationCreateOrUpdate | Utworzenie nowej rezerwacji |
| `PUT` | `/api/reservations/{id}` | UpdateReservation | ReservationCreateOrUpdate | Aktualizacja rezerwacji |
| `DELETE` | `/api/reservations/{id}` | DeleteReservation | ReservationCreateOrUpdate | Usunięcie rezerwacji |
| `POST` | `…/{reservationId}/rooms/{roomId}` | AddRoomToReservation | ReservationCreateOrUpdate | Dodanie pokoju do rezerwacji |
| `POST` | `…/{reservationId}/services/{serviceId}` | AddServiceToReservation | ReservationCreateOrUpdate | Dodanie usługi do rezerwacji |

### `GET /api/reservations`

- **Odpowiedź `200 OK`:** `List<Reservation>` (z relacjami: Guest, ReservationRooms→Room, ReservationServices→Service)

### `GET /api/reservations/my`

- **Opis:** Pobiera rezerwacje powiązane z zalogowanym klientem na podstawie claimu `guestId` z tokenu JWT.
- **Odpowiedź `200 OK`:** `List<Reservation>`
- **Odpowiedź `403 Forbidden`:** brak claimu `guestId` w tokenie

### `GET /api/reservations/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `200 OK`:** `Reservation` (z relacjami)
- **Odpowiedź `404 Not Found`:** rezerwacja nie została znaleziona

### `GET /api/reservations/guest/{guestId}`

- **Parametr ścieżki:** `guestId` (int)
- **Odpowiedź `200 OK`:** `List<Reservation>`

### `POST /api/reservations`

- **Opis:** Tworzy nową rezerwację. Dla klienta automatycznie ustawia `GuestID` na podstawie claimu `guestId`.
- **Żądanie (body JSON):** obiekt `Reservation`
- **Odpowiedź `201 Created`:** utworzony obiekt `Reservation`
- **Odpowiedź `403 Forbidden`:** klient nie ma claimu `guestId`

### `PUT /api/reservations/{id}`

- **Parametr ścieżki:** `id` (int)
- **Żądanie (body JSON):** obiekt `Reservation` (pola: `reservationStatus`, `reservationDate`)
- **Odpowiedź `204 No Content`:** sukces
- **Odpowiedź `404 Not Found`:** rezerwacja nie została znaleziona

### `DELETE /api/reservations/{id}`

- **Parametr ścieżki:** `id` (int)
- **Odpowiedź `204 No Content`:** sukces (usuwa kaskadowo powiązane ReservationRooms i ReservationServices)
- **Odpowiedź `404 Not Found`:** rezerwacja nie została znaleziona

### `POST /api/reservations/{reservationId}/rooms/{roomId}`

- **Parametry ścieżki:** `reservationId` (int), `roomId` (int)
- **Opis:** Dodaje pokój do istniejącej rezerwacji. Cena za noc jest kopiowana z pokoju.
- **Odpowiedź `201 Created`:** obiekt `ReservationRoom`
- **Odpowiedź `404 Not Found`:** rezerwacja lub pokój nie istnieje

### `POST /api/reservations/{reservationId}/services/{serviceId}`

- **Parametry ścieżki:** `reservationId` (int), `serviceId` (int)
- **Żądanie (body JSON):** obiekt `ReservationService` (pola: `quantity`, `serviceDate`)
- **Opis:** Dodaje usługę do rezerwacji. Jeśli usługa jest już dodana – aktualizuje ilość i datę.
- **Odpowiedź `201 Created`:** nowy obiekt `ReservationService`
- **Odpowiedź `200 OK`:** zaktualizowany obiekt `ReservationService` (jeśli już istniał)
- **Odpowiedź `404 Not Found`:** rezerwacja lub usługa nie istnieje

---

## 7. Role i uprawnienia

### Role aplikacji

| Rola | Opis |
|------|------|
| **Client** | Klient hotelowy – może zarządzać własnymi rezerwacjami |
| **Receptionist** | Recepcjonista – zarządza gośćmi i przegląda rezerwacje |
| **Administrator** | Administrator – pełny dostęp do wszystkich zasobów |

### Polityki autoryzacji

| Polityka | Dozwolone role | Opis |
|----------|----------------|------|
| GuestManagement | Receptionist, Administrator | Zarządzanie danymi gości (CRUD) |
| RoomManagement | Administrator | Zarządzanie pokojami (tworzenie, edycja, usuwanie) |
| RoomTypeManagement | Administrator | Zarządzanie typami pokojów (tworzenie, edycja, usuwanie) |
| ServiceManagement | Administrator | Zarządzanie usługami (tworzenie, edycja, usuwanie) |
| ReservationRead | Receptionist, Administrator | Odczyt wszystkich rezerwacji |
| ReservationCreateOrUpdate | Client, Receptionist, Administrator | Tworzenie i modyfikacja rezerwacji |

### Macierz uprawnień: Role × Operacje

| Operacja | Anonim | Client | Receptionist | Administrator |
|----------|:------:|:------:|:------------:|:-------------:|
| **Auth** – POST /token | ✅ | ✅ | ✅ | ✅ |
| **Guests** – GET (lista, szczegóły) | ❌ | ❌ | ✅ | ✅ |
| **Guests** – POST / PUT / DELETE | ❌ | ❌ | ✅ | ✅ |
| **Rooms** – GET (lista, szczegóły, dostępne) | ✅ | ✅ | ✅ | ✅ |
| **Rooms** – POST / PUT / DELETE | ❌ | ❌ | ❌ | ✅ |
| **RoomTypes** – GET (lista, szczegóły) | ✅ | ✅ | ✅ | ✅ |
| **RoomTypes** – POST / PUT / DELETE | ❌ | ❌ | ❌ | ✅ |
| **Services** – GET (lista, szczegóły, dostępne) | ✅ | ✅ | ✅ | ✅ |
| **Services** – POST / PUT / DELETE | ❌ | ❌ | ❌ | ✅ |
| **Reservations** – GET (wszystkie, po ID, po gościu) | ❌ | ❌ | ✅ | ✅ |
| **Reservations** – GET /my | ❌ | ✅ | ✅ | ✅ |
| **Reservations** – POST / PUT / DELETE | ❌ | ✅ | ✅ | ✅ |
| **Reservations** – dodanie pokoju/usługi | ❌ | ✅ | ✅ | ✅ |

---

## 8. Modele danych

### Guest

| Pole | Typ | Wymagane | Opis |
|------|-----|:--------:|------|
| `guestID` | int | auto | Klucz główny |
| `firstName` | string (max 100) | ✅ | Imię gościa |
| `lastName` | string (max 100) | ✅ | Nazwisko gościa |
| `email` | string (max 255) | ✅ | Adres e-mail |
| `phone` | string (max 20) | ❌ | Numer telefonu |
| `taxID` | string (max 20) | ❌ | NIP |
| `notes` | string (max 500) | ❌ | Uwagi |

### Room

| Pole | Typ | Wymagane | Opis |
|------|-----|:--------:|------|
| `roomID` | int | auto | Klucz główny |
| `roomTypeID` | int | ✅ | Klucz obcy do RoomType |
| `roomNumber` | string | ✅ | Numer pokoju |
| `capacity` | int | ✅ | Pojemność (liczba osób) |
| `pricePerNight` | decimal | ✅ | Cena za noc |
| `status` | string | ✅ | Status pokoju (domyślnie: `"Available"`) |

### RoomType

| Pole | Typ | Wymagane | Opis |
|------|-----|:--------:|------|
| `roomTypeID` | int | auto | Klucz główny |
| `typeName` | string | ✅ | Nazwa typu pokoju |
| `description` | string | ❌ | Opis typu pokoju |
| `standard` | string | ❌ | Standard pokoju |

### Service

| Pole | Typ | Wymagane | Opis |
|------|-----|:--------:|------|
| `serviceID` | int | auto | Klucz główny |
| `serviceName` | string | ✅ | Nazwa usługi |
| `description` | string | ❌ | Opis usługi |
| `unitPrice` | decimal | ✅ | Cena jednostkowa |
| `availability` | string | ✅ | Dostępność (domyślnie: `"Available"`) |

### Reservation

| Pole | Typ | Wymagane | Opis |
|------|-----|:--------:|------|
| `reservationID` | int | auto | Klucz główny |
| `guestID` | int | ✅ | Klucz obcy do Guest |
| `reservationDate` | DateTime | auto | Data utworzenia rezerwacji |
| `checkInDate` | DateOnly | ✅ | Data zameldowania |
| `checkOutDate` | DateOnly | ✅ | Data wymeldowania |
| `numberOfGuests` | int | ✅ | Liczba gości |
| `totalPrice` | decimal | ✅ | Cena całkowita |
| `reservationStatus` | string | ✅ | Status rezerwacji (domyślnie: `"Confirmed"`) |

### ReservationRoom (tabela łącząca)

| Pole | Typ | Wymagane | Opis |
|------|-----|:--------:|------|
| `reservationID` | int | ✅ | Klucz obcy do Reservation |
| `roomID` | int | ✅ | Klucz obcy do Room |
| `pricePerNight` | decimal | ✅ | Cena za noc (kopiowana z pokoju) |

### ReservationService (tabela łącząca)

| Pole | Typ | Wymagane | Opis |
|------|-----|:--------:|------|
| `reservationID` | int | ✅ | Klucz obcy do Reservation |
| `serviceID` | int | ✅ | Klucz obcy do Service |
| `quantity` | int | ✅ | Ilość |
| `serviceDate` | DateOnly | ✅ | Data wykonania usługi |
