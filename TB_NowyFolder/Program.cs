using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TB_NowyFolder.Data;
using TB_NowyFolder.Endpoints;
using TB_NowyFolder.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add Database Context
builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"] ?? "TB_NowyFolder";
var jwtAudience = jwtSection["Audience"] ?? "TB_NowyFolder.Client";
var jwtKey = jwtSection["Key"] ?? "ReplaceThisWithAStrongKey_AtLeast32Chars";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.GuestManagement, policy =>
        policy.RequireRole(ApplicationRoles.Receptionist, ApplicationRoles.Administrator));

    options.AddPolicy(AuthorizationPolicies.RoomManagement, policy =>
        policy.RequireRole(ApplicationRoles.Administrator));

    options.AddPolicy(AuthorizationPolicies.RoomTypeManagement, policy =>
        policy.RequireRole(ApplicationRoles.Administrator));

    options.AddPolicy(AuthorizationPolicies.ServiceManagement, policy =>
        policy.RequireRole(ApplicationRoles.Administrator));

    options.AddPolicy(AuthorizationPolicies.ReservationRead, policy =>
        policy.RequireRole(ApplicationRoles.Receptionist, ApplicationRoles.Administrator));

    options.AddPolicy(AuthorizationPolicies.ReservationCreateOrUpdate, policy =>
        policy.RequireRole(ApplicationRoles.Client, ApplicationRoles.Receptionist, ApplicationRoles.Administrator));
});

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Reservation API",
        Version = "v1",
        Description = "API for managing hotel reservations, guests, rooms, and services"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Reservation API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseRouting();

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

app.Run();
