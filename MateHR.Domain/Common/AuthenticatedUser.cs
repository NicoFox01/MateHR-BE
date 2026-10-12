using MateHR.Domain.Users.Enums;

namespace MateHR.Domain.Common
{
    public sealed record AuthenticatedUser(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        UserRole Role,
        Guid? TenantId,
        Guid SecurityStamp
        );
}