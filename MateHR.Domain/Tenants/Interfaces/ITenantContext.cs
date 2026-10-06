namespace MateHR.Domain.Tenants.Interfaces
{
    public interface ITenantContext
    {
        Guid? CurrentTenantId { get; }
    }
}
