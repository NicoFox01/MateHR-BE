using MateHR.Application.Tenants.Commands;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Application.Tenants.Queries;
using Microsoft.Extensions.DependencyInjection;


namespace MateHR.Application.Tenants
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddTenants(this IServiceCollection services)
        {
            services.AddScoped<ICreateTenant, CreateTenant>();
            services.AddScoped<IUpdateTenant, UpdateTenant>();
            services.AddScoped<IChangeStatusTenant, ChangeStatusTenant>();
            services.AddScoped<IChangeRecruitmentModeTenant, ChangeRecruitmentModeTenant>();
            services.AddScoped<IChangeSubscriptionTypeTenant, ChangeSubscriptionTypeTenant>();
            services.AddScoped<IGetTenantById, GetTenantById>();
            services.AddScoped<IGetTenantBySlug, GetTenantBySlug>();
            services.AddScoped<IGetTenants, GetTenants>();
            return services;
        }
    }
}
