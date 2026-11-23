using MateHR.Domain.Recruitment.Common;

namespace MateHR.Domain.Recruitment.Entities
{
    public class Area : ICatalogEntity
    {
        public const int NameMaxLength = 100;
        private Area() { }
        public Area(Guid tenantId, string name)
        {
            RecruitmentGuard.NotEmptyGuid(tenantId, nameof(tenantId));
            Id = Guid.NewGuid();
            TenantId = tenantId;
            Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
        }
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        public void Rename(string name)
        {
            Name = RecruitmentGuard.RequiredName(name, NameMaxLength, nameof(name));
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
    }
}