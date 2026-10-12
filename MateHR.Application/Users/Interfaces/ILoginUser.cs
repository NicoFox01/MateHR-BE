using MateHR.Application.Users.DTOs;

namespace MateHR.Application.Users.Interfaces
{
    public interface ILoginUser
    {
        Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default);
    }
}