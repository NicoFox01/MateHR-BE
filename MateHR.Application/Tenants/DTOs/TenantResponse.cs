using MateHR.Domain.Tenants.Enums;


namespace MateHR.Application.Tenants.DTOs
{
    public class TenantResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get;  set; } = string.Empty;
        public string CUIT { get;  set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public Industry Industry { get; set; }
        public AddressDto? Address { get; set; }
        public TenantStatus Status { get; set; }
        public RecruitmentMode RecruitmentMode { get; set; }
        public SubscriptionType SubscriptionType { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public DateTimeOffset? SubscriptionExpiresAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
