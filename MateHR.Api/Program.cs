using System.Text;
using System.Text.Json.Serialization;
using MateHR.Api.Authorization;
using MateHR.Api.Extensions;
using MateHR.Api.Middleware;
using MateHR.Domain.Common;
using MateHR.Infrastructure;
using MateHR.Infrastructure.Settings;
using MateHR.Application;
using MateHR.Application.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = AuthClaims.Role,
            NameClaimType = AuthClaims.UserId,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = string.IsNullOrWhiteSpace(jwt.SigningKey)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.RequireSuperAdmin, policy =>
        policy.RequireRole(AuthPolicies.SuperAdminRole));

    options.AddPolicy(AuthPolicies.RequireAdmin, policy =>
        policy.RequireRole(AuthPolicies.SuperAdminRole, AuthPolicies.AdminRole));
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (string.IsNullOrWhiteSpace(builder.Configuration[$"{JwtSettings.SectionName}:SigningKey"]))
{
    app.Logger.LogWarning(
        "No se encontro 'Jwt:SigningKey'. Definila con 'dotnet user-secrets set \"Jwt:SigningKey\" <clave de 32+ caracteres>'. " +
        "Los endpoints con [Authorize] responderan 401 hasta entonces.");
}

app.UseApiMiddleware();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseMiddleware<TenantContextMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();