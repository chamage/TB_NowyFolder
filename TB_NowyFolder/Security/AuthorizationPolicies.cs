namespace TB_NowyFolder.Security;

/// <summary>
/// Polityki RBAC używane w endpointach do kontroli dostępu.
/// Trzy poziomy: AuthenticatedUser (zalogowany), StaffOrAdmin, AdminOnly.
/// </summary>
public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string StaffOrAdmin = "StaffOrAdmin";
    public const string AuthenticatedUser = "AuthenticatedUser";

    public static IServiceCollection AddHotelAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminOnly, policy =>
                policy.RequireRole(ApplicationRoles.Administrator))
            .AddPolicy(StaffOrAdmin, policy =>
                policy.RequireRole(ApplicationRoles.Receptionist, ApplicationRoles.Administrator))
            // AuthenticatedUser — używany jako bazowa polityka grupy endpointów.
            // Oznacza tylko tyle, że żądanie musi zawierać ważny token JWT.
            .AddPolicy(AuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

        return services;
    }
}
