# Setup projektu na nowym komputerze

## Wymagania wstępne

1. **.NET 9 SDK** — pobrać z https://dotnet.microsoft.com/download
2. **SQL Server** lub **SQL Server Express** (albo LocalDB — instaluje się z Visual Studio)
3. **Git** (jeśli klonujesz repo)
4. **Visual Studio** lub **VS Code** + terminal

---

## Krok 1: Klonowanie / Pobieranie projektu

Jeśli na GitHubie:
```bash
git clone https://github.com/chamage/TB_NowyFolder.git
cd TB_NowyFolder
```

Lub rozpakuj folder projektu.

---

## Krok 2: Zainstaluj Entity Framework Tools (globalnie)

```bash
dotnet tool install --global dotnet-ef
```

---

## Krok 3: Przywróć NuGet packages

W folderze głównym projektu (`TB_NowyFolder/`):
```bash
dotnet restore
```

---

## Krok 4: Skonfiguruj sekrety lokalne

Sekrety (connection string, klucz JWT) przechowywane są w pliku `appsettings.Local.json`, który **nie trafia do repozytorium** (plik w `.gitignore`).

Utwórz plik `TB_NowyFolder/appsettings.Local.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Jwt": {
    "Issuer": "TB_NowyFolder",
    "Audience": "TB_NowyFolder.Client",
    "Key": "Zx7Pq2Lm9Vt4Rs8Ny3Ka1Wd6Gh5BcEf0"
  }
}
```

### Opcja A: LocalDB (jeśli masz Visual Studio)
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HotelReservationDB;Trusted_Connection=True;MultipleActiveResultSets=true"
```

### Opcja B: SQL Server Express (na localhost)
```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=HotelReservationDB;Trusted_Connection=True;MultipleActiveResultSets=true"
```

### Opcja C: SQL Server z nazwą (jeśli masz instalację)
```json
"DefaultConnection": "Server=YOUR_SERVER_NAME;Database=HotelReservationDB;Trusted_Connection=True;MultipleActiveResultSets=true"
```

---

## Krok 5: Utwórz i zaktualizuj bazę danych

W folderze głównym projektu:

**Opcja A: Kasowanie starej bazy + nowa migracja**
```bash
dotnet ef database drop --project TB_NowyFolder --startup-project TB_NowyFolder --force
dotnet ef database update --project TB_NowyFolder --startup-project TB_NowyFolder
```

**Opcja B: Tylko update (jeśli baza nie istnieje)**
```bash
dotnet ef database update --project TB_NowyFolder --startup-project TB_NowyFolder
```

---

## Krok 6: Buduj i uruchamiaj

```bash
dotnet build
dotnet run --project TB_NowyFolder
```

Aplikacja powinna być dostępna pod: `https://localhost:7xxx/`

---

## Krok 7: Sprawdź, czy działa

1. Otwórz `https://localhost:7xxx/swagger` → powinna być dokumentacja API
2. Otwórz `https://localhost:7xxx/` → powinna być strona główna z loginowaniem
3. Zaloguj się demo: `admin / admin123!`

---

## Troubleshooting

### Błąd: "A network-related or instance-specific error occurred"
- Sprawdź czy SQL Server / LocalDB jest uruchomiony (w Visual Studio: SQL Server Object Explorer, albo `sqllocaldb info MSSQLLocalDB`)

### Błąd: "The required 'Microsoft.EntityFrameworkCore' package was not found"
```bash
dotnet restore
```

### Błąd przy migrations
```bash
dotnet ef migrations list --project TB_NowyFolder --startup-project TB_NowyFolder
```

Pokaż jakie migracje są dostępne.

### Błąd: "Unable to find a matching process"
- Zamknij Visual Studio i spróbuj z terminala:
```bash
dotnet ef database drop --force
dotnet ef database update
```

---

## Konta demo (po zalogowaniu się)

- `admin` / `admin123!` → Administrator
- `reception` / `reception123!` → Receptionist
- `client` / `client123!` → Client

---

## Jeśli dalej nie działa

Sprawdź:
1. Czy plik `appsettings.Local.json` istnieje i ma poprawny connection string.
2. Czy migracje zostały zastosowane (`dotnet ef database update`).
3. Czy SQL Server / LocalDB jest uruchomiony.
4. Dokładny tekst błędu z terminala (najczęściej jest tam konkretna przyczyna).
