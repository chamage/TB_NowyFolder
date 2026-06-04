# Krótki scenariusz prezentacji — REST API + RBAC

## 1) Wprowadzenie (30–45 sek)
"Projekt to system rezerwacji hotelowej w `ASP.NET Core (.NET 9)` na `Razor Pages` + Minimal API. Celem zadania było zaprojektowanie REST API oraz kontroli dostępu RBAC (role i uprawnienia)."

---

## 2) Co było wymagane w zadaniu (15–20 sek)
- zdefiniować zasoby API,
- przygotować endpointy CRUD (`GET`, `POST`, `PUT`, `DELETE`),
- wdrożyć RBAC,
- opisać endpointy, role i request/response.

---

## 3) Zasoby API w projekcie (20 sek)
W projekcie są zasoby:
- `guests`
- `rooms`
- `roomtypes`
- `services`
- `reservations`
- `auth` (wydawanie JWT)

To pokrywa wymaganie struktury zasobów.

---

## 4) RBAC — role i logika dostępu (45 sek)
Role:
- `Client`
- `Receptionist`
- `Administrator`
- `Anonymous` (tylko odczyt oferty)

Najważniejsze zasady:
- publiczne odczyty oferty: pokoje/typy/usługi,
- zarządzanie katalogiem (`POST/PUT/DELETE` pokoi, typów, usług): tylko `Administrator`,
- goście (`/api/guests`): `Receptionist` i `Administrator`,
- rezerwacje:
  - odczyt wszystkich: `Receptionist`/`Administrator`,
  - modyfikacje: `Client`/`Receptionist`/`Administrator`,
  - klient ma własny endpoint: `GET /api/reservations/my`.

---

## 5) Co zostało dodane technicznie (1 min)
### Backend
- JWT Authentication + Authorization w `Program.cs`,
- polityki RBAC w `Security/AuthorizationPolicies.cs`,
- role w `Security/ApplicationRoles.cs`,
- endpoint logowania `POST /api/auth/token` w `Endpoints/AuthEndpoints.cs`,
- zabezpieczenia `.RequireAuthorization(...)` na endpointach,
- w `ReservationEndpoints`:
  - dodany `GET /api/reservations/my` dla klienta,
  - tworzenie rezerwacji przez klienta przypina `GuestID` z claimu `guestId`,
  - poprawione ładowanie danych szczegółów rezerwacji (`RoomType` przez `ThenInclude`).

### Frontend (Razor Pages)
- panel logowania na `Index` (demo konta),
- przechowywanie tokena JWT w `localStorage`,
- wysyłanie `Authorization: Bearer <token>` do API,
- ukrywanie/pokazywanie opcji UI wg roli,
- klient widzi i obsługuje własne rezerwacje.

---

## 6) Szybkie demo na zajęcia (2–3 min)
1. **Swagger**: `POST /api/auth/token` (np. admin) → kopiuję `accessToken`.
2. Klikam **Authorize** i wklejam `Bearer <token>`.
3. Pokazuję endpoint z kłódką:
   - bez tokena: `401`,
   - z nieodpowiednią rolą: `403`,
   - z poprawną rolą: sukces (`200/201/204`).
4. **Frontend**:
   - logowanie jako `client`,
   - utworzenie własnej rezerwacji,
   - wejście w szczegóły rezerwacji i pokazanie, że "Type" pokoju już się poprawnie wyświetla.

---

## 7) Gotowe hasło kończące prezentację (10 sek)
"Projekt spełnia wymagania zadania: ma zasoby i CRUD, poprawnie wdrożone RBAC, endpoint logowania JWT, udokumentowane role/uprawnienia i działające testy w Swaggerze oraz na frontendzie."