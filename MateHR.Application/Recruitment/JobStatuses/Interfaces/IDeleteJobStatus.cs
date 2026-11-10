namespace MateHR.Application.Recruitment.JobStatuses.Interfaces
{
    public interface IDeleteJobStatus
    {
        Task ExecuteAsync(Guid id, CancellationToken ct = default);
    }
}