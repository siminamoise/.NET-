using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ProductApp.Common.Middleware
{
    public class CorrelationMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationMiddleware> _logger;

        public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers[HeaderName] = correlationId;
            }

            // folosim indexer, nu Add, ca să evităm excepții la duplicate
            context.Response.Headers[HeaderName] = correlationId.ToString();

            using (_logger.BeginScope("{CorrelationId}", correlationId.ToString()))
            {
                await _next(context);
            }
        }
    }
}
