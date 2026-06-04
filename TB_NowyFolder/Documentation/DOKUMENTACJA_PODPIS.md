# Dokumentacja wdrożenia mechanizmu autentyczności i integralności danych

## 1. Wstęp
W ramach zadania zaimplementowano mechanizm weryfikacji zapewniający dwa kluczowe standardy bezpieczeństwa dla dokumentów wystawianych przez aplikację:
- **Integralność danych** - gwarantująca, że dane nie uległy nieautoryzowanej modyfikacji po wygenerowaniu przez serwer.
- **Autentyczność dokumentu** – potwierdzająca, że dokument faktycznie pochodzi ze sprawdzonego źródła (został wygenerowany przez system).

Mechanizmy te zrealizowano przy pomocy wbudowanych przestrzeni nazw `.NET System.Security.Cryptography`, opierając się na asymetrycznej kryptografii RSA i funkcji hashującej SHA-256 (rozwiązanie typu "odłączony podpis"). Wdrożono je w formie izolowanego interfejsu testowego "Weryfikator dokumentów".

## 2. Architektura rozwiązania

### Logika kryptograficzna
Główną warstwę obsługi stanowi dedykowana klasa `DigitalSignatureService`, użyta w kontenerze DI (Dependency Injection) jako Singleton. 
Podczas startu usługi generowana jest pojedyncza para kluczy: klucz prywatny oraz klucz publiczny RSA (o rozmiarze 2048 bitów). Takie podejście zapobiega utracie kluczy w trakcie działania procesu oraz minimalizuje narzut obliczeniowy, co jest wystarczające dla środowiska demonstracyjnego.

Składają się na nią dwie główne metody:
1. `SignData(string payload)` - służy do procesowania tekstowej, surowej formy dokumentu (np. formatu JSON). Aplikacja wylicza z niej skrót (hash) za pomocą SHA-256 i tworzy podpis cyfrowy dla tego skrótu przy użyciu klucza prywatnego RSA. Zwraca ciąg tekstowy (Base64) stanowiący cyfrową sygnaturę wiadomości.

2. `VerifySignature(string payload, string signatureBase64)` - przyjmuje sprawdzane dane tekstowe (payload) oraz deklarowaną sygnaturę. Weryfikuje podpis cyfrowy przy użyciu klucza publicznego RSA oraz porównuje go z hashem obliczonym dla przekazanych danych. Jakakolwiek różnica (np. modyfikacja tekstu, użycie niewłaściwego klucza prywatnego) skutkuje odrzuceniem weryfikacji (wynik `false`).

### Integracja z API Web

W pliku `Endpoints/DocumentEndpoints.cs` zaimplementowano i mapowano poniższe endpointy:
- `GET /api/documents/generate/{id}` - symuluje wystawienie rachunku dla istniejącej rezerwacji o wskazanym ID. Generuje tekstową odpowiedź JSON wraz z obliczoną na jej podstawie sygnaturą cyfrową.
- `POST /api/documents/verify` - metoda sprawdzająca zgodność przesłanego w ciele zapytania tekstu oraz sygnatury i zwracająca stan autoryzacji (wartość logiczną).

Dla zilustrowania funkcjonalności udostępniono dedykowany widok w interfejsie webowym (`Pages/VerifyDocument.cshtml`).

### Zintegrowany przepływ danych (Data flow)
Mechanizm wykorzystuje sekwencyjny przepływ danych. 
Dane wprowadzone w widoku `VerifyDocument.cshtml` wysyłane są do odpowiedniego endpointu API aplikacji. 
Następnie `HotelDbContext` pobiera rekord rezerwacji i asynchronicznie (nie blokując działania aplikacji) wywołuje serwis `DigitalSignatureService`. 
Serwis ten bezpośrednio w pamięci operacyjnej serwera wykonuje operacje kryptograficzne. 
Po zakończeniu operacji interfejs API odpowiada na żądanie, dołączając treść dokumentu i powiązany z nim podpis cyfrowy z powrotem na ekran administratora.

## 3. Instrukcja weryfikacji działania (z perspektywy użytkownika)

Interfejs aplikacji zaprojektowano w sposób upraszczający proces weryfikacji dokumentu, który ukrywa przed użytkownikiem szczegóły operacji kryptograficznych.

1. **Uruchomienie** - po wejściu w zabezpieczoną zakładkę "Weryfikator" (operacja wymaga posiadania uprawnień w roli Administrator), ekran dzieli się na strefy wystawiania oraz weryfikacji dokumentu.
2. **Proces wystawienia dokumentu** - w wyznaczonym polu należy podać identyfikator wybranej rezerwacji (np. "1") i rozpocząć generowanie przypisanym przyciskiem. System pobiera informacje powiązane z klientem, tworzy z danych dokument JSON, następnie oblicza hash SHA-256 (integralność), po czym generowany jest podpis cyfrowy z użyciem klucza prywatnego RSA (autentyczność).
3. **Proces sprawdzenia (Audyt)** - interfejs webowy udostępnia gotową treść dokumentu z powiązaną sygnaturą. W celu przeprowadzenia weryfikacji należy zestawić te informacje w dedykowanym formularzu. Po zatwierdzeniu żądania system oblicza hash przekazanego tekstu i weryfikuje podpis cyfrowy za pomocą klucza publicznego. Jeżeli wartości są zgodne, system potwierdza poprawną integralność dokumentu.
4. **Wykrycie manipulacji** - przetestowanie mechanizmów bezpieczeństwa polega na modyfikacji chociażby jednego znaku w treści dokumentu dostępnego w polu weryfikacyjnym przy równoczesnym wykorzystaniu oryginalnego podpisu. Wymuszenie weryfikacji poskutkuje wygenerowaniem komunikatu o błędzie, ponieważ hash zmodyfikowanego dokumentu nie odpowiada wartości zapisanej w podpisie cyfrowym wejściowego pliku.

Mechanizm ten symuluje rzeczywiste systemy chroniące elektroniczny obieg dokumentów i został celowo oparty na formacie tekstowym bez wykorzystania plików binarnych (jak np. Portable Document Format - PDF), aby zachować maksymalną zrozumiałość procesu i pominąć integracje obszernych zestawów bibliotek firm trzecich. Rozwiązanie poprawnie spełnia zadane wymagania projektowe dotyczące weryfikacji integralności oraz autentyczności dokumentów, posługując się natywnymi narzędziami bibliotek platformy ASP.NET Core.

---

## 4. Perspektywa produkcyjna – systemy komercyjne

Obecne rozwiązanie stanowi poprawne pod względem matematycznym i logicznym narzędzie demonstracyjne (Proof of Concept). Adaptacja tej techniki w środowiskach produkcyjnych, przetwarzających dokumentację dla setek użytkowników, z reguły obejmuje implementację trzech zaawansowanych modyfikacji powiązanych z wygodą i mocą prawną:

**1. Integracja z PDF i kodami QR**
Zamiast generować odpowiedź tekstową do podglądu z sygnaturą obok, cały proces integruje się bezpośrednio z generatorem raportów. Dokument (np. faktura) wystawiany jest w formacie PDF, a serwer osadza wyliczony podpis cyfrowy w strukturze pliku zgodnie ze standardem PAdES. Dzięki temu niezależne oprogramowanie (np. Adobe Reader) powiadamia użytkownika o poprawnej weryfikacji sygnatury bezpośrednio po otwarciu dokumentu.
Alternatywną metodą jest użycie kodów QR drukowanych na odpowiednikach papierowych. Skaner kodu może wywołać adres sprzężony z API weryfikacyjnym (np. `?payload=&sig=`), dając możliwość bezpośredniego potwierdzenia autentyczności dokumentu.

**2. Zewnętrzne magazyny kluczy (Key Vault)**
W tym projekcie, klucz prywatny generowany jest ulotnie w pamięci RAM. Aby zapewnić trwałość oraz bezpieczeństwo podpisu i uchronić środowisko przed wyciekiem, autoryzacje produkcyjne wykonuje się, wykorzystując platformy chmurowe (np. Azure Key Vault) lub dedykowane moduły sprzętowe (HSM - Hardware Security Module). Zewnętrzne magazyny kluczy skutecznie chronią i separują zasoby przed utratą.

**3. Certyfikaty kwalifikowane (Podstawa prawna)**
Opisywany w raporcie prototyp opiera się na poprawnym kryptograficznie, jednakże niekwalifikowanym mechanizmie wykorzystującym samopodpisany klucz testowy. W systemach komercyjnych współpracujących m.in. ze środowiskami państwowymi (np. przy generowaniu e-faktur KSeF), użyty klucz publiczny potwierdzany jest certyfikatem kwalifikowanym, gwarantując potwierdzenie tożsamości podmiotu przypisanego do danego dokumentu. Certyfikat taki wymaga nadania przez zaufanego dostawcę usług zaufania (np. Certum, KIR), spełniającego rygorystyczne normy eIDAS. Wdrożenie zaprezentowanego tu kodu do środowiska produkcyjnego wymagałoby wyłącznie wczytywania certyfikatów i kluczy dostarczonych przez wskazanego kwalifikowanego dostawcę zamiast tworzenia ich samodzielnie.
