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

    private sealed record UserAccount(string Username, string Password, string Role, int? GuestId = null);

    private static readonly List<UserAccount> DemoUsers =
    [
        new("admin", "admin123!", ApplicationRoles.Administrator),
        new("reception", "reception123!", ApplicationRoles.Receptionist),
        new("client", "client123!", ApplicationRoles.Client, 1)
    ];

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/token", ([FromBody] LoginRequest login, IConfiguration configuration) =>
        {
            var user = DemoUsers.FirstOrDefault(u =>
                u.Username.Equals(login.Username, StringComparison.OrdinalIgnoreCase)
                && u.Password == login.Password);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            var jwtSection = configuration.GetSection("Jwt");
            var issuer = jwtSection["Issuer"] ?? "TB_NowyFolder";
            var audience = jwtSection["Audience"] ?? "TB_NowyFolder.Client";
            var key = jwtSection["Key"] ?? "ReplaceThisWithAStrongKey_AtLeast32Chars";

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
