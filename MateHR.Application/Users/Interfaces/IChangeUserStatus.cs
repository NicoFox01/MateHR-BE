using MateHR.Application.Users.DTOs;

namespace MateHR.Application.Users.Interfaces
{
    public interface IChangeUserStatus
    {
        Task<UserResponse> ExecuteAsync(Guid userId, ChangeUserStatusDto request, CancellationToken cancellationToken = default);
    }
}
