# Opis projektu - technologie i bezpieczeństwo

Projekt to funkcjonalny system do zarządzania pobytami w **gospodarstwie agroturystycznym**. Aplikacja ma za zadanie zinformatyzować proces ofertowy (definiowanie pokoi i dodatkowych usług) oraz ułatwić zarządzanie i rezerwowanie noclegów zarówno z perspektywy gościa, jak i obsługi obiektu.

## 1. Wykorzystane technologie

Projekt oparty jest na stosie technologicznym platformy .NET, zapewniającym budowę skalowalnych usług webowych.

### Warstwa serwerowa i dostęp do danych
- **.NET 9 / ASP.NET Core API** - technologia wykorzystana do implementacji backendu. Umożliwia tworzenie punktów dostępowych (endpointów) HTTP obsługujących operacje CRUD.
- **Entity Framework Core (EF Core 9.0)** - mechanizm mapowania obiektowo-relacyjnego (ORM). Zarządza procesem komunikacji z bazą danych i pozwala na tworzenie struktury tabel z poziomu kodu (tzw. podejście Code-First) wraz z systemem migracji.
- **Microsoft SQL Server** - system zarządzania relacyjną bazą danych używany w projekcie do utrwalania informacji o pokojach, rezerwacjach oraz gościach.

### Warstwa prezentacji i dokumentacji
- **ASP.NET Core Razor Pages** - model generowania interfejsu użytkownika po stronie serwera.
- **Swashbuckle.AspNetCore (Swagger)** - narzędzie zintegrowane w celu automatycznego wygenerowania wizualnego interfejsu (UI) dokumentacji. Opcja ta jest traktowana aktualnie jako główne narzędzie do ręcznego testowania całego API.

---

## 2. Kwestie bezpieczeństwa (i planowana autoryzacja)

W obecnej wersji projektu (na etapie realizacji zadania wstępnego) aplikacja udostępniająca REST API jest traktowana jako model otwarty - **nie posiada zaimplementowanych zabezpieczeń oraz logowania**. Narzędzie Swagger pozwala na pełny dostęp do wszystkich metod bez konieczności poświadczania tożsamości.

Jest to etap przejściowy. W docelowej architekturze, przewidzianej na kolejne fazy zadania projektowego, wprowadzony zostanie odpowiedni model zapewniający kontrolę dostępu i weryfikowanie uprawnień, który zakłada:

### Planowane rozwiązania ochronne (w ramach modelu RBAC)

1. **Uwierzytelnianie oparte o tokeny JSON (JWT)**
   Planowane jest zastosowanie wbudowanych bibliotek autoryzacyjnych wewnątrz warstwy middleware. Żądanie skierowane na punkt dostępowy związany z systemem autoryzacji poskutkuje wygenerowaniem tymczasowego Tokena, który będzie podstawą weryfikacji tożsamości przy kolejnych operacjach.

2. **Poziom Ról Zabezpieczeń (RBAC)**
   Punkty dostępowe w API zostaną zabezpieczone blokadą (Authorization Policy). Model bazy danych i aplikacji przewiduje występowanie odrębnych ról posiadających specyficzne przywileje do poszczególnych widoków/danych:
   - **Administrator** - całkowity dostęp operacyjny do baz usług, typów, pokojów oraz bazy gości czy rezerwacji (zarządzenie słownikami i CRUD wszystkich tablic).
   - **Recepcjonista** - dostęp odczytowy w kwestii katalogów systemowych i częściowy dostęp zapisowy przy bieżącej obsłudze np. listy rezerwacji, bądź rejestracji fizycznego gościa.
   - **Klient** - ograniczony profil, który nie będzie zarządzał niczym poza własnym identyfikatorem konta i tworzeniem na jego bazie rezerwacji noclegu oraz usług.
   - **Brak uwierzytelnienia (Gość)** - zachowany zostanie jedynie dostęp bezpieczny (np. `GET`), umożliwiający listowanie dostępnej oferty.

Wdrożenie powyższych podpunktów jest następnym naturalnym krokiem realizacyjnym.
