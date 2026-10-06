using MateHR.Domain.Tenants.Enums;

namespace MateHR.Application.Tenants.DTOs
{
    public class UpdateTenantDto
    {
        public string Name { get;  set; } = string.Empty;
        public string CUIT { get;  set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public Industry? Industry { get;  set; }
        public AddressDto? Address { get;  set; }
    }
}
