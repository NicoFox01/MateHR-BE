using AutoMapper;
using MateHR.Application.Common.Interfaces;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Interfaces;

namespace MateHR.Application.Users.Commands
{
    /// <summary>
    /// Canjea un refresh token por un access token nuevo, rotando el refresh.
    /// </summary>
    /// <remarks>
    /// Si llega un token que ya fue rotado o revocado, se asume que alguien esta
    /// reusando un token robado y se revocan todos los refresh del usuario.
    /// </remarks>
    public class RefreshAccessToken : IRefreshAccessToken
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IJwtService _jwtService;
        private readonly IUserSecurityStampValidator _stampValidator;
        private readonly IMapper _mapper;

        public RefreshAccessToken(
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            IRefreshTokenService refreshTokenService,
            IJwtService jwtService,
            IUserSecurityStampValidator stampValidator,
            IMapper mapper)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _refreshTokenService = refreshTokenService;
            _jwtService = jwtService;
            _stampValidator = stampValidator;
            _mapper = mapper;
        }

        public async Task<LoginResponse> ExecuteAsync(
            string? refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new InvalidCredentialsException("El refresh token no es valido.");
            }

            var now = DateTimeOffset.UtcNow;
            var tokenHash = _refreshTokenService.ComputeHash(refreshToken);

            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (storedToken is null)
            {
                throw new InvalidCredentialsException("El refresh token no es valido.");
            }

            if (!storedToken.IsActive(now))
            {
                await HandleReuseAsync(storedToken, now, cancellationToken);
                throw new InvalidCredentialsException("El refresh token no es valido.");
            }

            var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

            if (!user.CanAuthenticate())
            {
                await _refreshTokenRepository.RevokeAllUserRefreshTokensAsync(user.Id, now, cancellationToken);
                throw new InvalidCredentialsException("El usuario esta inactivo.");
            }

            storedToken.ReplaceWith(now);
            await _refreshTokenRepository.UpdateRefreshTokenAsync(storedToken, cancellationToken);

            var authenticatedUser = new AuthenticatedUser(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.TenantId,
                user.SecurityStamp);

            var accessToken = _jwtService.GenerateAccessToken(authenticatedUser);

            var rotatedToken = _refreshTokenService.Generate();
            var rotatedEntity = new RefreshToken(
                user.Id,
                _refreshTokenService.ComputeHash(rotatedToken.Token),
                rotatedToken.ExpiresAt,
                now);

            await _refreshTokenRepository.CreateRefreshTokenAsync(rotatedEntity, cancellationToken);

            _stampValidator.Invalidate(user.Id);

            return new LoginResponse
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = rotatedToken.Token,
                RefreshTokenExpiresAt = rotatedToken.ExpiresAt,
                User = _mapper.Map<UserResponse>(user)
            };
        }

        /// <summary>
        /// Token ya rotado o revocado: se tratan todos los refresh del usuario como
        /// comprometidos. Es la unica defensa contra el reuso de tokens robados.
        /// </summary>
        private async Task HandleReuseAsync(
            RefreshToken storedToken,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (storedToken.IsActive(now))
            {
                return;
            }

            await _refreshTokenRepository.RevokeAllUserRefreshTokensAsync(
                storedToken.UserId,
                now,
                cancellationToken);
        }
    }
}