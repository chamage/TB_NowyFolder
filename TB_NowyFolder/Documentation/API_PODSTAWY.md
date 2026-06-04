# Dokumentacja Wstępnej Wersji REST API

## 1. Cel i charakter zadania
Zadanie polegało na zaprojektowaniu i utworzeniu **wstępnej wersji backendu** udostępniającego operacje CRUD za pomocą tzw. punktów dostępu (endpointów).

Głównym sposobem komunikacji z API i demonstracji działania na tym etapie jest interfejs **Swagger**, dający szybki wgląd we wszystkie utworzone ścieżki (routingi) i umożliwiający ręczne wstrzykiwanie danych (np. rezerwacji czy nowych gości).

---

## 2. Główne zasoby API (Endpointy)

Utworzone API zarządza pięcioma głównymi "bytami" (zasobami) związanymi z działaniem agroturystyki:

1. **Guests** (Goście u których przechowywane są chociażby dane kontaktowe)
2. **Rooms** (Konkretne pokoje)
3. **RoomTypes** (Słownik dostępnych konfiguracji, np. "Pokój dwuosobowy", "Apartament")
4. **Services** (Usługi dodatkowe przypinane w locie)
5. **Reservations** (Systemowy trzon spinający gości, usługi i pokoje w odpowiednim czasie)

---

## 3. Pełna lista endpointów REST API

Dla każdego z zasobów zadeklarowane zostały standardowe akcje typu CRUD (Create, Read, Update, Delete) realizujące określone zachowanie wobec bazy danych.

### Goście (`/api/guests`)
* `GET /api/guests` – Wylistuj wszystkich gości.
* `GET /api/guests/{id}` – Zwróć szczegóły konkretnego gościa.
* `POST /api/guests` – Zarejestruj (utwórz) nowego gościa.
* `PUT /api/guests/{id}` – Zaaktualizuj dane gościa o wskazanym ID.
* `DELETE /api/guests/{id}` – Usuń wskazanego gościa z sytemu.

### Pokoje (`/api/rooms`)
* `GET /api/rooms` – Wylistuj wszystkie pokoje.
* `GET /api/rooms/{id}` – Szczegóły danego pokoju.
* `GET /api/rooms/available` – Pokaż tylko pokoje o statusie "Available".
* `POST /api/rooms` – Dodaj nową pozycję pokoju w systemie.
* `PUT /api/rooms/{id}` – Edytuj informacje o pokoju (np. zmiana statusu lub ceny).
* `DELETE /api/rooms/{id}` – Zwiń pokój z oferty agroturystyki.

### Typy Pokoi (`/api/roomtypes`)
* `GET /api/roomtypes` – Pobierz strukturę (kategorie) pokoi.
* `GET /api/roomtypes/{id}` – Informacje o konkretnym typie.
* `POST /api/roomtypes` – Dodaj nową kategorię pokoju.
* `PUT /api/roomtypes/{id}` – Modyfikacja detali określonej kategorii.
* `DELETE /api/roomtypes/{id}` – Usunięcie typu pokoju.

### Usługi (`/api/services`)
* `GET /api/services` – Zobacz bazę przewidywanych usług pobocznych.
* `GET /api/services/{id}` – Odbierz pojedynczą usługę.
* `GET /api/services/available` – Wstępnie wyselekcjonowana lista dostępnych usług.
* `POST /api/services` – Stworzenie usługi w ofercie.
* `PUT /api/services/{id}` – Modyfikacja istniejącej usługi (np. podbicie ceny).
* `DELETE /api/services/{id}` – Likwidacja zapisu o usłudze.

### Rezerwacje (`/api/reservations`)
To unikalna architektura posiadająca tzw. sub-ścieżki operujące między relacjami.
* `GET /api/reservations` – Zbierz i przedstaw wszystkie rezerwacje z dołączonymi szczegółami (wylistowani powiązani goście i dodane pokoje).
* `GET /api/reservations/{id}` – Pojedyncza rezerwacja w detalu.
* `GET /api/reservations/guest/{guestId}` – Wyśledź wszystkie rezerwacje zrobione na wskazanego gościa.
* `POST /api/reservations` – Właściwe założenie rezerwacji (nagłówek, daty, gość).
* `PUT /api/reservations/{id}` – Edycja rezerwacji.
* `DELETE /api/reservations/{id}` – Kasacja rezerwacji (w tle puszczany jest mechanizm zwalniający zajmowane pokoje ze statusu 'Occupied' z powrotem na 'Available').
* `POST /api/reservations/{reservationId}/rooms/{roomId}` – Dopnij dany pokój do zarejestrowanej już rezerwacji i dolicz koszty (akcja powiązana z dodaniem tabeli relacji).
* `POST /api/reservations/{reservationId}/services/{serviceId}` – Realizacja zamówionej usługi w ramach opłacanej rezerwacji z automatu aktualizująca kwotę w rachunku łącznym (TotalPrice).

---

## 4. Krótki opis żądania i odpowiedzi (Przykłady ze Swaggera)

Poniżej przedstawiono dwie wybrane ścieżki prezentujące w jaki sposób proste jest modelowanie danych względem formatu JSON.

### Odczyt dostępnych pokoi: `GET /api/rooms/available`
- **Request (Żądanie)**: Akcja jest bezparametrowa – zwykłe odebranie po podaniu URLa. Wystarczy puste uderzenie endpointu. Brak konieczności przesyłania Body JSON.
- **Response (Odpowiedź) [200 OK]**: Serwer wyciąga tylko te pokoje, które posiadają Status="Available" wraz z zaciągnięciem (Include) tabeli słownikowej, by pokazać o razu opis/wymiary (Standard) i nazwę z TypuPokoju. Lista JSON w formie surowca pożądanego na frontedzie.

### Założenie pokoju w systemie: `POST /api/rooms`
- **Request (Żądanie)**: Do endpointu wysyłany jest czysty model JSON opisujący konkretny pokój. Przykład danych z Request Body:
```json
{
  "roomTypeID": 1,
  "roomNumber": "10A",
  "capacity": 2,
  "pricePerNight": 190.50,
  "status": "Available"
}
```
- **Response (Odpowiedź) [201 Created]**: System odbiera format, puszcza zapytanie INSERT do bazy za pomocą powiązanego Entity Frameworka i nadaje nowy unikalny numer `roomID`. Odpowiedzią jest ten sam rozbudowany JSON plus dodany automatycznie ID (co ostatecznie potwierdza pomyślne przeprocessowanie).

---

## 5. Dane testowe do prezentacji w interfejsie Swagger

Podczas zajęć można skorzystać z poniższych gotowych bloków w formacie JSON, wklejając je w Swaggerze (przycisk **"Try it out"**) na odpowiednich endpointach z metodą `POST`. Należy tylko upewnić się, że w polu ID zawsze widnieje `0` (serwer nada numery ID samodzielnie po trafieniu do bazy danych).

### A. Dodawanie Gościa (`POST /api/guests`)
Endpoint służy do rejestracji fizycznej osoby w systemie.
```json
{
  "guestID": 0,
  "firstName": "Jan",
  "lastName": "Kowalski",
  "email": "jan.kowalski@example.com",
  "phone": "500600700",
  "taxID": "PL1234567890",
  "notes": "Gość poprosił o rezerwację telefonicznie."
}
```

### B. Dodawanie Kategorii Pokoju (`POST /api/roomtypes`)
Słownik wymuszający zdefiniowanie "jakie w ogóle kwatery posiadamy".
```json
{
  "roomTypeID": 0,
  "typeName": "Apartament z widokiem",
  "description": "Duży pokój z bezpośrednim wyjściem na górskie szlaki.",
  "standard": "Premium"
}
```

### C. Dodawanie konkretnego Pokoju (`POST /api/rooms`)
Tworzy fizyczny obiekt pokoju w ofercie. Należy wpisać poprawne `roomTypeID` (jeśli w kroku wyżej podano Typ i serwer utworzył go z np. ID `1`, wpisz tutaj `1`).

**UWAGA w Swaggerze:** Endpoint ten przyjmuje **tylko jeden pokój naraz**. Kopiuj tylko pierwszy obiekt JSON, nie używaj całych tablic.
```json
{
  "roomID": 0,
  "roomTypeID": 1,
  "roomNumber": "101",
  "capacity": 3,
  "pricePerNight": 250.00,
  "status": "Available"
}
```

### D. Dodawanie Usługi Pobocznej (`POST /api/services`)
Definiuje to, co można w obiekcie dokupić dodatkowo do pobytu.
```json
{
  "serviceID": 0,
  "serviceName": "Wypożyczenie roweru",
  "description": "Rower górski na 24h",
  "unitPrice": 45.00,
  "availability": "Available"
}
```

### E. Złożenie kompletnej Rezerwacji (`POST /api/reservations`)
Tworzy wpis o pobycie wraz z przypisanymi elementami. Wymagane jest wpisanie ID już istniejącego w bazie gościa (`guestID`: 1), pokoju i ewentualnie usługi (które stworzone były w poprzednich krokach A-D).

```json
{
  "reservationID": 0,
  "guestID": 1,
  "reservationDate": "2026-04-10T10:00:00.000Z",
  "checkInDate": "2026-04-15",
  "checkOutDate": "2026-04-20",
  "numberOfGuests": 2,
  "totalPrice": 1250.00,
  "reservationStatus": "Confirmed"
}
```

### F. Obsługa żądań GET, PUT i DELETE
Powyższe przykłady skupiają się na wprowadzaniu nowych danych (`POST`), co jest niezbędne, aby wygenerować początkowe zasoby w pustej bazie. Po wypełnieniu systemu danymi wstępnymi (kroki A-E), pozostałe punkty dostępowe można przetestować w następujący sposób:

* **Odczyt zasobów (GET):** Endpoint obsługujący zbiorcze pobieranie (np. `/api/guests`) nie wymaga wpisywania żadnych parametrów – zwraca od razu pełną listę. Aby odczytać obiekty pojedyncze (np. `/api/guests/{id}`), konieczne jest podanie nadanego wcześniej w bazie numeru ID.
* **Modyfikacja zasobów (PUT):** Do przetestowania edycji należy wykorzystać ścieżkę z parametrem wybranego ID (np. `/api/guests/{id}`) i przekazać w Body JSON odpowiednio zmodyfikowany model z nowymi wartościami (np. zmieniony numer telefonu).
* **Usuwanie zasobów (DELETE):** Żądanie usunięcia nie przyjmuje wartości w Body. Wymaga jedynie wskazania poprawnego numeru ID jako precyzyjnego celu do likwidacji (np. `/api/guests/{id}`).

Poprawne wykonanie kompletu powyższych operacji wyczerpująco weryfikuje logikę i stabilność systemu od momentu zapisu nowego wiersza, po jego edycję i ostateczne skasowanie.