using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Interfaces
{
    public interface IChangeRecruitmentModeTenant
    {
        Task<TenantResponse> ExecuteAsync (Guid tenantId, ChangeRecruitmentModeDto changeRecruitmentModeDto, CancellationToken cancellationToken = default);
    }
}