using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TB_NowyFolder.Data;
using TB_NowyFolder.Endpoints;
using TB_NowyFolder.Security;

var builder = WebApplication.CreateBuilder(args);

// Kolejność ładowania konfiguracji (każdy następny nadpisuje poprzedni):
//   appsettings.json -> appsettings.{Env}.json -> appsettings.Local.json -> zmienne środowiskowe
// appsettings.Local.json zawiera lokalne sekrety i jest wykluczone z repo (.gitignore)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Bearer - middleware waliduje token przy każdym żądaniu.
// Wszystkie cztery flagi muszą się zgadzać, żeby token przeszedł weryfikację.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        // Ten sam klucz co przy podpisywaniu - niezgodność = odrzucenie tokenu.
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// Rejestracja polityk RBAC (AdminOnly, StaffOrAdmin, AuthenticatedUser)
builder.Services.AddHotelAuthorizationPolicies();

// Singleton - jeden klucz RSA na całą sesję. Po restarcie wcześniejsze podpisy stają się nieweryfikowalne.
builder.Services.AddSingleton<DigitalSignatureService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Reservation API",
        Version = "v1",
        Description = "API for managing hotel reservations, guests, rooms, and services. Secured with JWT + RBAC."
    });

    // Obsługa JWT w Swagger UI - można testować chronione endpointy bez Postmana.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGciOi..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS otwarty na wszystkie domeny - tylko dla lokalnego developmentu.
// Przed produkcją zmienić na WithOrigins("https://konkretna-domena.pl")
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Nieobsłużone wyjątki trafiają na /Error - stack trace nie wychodzi do klienta.
    app.UseExceptionHandler("/Error");
    // HSTS - przeglądarka wymusza HTTPS przez określony czas.
    app.UseHsts();
}

// Swagger tylko w trybie deweloperskim - w produkcji dokumentacja nie jest dostępna.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Reservation API v1");
        options.RoutePrefix = "swagger";
    });
}

// Wymusza HTTPS - przeglądarka musi używać bezpiecznego połączenia.
app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

// Ważna kolejność: Authentication przed Authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapAuthEndpoints();
app.MapGuestEndpoints();
app.MapRoomTypeEndpoints();
app.MapRoomEndpoints();
app.MapServiceEndpoints();
app.MapReservationEndpoints();
app.MapDocumentEndpoints();

app.Run();
