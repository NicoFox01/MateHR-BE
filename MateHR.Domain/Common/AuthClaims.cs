namespace MateHR.Domain.Common
{
    public static class AuthClaims
    {
        public const string UserId = "sub";
        public const string TenantId = "tenant_id";
        public const string Role = "role";
        public const string Email = "email";
        public const string SecurityStamp = "security_stamp";
    }
}