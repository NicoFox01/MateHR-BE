using MateHR.Application.Recruitment.Areas.DTOs;

namespace MateHR.Application.Recruitment.Areas.Interfaces
{
    public interface ICreateArea
    {
        Task<AreaResponse> ExecuteAsync(CreateAreaDto dto, CancellationToken ct = default);
    }
}
