using MateHR.Application.Users.DTOs;

namespace MateHR.Application.Users.Interfaces
{
    public interface IGetCurrentUser
    {
        Task<UserResponse> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}