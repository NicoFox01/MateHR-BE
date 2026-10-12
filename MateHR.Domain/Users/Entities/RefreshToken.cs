namespace MateHR.Domain.Users.Entities
{
    public class RefreshToken
    {
        public const int TokenHashMaxLength = 64;

        private RefreshToken()
        {
        }

        public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("El usuario no puede ser un Guid vacio.", nameof(userId));
            }

            if (expiresAt <= createdAt)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expiresAt),
                    expiresAt,
                    "El refresh token debe expirar despues de su creacion.");
            }

            Id = Guid.NewGuid();
            UserId = userId;
            TokenHash = ValidateTokenHash(tokenHash);
            ExpiresAt = expiresAt;
            CreatedAt = createdAt;
        }

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }
        public DateTimeOffset? ReplacedAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public void Revoke(DateTimeOffset moment)
        {
            if (RevokedAt.HasValue)
            {
                return;
            }

            RevokedAt = moment;
        }

        public void ReplaceWith(DateTimeOffset moment)
        {
            RevokedAt = moment;
            ReplacedAt = moment;
        }

        public bool IsActive(DateTimeOffset moment)
        {
            return !RevokedAt.HasValue && ExpiresAt > moment;
        }

        private static string ValidateTokenHash(string tokenHash)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
            {
                throw new ArgumentException("El hash del token no puede ser nulo ni vacio.", nameof(tokenHash));
            }

            var trimmed = tokenHash.Trim();

            if (trimmed.Length > TokenHashMaxLength)
            {
                throw new ArgumentException(
                    $"El hash del token no puede superar los {TokenHashMaxLength} caracteres.",
                    nameof(tokenHash));
            }

            return trimmed;
        }
    }
}