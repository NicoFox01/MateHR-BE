using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MateHR.Infrastructure.Persistance.Repositories.Recruitment
{
    public class AreaRepository : CatalogRepository<Area>, IAreaRepository
    {
        public AreaRepository(ApplicationDbContext context) : base(context) { }
    }
}
