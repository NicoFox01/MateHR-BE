using System.Security.Cryptography;
using System.Text;
using MateHR.Application.Common.Interfaces;
using MateHR.Domain.Common;
using MateHR.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace MateHR.Infrastructure.Authentication
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private const int TokenBytes = 64;

        private readonly JwtSettings _settings;

        public RefreshTokenService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public IssuedToken Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(TokenBytes);
            var token = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            var expiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenDays);

            return new IssuedToken(token, expiresAt);
        }

        public string ComputeHash(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("El token no puede ser nulo ni vacio.", nameof(token));
            }

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));

            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}