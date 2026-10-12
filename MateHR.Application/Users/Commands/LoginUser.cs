using AutoMapper;
using FluentValidation;
using MateHR.Application.Common.Interfaces;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Interfaces;

namespace MateHR.Application.Users.Commands
{
    public class LoginUser : ILoginUser
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly IValidator<LoginRequest> _validator;

        public LoginUser(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IRefreshTokenService refreshTokenService,
            IJwtService jwtService,
            IMapper mapper,
            IValidator<LoginRequest> validator)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _refreshTokenService = refreshTokenService;
            _jwtService = jwtService;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);

            if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new InvalidCredentialsException();
            }

            if (!user.CanAuthenticate())
            {
                throw new InvalidCredentialsException("El usuario esta inactivo.");
            }

            var now = DateTimeOffset.UtcNow;

            user.RecordLogin(now);
            await _refreshTokenRepository.InvalidateExpiredUserRefreshTokensAsync(user.Id, now, cancellationToken);

            var authenticatedUser = new AuthenticatedUser(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.TenantId,
                user.SecurityStamp);

            var accessToken = _jwtService.GenerateAccessToken(authenticatedUser);

            var refreshToken = _refreshTokenService.Generate();
            var refreshTokenEntity = new RefreshToken(
                user.Id,
                _refreshTokenService.ComputeHash(refreshToken.Token),
                refreshToken.ExpiresAt,
                now);

            await _refreshTokenRepository.CreateRefreshTokenAsync(refreshTokenEntity, cancellationToken);
            await _userRepository.UpdateUserAsync(user, cancellationToken);

            return new LoginResponse
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                User = _mapper.Map<UserResponse>(user)
            };
        }
    }
}