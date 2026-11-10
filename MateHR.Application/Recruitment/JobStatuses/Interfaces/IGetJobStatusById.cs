using MateHR.Application.Recruitment.JobStatuses.DTOs;

namespace MateHR.Application.Recruitment.JobStatuses.Interfaces
{
    public interface IGetJobStatusById
    {
        Task<JobStatusResponse> ExecuteAsync(Guid id, CancellationToken ct = default);
    }
}