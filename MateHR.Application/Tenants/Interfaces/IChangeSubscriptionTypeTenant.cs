using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Interfaces
{
    public interface IChangeSubscriptionTypeTenant
    {
        Task <TenantResponse> ExecuteAsync (Guid tenantId, ChangeSubscriptionTypeDto changeSubscriptionTypeDto, CancellationToken cancellationToken = default);
    }
}