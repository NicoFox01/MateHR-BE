using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MateHR.Api.Middleware;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Recruitment.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace MateHR.Api.Tests.Middleware
{
    public class ExceptionMiddlewareTests
    {
        private static async Task<(int StatusCode, JsonElement Body)> InvokeAsync(Exception exception)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/test";
            context.Response.Body = new MemoryStream();

            RequestDelegate next = _ => throw exception;
            var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var body = JsonSerializer.Deserialize<JsonElement>(json);

            return (context.Response.StatusCode, body);
        }

        [Fact]
        public async Task InvokeAsync_KeyNotFoundException_Returns404()
        {
            var result = await InvokeAsync(new KeyNotFoundException("No se encontro el tenant"));

            result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            result.Body.GetProperty("message").GetString().Should().Be("No se encontro el tenant");
            result.Body.TryGetProperty("traceId", out _).Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_SlugAlreadyExistsException_Returns409()
        {
            var result = await InvokeAsync(new SlugAlreadyExistsException("acme-corp"));

            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        }

        [Fact]
        public async Task InvokeAsync_EmailAlreadyExistsException_Returns409()
        {
            var result = await InvokeAsync(new EmailAlreadyExistsException("ana@acme.com"));

            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        }

        [Fact]
        public async Task InvokeAsync_InvalidCredentialsException_Returns401()
        {
            var result = await InvokeAsync(new InvalidCredentialsException());

            result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_Returns400()
        {
            var result = await InvokeAsync(new ArgumentException("mensaje"));

            result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task InvokeAsync_ValidationException_Returns400WithErrors()
        {
            var errors = new[]
            {
                new ValidationFailure("Name", "El nombre es obligatorio."),
                new ValidationFailure("CUIT", "El CUIT es invalido.")
            };

            var result = await InvokeAsync(new ValidationException(errors));

            result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            var errorsElement = result.Body.GetProperty("errors");
            errorsElement.GetProperty("Name")[0].GetString().Should().Be("El nombre es obligatorio.");
            errorsElement.GetProperty("CUIT")[0].GetString().Should().Be("El CUIT es invalido.");
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Returns500()
        {
            var result = await InvokeAsync(new Exception("boom"));

            result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            result.Body.GetProperty("message").GetString().Should().Be("Ocurrio un error inesperado.");
        }

        [Fact]
        public async Task InvokeAsync_HttpRequestException_Returns502()
        {
            var result = await InvokeAsync(new HttpRequestException("falla externa"));

            result.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
            result.Body.GetProperty("message").GetString().Should().Be("No se pudo comunicar con un servicio externo.");
        }
        [Fact]
        public async Task InvokeAsync_EntityInUseException_Returns409()
        {
            var result = await InvokeAsync(new EntityInUseException("Area", Guid.NewGuid()));

            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        }
        [Fact]
        public async Task InvokeAsync_DuplicateEntityNameException_Returns409()
        {
            var result = await InvokeAsync(new DuplicateEntityNameException("Area"));
            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        }
        [Fact]
        public async Task InvokeAsync_JobAlreadyClosedException_Returns409()
        {
            var result = await InvokeAsync(new JobAlreadyClosedException(Guid.NewGuid()));
            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        }
        [Fact]
        public async Task InvokeAsync_UnauthorizedAccessException_Returns403()
        {
            var result = await InvokeAsync(new UnauthorizedAccessException("No iene permisos apra realizar esta acción."));
            result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

    }
}
