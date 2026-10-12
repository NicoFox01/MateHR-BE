using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;

namespace MateHR.Domain.Users.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<User>> GetUsersAsync(Guid? tenantId, UserRole? role, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);
        Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    }
}