using MateHR.Application.Users.DTOs;

namespace MateHR.Application.Users.Interfaces
{
    public interface IRefreshAccessToken
    {
        Task<LoginResponse> ExecuteAsync(string? refreshToken, CancellationToken cancellationToken = default);
    }
}