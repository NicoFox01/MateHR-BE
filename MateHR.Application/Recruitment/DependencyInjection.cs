using MateHR.Application.Recruitment.JobStatuses.Commands;
using MateHR.Application.Recruitment.JobStatuses.Interfaces;
using MateHR.Application.Recruitment.JobStatuses.Queries;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Recruitment
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRecruitment(this IServiceCollection services)
        {
            services.AddScoped<ICreateJobStatus, CreateJobStatus>();
            services.AddScoped<IUpdateJobStatus, UpdateJobStatus>();
            services.AddScoped<IDeleteJobStatus, DeleteJobStatus>();
            services.AddScoped<IGetJobStatuses, GetJobStatuses>();
            services.AddScoped<IGetJobStatusById, GetJobStatusById>();
            return services;
        }
    }
}
