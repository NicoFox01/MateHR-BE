namespace MateHR.Api.Authorization
{
    public static class AuthPolicies
    {
        public const string RequireAdmin = nameof(RequireAdmin);
        public const string RequireSuperAdmin = nameof(RequireSuperAdmin);

        public const string SuperAdminRole = "SuperAdmin";
        public const string AdminRole = "Admin";
    }
}