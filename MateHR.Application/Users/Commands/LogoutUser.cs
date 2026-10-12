using MateHR.Application.Common.Interfaces;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Users.Interfaces;

namespace MateHR.Application.Users.Commands
{
    public class LogoutUser : ILogoutUser
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IRefreshTokenService _refreshTokenService;

        public LogoutUser(
            IRefreshTokenRepository refreshTokenRepository,
            IRefreshTokenService refreshTokenService)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _refreshTokenService = refreshTokenService;
        }

        public async Task ExecuteAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var tokenHash = _refreshTokenService.ComputeHash(refreshToken);

            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (storedToken is null)
            {
                return;
            }

            storedToken.Revoke(DateTimeOffset.UtcNow);

            await _refreshTokenRepository.UpdateRefreshTokenAsync(storedToken, cancellationToken);
        }
    }
}