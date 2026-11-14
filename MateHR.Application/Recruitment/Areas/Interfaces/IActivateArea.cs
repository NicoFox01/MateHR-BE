using MateHR.Application.Recruitment.Areas.DTOs;

namespace MateHR.Application.Recruitment.Areas.Interfaces
{
    public interface IActivateArea
    {
        Task<AreaResponse> ExecuteAsync(Guid id, CancellationToken ct = default);
    }
}
