using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Domain.Common;

namespace MateHR.Application.Recruitment.Areas.Interfaces
{
    public interface IGetAreas
    {
        Task<PagedResult<AreaResponse>> ExecuteAsync(GetAreasRequest request, CancellationToken ct = default);
    }
}
