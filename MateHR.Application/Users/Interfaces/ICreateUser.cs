using MateHR.Application.Users.DTOs;

namespace MateHR.Application.Users.Interfaces
{
    public interface ICreateUser
    {
        Task<UserResponse> ExecuteAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    }
}