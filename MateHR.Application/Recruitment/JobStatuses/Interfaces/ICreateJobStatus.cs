using MateHR.Application.Recruitment.JobStatuses.DTOs;

namespace MateHR.Application.Recruitment.JobStatuses.Interfaces
{
    public interface ICreateJobStatus
    {
        Task<JobStatusResponse> ExecuteAsync(CreateJobStatusDto dto, CancellationToken ct = default);
    }
}