using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MateHR.Application.Common.Interfaces;
using MateHR.Domain.Common;
using MateHR.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MateHR.Infrastructure.Authentication
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _settings;

        public JwtService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
            ValidateSettings();
        }

        public IssuedToken GenerateAccessToken(AuthenticatedUser user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddMinutes(_settings.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(AuthClaims.UserId, user.Id.ToString()),
                new(AuthClaims.Email, user.Email),
                new(AuthClaims.Role, user.Role.ToString()),
                new(AuthClaims.SecurityStamp, user.SecurityStamp.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (user.TenantId.HasValue)
            {
                claims.Add(new Claim(AuthClaims.TenantId, user.TenantId.Value.ToString()));
            }

            var credentials = new SigningCredentials(BuildKey(), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: issuedAt.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: credentials);

            return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        private SymmetricSecurityKey BuildKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.Issuer))
            {
                throw new InvalidOperationException($"No se encontro 'Jwt:{nameof(JwtSettings.Issuer)}' en la configuracion.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Audience))
            {
                throw new InvalidOperationException($"No se encontro 'Jwt:{nameof(JwtSettings.Audience)}' en la configuracion.");
            }

            if (string.IsNullOrWhiteSpace(_settings.SigningKey))
            {
                throw new InvalidOperationException(
                    $"No se encontro 'Jwt:{nameof(JwtSettings.SigningKey)}' en la configuracion. " +
                    $"Definela con 'dotnet user-secrets set \"Jwt:{nameof(JwtSettings.SigningKey)}\" <valor>'.");
            }

            if (_settings.SigningKey.Length < _settings.SigningKeyMinimumLength)
            {
                throw new InvalidOperationException(
                    $"'Jwt:{nameof(JwtSettings.SigningKey)}' debe tener al menos " +
                    $"{_settings.SigningKeyMinimumLength} caracteres para HMAC-SHA256.");
            }

            if (_settings.AccessTokenMinutes < 1)
            {
                throw new InvalidOperationException($"'Jwt:{nameof(JwtSettings.AccessTokenMinutes)}' debe ser mayor o igual a 1.");
            }

            if (_settings.RefreshTokenDays < 1)
            {
                throw new InvalidOperationException($"'Jwt:{nameof(JwtSettings.RefreshTokenDays)}' debe ser mayor o igual a 1.");
            }
        }
    }
}