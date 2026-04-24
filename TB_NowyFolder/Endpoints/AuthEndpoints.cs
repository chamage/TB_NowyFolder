using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TB_NowyFolder.Security;

namespace TB_NowyFolder.Endpoints;

public static class AuthEndpoints
{
    /// <summary>
    /// Demo user accounts for presentation purposes.
    /// In production, these would come from a database.
    /// </summary>
    private record DemoUser(string Username, string Password, string Role, int? GuestId = null);

    private static readonly DemoUser[] DemoUsers =
    [
        new("admin", "admin123!", ApplicationRoles.Administrator),
        new("reception", "reception123!", ApplicationRoles.Receptionist),
        new("client", "client123!", ApplicationRoles.Client, GuestId: 1)
    ];

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AllowAnonymous();

        // POST /api/auth/token — issue a JWT
        group.MapPost("/token", (LoginRequest request, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Username and password are required." });

            var user = DemoUsers.FirstOrDefault(u =>
                u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == request.Password);

            if (user is null)
                return Results.Unauthorized();

            var token = GenerateJwtToken(user, config);

            return Results.Ok(new
            {
                accessToken = token,
                role = user.Role,
                username = user.Username,
                guestId = user.GuestId,
                expiresIn = 3600
            });
        })
        .WithName("GetAuthToken")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
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

    private static string GenerateJwtToken(DemoUser user, IConfiguration config)
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

        if (user.GuestId.HasValue)
            claims.Add(new Claim("guestId", user.GuestId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public record LoginRequest(string Username, string Password);
}
