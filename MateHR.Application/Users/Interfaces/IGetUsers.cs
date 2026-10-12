using MateHR.Application.Users.DTOs;

namespace MateHR.Application.Users.Interfaces
{
    public interface IGetUsers
    {
        Task<IReadOnlyList<UserResponse>> ExecuteAsync(GetUsersRequest request, CancellationToken cancellationToken = default);
    }
}
