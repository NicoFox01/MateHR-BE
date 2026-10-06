using MateHR.Application.Common.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using MateHR.Domain.Users.Interfaces;
using MateHR.Infrastructure.Authentication;
using MateHR.Infrastructure.Persistance;
using MateHR.Infrastructure.Persistance.Repositories;
using MateHR.Infrastructure.Services;
using MateHR.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MateHR.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No se encontro el connection string 'DefaultConnection'.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<ITenantContext, TenantContext>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
            services.AddSingleton<IJwtService, JwtService>();

            return services;
        }
    }
}