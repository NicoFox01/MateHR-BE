using MateHR.Application.Users.Commands;
using MateHR.Application.Users.Interfaces;
using MateHR.Application.Users.Queries;
using MateHR.Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MateHR.Application.Users
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUsers(this IServiceCollection services)
        {
            services.AddScoped<ILoginUser, LoginUser>();
            services.AddScoped<ILogoutUser, LogoutUser>();
            services.AddScoped<IRefreshAccessToken, RefreshAccessToken>();
            services.AddScoped<ICreateUser, CreateUser>();
            services.AddScoped<IGetCurrentUser, GetCurrentUser>();
            services.AddScoped<IUserProvisioningService, UserProvisioningService>();

            return services;
        }
    }
}