using AutoMapper;
using MateHR.Application.Recruitment.Areas.Mappings;
using MateHR.Application.Recruitment.JobStatuses.Mappings;
using MateHR.Application.Tenants.Mappings;
using MateHR.Application.Users.Mappings;
using Microsoft.Extensions.Logging.Abstractions;

namespace MateHR.Application.Tests.Common
{
    internal static class TestMapper
    {
        public static IMapper Create()
        {
            var configuration = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddProfile<TenantMapping>();
                    cfg.AddProfile<UserMapping>();
                    cfg.AddProfile<JobStatusMapping>();
                    cfg.AddProfile<AreaMapping>();
                },
                NullLoggerFactory.Instance);

            return configuration.CreateMapper();
        }
    }
}
