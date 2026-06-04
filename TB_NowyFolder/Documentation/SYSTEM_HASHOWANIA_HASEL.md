# System Hashowania Haseł i Bezpieczeństwo Uwierzytelniania

W pierwszej fazie projektu uwierzytelnianie opierało się na rozwiązaniu "hardcoded" (na sztywno wpisanych loginach i hasłach w kodzie źródłowym w formie niezaszyfrowanej). Jest to podejście akceptowalne wyłącznie na etapie budowania fundamentów, ale niedopuszczalne w środowisku produkcyjnym ze względu na ryzyko ujawnienia kodów dostępu.

Docelowo w projekcie poświadczenia będą przechowywane w bazie danych (SQL Server), a wszystkie hasła będą musiały być hashowane z użyciem "soli" (salting).

## Biblioteki i Algorytmy

Zgodnie z dobrymi praktykami branżowymi w obszarze bezpieczeństwa, dla poszczególnych technologii stosuje się wbudowane lub sprawdzone biblioteki zapewniające jednokierunkowe przekształcanie haseł (hashowanie).

| Technologia | Biblioteka | Algorytmy |
| :--- | :--- | :--- |
| **Java** | `Spring Security (PasswordEncoder)` | BCrypt, Argon2, PBKDF2 |
| **Java** | `jBCrypt` | BCrypt |
| **ASP.NET Core** | `ASP.NET Identity (PasswordHasher)` | **PBKDF2** |
| **ASP.NET Core** | `BCrypt.Net-Next` | BCrypt |
| **Python** | `bcrypt` | BCrypt |
| **Python** | `passlib` | BCrypt, Argon2, PBKDF2 |
| **Node.js** | `bcrypt` | BCrypt |
| **Node.js** | `argon2` | Argon2 |

## Implementacja docelowa / Prototyp w naszym systemie

Dla naszego projektu bazującego na platformie **ASP.NET Core**, wybrano natywną bibliotekę dostarczaną wraz z ekosystemem .NET: `ASP.NET Identity (PasswordHasher)`.  Mechanizm ten domyślnie korzysta z bezpiecznego algorytmu **PBKDF2** (Password-Based Key Derivation Function 2) ze zintegrowanym systemem losowania unikalnej soli dla każdego generowanego hashu.

### Wdrożenie prototypowe

Z uwagi na to, że system docelowo zakłada bazę danych, która dopiero będzie podpięta pod moduł uwierzytelniania, przygotowano **prototyp in-memory** bezpośrednio w pliku autoryzacji (`Endpoints/AuthEndpoints.cs`), który odchodzi od jawnych haseł "hardcoded" w procesie logowania.

1. W pliku `AuthEndpoints.cs` zaimplementowano obiekt klasy `PasswordHasher<string>`.
2. Model demonstracyjny `DemoUser` przetrzymuje teraz wyłącznie zhashowaną postać hasła (pole `PasswordHash`). Hasła są hashowane "w locie" podczas inicjalizacji aplikacji za pomocą metody `HashPassword()`. Nikt (nawet podczas zrzutu pamięci) nie jest w stanie odczytać hasła w formie plain-text.
3. Podczas przesyłania żądania `POST /api/auth/token` przez użytkownika, następuje walidacja podanego hasła (tzw. check) względem zapisanego hasha za pomocą funkcji weryfikującej: `VerifyHashedPassword()`.
4. Jeśli weryfikacja się nie powiedzie, system odrzuca żądanie komunikatem `401 Unauthorized`.

Taki prototyp w pełni realizuje schemat bezpieczeństwa bazy danych i jest gotowy na tzw. "przepięcie" w fazie produkcyjnej na prawdziwe dane z `Entity Framework Core`.
