using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.ValueObjects;
using System.Net.Mail;
using System.Text;

namespace MateHR.Domain.Tenants.Entities
{
    public class Tenant
    {
        public const int NameMaxLength = 200;
        public const int SlugMaxLength = 100;
        public const int CuitMaxLength = 11;
        public const int OwnerEmailMaxLength = 320;
        private Tenant() 
        { 
        }
        public Tenant(
            string name,
            string slug,
            string cuit,
            string ownerEmail,
            Industry industry,
            Address? address,
            TenantStatus status,
            RecruitmentMode recruitmentMode,
            SubscriptionType subscriptionType
            )
        {
            ValidateEnum(industry);
            ValidateEnum(status);
            ValidateEnum(recruitmentMode);
            ValidateEnum(subscriptionType);

            Id = Guid.NewGuid();
            Name = ValidateAndTrim(name, NameMaxLength, nameof(name));
            Slug = NormalizeSlug(ValidateAndTrim(slug, SlugMaxLength, nameof(slug)));
            CUIT = ValidateCuit(cuit);
            OwnerEmail = ValidateEmail(ownerEmail);
            Industry = industry;
            Address = address;
            Status = status;
            RecruitmentMode = recruitmentMode;
            SubscriptionType = subscriptionType;
            SubscriptionStatus = SubscriptionStatus.None;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public string CUIT { get; private set; } = string.Empty;
        public string OwnerEmail { get; private set; } = string.Empty;
        public Industry Industry { get; private set; }
        public Address? Address { get; private set; }
        public TenantStatus Status { get; private set; }
        public RecruitmentMode RecruitmentMode { get; private set; }
        public SubscriptionType SubscriptionType { get; private set; }
        public SubscriptionStatus SubscriptionStatus { get; private set; }
        public DateTimeOffset? SubscriptionExpiresAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }


        public void ChangeStatus(TenantStatus status)
        {
            ValidateEnum(status);
            Status = status;
        }

        public void ChangeRecruitmentMode(RecruitmentMode recruitmentMode)
        {
            ValidateEnum(recruitmentMode);
            RecruitmentMode = recruitmentMode;
        }

        public void ChangeSubscriptionType(SubscriptionType subscriptionType)
        {
            ValidateEnum(subscriptionType);
            SubscriptionType = subscriptionType;
            SubscriptionStatus = SubscriptionStatus.None;
            SubscriptionExpiresAt = null;
        }
        public void Update(
            string name,
            string slug,
            string cuit,
            string ownerEmail,
            Industry industry,
            Address? address
            )
        {
            ValidateEnum(industry);

            Name = ValidateAndTrim(name, NameMaxLength, nameof(name));
            Slug = NormalizeSlug(ValidateAndTrim(slug, SlugMaxLength, nameof(slug)));
            CUIT = ValidateCuit(cuit);
            OwnerEmail = ValidateEmail(ownerEmail);
            Industry = industry;
            Address = address;
        }

        private static string ValidateAndTrim(string value, int maxLength, string paramName)
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

        private static string NormalizeSlug(string slug)
        {
            return SlugGenerator.Generate(slug);
        }

        private static string ValidateCuit(string cuit)
        {
            if (string.IsNullOrWhiteSpace(cuit))
            {
                throw new ArgumentException("El CUIT no puede ser nulo ni vacio.", nameof(cuit));
            }

            var trimmed = cuit.Trim();
            if (trimmed.Length != CuitMaxLength || !trimmed.All(char.IsAsciiDigit))
            {
                throw new ArgumentException("El CUIT debe tener 11 digitos numericos.", nameof(cuit));
            }

            return trimmed;
        }

        private static string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("El email no puede ser nulo ni vacio.", nameof(email));
            }

            var trimmed = email.Trim();
            if (trimmed.Length > OwnerEmailMaxLength)
            {
                throw new ArgumentException($"El email no puede superar los {OwnerEmailMaxLength} caracteres.", nameof(email));
            }

            if (!MailAddress.TryCreate(trimmed, out _))
            {
                throw new ArgumentException("El email no tiene un formato valido.", nameof(email));
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
