using MateHR.Domain.Users.Entities;

namespace MateHR.Domain.Users.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
        Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        Task<RefreshToken> UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        Task<int> InvalidateExpiredUserRefreshTokensAsync(Guid userId, DateTimeOffset moment, CancellationToken cancellationToken = default);
        Task<int> RevokeAllUserRefreshTokensAsync(Guid userId, DateTimeOffset moment, CancellationToken cancellationToken = default);
    }
}