using System.Diagnostics;
using AutoMapper;

namespace MateHR.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogDebug("[{TraceId}] Request cancelado por el cliente: {Method} {Path}",
                    GetTraceId(context), context.Request.Method, context.Request.Path);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogError(ex,
                        "[{TraceId}] Respuesta ya iniciada en {Method} {Path}; la respuesta de error no se puede escribir",
                        GetTraceId(context), context.Request.Method, context.Request.Path);
                    throw;
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = GetTraceId(context);
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "/";

            var effective = Unwrap(exception);

            var (statusCode, message) = effective switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, effective.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, effective.Message),
                HttpRequestException => (StatusCodes.Status502BadGateway, "No se pudo comunicar con un servicio externo."),
                _ => (StatusCodes.Status500InternalServerError, "Ocurrio un error inesperado.")
            };

            if (statusCode >= 500)
            {
                _logger.LogError(exception,
                    "[{TraceId}] {StatusCode} en {Method} {Path}: {Message}",
                    traceId, statusCode, method, path, effective.Message);
            }
            else
            {
                _logger.LogWarning("[{TraceId}] {StatusCode} en {Method} {Path}: {Message}",
                    traceId, statusCode, method, path, effective.Message);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new { message, traceId });
        }

        private static string GetTraceId(HttpContext context)
            => Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        private static Exception Unwrap(Exception exception)
        {
            if (exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0)
            {
                exception = aggregate.InnerExceptions[0];
            }

            while (exception is AutoMapperMappingException && exception.InnerException is not null)
            {
                exception = exception.InnerException;
            }

            return exception;
        }
    }
}