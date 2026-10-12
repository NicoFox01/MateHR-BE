using System.Text;
using System.Text.Json.Serialization;
using MateHR.Api.Authorization;
using MateHR.Api.Extensions;
using MateHR.Api.Middleware;
using MateHR.Api.Seeding;
using MateHR.Domain.Common;
using MateHR.Infrastructure;
using MateHR.Infrastructure.Settings;
using MateHR.Application;
using MateHR.Application.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[]
{
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:5175",
    "http://localhost:5176",
    "http://localhost:5180",
    "http://localhost:3000",
    "http://127.0.0.1:5173"
};

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var runSeeder = SuperAdminSeeder.IsRequested(args);

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
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

if (runSeeder)
{
    await app.Services.SeedSuperAdminAsync(args);
    return;
}

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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("DefaultCors");

app.UseAuthentication();

app.UseMiddleware<SecurityStampValidationMiddleware>();

app.UseMiddleware<TenantContextMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }