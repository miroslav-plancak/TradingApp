using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TradingApp.Business.Interfaces.Logger;

namespace TradingApp.Business.Middleware
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ITradingAppLogger _logger;

        public ExceptionHandlingMiddleware(ITradingAppLogger logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                await WriteProblemDetails(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
            }
        }

        private static Task WriteProblemDetails(HttpContext context, int statusCode, string title, string detail)
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
