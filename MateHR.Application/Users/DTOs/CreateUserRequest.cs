using MateHR.Domain.Users.Enums;

namespace MateHR.Application.Users.DTOs
{
    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public Guid? TenantId { get; set; }
    }
}