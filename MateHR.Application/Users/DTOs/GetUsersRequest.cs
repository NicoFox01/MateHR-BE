using MateHR.Domain.Users.Enums;

namespace MateHR.Application.Users.DTOs
{
    public class GetUsersRequest
    {
        public Guid? TenantId { get; set; }
        public UserRole? Role { get; set; }
    }
}
