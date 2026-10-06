using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;

namespace MateHR.Application.Users.Interfaces
{
    /// <summary>
    /// Alta de usuario sin pasar por HTTP. La usa el comando de seed para crear el
    /// primer SuperAdmin, que por definicion no puede existir todavia en la base.
    /// </summary>
    public interface IUserProvisioningService
    {
        Task<User> CreateAsync(
            string firstName,
            string lastName,
            string email,
            string plainTextPassword,
            UserRole role,
            Guid? tenantId,
            CancellationToken cancellationToken = default);
    }
}