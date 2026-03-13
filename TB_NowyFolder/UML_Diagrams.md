# Diagramy UML — System Rezerwacji Hotelowej

Role w systemie:
1. **Niezalogowany Użytkownik** (Gość) — przeglądanie oferty
2. **Zalogowany Użytkownik** (Klient) — samodzielne rezerwacje i zarządzanie kontem
3. **Recepcjonista** (Obsługa klienta) — obsługa gości na miejscu, zameldowania, rezerwacje telefoniczne, fakturowanie
4. **Administrator** — pełne zarządzanie systemem (konfiguracja pokoi, usług, cenników)

---

## 1. Diagram przypadków użycia (Use Case)

```mermaid
graph LR
    subgraph System Rezerwacji Hotelowej
        UC1["Przeglądanie dostępnych pokoi"]
        UC2["Przeglądanie typów pokoi"]
        UC3["Przeglądanie usług hotelowych"]
        UC4["Rejestracja konta"]
        UC5["Logowanie"]
        UC6["Tworzenie rezerwacji"]
        UC7["Przeglądanie swoich rezerwacji"]
        UC8["Anulowanie rezerwacji"]
        UC9["Dodawanie pokoju do rezerwacji"]
        UC10["Dodawanie usługi do rezerwacji"]
        UC11["Edycja danych profilu"]
        UC12["Zarządzanie gośćmi (rejestracja na miejscu)"]
        UC13["Zarządzanie pokojami (status czystości/awarii)"]
        UC14["Zarządzanie typami pokoi"]
        UC15["Zarządzanie usługami"]
        UC16["Zarządzanie wszystkimi rezerwacjami"]
        UC17["Przeglądanie wszystkich rezerwacji"]
        UC18["Check-in (Zameldowanie)"]
        UC19["Check-out (Wymeldowanie / Rozliczenie)"]
        UC20["Dodawanie usług w trakcie pobytu"]
    end

    A1(("👤 Niezalogowany\nUżytkownik"))
    A2(("👤 Zalogowany\nUżytkownik"))
    A3(("👤 Recepcjonista"))
    A4(("👤 Administrator"))

    A1 --> UC1
    A1 --> UC2
    A1 --> UC3
    A1 --> UC4
    A1 --> UC5

    A2 --> UC1
    A2 --> UC2
    A2 --> UC3
    A2 --> UC6
    A2 --> UC7
    A2 --> UC8
    A2 --> UC9
    A2 --> UC10
    A2 --> UC11

    A3 --> UC12
    A3 --> UC16
    A3 --> UC17
    A3 --> UC18
    A3 --> UC19
    A3 --> UC20
    
    A4 --> UC12
    A4 --> UC13
    A4 --> UC14
    A4 --> UC15
    A4 --> UC16
    A4 --> UC17
```

---

## 2. Diagram klas (Class Diagram)

```mermaid
classDiagram
    class Guest {
        +int GuestID
        +string FirstName
        +string LastName
        +string Email
        +string Phone
        +string TaxID
        +string Notes
        +ICollection~Reservation~ Reservations
    }

    class Reservation {
        +int ReservationID
        +int GuestID
        +DateTime ReservationDate
        +DateOnly CheckInDate
        +DateOnly CheckOutDate
        +int NumberOfGuests
        +decimal TotalPrice
        +string ReservationStatus
        +Guest Guest
        +ICollection~ReservationRoom~ ReservationRooms
        +ICollection~ReservationService~ ReservationServices
    }

    class Room {
        +int RoomID
        +int RoomTypeID
        +string RoomNumber
        +int Capacity
        +decimal PricePerNight
        +string Status
        +RoomType RoomType
        +ICollection~ReservationRoom~ ReservationRooms
    }

    class RoomType {
        +int RoomTypeID
        +string TypeName
        +string Description
        +string Standard
        +ICollection~Room~ Rooms
    }

    class Service {
        +int ServiceID
        +string ServiceName
        +string Description
        +decimal UnitPrice
        +string Availability
        +ICollection~ReservationService~ ReservationServices
    }

    class ReservationRoom {
        +int ReservationID
        +int RoomID
        +decimal PricePerNight
        +Reservation Reservation
        +Room Room
    }

    class ReservationService {
        +int ReservationID
        +int ServiceID
        +int Quantity
        +DateOnly ServiceDate
        +Reservation Reservation
        +Service Service
    }

    Guest "1" --> "*" Reservation : składa
    Reservation "1" --> "*" ReservationRoom : zawiera
    Reservation "1" --> "*" ReservationService : korzysta z
    Room "1" --> "*" ReservationRoom : przypisany do
    RoomType "1" --> "*" Room : kategoryzuje
    Service "1" --> "*" ReservationService : dotyczy
```

---

## 3. Diagram aktywności — Przeglądanie oferty (Niezalogowany Użytkownik)

```mermaid
flowchart TD
    A([Start]) --> B[Odwiedzenie strony hotelu]
    B --> C{Wybór sekcji}
    C --> D[Przeglądanie dostępnych pokoi]
    C --> E[Przeglądanie typów pokoi]
    C --> F[Przeglądanie usług hotelowych]
    D --> G[Wyświetlenie listy wolnych pokoi z cenami]
    E --> H[Wyświetlenie kategorii pokoi z opisami]
    F --> I[Wyświetlenie dostępnych usług z cenami]
    G --> J{Chce zarezerwować?}
    J -- Tak --> K{Zalogowany?}
    K -- Nie --> L[Przekierowanie do logowania / rejestracji]
    K -- Tak --> M[Przejście do procesu rezerwacji]
    J -- Nie --> N([Koniec])
    H --> N
    I --> N
    L --> N
    M --> N
```

---

## 4. Diagram aktywności — Proces rezerwacji (Zalogowany Użytkownik)

```mermaid
flowchart TD
    A([Start]) --> B[Przeglądanie dostępnych pokoi]
    B --> C{Znaleziono odpowiedni pokój?}
    C -- Nie --> B
    C -- Tak --> D[Wybór daty zameldowania i wymeldowania]
    D --> E[Podanie liczby gości]
    E --> F[Tworzenie rezerwacji]
    F --> G[Dodanie pokoju do rezerwacji]
    G --> H{Dodać usługę?}
    H -- Tak --> I[Wybór usługi i ilości]
    I --> J[Dodanie usługi do rezerwacji]
    J --> H
    H -- Nie --> K[Potwierdzenie rezerwacji]
    K --> L([Koniec])
```

---

## 5. Diagram aktywności — Zarządzanie pokojami (Administrator)

```mermaid
flowchart TD
    A([Start]) --> B[Panel administracyjny]
    B --> C{Wybór operacji}
    C --> D[Dodanie nowego pokoju]
    C --> E[Edycja istniejącego pokoju]
    C --> F[Usunięcie pokoju]
    C --> G[Zarządzanie cennikiem]
    D --> H[Podanie numeru, typu, pojemności, ceny]
    H --> I[Zapis do bazy danych]
    E --> J[Wybór pokoju do edycji]
    J --> K[Modyfikacja danych parametrow głównych]
    K --> I
    F --> L[Wybór pokoju do usunięcia]
    L --> M{Pokój ma aktywne rezerwacje?}
    M -- Tak --> N[Odmowa usunięcia]
    M -- Nie --> O[Usunięcie z bazy]
    G --> P[Modyfikacja cen bazowych]
    P --> I
    I --> Q([Koniec])
    N --> Q
    O --> Q
```

---

## 6. Diagram aktywności — Obsługa klienta na miejscu (Recepcjonista)

```mermaid
flowchart TD
    A([Klient przybywa do hotelu]) --> B{Rodzaj obsługi?}
    
    B -- Nowa rezerwacja --> C[Sprawdzenie dostępności pokoi]
    C --> D{Są wolne miejsca?}
    D -- Nie --> E[Odmowa i przeprosiny]
    D -- Tak --> F[Rejestracja danych Gościa]
    F --> G[Utworzenie rezerwacji w systemie]
    G --> H[Check-in i Zameldowanie]
    
    B -- Klient ma rezerwację --> I[Wyszukanie rezerwacji w systemie]
    I --> H
    
    H --> J[Wydanie kluczy lub kart]
    J --> K([Koniec obsługi awansu])
    
    B -- Obsługa w trakcie pobytu --> L{Czego potrzebuje?}
    L -- Dodatkowa usługa --> M[Dodanie usługi do rachunku pokoju]
    L -- Zgłoszenie usterki --> N[Zmiana statusu pokoju i wezwanie serwisu]
    M --> O([Koniec])
    N --> O
    
    B -- Wymeldowanie --> P[Check-out i Rozliczenie]
    P --> Q[Wygenerowanie rachunku kosztów całkowitych]
    Q --> R[Przyjęcie płatności]
    R --> S[Zwolnienie pokoju]
    S --> T([Koniec wizyty])
    
    E --> U([Koniec])
```

---

## 7. Diagram sekwencji — Tworzenie rezerwacji (API)

```mermaid
sequenceDiagram
    actor Klient as Zalogowany Użytkownik
    participant API as API Server
    participant DB as Baza Danych

    Klient->>API: GET /api/rooms/available
    API->>DB: SELECT * FROM Rooms WHERE Status = 'Available'
    DB-->>API: Lista dostępnych pokoi
    API-->>Klient: 200 OK + JSON pokoi

    Klient->>API: POST /api/reservations
    Note right of Klient: guestID, checkIn, checkOut, guests, price
    API->>DB: INSERT INTO Reservations
    DB-->>API: Nowa rezerwacja (ID)
    API-->>Klient: 201 Created

    Klient->>API: POST /api/reservations/{id}/rooms/{roomId}
    API->>DB: INSERT INTO ReservationRooms
    DB-->>API: OK
    API-->>Klient: 200 OK

    Klient->>API: POST /api/reservations/{id}/services/{serviceId}
    Note right of Klient: quantity, serviceDate
    API->>DB: INSERT INTO ReservationServices
    DB-->>API: OK
    API-->>Klient: 200 OK
```

---

## 8. Diagram sekwencji — Zarządzanie zasobami (Administrator)

```mermaid
sequenceDiagram
    actor Admin as Administrator
    participant API as API Server
    participant DB as Baza Danych

    Admin->>API: GET /api/reservations
    API->>DB: SELECT * FROM Reservations (z JOIN)
    DB-->>API: Wszystkie rezerwacje
    API-->>Admin: 200 OK + JSON

    Admin->>API: PUT /api/reservations/{id}
    Note right of Admin: Zmiana statusu na "Cancelled"
    API->>DB: UPDATE Reservations SET Status
    DB-->>API: OK
    API-->>Admin: 200 OK

    Admin->>API: POST /api/rooms
    Note right of Admin: Nowy pokój (numer, typ, cena)
    API->>DB: INSERT INTO Rooms
    DB-->>API: Nowy pokój (ID)
    API-->>Admin: 201 Created

    Admin->>API: DELETE /api/guests/{id}
    API->>DB: DELETE FROM Guests WHERE GuestID = id
    DB-->>API: OK
    API-->>Admin: 200 OK
```

---

## 9. Diagram sekwencji — Check-in i doliczenie usługi (Recepcjonista)

```mermaid
sequenceDiagram
    actor Recep as Recepcjonista
    participant API as API Server
    participant DB as Baza Danych

    Recep->>API: GET /api/reservations?guestName=Kowalski
    API->>DB: SELECT * FROM Reservations JOIN Guests
    DB-->>API: Znaleziona rezerwacja
    API-->>Recep: 200 OK + JSON rezerwacji

    Note right of Recep: Zmiana statusu rezerwacji na "In Progress" / Pokoju na "Occupied"
    Recep->>API: PUT /api/reservations/{id}
    API->>DB: UPDATE Reservations SET Status = 'In Progress'
    DB-->>API: OK
    API-->>Recep: 200 OK

    Note right of Recep: Klient zamawia śniadanie rano
    Recep->>API: POST /api/reservations/{id}/services/{serviceId}
    Note right of Recep: quantity: 2, date: tomorrow
    API->>DB: INSERT INTO ReservationServices
    DB-->>API: OK
    API-->>Recep: 201 Created (Usługa doliczona do rachunku)
```
