## Zależności (wybrane komponenty)

Poniższa tabela przedstawia kluczowe zależności projektu, ich wersje oraz krótkie opisy.

| Komponent | Wersja | Opis |
|---|---:|---|
| .NET SDK / Runtime | net9.0 | Platforma uruchomieniowa i SDK aplikacji (Razor Pages + Minimal API). |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | Provider EF Core dla SQL Server (dostęp do bazy danych). |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 | Narzędzia projektowe EF Core (migrations, scaffolding). |
| Microsoft.EntityFrameworkCore.Tools | 9.0.0 | CLI / narzędzia dla EF Core (dotnet ef). |
| Swashbuckle.AspNetCore | 7.2.0 | Generowanie dokumentacji OpenAPI / Swagger UI. |
| SQL Server (LocalDB) | LocalDB (instancja deweloperska) | Środowiskowa instancja bazy danych używana lokalnie (connection string w appsettings.json). |
| jQuery, Bootstrap | (zależne od plików w wwwroot) | Biblioteki frontendowe używane w klientach (UI i klient API w wwwroot/js). |

---

## Wybrane przykłady podatności / istotnych błędów w poprzednich wersjach

Poniżej opisano trzy istotne problemy z punktu widzenia bezpieczeństwa, odnalezione w repozytorium lub dokumentacji projektu.

1. Słaby / wyeksponowany klucz JWT w konfiguracji

   - Opis: W `appsettings.json` znajduje się wpis `Jwt:Key` ustawiony domyślnie na `ChangeThisSecretKey_ToAtLeast32CharactersLong`. Taki wartość domyślna jest niewystarczająco bezpieczna do środowiska produkcyjnego oraz może zostać łatwo odgadnięta.
   - Skutek: Atakujący, który pozyska lub odgadnie klucz, może tworzyć własne tokeny JWT i eskalować uprawnienia (np. uzyskać rolę Administratora).
   - Mitigacja: Przechowywać klucze w bezpiecznym magazynie (np. Azure Key Vault) lub ustawiać je przez zmienne środowiskowe; używać silnych, losowych sekretów i wdrożyć rotację kluczy.

2. Brak rejestracji i wymuszenia middleware uwierzytelniania/autoryzacji (ZROBIONE / NAPRAWIONE)

   - Opis: W starszej wersji projektu w `Program.cs` brakowało wywołań `AddAuthentication(...)`, `AddAuthorization()`, `UseAuthentication()` i `UseAuthorization()`. Dokumentacja opisywała RBAC i JWT, lecz kod nie konfigurował mechanizmów zabezpieczeń.
   - Skutek: Endpointy API nie były chronione, co umożliwiało nieautoryzowany dostęp do operacji CRUD.
   - Status: Błąd został naprawiony. Aplikacja pomyślnie używa JWT Bearer (m.in. `app.UseAuthentication()`, `app.UseAuthorization()`), zdefiniowano polityki RBAC (w `Security/AuthorizationPolicies.cs`) i są one sprawdzane na endpointach (np. `RequireAuthorization(AuthorizationPolicies.StaffOrAdmin)`).

3. Przechowywanie tokenu JWT w localStorage i ryzyko XSS po stronie frontend

   - Opis: Frontend przechowuje token JWT w `localStorage` (plik `wwwroot/js/api-client.js`) i przesyła go w nagłówku `Authorization`. Przy braku polityki CSP oraz potencjalnych punktach XSS token może zostać wykradziony.
   - Skutek: Wykradziony token pozwala na przejęcie sesji użytkownika i wykonanie akcji w jego imieniu.
   - Mitigacja: Rozważyć przechowywanie tokenów w ciasteczkach HttpOnly z flagami `Secure` i `SameSite`, wdrożyć Content-Security-Policy, ograniczyć inline-scripts oraz sanityzować/escape'ować wszystkie dynamiczne dane w UI.

