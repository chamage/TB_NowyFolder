# Dokumentacja Wstêpnej Wersji REST API

## 1. Cel i charakter zadania
Zadanie polega³o na zaprojektowaniu i utworzeniu **wstêpnej wersji backendu** udostêpniaj¹cego operacje CRUD za pomoc¹ tzw. punktów dostêpu (endpointów).

G³ównym sposobem komunikacji z API i demonstracji dzia³ania na tym etapie jest interfejs **Swagger**, daj¹cy szybki wgl¹d we wszystkie utworzone œcie¿ki (routingi) i umo¿liwiaj¹cy rêczne wstrzykiwanie danych (np. rezerwacji czy nowych goœci).

---

## 2. G³ówne zasoby API (Endpointy)

Utworzone API zarz¹dza piêcioma g³ównymi "bytami" (zasobami) zwi¹zanymi z dzia³aniem agroturystyki:

1. **Guests** (Goœcie u których przechowywane s¹ chocia¿by dane kontaktowe)
2. **Rooms** (Konkretne pokoje)
3. **RoomTypes** (S³ownik dostêpnych konfiguracji, np. "Pokój dwuosobowy", "Apartament")
4. **Services** (Us³ugi dodatkowe przypinane w locie)
5. **Reservations** (Systemowy trzon spinaj¹cy goœci, us³ugi i pokoje w odpowiednim czasie)

---

## 3. Pe³na lista endpointów REST API

Dla ka¿dego z zasobów zadeklarowane zosta³y standardowe akcje typu CRUD (Create, Read, Update, Delete) realizuj¹ce okreœlone zachowanie wobec bazy danych.

### Goœcie (`/api/guests`)
* `GET /api/guests` – Wylistuj wszystkich goœci.
* `GET /api/guests/{id}` – Zwróæ szczegó³y konkretnego goœcia.
* `POST /api/guests` – Zarejestruj (utwórz) nowego goœcia.
* `PUT /api/guests/{id}` – Zaaktualizuj dane goœcia o wskazanym ID.
* `DELETE /api/guests/{id}` – Usuñ wskazanego goœcia z sytemu.

### Pokoje (`/api/rooms`)
* `GET /api/rooms` – Wylistuj wszystkie pokoje.
* `GET /api/rooms/{id}` – Szczegó³y danego pokoju.
* `GET /api/rooms/available` – Poka¿ tylko pokoje o statusie "Available".
* `POST /api/rooms` – Dodaj now¹ pozycjê pokoju w systemie.
* `PUT /api/rooms/{id}` – Edytuj informacje o pokoju (np. zmiana statusu lub ceny).
* `DELETE /api/rooms/{id}` – Zwiñ pokój z oferty agroturystyki.

### Typy Pokoi (`/api/roomtypes`)
* `GET /api/roomtypes` – Pobierz strukturê (kategorie) pokoi.
* `GET /api/roomtypes/{id}` – Informacje o konkretnym typie.
* `POST /api/roomtypes` – Dodaj now¹ kategoriê pokoju.
* `PUT /api/roomtypes/{id}` – Modyfikacja detali okreœlonej kategorii.
* `DELETE /api/roomtypes/{id}` – Usuniêcie typu pokoju.

### Us³ugi (`/api/services`)
* `GET /api/services` – Zobacz bazê przewidywanych us³ug pobocznych.
* `GET /api/services/{id}` – Odbierz pojedyncz¹ us³ugê.
* `GET /api/services/available` – Wstêpnie wyselekcjonowana lista dostêpnych us³ug.
* `POST /api/services` – Stworzenie us³ugi w ofercie.
* `PUT /api/services/{id}` – Modyfikacja istniej¹cej us³ugi (np. podbicie ceny).
* `DELETE /api/services/{id}` – Likwidacja zapisu o us³udze.

### Rezerwacje (`/api/reservations`)
To unikalna architektura posiadaj¹ca tzw. sub-œcie¿ki operuj¹ce miêdzy relacjami.
* `GET /api/reservations` – Zbierz i przedstaw wszystkie rezerwacje z do³¹czonymi szczegó³ami (wylistowani powi¹zani goœcie i dodane pokoje).
* `GET /api/reservations/{id}` – Pojedyncza rezerwacja w detalu.
* `GET /api/reservations/guest/{guestId}` – WyœledŸ wszystkie rezerwacje zrobione na wskazanego goœcia.
* `POST /api/reservations` – W³aœciwe za³o¿enie rezerwacji (nag³ówek, daty, goœæ).
* `PUT /api/reservations/{id}` – Edycja rezerwacji.
* `DELETE /api/reservations/{id}` – Kasacja rezerwacji (w tle puszczany jest mechanizm zwalniaj¹cy zajmowane pokoje ze statusu 'Occupied' z powrotem na 'Available').
* `POST /api/reservations/{reservationId}/rooms/{roomId}` – Dopnij dany pokój do zarejestrowanej ju¿ rezerwacji i dolicz koszty (akcja powi¹zana z dodaniem tabeli relacji).
* `POST /api/reservations/{reservationId}/services/{serviceId}` – Realizacja zamówionej us³ugi w ramach op³acanej rezerwacji z automatu aktualizuj¹ca kwotê w rachunku ³¹cznym (TotalPrice).

---

## 4. Krótki opis ¿¹dania i odpowiedzi (Przyk³ady ze Swaggera)

Poni¿ej przedstawiono dwie wybrane œcie¿ki prezentuj¹ce w jaki sposób proste jest modelowanie danych wzglêdem formatu JSON.

### Odczyt dostêpnych pokoi: `GET /api/rooms/available`
- **Request (¯¹danie)**: Akcja jest bezparametrowa – zwyk³e odebranie po podaniu URLa. Wystarczy puste uderzenie endpointu. Brak koniecznoœci przesy³ania Body JSON.
- **Response (OdpowiedŸ) [200 OK]**: Serwer wyci¹ga tylko te pokoje, które posiadaj¹ Status="Available" wraz z zaci¹gniêciem (Include) tabeli s³ownikowej, by pokazaæ o razu opis/wymiary (Standard) i nazwê z TypuPokoju. Lista JSON w formie surowca po¿¹danego na frontedzie.

### Za³o¿enie pokoju w systemie: `POST /api/rooms`
- **Request (¯¹danie)**: Do endpointu wysy³any jest czysty model JSON opisuj¹cy konkretny pokój. Przyk³ad danych z Request Body:
```json
{
  "roomTypeID": 1,
  "roomNumber": "10A",
  "capacity": 2,
  "pricePerNight": 190.50,
  "status": "Available"
}
```
- **Response (OdpowiedŸ) [201 Created]**: System odbiera format, puszcza zapytanie INSERT do bazy za pomoc¹ powi¹zanego Entity Frameworka i nadaje nowy unikalny numer `roomID`. Odpowiedzi¹ jest ten sam rozbudowany JSON plus dodany automatycznie ID (co ostatecznie potwierdza pomyœlne przeprocessowanie).

---

## 5. Dane testowe do prezentacji w interfejsie Swagger

Podczas zajêæ mo¿na skorzystaæ z poni¿szych gotowych bloków w formacie JSON, wklejaj¹c je w Swaggerze (przycisk **"Try it out"**) na odpowiednich endpointach z metod¹ `POST`. Nale¿y tylko upewniæ siê, ¿e w polu ID zawsze widnieje `0` (serwer nada numery ID samodzielnie po trafieniu do bazy danych).

### A. Dodawanie Goœcia (`POST /api/guests`)
Endpoint s³u¿y do rejestracji fizycznej osoby w systemie.
```json
{
  "guestID": 0,
  "firstName": "Jan",
  "lastName": "Kowalski",
  "email": "jan.kowalski@example.com",
  "phone": "500600700",
  "taxID": "PL1234567890",
  "notes": "Goœæ poprosi³ o rezerwacjê telefonicznie."
}
```

### B. Dodawanie Kategorii Pokoju (`POST /api/roomtypes`)
S³ownik wymuszaj¹cy zdefiniowanie "jakie w ogóle kwatery posiadamy".
```json
{
  "roomTypeID": 0,
  "typeName": "Apartament z widokiem",
  "description": "Du¿y pokój z bezpoœrednim wyjœciem na górskie szlaki.",
  "standard": "Premium"
}
```

### C. Dodawanie konkretnego Pokoju (`POST /api/rooms`)
Tworzy fizyczny obiekt pokoju w ofercie. Nale¿y wpisaæ poprawne `roomTypeID` (jeœli w kroku wy¿ej podano Typ i serwer utworzy³ go z np. ID `1`, wpisz tutaj `1`).

**UWAGA w Swaggerze:** Endpoint ten przyjmuje **tylko jeden pokój naraz**. Kopiuj tylko pierwszy obiekt JSON, nie u¿ywaj ca³ych tablic.
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

### D. Dodawanie Us³ugi Pobocznej (`POST /api/services`)
Definiuje to, co mo¿na w obiekcie dokupiæ dodatkowo do pobytu.
```json
{
  "serviceID": 0,
  "serviceName": "Wypo¿yczenie roweru",
  "description": "Rower górski na 24h",
  "unitPrice": 45.00,
  "availability": "Available"
}
```

### E. Z³o¿enie kompletnej Rezerwacji (`POST /api/reservations`)
Tworzy wpis o pobycie wraz z przypisanymi elementami. Wymagane jest wpisanie ID ju¿ istniej¹cego w bazie goœcia (`guestID`: 1), pokoju i ewentualnie us³ugi (które stworzone by³y w poprzednich krokach A-D).

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

### F. Obs³uga ¿¹dañ GET, PUT i DELETE
Powy¿sze przyk³ady skupiaj¹ siê na wprowadzaniu nowych danych (`POST`), co jest niezbêdne, aby wygenerowaæ pocz¹tkowe zasoby w pustej bazie. Po wype³nieniu systemu danymi wstêpnymi (kroki A-E), pozosta³e punkty dostêpowe mo¿na przetestowaæ w nastêpuj¹cy sposób:

* **Odczyt zasobów (GET):** Endpoint obs³uguj¹cy zbiorcze pobieranie (np. `/api/guests`) nie wymaga wpisywania ¿adnych parametrów – zwraca od razu pe³n¹ listê. Aby odczytaæ obiekty pojedyncze (np. `/api/guests/{id}`), konieczne jest podanie nadanego wczeœniej w bazie numeru ID.
* **Modyfikacja zasobów (PUT):** Do przetestowania edycji nale¿y wykorzystaæ œcie¿kê z parametrem wybranego ID (np. `/api/guests/{id}`) i przekazaæ w Body JSON odpowiednio zmodyfikowany model z nowymi wartoœciami (np. zmieniony numer telefonu).
* **Usuwanie zasobów (DELETE):** ¯¹danie usuniêcia nie przyjmuje wartoœci w Body. Wymaga jedynie wskazania poprawnego numeru ID jako precyzyjnego celu do likwidacji (np. `/api/guests/{id}`).

Poprawne wykonanie kompletu powy¿szych operacji wyczerpuj¹co weryfikuje logikê i stabilnoœæ systemu od momentu zapisu nowego wiersza, po jego edycjê i ostateczne skasowanie.