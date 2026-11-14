using MateHR.Application.Recruitment.Areas.DTOs;

namespace MateHR.Application.Recruitment.Areas.Interfaces
{
    public interface IUpdateArea
    {
        Task<AreaResponse> ExecuteAsync(Guid id, UpdateAreaDto dto, CancellationToken ct = default);
    }
}
