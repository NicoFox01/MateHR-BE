using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Interfaces;

namespace MateHR.Infrastructure.Persistance.Repositories.Recruitment
{
    public class JobStatusRepository : CatalogRepository<JobStatus>, IJobStatusRepository
    {
        public JobStatusRepository(ApplicationDbContext context) : base(context) { }
    }
}
