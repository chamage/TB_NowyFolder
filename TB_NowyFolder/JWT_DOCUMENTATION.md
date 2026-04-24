# Dokumentacja JWT — Mechanizm tokenów i kontrola dostępu RBAC

## 1. Czym jest JWT?

**JSON Web Token (JWT)** to otwarty standard (RFC 7519) definiujący sposób bezpiecznego przekazywania informacji między stronami jako obiekt JSON. Token jest cyfrowo podpisany, więc może być zweryfikowany i zaufany.

Token JWT składa się z trzech części oddzielonych kropkami:

```
HEADER.PAYLOAD.SIGNATURE
```

| Część | Opis |
|---|---|
| **Header** | Algorytm podpisu (`HS256`) i typ tokenu (`JWT`) |
| **Payload** | Dane użytkownika — tzw. **claims** (nazwa, rola, czas wygaśnięcia) |
| **Signature** | Podpis cyfrowy zapewniający integralność tokenu |

Przykładowy token (skrócony):
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJBZG1pbmlzdHJhdG9yIiwiZXhwIjoxNzE5MjQ1MjAwfQ.abc123signature
```

---

## 2. Konfiguracja JWT w projekcie

### 2.1 Ustawienia — `appsettings.json`

```json
{
  "Jwt": {
    "Issuer": "TB_NowyFolder",
    "Audience": "TB_NowyFolder.Client",
    "Key": "ChangeThisSecretKey_ToAtLeast32CharactersLong"
  }
}
```

| Parametr | Znaczenie |
|---|---|
| `Issuer` | Kto wystawia token (nazwa serwera/aplikacji) |
| `Audience` | Dla kogo jest token (klient API) |
| `Key` | Klucz symetryczny do podpisywania tokenu (min. 32 znaki) |

### 2.2 Rejestracja middleware — `Program.cs`

W pliku `Program.cs` konfiguracja JWT przebiega w dwóch etapach:

**Etap 1 — Rejestracja schematu uwierzytelniania:**
```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});
```

**Etap 2 — Aktywacja middleware w potoku HTTP:**
```csharp
app.UseAuthentication();   // ← sprawdza token
app.UseAuthorization();    // ← sprawdza uprawnienia
```

> **Kolejność jest istotna** — `UseAuthentication()` musi być przed `UseAuthorization()`, a oba przed mapowaniem endpointów.

### 2.3 Wymagany pakiet NuGet

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
```

---

## 3. Generowanie tokenu — `POST /api/auth/token`

### 3.1 Request

```http
POST /api/auth/token
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123!"
}
```

### 3.2 Response (sukces — 200 OK)

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "Administrator",
  "username": "admin",
  "guestId": null,
  "expiresIn": 3600
}
```

### 3.3 Response (błąd — 401 Unauthorized)

Zwracany, gdy login lub hasło są nieprawidłowe.

### 3.4 Kod generujący token — `AuthEndpoints.cs`

```csharp
private static string GenerateJwtToken(DemoUser user, IConfiguration config)
{
    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, user.Username),        // nazwa użytkownika
        new(ClaimTypes.Role, user.Role),             // ← ROLA (klucz RBAC)
        new(JwtRegisteredClaimNames.Sub, user.Username),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    if (user.GuestId.HasValue)
        claims.Add(new Claim("guestId", user.GuestId.Value.ToString()));

    var token = new JwtSecurityToken(
        issuer:             config["Jwt:Issuer"],
        audience:           config["Jwt:Audience"],
        claims:             claims,
        expires:            DateTime.UtcNow.AddHours(1),  // ← ważność 1h
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### 3.5 Struktura Payload tokenu

Po zdekodowaniu tokenu (np. na https://jwt.io) widoczne są claims:

```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "admin",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Administrator",
  "sub": "admin",
  "jti": "a1b2c3d4-e5f6-...",
  "exp": 1719245200,
  "iss": "TB_NowyFolder",
  "aud": "TB_NowyFolder.Client"
}
```

| Claim | Opis |
|---|---|
| `name` | Nazwa użytkownika |
| `role` | **Rola** — kluczowy claim do RBAC |
| `sub` | Subject — identyfikator użytkownika |
| `jti` | Unikalny identyfikator tokenu |
| `guestId` | (opcjonalnie) ID gościa powiązanego z kontem klienta |
| `exp` | Data wygaśnięcia (Unix timestamp) |
| `iss` / `aud` | Wystawca / odbiorca — walidowane przez serwer |

---

## 4. Weryfikacja tokenu przez endpointy

### 4.1 Wysyłanie tokenu w nagłówku HTTP

Każde żądanie do zabezpieczonego endpointu musi zawierać nagłówek:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 4.2 Co sprawdza serwer?

Middleware `JwtBearer` automatycznie weryfikuje:

1. **Podpis** — czy token nie został zmodyfikowany (klucz `Jwt:Key`)
2. **Issuer** — czy `iss` w tokenie = `Jwt:Issuer` z konfiguracji
3. **Audience** — czy `aud` w tokenie = `Jwt:Audience` z konfiguracji
4. **Lifetime** — czy `exp` nie minął (token nie wygasł)

Jeśli weryfikacja się nie powiedzie → **401 Unauthorized**.

### 4.3 Kody odpowiedzi HTTP

| Kod | Znaczenie |
|---|---|
| `200` / `201` / `204` | Sukces — token ważny, rola wystarczająca |
| `401 Unauthorized` | Brak tokenu lub token nieprawidłowy / wygasły |
| `403 Forbidden` | Token ważny, ale **rola nie pozwala** na tę operację |

---

## 5. Role i polityki RBAC

### 5.1 Zdefiniowane role — `Security/ApplicationRoles.cs`

```csharp
public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string Receptionist  = "Receptionist";
    public const string Client        = "Client";
}
```

### 5.2 Polityki autoryzacji — `Security/AuthorizationPolicies.cs`

```csharp
.AddPolicy("AdminOnly", policy =>
    policy.RequireRole("Administrator"))

.AddPolicy("StaffOrAdmin", policy =>
    policy.RequireRole("Receptionist", "Administrator"))

.AddPolicy("AuthenticatedUser", policy =>
    policy.RequireAuthenticatedUser())
```

### 5.3 Zabezpieczanie endpointów

```csharp
// Publiczny — każdy może odczytać ofertę pokoi
group.MapGet("/", ...).AllowAnonymous();

// Tylko Administrator — np. dodawanie pokoi
group.MapPost("/", ...).RequireAuthorization("AdminOnly");

// Staff lub Admin — np. lista gości
var group = app.MapGroup("/api/guests")
    .RequireAuthorization("StaffOrAdmin");
```

### 5.4 Macierz uprawnień

| Zasób | Metoda | Anonymous | Client | Receptionist | Administrator |
|---|---|---|---|---|---|
| `/api/rooms` | GET | ✅ | ✅ | ✅ | ✅ |
| `/api/rooms` | POST/PUT/DELETE | ❌ | ❌ | ❌ | ✅ |
| `/api/roomtypes` | GET | ✅ | ✅ | ✅ | ✅ |
| `/api/roomtypes` | POST/PUT/DELETE | ❌ | ❌ | ❌ | ✅ |
| `/api/services` | GET | ✅ | ✅ | ✅ | ✅ |
| `/api/services` | POST/PUT/DELETE | ❌ | ❌ | ❌ | ✅ |
| `/api/guests` | GET/POST/PUT/DELETE | ❌ | ❌ | ✅ | ✅ |
| `/api/reservations` | GET (wszystkie) | ❌ | ❌ | ✅ | ✅ |
| `/api/reservations/my` | GET (własne) | ❌ | ✅ | ❌ | ❌ |
| `/api/reservations` | POST/PUT/DELETE | ❌ | ✅ | ✅ | ✅ |
| `/api/auth/token` | POST | ✅ | ✅ | ✅ | ✅ |
| `/api/auth/me` | GET | ❌ | ✅ | ✅ | ✅ |

---

## 6. Konta demonstracyjne

| Login | Hasło | Rola | Powiązany gość |
|---|---|---|---|
| `admin` | `admin123!` | Administrator | — |
| `reception` | `reception123!` | Receptionist | — |
| `client` | `client123!` | Client | Guest ID = 1 (John Doe) |

> **Uwaga:** W wersji produkcyjnej konta byłyby przechowywane w bazie danych z haszowanymi hasłami. Powyższe konta są hardcoded wyłącznie na potrzeby demonstracji.

---

## 7. Demonstracja działania — scenariusze testowe

### Scenariusz 1 — Logowanie i uzyskanie tokenu (Swagger)

1. Otworzyć `/swagger`
2. Znaleźć `POST /api/auth/token`
3. Kliknąć **Try it out**
4. Wpisać body:
   ```json
   { "username": "admin", "password": "admin123!" }
   ```
5. Kliknąć **Execute**
6. **Wynik**: `200 OK` — w odpowiedzi pole `accessToken` zawiera token JWT
7. Skopiować wartość `accessToken`

### Scenariusz 2 — Autoryzacja w Swagger UI

1. Kliknąć przycisk **Authorize** (na górze Swaggera)
2. Wkleić skopiowany token (bez „Bearer ", sam token)
3. Kliknąć **Authorize** → **Close**
4. Od teraz wszystkie żądania będą wysyłane z nagłówkiem `Authorization: Bearer <token>`

### Scenariusz 3 — Żądanie bez tokenu → 401

1. **Wyloguj się** ze Swaggera (kliknij Authorize → Logout)
2. Spróbuj wywołać `GET /api/guests`
3. **Wynik**: `401 Unauthorized` — endpoint wymaga uwierzytelnienia

### Scenariusz 4 — Żądanie z nieodpowiednią rolą → 403

1. Zaloguj się jako `client` / `client123!` → skopiuj token → Authorize
2. Spróbuj wywołać `GET /api/guests`
3. **Wynik**: `403 Forbidden` — klient nie ma roli `Receptionist` ani `Administrator`

### Scenariusz 5 — Żądanie z odpowiednią rolą → 200

1. Zaloguj się jako `admin` / `admin123!` → skopiuj token → Authorize
2. Wywołaj `GET /api/guests`
3. **Wynik**: `200 OK` — lista gości zwrócona poprawnie

### Scenariusz 6 — Publiczny odczyt oferty (bez tokenu)

1. **Bez tokenu** wywołaj `GET /api/rooms`
2. **Wynik**: `200 OK` — publiczny dostęp do oferty pokoi

### Scenariusz 7 — Klient tworzy rezerwację (Frontend)

1. Na stronie głównej kliknij demo **Client** → **Sign In**
2. Przejdź do zakładki **Reservations** → kliknij **+ New Booking**
3. Pole „Guest" jest automatycznie ustawione na konto klienta (z claimu `guestId` w JWT)
4. Wypełnij daty i zatwierdź
5. **Wynik**: Rezerwacja utworzona — klient widzi tylko swoje rezerwacje (`/api/reservations/my`)

### Scenariusz 8 — Weryfikacja tokenu (endpoint `/api/auth/me`)

1. Zaloguj się w Swaggerze z dowolnym tokenem
2. Wywołaj `GET /api/auth/me`
3. **Wynik**: Serwer odczytuje claims z tokenu i zwraca:
   ```json
   {
     "username": "admin",
     "role": "Administrator",
     "guestId": null,
     "isAuthenticated": true
   }
   ```
4. To potwierdza, że serwer prawidłowo **weryfikuje i dekoduje** token JWT

---

## 8. Diagram przepływu uwierzytelniania

```
Klient                         Serwer API
  │                                 │
  │  POST /api/auth/token           │
  │  { username, password }         │
  │────────────────────────────────▶│
  │                                 │  Weryfikacja danych logowania
  │                                 │  Generowanie tokenu JWT
  │  200 OK                         │  (z claimami: name, role, guestId)
  │  { accessToken: "eyJ..." }      │
  │◀────────────────────────────────│
  │                                 │
  │  GET /api/guests                │
  │  Authorization: Bearer eyJ...   │
  │────────────────────────────────▶│
  │                                 │  1. Walidacja podpisu tokenu
  │                                 │  2. Sprawdzenie iss, aud, exp
  │                                 │  3. Odczyt roli z claimów
  │                                 │  4. Porównanie z polityką RBAC
  │                                 │
  │  200 OK / 401 / 403             │
  │◀────────────────────────────────│
```

---

## 9. Pliki źródłowe związane z JWT/RBAC

| Plik | Odpowiedzialność |
|---|---|
| `appsettings.json` | Konfiguracja JWT (Issuer, Audience, Key) |
| `Program.cs` | Rejestracja JWT Auth, polityk RBAC, middleware |
| `Security/ApplicationRoles.cs` | Definicje ról (`Administrator`, `Receptionist`, `Client`) |
| `Security/AuthorizationPolicies.cs` | Polityki: `AdminOnly`, `StaffOrAdmin`, `AuthenticatedUser` |
| `Endpoints/AuthEndpoints.cs` | Generowanie tokenu (`POST /token`) i weryfikacja (`GET /me`) |
| `Endpoints/*Endpoints.cs` | Zabezpieczenia `.RequireAuthorization(...)` na każdym endpoincie |
| `wwwroot/js/api-client.js` | Frontend: wysyłanie `Authorization: Bearer <token>` w nagłówku |

---

## 10. Podsumowanie

Mechanizm JWT w projekcie obejmuje pełny cykl:

1. **Generowanie** — endpoint `POST /api/auth/token` tworzy token z claimami (nazwa, rola, guestId)
2. **Przesyłanie** — klient dołącza token w nagłówku `Authorization: Bearer ...`
3. **Weryfikacja** — middleware `JwtBearer` waliduje podpis, wystawcę, odbiorcę i czas ważności
4. **Autoryzacja** — polityki RBAC sprawdzają rolę użytkownika z claimu `role` w tokenie
5. **Odmowa** — `401` gdy brak/nieprawidłowy token, `403` gdy rola nie pozwala na operację

Token zawiera informację o roli użytkownika, co pozwala na kontrolę dostępu bez odpytywania bazy danych przy każdym żądaniu.
