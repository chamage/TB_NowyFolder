namespace TB_NowyFolder.Security;

// Stałe z nazwami ról - używane w atrybutach autoryzacji i przy generowaniu tokenów JWT.
public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string Receptionist = "Receptionist";
    public const string Client = "Client";
}
