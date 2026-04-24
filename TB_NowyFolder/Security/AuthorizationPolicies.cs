namespace TB_NowyFolder.Security;

/// <summary>
/// Registers RBAC authorization policies for the hotel API.
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
            .AddPolicy(AuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

        return services;
    }
}
