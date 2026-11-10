using MateHR.Application.Recruitment.JobStatuses.DTOs;

namespace MateHR.Application.Recruitment.JobStatuses.Interfaces
{
    public interface IUpdateJobStatus
    {
        Task<JobStatusResponse> ExecuteAsync(Guid id, UpdateJobStatusDto dto, CancellationToken ct = default);
    }
}