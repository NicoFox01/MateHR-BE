using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Domain.Common;

namespace MateHR.Application.Recruitment.JobStatuses.Interfaces
{
    public interface IGetJobStatuses
    {
        Task<PagedResult<JobStatusResponse>> ExecuteAsync(GetJobStatusesRequest request, CancellationToken ct = default);
    }
}