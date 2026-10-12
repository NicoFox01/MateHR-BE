using MateHR.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace MateHR.Infrastructure.Tests.Common
{
    internal static class InMemoryDbContextFactory
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"matehr-tests-{Guid.NewGuid()}")
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
