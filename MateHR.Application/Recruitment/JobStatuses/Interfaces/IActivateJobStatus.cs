using MateHR.Application.Recruitment.JobStatuses.DTOs;

namespace MateHR.Application.Recruitment.JobStatuses.Interfaces
{
    public interface IActivateJobStatus
    {
        Task<JobStatusResponse> ExecuteAsync(Guid id, CancellationToken ct = default);
    }
}
