# Sprawozdanie bezpieczeństwa - Hotel Reservation API

## 1. Link do repozytorium

[https://github.com/chamage/TB_NowyFolder](https://github.com/chamage/TB_NowyFolder)

---

## 2. Definicja i projekt

### 2.1. Wymagania projektu

Aplikacja to system obsługi rezerwacji hotelowych. Wymagania obejmują:

- zarządzanie gośćmi, pokojami (z typami) i usługami hotelowymi,
- składanie i modyfikowanie rezerwacji z przypisaniem pokoi i usług,
- wyliczanie ceny rezerwacji na podstawie liczby nocy i cen pokoi,
- system ról (Administrator, Recepcjonista, Klient) oparty na RBAC,
- uwierzytelnianie przez tokeny JWT (logowanie, rejestracja),
- generowanie i weryfikację dokumentów podpisanych cyfrowo (RSA-SHA256),
- interfejs webowy (Razor Pages + jQuery AJAX) komunikujący się z API,
- dokumentację API przez Swagger UI - dostępną tylko w trybie deweloperskim.

### 2.2. Architektura

Projekt oparty jest na architekturze klient-serwer:

- **Backend:** ASP.NET Core (.NET 9), Minimal API - 7 grup endpointów
- **Frontend:** Razor Pages + JavaScript (jQuery, Bootstrap) - komunikacja AJAX z API
- **Baza danych:** SQL Server (LocalDB lokalnie), EF Core 9 jako ORM, model Code-First
- **Bezpieczeństwo:** JWT Bearer, PBKDF2 do hashowania haseł, RBAC przez polityki autoryzacji, podpis cyfrowy RSA
- **Zewnętrzne API:** Open-Meteo (`api.open-meteo.com`) - darmowe API pogodowe wywoływane przez frontend (JavaScript) przy załadowaniu strony. Wyświetla aktualną pogodę dla Krosna w widgecie na stronie głównej. Nie wymaga klucza API.

```
[Przeglądarka / Klient]
        |
        | HTTP/HTTPS (AJAX, JSON)
        v
[ASP.NET Core — Minimal API]
  |-- AuthEndpoints        (/api/auth/token, /register, /me)
  |-- GuestEndpoints       (/api/guests)
  |-- RoomEndpoints        (/api/rooms, /available)
  |-- RoomTypeEndpoints    (/api/roomtypes)
  |-- ServiceEndpoints     (/api/services)
  |-- ReservationEndpoints (/api/reservations)
  |-- DocumentEndpoints    (/api/documents/generate, /verify)
        |
        | EF Core (LINQ → SQL)
        v
[SQL Server / LocalDB]
```

### 2.3. Diagramy UML

Pełne diagramy UML (przypadki użycia, klasy, aktywności, sekwencje) są w pliku `Documentation/UML_Diagrams.md`.

Skrótowy diagram encji bazy danych:

```
Guest ──< Reservation ──< ReservationRoom >── Room >── RoomType
                      ──< ReservationService >── Service
User >── Guest (powiązanie opcjonalne - klient ma przypisanego gościa)
```

### 2.4. Potencjalne zagrożenia

Zagrożenia rozpoznane przed implementacją:

| Zagrożenie | Opis | Mitygacja |
|---|---|---|
| Nieuprawniony dostęp do zasobów | Klient mógłby czytać lub zmieniać cudze rezerwacje | RBAC + walidacja `guestId` z tokenu JWT na poziomie endpointu |
| Przejęcie tokenu JWT przez XSS | Token w `localStorage` jest dostępny przez JavaScript | Znane ryzyko; w produkcji zalecaną alternatywą jest HttpOnly cookie |
| Wyciek bazy danych z hasłami | Hasła w postaci jawnej po wycieku są bezużyteczne | PBKDF2 z solą - hasze są odporne na ataki słownikowe |
| Ujawnienie sekretów w repozytorium | Klucz JWT i connection string w kodzie źródłowym | `appsettings.Local.json` w `.gitignore`; produkcja: zmienne środowiskowe |
| Hasze seedowych kont w repo | `HasData()` w EF Core nie obsługuje IConfiguration - hasze kont demo są częścią migracji i trafiają do repo | Konta są wyłącznie testowe; hasze PBKDF2 nie ujawniają haseł wprost; w produkcji seed należy usunąć |
| Brak spójności danych | Przerwanie wieloetapowego zapisu w połowie | `SaveChangesAsync()` w EF Core to jedna atomowa transakcja |
| Nadużycie ról | Klient wykonuje operacje zarezerwowane dla personelu | Polityki autoryzacji przypisane do każdego endpointu |

---

## 3. Tabela zależności

| Komponent / Zależność | Wersja | Rola |
|---|---|---|
| **.NET SDK / Runtime** | 9.0 | Platforma uruchomieniowa (Minimal API, Razor Pages) |
| **Microsoft.EntityFrameworkCore.SqlServer** | 9.0.0 | ORM - komunikacja z SQL Server |
| **Microsoft.EntityFrameworkCore.Design** | 9.0.0 | Narzędzia do generowania migracji EF Core |
| **Microsoft.EntityFrameworkCore.Tools** | 9.0.0 | CLI do migracji (`dotnet ef`) |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | 9.0.0 | Walidacja tokenów JWT w middleware |
| **Swashbuckle.AspNetCore** | 7.2.0 | Generowanie dokumentacji OpenAPI i Swagger UI |
| **SQL Server (LocalDB)** | MSSQLLocalDB | Relacyjna baza danych |
| **Bootstrap** | 5.3.3 | Biblioteka CSS |
| **jQuery** | 3.7.1 | Biblioteka JavaScript - AJAX, manipulacja DOM |
| **Open-Meteo API** | - (bez wersjonowania) | Zewnętrzne API pogodowe - wywoływane przez frontend (JS) przy załadowaniu strony. Bezpłatne, bez klucza API. |

> `Microsoft.AspNetCore.Identity.PasswordHasher<T>` jest częścią platformy .NET, nie wymaga osobnej paczki NuGet. Stosowany do hashowania haseł algorytmem PBKDF2.

---

## 4. Analiza bezpieczeństwa - według OWASP Top 10

### 4.1. Kontrola dostępu i role

*(OWASP A01:2021 - Broken Access Control)*

W projekcie zastosowano RBAC z trzema rolami: Administrator, Recepcjonista, Klient oraz trzy polityki autoryzacji:
- `AuthenticatedUser` - wymagane zalogowanie (bazowa polityka grupy)
- `StaffOrAdmin` - Recepcjonista lub Administrator
- `AdminOnly` - tylko Administrator

Każdy endpoint ma przypisane konkretne wymagania. Klient może odczytywać i usuwać tylko własne rezerwacje - próba dostępu do cudzej przez `GET /api/reservations/{id}` lub `DELETE /api/reservations/{id}` kończy się `403 Forbidden`:

```csharp
if (user.IsInRole(ApplicationRoles.Client))
{
    var guestIdClaim = user.FindFirst("guestId")?.Value;
    if (!int.TryParse(guestIdClaim, out var guestId) || reservation.GuestID != guestId)
        return Results.Forbid();
}
```

Klient tworzący rezerwację ma nadpisywane `GuestID` wartością z tokenu - zapobiega to podmienieniu właściciela rezerwacji (IDOR). Dodawanie pokójów i usług do rezerwacji jest ograniczone do `StaffOrAdmin` - klient nie może modyfikować cudzych rezerwacji przez te endpointy.

**Ograniczenia:** brak stronicowania na listach - `GET /api/reservations` zwraca wszystkie rekordy naraz.

---

### 4.2. Uwierzytelnianie i JWT

*(OWASP A07:2021 - Identification and Authentication Failures)*

Token JWT jest generowany przy logowaniu i ważny przez 1 godzinę. Middleware sprawdza: issuer, audience, lifetime i klucz podpisujący (`ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`).

**Token jest przechowywany w `localStorage`.** Oznacza to, że przy skutecznym ataku XSS na frontend token może zostać odczytany przez skrypt. Alternatywą ograniczającą to ryzyko byłoby HttpOnly cookie.

Braki:
- brak refresh tokena - po 1 godzinie użytkownik musi się ponownie zalogować,
- brak rate limitingu na `/api/auth/token` - możliwe ataki brute force,
- brak blokady konta po wielokrotnych nieudanych próbach,
- brak wymagań dotyczących złożoności hasła przy rejestracji.

---

### 4.3. Hasła i kryptografia

*(OWASP A02:2021 - Cryptographic Failures)*

Hasła są hashowane przez `PasswordHasher<T>` algorytmem PBKDF2 z automatycznie generowaną solą. Weryfikacja przez `VerifyHashedPassword()`.

Klucz JWT (HMAC-SHA256) nie jest w repozytorium - lokalnie czytany z `appsettings.Local.json` (plik w `.gitignore`). W produkcji klucz powinien być przekazywany przez zmienną środowiskową `Jwt__Key`.

---

### 4.4. Walidacja danych wejściowych

*(OWASP A03:2021 - Injection, powiązane)*

Szczegółowy opis w sekcji 6. W skrócie: walidacja po stronie frontendu można pominąć przez bezpośrednie wywołania API (curl, Postman). Ochrona działa na poziomie backendu i bazy danych.

---

### 4.5. SQL Injection i baza danych

*(OWASP A03:2021 - Injection)*

Zapytania do bazy są realizowane przez EF Core z LINQ (`FindAsync`, `FirstOrDefaultAsync`, `Where`). EF Core generuje zapytania parametryzowane - dane użytkownika nie są wklejane bezpośrednio do SQL, co ogranicza ryzyko SQL Injection.

```csharp
var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
// EF Core: SELECT * FROM Users WHERE Username = @p0
```

Ryzyko wzrasta przy użyciu `ExecuteSqlRaw()` - w tym projekcie takie wywołania nie występują (do weryfikacji przy zmianach w kodzie).

---

### 4.6. CORS, Swagger i konfiguracja

*(OWASP A05:2021 - Security Misconfiguration)*

**CORS:** ustawiony jako `AllowAnyOrigin()` - dopuszczalne tylko lokalnie. W produkcji należy ograniczyć do konkretnej domeny frontendu.

**Swagger UI:** dostępny tylko w trybie deweloperskim (`if (app.Environment.IsDevelopment())`). W produkcji dokumentacja API nie jest eksponowana publicznie.

**HSTS:** włączony w trybie produkcyjnym (`app.UseHsts()`).

**Sekrety:** klucz JWT i connection string nie trafiają do repozytorium. Lokalnie w `appsettings.Local.json` (plik w `.gitignore`), produkcyjnie przez zmienne środowiskowe.

---

### 4.7. Obsługa błędów i sytuacje wyjątkowe

*(OWASP A05:2021 - powiązane)*

**Awaria bazy danych:** EF Core zgłasza `SqlException`. W trybie produkcyjnym middleware `app.UseExceptionHandler("/Error")` zwraca ogólną stronę błędu bez stack trace. Brak retry logic (np. biblioteka Polly) - przejściowa awaria połączenia od razu skutkuje błędem 500.

**Zewnętrzne API niedostępne (Open-Meteo):** widget pogodowy po prostu się nie pojawia - błąd jest obsłużony w JavaScript przez `console.warn()`. Backend nie jest zaangażowany w to wywołanie (jest ono realizowane bezpośrednio z przeglądarki), więc niedostępność Open-Meteo nie wpływa na działanie API ani bazy danych.

**Nieprawidłowe dane wejściowe:** backend zwraca `400 Bad Request` z komunikatem JSON `{ "error": "..." }`. Stack trace nie jest ujawniany w produkcji.

**Wygaśnięcie tokenu JWT:** po 1 godzinie API zwraca `401 Unauthorized`. Brak refresh tokena - użytkownik musi się ponownie zalogować.

---

### 4.8. Logowanie i monitoring

*(OWASP A09:2021 - Security Logging and Monitoring Failures)*

W projekcie działa tylko domyślny logger ASP.NET Core (`Microsoft.Extensions.Logging`). Nie zostało zaimplementowane:

- logowanie nieudanych prób logowania,
- alerty przy podejrzanej aktywności,
- centralny system logów (np. Serilog, Seq, Application Insights).

W produkcji brak logowania zdarzeń bezpieczeństwa utrudnia wykrycie ataku i analizę po incydencie.

---

### 4.9. Podpis cyfrowy i integralność danych

*(OWASP A08:2021 - Software and Data Integrity Failures)*

`DigitalSignatureService` podpisuje i weryfikuje dokumenty algorytmem RSA-SHA256. Klucz RSA jest generowany przy każdym starcie aplikacji i nie jest utrwalany. Podpisy z poprzedniej sesji nie mogą być zweryfikowane po restarcie - to ograniczenie eliminuje praktyczne użycie tej funkcji poza środowiskiem deweloperskim.

**SSRF (A10:2021):** nie dotyczy - aplikacja nie wykonuje żądań HTTP do zewnętrznych adresów na podstawie danych od użytkownika.

---

## 5. Testy jednostkowe

Testy jednostkowe nie zostały zaimplementowane ze względu na ograniczenia czasowe.

Obszary do pokrycia przy dalszym rozwoju:

- `AuthEndpoints` - generowanie tokenu, weryfikacja hasła, rejestracja,
- `DigitalSignatureService` - podpisywanie i weryfikacja,
- `ReservationEndpoints` - wyliczanie ceny, wymuszanie `GuestID` dla klienta,
- modele - atrybuty `[Required]`, `[MaxLength]`, `[EmailAddress]`.

Rekomendowany framework: **xUnit** + **Moq**.

---

## 6. Walidacja danych

W projekcie zastosowano walidację na trzech warstwach:

**Warstwa 1 - frontend (HTML5, JavaScript)**
Pola formularzy z typami (`<input type="email">`, `<input type="number" min="1">`) i atrybutem `required`. Cel to poprawa UX i redukcja zbędnych żądań. Warstwa może być całkowicie pominięta przez bezpośrednie wywołania API - nie stanowi zabezpieczenia.

**Warstwa 2 - backend (ASP.NET Core)**
Atrybuty `DataAnnotations` na modelach (`[Required]`, `[MaxLength]`, `[EmailAddress]`) i manualna walidacja w endpointach:

```csharp
if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    return Results.BadRequest(new { error = "Username and password are required." });
```

**Warstwa 3 - baza danych (SQL Server przez EF Core)**
Unikalne indeksy (`Username`), klucze kompozytowe (`ReservationRoom`, `ReservationService`), constrainty `NOT NULL` i długości kolumn z modeli. Niespójne dane są odrzucane na poziomie bazy.

Każda warstwa działa niezależnie. Atak omijający frontend trafia na walidację backendu; atak omijający backend trafia na constrainty bazy.

---

## 7. Transakcje i ORM

Operacje na bazie danych są realizowane przez Entity Framework Core (wzorzec Unit of Work). Jawne transakcje (`db.Database.BeginTransaction()`) nie są stosowane. Atomowość zapewnia `SaveChangesAsync()`, który wysyła wszystkie zgromadzone zmiany w jednej transakcji SQL:

```csharp
db.Guests.Add(guest);
db.Users.Add(user);
await db.SaveChangesAsync(); // albo oba zapisy, albo żaden
```

Jeśli operacja się nie powiedzie (np. naruszenie unikalności), EF Core wycofa całą transakcję.

**Właściwości ACID:**
- **Atomowość:** `SaveChangesAsync()` to jedna niepodzielna operacja.
- **Spójność:** constrainty bazy odrzucą dane naruszające integralność.
- **Izolacja:** EF Core stosuje domyślny poziom izolacji SQL Server (Read Committed).
- **Trwałość:** dane zatwierdzone przez SQL Server są zapisywane na dysku.

**Ograniczenie:** tworzenie rezerwacji obejmuje zapis rezerwacji i zmianę statusu pokoju. Oba zapisy trafiają do jednego `SaveChangesAsync()` - to zapewnia atomowość. Przy bardziej złożonych operacjach wymagana byłaby jawna transakcja.

---

## 8. Scenariusz wdrożenia

Poniżej przykładowy scenariusz wdrożenia. Nie został zrealizowany - ma charakter koncepcyjny.

### Opcja A - Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TB_NowyFolder.dll"]
```

Sekrety przekazywane przez zmienne środowiskowe:

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=db;..." \
  -e Jwt__Key="SilnyKluczProdukcyjny" \
  hotel-api
```

### Opcja B - Azure App Service

1. Publikacja: `dotnet publish -c Release`
2. Wdrożenie przez Azure CLI: `az webapp deployment source config-zip`
3. Zmienne środowiskowe w Application Settings:
   - `ConnectionStrings__DefaultConnection` → Azure SQL Database
   - `Jwt__Key` → klucz min. 32 znaki, losowy
4. Baza danych: Azure SQL Database zamiast LocalDB

```
[Azure App Service]     [Azure SQL Database]
  Hotel API (.NET 9) ──> HotelReservationDB
        |
        | HTTPS
        v
  [Użytkownicy / Frontend]
```

Klucz JWT i connection string nie trafiają do repozytorium - przekazywane przez zmienne środowiskowe lub Azure Key Vault.

---

## 9. Podsumowanie

**Co zostało zrealizowane:**
- uwierzytelnianie JWT z walidacją issuer, audience, lifetime i klucza podpisującego,
- RBAC z trzema rolami przypisanymi na poziomie endpointów,
- ochrona przed IDOR przez walidację `guestId` z tokenu przy dostępie do rezerwacji,
- hashowanie haseł algorytmem PBKDF2 z solą,
- walidacja danych na trzech warstwach (frontend, backend, baza),
- atomowość operacji zapisu przez `SaveChangesAsync()` EF Core,
- sekrety poza repozytorium przez `appsettings.Local.json` i `.gitignore`.

**Ograniczenia:**
- token JWT w `localStorage` - przy XSS token może zostać odczytany przez skrypt,
- brak refresh tokena,
- brak rate limitingu i blokady konta - endpoint logowania jest podatny na brute force,
- klucz RSA w `DigitalSignatureService` nie jest utrwalany - podpisy nie są weryfikowalne po restarcie,
- brak logowania zdarzeń bezpieczeństwa,
- brak testów jednostkowych,
- CORS jako `AllowAnyOrigin()` - wymaga zmiany przed wdrożeniem produkcyjnym.

**Minimum przed wdrożeniem produkcyjnym:**
- zmienić `AllowAnyOrigin()` na konkretną domenę,
- przenieść token do HttpOnly cookie lub dodać refresh token,
- dodać rate limiting na endpoint logowania,
- utrwalić klucz RSA lub zrezygnować z podpisu cyfrowego,
- wdrożyć centralne logowanie zdarzeń bezpieczeństwa.
