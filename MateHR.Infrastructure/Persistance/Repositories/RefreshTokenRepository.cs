#nullable enable

using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MateHR.Infrastructure.Persistance.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return refreshToken;
        }

        public async Task<RefreshToken> UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return refreshToken;
        }

        public async Task<int> InvalidateExpiredUserRefreshTokensAsync(Guid userId, DateTimeOffset moment, CancellationToken cancellationToken = default)
        {
            var expired = await _context.RefreshTokens
                .Where(t => t.UserId == userId
                    && t.RevokedAt == null
                    && t.ExpiresAt <= moment)
                .ToListAsync(cancellationToken);

            foreach (var token in expired)
            {
                token.Revoke(moment);
            }

            if (expired.Count == 0)
            {
                return 0;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return expired.Count;
        }
    }
}