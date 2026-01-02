using ECommerce.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.Shared.Middleware
{
    public sealed class ExceptionMappingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMappingMiddleware> _logger;

        public ExceptionMappingMiddleware(RequestDelegate next, ILogger<ExceptionMappingMiddleware> logger)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                int status;
                string title;

                switch (ex)
                {
                    case NotFoundException:
                        status = StatusCodes.Status404NotFound;
                        title = "Not Found";
                        break;
                    case ConflictException:
                        status = StatusCodes.Status400BadRequest;
                        title = "Bad Request, conflict";
                        break;
                    case ArgumentException:
                        status = StatusCodes.Status400BadRequest;
                        title = "Bad Request";
                        break;
                    case ServiceUnavailableException:
                        status = StatusCodes.Status503ServiceUnavailable;
                        title = "Service Unavailable";
                        break;
                    default:
                        status = StatusCodes.Status500InternalServerError;
                        title = "Internal Server Error";
                        break;
                }

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = ex.Message,
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
                
            }
        }
    }
}
