namespace MateHR.Api.Authorization
{
    public static class AuthPolicies
    {
        public const string RequireAdmin = nameof(RequireAdmin);
        public const string RequireSuperAdmin = nameof(RequireSuperAdmin);
        public const string RequireTenantAdmin = nameof(RequireTenantAdmin);
        public const string RequireRecruiter = nameof(RequireRecruiter);

        public const string SuperAdminRole = "SuperAdmin";
        public const string AdminRole = "Admin";
        public const string RecruiterRole = "Recruiter";

    }
}