using MateHR.Domain.Users.Enums;
using System.Net.Mail;

namespace MateHR.Domain.Users.Entities
{
    public class User
    {
        public const int FirstNameMaxLength = 100;
        public const int LastNameMaxLength = 100;
        public const int EmailMaxLength = 320;
        public const int PasswordHashMaxLength = 512;

        private User()
        {
        }

        public User(
            string firstName,
            string lastName,
            string email,
            string passwordHash,
            UserRole role,
            Guid? tenantId
            )
        {
            ValidateEnum(role);

            if (role == UserRole.SuperAdmin && tenantId.HasValue)
            {
                throw new ArgumentException(
                    "Un SuperAdmin no pertenece a un tenant.",
                    nameof(tenantId));
            }

            if (role != UserRole.SuperAdmin && !tenantId.HasValue)
            {
                throw new ArgumentException(
                    "Un usuario con rol debe pertenecer a un tenant.",
                    nameof(tenantId));
            }

            if (tenantId.HasValue && tenantId.Value == Guid.Empty)
            {
                throw new ArgumentException("El tenant no puede ser un Guid vacio.", nameof(tenantId));
            }

            Id = Guid.NewGuid();
            FirstName = ValidateName(firstName, FirstNameMaxLength, nameof(firstName));
            LastName = ValidateName(lastName, LastNameMaxLength, nameof(lastName));
            Email = ValidateEmail(email);
            PasswordHash = ValidatePasswordHash(passwordHash);
            Role = role;
            TenantId = tenantId;
            IsActive = true;
            SecurityStamp = Guid.NewGuid();
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public Guid Id { get; private set; }
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public UserRole Role { get; private set; }
        public Guid? TenantId { get; private set; }
        public bool IsActive { get; private set; }
        public Guid SecurityStamp { get; private set; }
        public DateTimeOffset? LastLoginAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }

        public void RecordLogin(DateTimeOffset moment)
        {
            if (moment < CreatedAt)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(moment),
                    moment,
                    "El inicio de sesion no puede ser anterior a la creacion del usuario.");
            }

            LastLoginAt = moment;
            UpdatedAt = moment;
        }

        public void ChangePassword(string newPasswordHash)
        {
            PasswordHash = ValidatePasswordHash(newPasswordHash);
            SecurityStamp = Guid.NewGuid();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Deactivate(DateTimeOffset moment)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("El usuario ya esta inactivo.");
            }

            IsActive = false;
            SecurityStamp = Guid.NewGuid();
            UpdatedAt = moment;
        }

        public void Activate(DateTimeOffset moment)
        {
            if (IsActive)
            {
                throw new InvalidOperationException("El usuario ya esta activo.");
            }

            IsActive = true;
            SecurityStamp = Guid.NewGuid();
            UpdatedAt = moment;
        }

        public bool CanAuthenticate()
        {
            return IsActive;
        }

        private static string ValidateName(string value, int maxLength, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("El valor no puede ser nulo ni vacio.", paramName);
            }

            var trimmed = value.Trim();

            if (trimmed.Length > maxLength)
            {
                throw new ArgumentException($"El valor no puede superar los {maxLength} caracteres.", paramName);
            }

            return trimmed;
        }

        private static string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("El email no puede ser nulo ni vacio.", nameof(email));
            }

            var normalized = email.Trim().ToLowerInvariant();

            if (normalized.Length > EmailMaxLength)
            {
                throw new ArgumentException($"El email no puede superar los {EmailMaxLength} caracteres.", nameof(email));
            }

            if (!MailAddress.TryCreate(normalized, out _))
            {
                throw new ArgumentException("El email no tiene un formato valido.", nameof(email));
            }

            return normalized;
        }

        private static string ValidatePasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentException("El hash de la contrasena no puede ser nulo ni vacio.", nameof(passwordHash));
            }

            var trimmed = passwordHash.Trim();

            if (trimmed.Length > PasswordHashMaxLength)
            {
                throw new ArgumentException($"El hash no puede superar los {PasswordHashMaxLength} caracteres.", nameof(passwordHash));
            }

            return trimmed;
        }

        private static void ValidateEnum<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "El valor no es un valor valido.");
            }
        }
    }
}