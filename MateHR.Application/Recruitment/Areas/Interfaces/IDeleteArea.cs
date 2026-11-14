namespace MateHR.Application.Recruitment.Areas.Interfaces
{
    public interface IDeleteArea
    {
        Task ExecuteAsync(Guid id, CancellationToken ct = default);
    }
}
