namespace MateHR.Application.Recruitment.Areas.DTOs
{
    public class GetAreasRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool IncludeInactive { get; set; } = false;
    }
}
