using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Users.Interfaces;
using MateHR.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MateHR.Infrastructure.Services
{
    /// <summary>
    /// Comprueba que el <c>SecurityStamp</c> del token siga siendo el vigente en la base.
    /// El cache evita una consulta por request: si el password cambia, la entrada se
    /// invalida explicitamente y ademas expira sola a los pocos segundos.
    /// </summary>
    public class UserSecurityStampValidator : IUserSecurityStampValidator
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public UserSecurityStampValidator(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<bool> IsStampCurrentAsync(
            Guid userId,
            Guid stamp,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                return false;
            }

            var cacheKey = BuildCacheKey(userId);

            if (_cache.TryGetValue(cacheKey, out Guid? cached) && cached.HasValue)
            {
                return cached.Value == stamp;
            }

            var stored = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.SecurityStamp, u.IsActive })
                .FirstOrDefaultAsync(cancellationToken);

            if (stored is null || !stored.IsActive)
            {
                return false;
            }

            _cache.Set(cacheKey, stored.SecurityStamp, CacheDuration);

            return stored.SecurityStamp == stamp;
        }

        public void Invalidate(Guid userId)
        {
            _cache.Remove(BuildCacheKey(userId));
        }

        private static string BuildCacheKey(Guid userId)
        {
            return $"security-stamp:{userId}";
        }
    }
}