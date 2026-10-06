using AutoMapper;
using FluentValidation;
using MateHR.Application.Tenants;
using MateHR.Application.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace MateHR.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var configuration = new MapperConfiguration(
                cfg => cfg.AddMaps(assembly),
                NullLoggerFactory.Instance);
            configuration.AssertConfigurationIsValid();

            services.AddSingleton<IConfigurationProvider>(configuration);
            services.AddSingleton<IMapper>(configuration.CreateMapper());

            services.AddValidatorsFromAssembly(assembly);

            services.AddTenants();
            services.AddUsers();

            return services;
        }
    }
}