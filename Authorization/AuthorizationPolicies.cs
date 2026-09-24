namespace English.Authorization;

public static class AuthorizationPolicies
{
    public const string ActiveAccount = nameof(ActiveAccount);
    public const string StudentOnly = nameof(StudentOnly);
    public const string AdminOnly = nameof(AdminOnly);
}
