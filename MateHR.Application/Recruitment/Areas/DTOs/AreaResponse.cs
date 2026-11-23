namespace MateHR.Application.Recruitment.Areas.DTOs
{
    public class AreaResponse
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
