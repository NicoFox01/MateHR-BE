using MateHR.Api.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace MateHR.Api.Extensions
{
    public static class SeedingExtensions
    {
        public static async Task<int> SeedSuperAdminAsync(
            this IServiceProvider services,
            string[] args)
        {
            using var scope = services.CreateScope();

            return await SuperAdminSeeder.RunAsync(scope.ServiceProvider, args);
        }
    }
}