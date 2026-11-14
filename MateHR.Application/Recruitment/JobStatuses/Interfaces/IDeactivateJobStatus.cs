using MateHR.Application.Recruitment.JobStatuses.DTOs;

namespace MateHR.Application.Recruitment.JobStatuses.Interfaces
{
    public interface IDeactivateJobStatus
    {
        Task<JobStatusResponse> ExecuteAsync(Guid id, CancellationToken ct = default);
    }
}
