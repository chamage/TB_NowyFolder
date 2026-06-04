using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TB_NowyFolder.Data;
using TB_NowyFolder.Models;
using TB_NowyFolder.Security;

namespace TB_NowyFolder.Endpoints;

public static class AuthEndpoints
{
    private static readonly Microsoft.AspNetCore.Identity.PasswordHasher<string> Hasher = new();

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AllowAnonymous();

        // POST /api/auth/token — issue a JWT
        group.MapPost("/token", async (LoginRequest request, HotelDbContext db, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Username and password are required." });

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user is null)
                return Results.Unauthorized();

            var verificationResult = Hasher.VerifyHashedPassword(user.Username, user.PasswordHash, request.Password);
            
            if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            var token = GenerateJwtToken(user, config);

            return Results.Ok(new
            {
                accessToken = token,
                role = user.Role,
                username = user.Username,
                guestId = user.GuestID,
                expiresIn = 3600
            });
        })
        .WithName("GetAuthToken")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /api/auth/register — register a client
        group.MapPost("/register", async (RegisterRequest request, HotelDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new { error = "Username, password, first name, last name, and email are required." });
            }

            var existingUser = await db.Users.AnyAsync(u => u.Username == request.Username);
            if (existingUser)
            {
                return Results.BadRequest(new { error = "Username is already taken." });
            }

            var passwordHash = Hasher.HashPassword(request.Username, request.Password);

            var guest = new Guest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone
            };

            var user = new User
            {
                Username = request.Username,
                PasswordHash = passwordHash,
                Role = ApplicationRoles.Client,
                Guest = guest
            };

            db.Guests.Add(guest);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Registration successful." });
        })
        .WithName("RegisterClient")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/auth/me — return current user info from JWT
        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            if (user.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var role = user.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            var name = user.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var guestId = user.FindFirst("guestId")?.Value;

            return Results.Ok(new
            {
                username = name,
                role,
                guestId = guestId != null ? int.Parse(guestId) : (int?)null,
                isAuthenticated = true
            });
        })
        .WithName("GetCurrentUser")
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }

    private static string GenerateJwtToken(User user, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.GuestID.HasValue)
            claims.Add(new Claim("guestId", user.GuestID.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public record LoginRequest(string Username, string Password);
    
    public record RegisterRequest(
        string Username,
        string Password,
        string FirstName,
        string LastName,
        string Email,
        string? Phone
    );
}
