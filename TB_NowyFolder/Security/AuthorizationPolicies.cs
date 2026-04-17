using Microsoft.AspNetCore.Authorization;

namespace TB_NowyFolder.Security;

public static class AuthorizationPolicies
{
    public const string GuestManagement = "GuestManagement";
    public const string RoomManagement = "RoomManagement";
    public const string RoomTypeManagement = "RoomTypeManagement";
    public const string ServiceManagement = "ServiceManagement";
    public const string ReservationRead = "ReservationRead";
    public const string ReservationCreateOrUpdate = "ReservationCreateOrUpdate";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(GuestManagement, policy => 
            policy.RequireRole(ApplicationRoles.Administrator, ApplicationRoles.Receptionist));
            
        options.AddPolicy(RoomManagement, policy => 
            policy.RequireRole(ApplicationRoles.Administrator));
            
        options.AddPolicy(RoomTypeManagement, policy => 
            policy.RequireRole(ApplicationRoles.Administrator));
            
        options.AddPolicy(ServiceManagement, policy => 
            policy.RequireRole(ApplicationRoles.Administrator));
            
        options.AddPolicy(ReservationRead, policy => 
            policy.RequireRole(ApplicationRoles.Administrator, ApplicationRoles.Receptionist));
            
        options.AddPolicy(ReservationCreateOrUpdate, policy => 
            policy.RequireRole(ApplicationRoles.Administrator, ApplicationRoles.Receptionist, ApplicationRoles.Client));
    }
}
