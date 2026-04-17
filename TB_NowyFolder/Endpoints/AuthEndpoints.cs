using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TB_NowyFolder.Security;

namespace TB_NowyFolder.Endpoints;

public static class AuthEndpoints
{
    private sealed record LoginRequest(string Username, string Password);

    // Przechowujemy zhashowane hasło w miejsce czystego tekstu
    private sealed record UserAccount(string Username, string PasswordHash, string Role, int? GuestId = null);

    private static readonly PasswordHasher<string> _passwordHasher = new();

    private static readonly List<UserAccount> _demoUsers =
    [
        new("admin", _passwordHasher.HashPassword("admin", "admin123!"), ApplicationRoles.Administrator),
        new("reception", _passwordHasher.HashPassword("reception", "reception123!"), ApplicationRoles.Receptionist),
        new("client", _passwordHasher.HashPassword("client", "client123!"), ApplicationRoles.Client, 1)
    ];

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/token", ([FromBody] LoginRequest login, IConfiguration configuration) =>
        {
            // Znajdujemy użytkownika po nazwie konta
            var user = _demoUsers.FirstOrDefault(u =>
                u.Username.Equals(login.Username, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                return Results.Unauthorized();
            }

            // Weryfikacja przysłanego hasła względem hasha korzystając z PasswordHasher'a
            var verificationResult = _passwordHasher.VerifyHashedPassword(user.Username, user.PasswordHash, login.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            // Pomyślne uwierzytelnienie - generowanie JWT
            var jwtSection = configuration.GetSection("Jwt");
            var issuer = jwtSection["Issuer"] ?? "TB_NowyFolder";
            var audience = jwtSection["Audience"] ?? "TB_NowyFolder.Client";
            var key = jwtSection["Key"] ?? "ReplaceThisWithAStrongKey_AtLeast32Chars_123!";

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Username),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (user.GuestId.HasValue)
            {
                claims.Add(new Claim("guestId", user.GuestId.Value.ToString()));
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(8);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

            return Results.Ok(new
            {
                accessToken = tokenValue,
                tokenType = "Bearer",
                expiresAt,
                role = user.Role,
                username = user.Username
            });
        })
        .AllowAnonymous()
        .WithName("CreateAccessToken")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
