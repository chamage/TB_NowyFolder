# Sprawozdanie Bezpieczeństwa - Hotel Reservation API

## 1. Link do repozytorium
[TUTAJ WKLEJ LINK DO REPOZYTORIUM GITHUB]

*(Zgodnie z wymaganiami, jeśli repozytorium jest prywatne, prowadzący btrybus otrzymał zaproszenie do repozytorium i informację na MS Teams).*

## 2. Definition and design

### Requirements review
Aplikacja została zaprojektowana jako system obsługi rezerwacji hotelowych. Do głównych wymagań projektu należą:
- Zarządzanie gośćmi, pokojami oraz usługami hotelowymi.
- Składanie i modyfikowanie rezerwacji wraz z przypisywaniem do nich pokoi i usług dodatkowych.
- Implementacja systemu ról i uprawnień (Administrator, Recepcjonista, Klient) oparta o Role-Based Access Control (RBAC).
- Zabezpieczenie systemu poprzez logowanie (hashing haseł PBKDF2) oraz uwierzytelnianie i autoryzację oparte na tokenach JWT.
- Obsługa interfejsu klienta webowego oraz udostępnienie dobrze opisanej dokumentacji API poprzez Swaggera.

### Design and architecture review
Projekt opiera się na architekturze typu Client-Server:
- **Backend:** Zbudowany w oparciu o ASP.NET Core (.NET 9), wykorzystujący Minimal API do udostępniania zasobów.
- **Baza danych:** SQL Server (LocalDB w środowisku deweloperskim) zarządzana poprzez Entity Framework Core (ORM) w modelu Code-First.
- **Frontend:** Dynamiczny interfejs webowy (Razor Pages) działający po stronie klienta, korzystający z JavaScript (oraz jQuery), który w asynchroniczny sposób komunikuje się z API (AJAX).
- **Bezpieczeństwo:** Zastosowano `DigitalSignatureService` z wykorzystaniem algorytmu PBKDF2 do bezpiecznego przechowywania haseł. Ochrona punktów końcowych realizowana jest za pomocą dedykowanych `AuthorizationPolicies`.

### UML diagrams
Szczegółowe diagramy architektoniczne, bazy danych oraz przypadków użycia znajdują się w osobnym dokumencie, który znajduje się w repozytorium: `UML_Diagrams.md`. (Diagramy zostały odświeżone i dostosowane do najnowszej wersji Minimal API oraz modelu bazy danych EF Core).

### Potencjalne zagrożenia (Threat modeling)
Przed przystąpieniem do szczegółowej implementacji zidentyfikowano następujące ryzyka:
- **Nieuprawniony dostęp do zasobów:** ryzyko manipulacji rezerwacjami przez innych użytkowników. Zminimalizowano je wprowadzając RBAC oraz walidację tożsamości z JWT (klient widzi tylko własne rezerwacje).
- **Przejęcie tokenu JWT (XSS):** aplikacja webowa wykorzystuje mechanizm `localStorage` do przechowywania tokenów, co stanowi teoretyczne ryzyko w przypadku podatności Cross-Site Scripting na froncie.
- **Wyciek bazy danych z hasłami:** gdyby baza danych wpadła w niepowołane ręce, ryzyko ograniczone jest poprzez implementację silnego hashowania haseł z użyciem soli.
- **Brak spójności danych:** podczas tworzenia rezerwacji aplikacja musi zarezerwować pokój oraz obliczyć cenę - przerwanie tych akcji w połowie może skutkować "wiszącymi" danymi (mitigacja na poziomie bazy danych).

## 3. Tabela z zależnościami

| Komponent / Zależność | Wersja | Rola / Opis |
|---|---|---|
| **.NET SDK / Runtime** | 9.0 | Główna platforma uruchomieniowa aplikacji (Minimal API / Razor Pages). |
| **Microsoft.EntityFrameworkCore.SqlServer** | 9.0.0 | Provider ORM umożliwiający aplikacji komunikację z bazą SQL Server. |
| **Microsoft.EntityFrameworkCore.Design** | 9.0.0 | Narzędzia ułatwiające zarządzanie kodem migracyjnym bazy danych. |
| **Swashbuckle.AspNetCore** | 7.2.0 | Automatyczne generowanie dokumentacji OpenAPI i interfejsu Swagger UI. |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | 9.0.0 | Obsługa procesu walidacji tokenów JWT. |
| **SQL Server (LocalDB)** | - | Instancja bazy relacyjnej pełniąca rolę trwałego magazynu danych (persistance layer). |
| **jQuery & Bootstrap** | - | Biblioteki w `wwwroot` obsługujące logikę widoków i asynchroniczną komunikację. |

## 4. Analiza bezpieczeństwa projektu

### Wybrane elementy na podstawie OWASP Top 10
1. **Broken Access Control (A01:2021) - Zrealizowane w dużej mierze:** Aplikacja skutecznie broni się przed nadużyciem uprawnień poprzez centralnie skonfigurowane polityki (`RequireAuthorization`, RBAC na endpointach, walidacja `guestId` z claimów). 
2. **Cryptographic Failures (A02:2021) - Zrealizowane częściowo:** Hasła są bezpieczne dzięki autorskiemu serwisowi `DigitalSignatureService` z solą i rozszerzoną liczbą iteracji. Słabą stroną pozostaje jawny klucz w `appsettings.json` (`Jwt:Key`), który dla środowiska produkcyjnego bezwzględnie musi zostać zmigrowany do Azure Key Vault lub zmiennych środowiskowych.
3. **Injection (A03:2021) - Dobrze zabezpieczone:** Do komunikacji z bazą danych wykorzystywany jest nowoczesny EF Core wykorzystujący mechanizmy parametryzacji zapytań (np. `FindAsync`, LINQ `Where`), co skutecznie eliminuje ataki SQL Injection w aplikacji.

### Sytuacje wyjątkowe / Awaryjne (Exceptional Conditions)
Aplikacja w podstawowym stopniu potrafi sobie poradzić z błędami (wykorzystuje `app.UseExceptionHandler("/Error")` w trybie nie-deweloperskim), ale w określonych sytuacjach awaryjnych mogą wystąpić braki w obsłudze.
- **Niedostępność bazy danych (np. awaria SQL Servera):** Obecnie aplikacja zwróci błąd ogólny 500 (Internal Server Error). Nie mamy zaimplementowanego mechanizmu ukrywającego komunikaty przed użytkownikiem w sposób kontrolowany na wszystkich ścieżkach API, chociaż `UseExceptionHandler` dba o to, by stos błędu (stack trace) nie wyciekł w trybie produkcyjnym.
- W projekcie brakuje również wzorca typu `Circuit Breaker` (np. przez bibliotekę Polly) do eleganckiej ponownej próby połączenia (retry logic).

## 5. Testy jednostkowe

Ze względu na ograniczenia czasowe i przyjęty model badawczy projektu, w aplikacji nie zaimplementowano pełnego pokrycia testami jednostkowymi. W dalszym rozwoju projektu (np. przy użyciu xUnit/Moq) należy pokryć:
- **Mechanizmy autoryzacyjne:** Walidację generowania oraz przypisywania claimów w `DigitalSignatureService` oraz `AuthEndpoints`.
- **Logikę wyliczania cen rezerwacji:** Upewnienie się, że `TotalPrice` jest zawsze poprawnie kalkulowana z użyciem długości pobytu w `ReservationEndpoints`.

## 6. Walidacja danych

Aby zapewnić rzetelność przepływających informacji, w systemie przyjęto koncepcję wielowarstwowej weryfikacji danych:
- **Baza danych:** Za sprawą restrykcji zdefiniowanych w modelach (np. `IsUnique()` dla Userów, klucze kompozytowe w klasie `DbContext`), struktura bazy odrzuci niespójne operacje zapisu.
- **Backend (.NET Models):** Klasy reprezentujące encje (np. `Guest.cs`) zaopatrzone są w atrybuty `DataAnnotations` (np. `[Required]`, `[MaxLength]`, `[EmailAddress]`). W Minimal API dla .NET warstwa ta może zostać zaostrzona poprzez implementację filtrów (EndpointFilters) walidujących wejście z pominięciem powtórnej weryfikacji logiki biznesowej.
- **Frontend (UI / JS):** Zabezpieczenia takie jak typy pól (np. `<input type="email">`), które dbają o to, by zmniejszyć obciążenie interfejsu sieciowego na niepoprawne formatki, jednak pełnią one wyłącznie rolę User Experience (wiedząc, że można ominąć je np. z poziomu postmana).

## 7. Transakcje i ORM

Wszystkie operacje bazodanowe są przeprowadzane przez mapowanie obiektowo-relacyjne (ORM) za pomocą środowiska Entity Framework Core.
W projekcie nie użyto jawnych wywołań bloków transakcji pokroju `BeginTransaction()`, co nie jest idealnym rozwiązaniem. Niemniej, polegamy na niejawnych transakcjach dostarczanych przez EF Core – metoda `SaveChangesAsync()` posiada wbudowaną właściwość tworzenia, operacji i commitowania w obrębie jednej wewnętrznej transakcji. 
Dzięki temu, w przypadku błędu przy dodawaniu rezerwacji (np. braku dostępności wskazanego pokoju podczas przeliczania relacji), proces zapisu nie dokonuje częściowych modyfikacji stanu – z zachowaniem cechy atomowości i spójności zgodnej z ACID.

## 8. Scenariusz wdrożenia aplikacji

Architektura oparta na .NET 9 sprawia, że aplikacja jest niezwykle przenośna. Przykładowy scenariusz produkcyjnego uruchomienia (wdrożenia) projektu w chmurze zakłada:
1. Skonteneryzowanie aplikacji przy pomocy pliku `Dockerfile` wskazującego obraz SDK dla procesu "Build" oraz wyizolowany obraz Runtime dla etapu uruchomienia.
2. Spakowanie aplikacji do obrazu kontenera na rejestrze DockerHub lub Azure Container Registry.
3. Wdrożenie kontenera jako aplikacji webowej przy użyciu Azure App Service for Containers. Zmienne konfiguracyjne (np. connection string do odseparowanego, prawdziwego środowiska SQL na serwerze i bezpieczny `Jwt:Key`) zostałyby przekazane przy starcie instancji z poziomu interfejsu zmiennych środowiskowych witryny, zabezpieczając system przed wyciekiem sekretów trzymanych w repozytorium gita.
