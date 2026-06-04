# Dokumentacja wdrożenia mechanizmu hashowania haseł (i JWT)

## 1. Wstęp
W ramach aktualizacji zabezpieczeń projektowych i rozbudowy systemu o weryfikację użytkowników (w tym wdrożenie JWT i RBAC), zaimplementowane zostało również **haszowanie haseł**. Proces ten wykonano przy użyciu natywnych mechanizmów platformy ASP.NET Core z przestrzeni `Microsoft.AspNetCore.Identity`.

## 2. Mechanizm implementacji
Logika procesu logowania i generowania bezpiecznych tokenów została ulokowana w pliku `AuthEndpoints.cs`. 

Zastosowano mechanizm `PasswordHasher<string>`, który realizuje jednokierunkowe hashowanie przy wykorzystaniu standardu PBKDF2 (z wbudowaną solą).

### Kluczowe fragmenty kodu:
Do przechowywania danych wykorzystano zdefiniowany rekord mapujący użytkowników (na potrzeby demonstracyjne działa to jako lokalna lista w pamięci):
```csharp
private sealed record UserAccount(string Username, string PasswordHash, string Role, int? GuestId = null);
private static readonly PasswordHasher<string> _passwordHasher = new();
```

Generowanie hashy na starcie systemu dla demonstracyjnej puli kont:
```csharp
private static readonly List<UserAccount> _demoUsers =
[
    new("admin", _passwordHasher.HashPassword("admin", "admin123!"), ApplicationRoles.Administrator),
    new("reception", _passwordHasher.HashPassword("reception", "reception123!"), ApplicationRoles.Receptionist),
    new("client", _passwordHasher.HashPassword("client", "client123!"), ApplicationRoles.Client, 1)
];
```
Dzięki temu rozwiązaniu **hasła nie są przechowywane w kodzie w postaci jawnej**. Są one hashowane i przetrzymywane w pamięci po inicjalizacji aplikacji.

Podejście to symuluje zachowanie produkcyjnej bazy danych. W obiekcie `UserAccount` widnieje tylko wygenerowany hash w polu `PasswordHash`.

**Uwaga:** Przedstawione rozwiązanie ma charakter demonstracyjny - dane użytkowników są inicjalizowane w pamięci aplikacji, a hasła pojawiają się w kodzie wyłącznie na etapie ich haszowania. 
W środowisku produkcyjnym hashe haseł byłyby przechowywane w bazie danych, a hasła nigdy nie znajdowałyby się w kodzie źródłowym.

### Logika weryfikacyjna:
Podczas żądania HTTP do endpointu `/api/auth/token`, przesłane zapytanie logowania jest wczytywane, a hasło poddawane weryfikacji względem zachowanego hasha przy użyciu metody z interfejsu Identity.
```csharp
var verificationResult = _passwordHasher.VerifyHashedPassword(
    user.Username, 
    user.PasswordHash, 
    login.Password);

if (verificationResult == PasswordVerificationResult.Failed)
{
    return Results.Unauthorized();
}
```

### Generowanie tokenu JWT:
Token JWT generowany po pomyślnej autoryzacji zawiera zestaw deklaracji (claims), które są następnie wykorzystywane w procesie autoryzacji:

`sub` - nazwa użytkownika (unikalny identyfikator),

`name` - nazwa użytkownika,

`role` - rola użytkownika (wykorzystywana w mechanizmie RBAC),

`jti` - unikalny identyfikator tokenu,

`guestId` - opcjonalny identyfikator klienta (dla roli `client`).

**Token:**
- posiada ograniczony czas ważności (8 godzin),
- jest podpisany przy użyciu algorytmu HMAC SHA-256,
- wykorzystuje klucz symetryczny z konfiguracji aplikacji `(appsettings.json)`.

Token jest następnie weryfikowany przez middleware ASP.NET Core (AddJwtBearer), który sprawdza jego podpis, issuer, audience oraz ważność.

## 3. Testowanie działania (Swagger)

Po konfiguracji autoryzacji i integracji z JWT (wraz z RBAC), funkcjonalność można przetestować w następujący sposób:

1. **Uruchomienie projektu**
   Po zbudowaniu i uruchomieniu projektu automatycznie otwiera się interfejs **Swagger**. Ikona kłódki przy endpointach oznacza wymóg autoryzacji (wynik konfiguracji `AddSecurityDefinition`).

2. **Pozyskanie tokena JWT**
   * Wybór publicznego endpointu `POST /api/auth/token`.
   * Przesłanie danych logowania dla jednego z kont demonstracyjnych w formacie JSON, np.:
     ```json
     {
       "username": "admin",
       "password": "admin123!"
     }
     ```
   * Wykonanie żądania zwraca kod `200 OK` oraz `accessToken` z deklaracją roli. W przypadku błędnego hasła zwracany jest błąd `401 Unauthorized`.

3. **Autoryzacja żądań**
   * Skopiowanie wartości otrzymanego `accessToken`.
   * Wybór opcji `Authorize` w interfejsie Swagger.
   * Wklejenie skopiowanej wartości z użyciem schematu: `Bearer <token>` (np. `Bearer eyJhbGc...`) i jej zatwierdzenie. 

Zabezpieczone endpointy wymagające określonych uprawnień (np. z polityką `RequireAuthorization(AuthorizationPolicies.RoomManagement)`) od tej pory zaczną przetwarzać żądania uwierzytelnione poprawnym tokenem z wymaganą rolą. 
Użycie tokena z rolą niemającą dostępu do danego zasobu (np. rola `client` wobec zasobu administracyjnego) zwraca błąd `403 Forbidden`.