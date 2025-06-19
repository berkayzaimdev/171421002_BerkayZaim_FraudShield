using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Analiz.Application.Exceptions;

namespace Analiz.API.Middleware;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);

        var response = new
        {
            error = GetErrorMessage(exception),
            statusCode = context.Response.StatusCode
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ModelNotFoundException => (int)HttpStatusCode.NotFound,
            ValidationException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ModelTrainingException => (int)HttpStatusCode.UnprocessableEntity,
            ModelEvaluationException => (int)HttpStatusCode.UnprocessableEntity,
            TransactionNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string GetErrorMessage(Exception exception)
    {
        return exception switch
        {
            ModelNotFoundException ex => $"Model not found: {ex.Message}",
            ValidationException ex => "Validation error",
            UnauthorizedAccessException => "Unauthorized access",
            ModelTrainingException ex => $"Model training error: {ex.Message}",
            ModelEvaluationException ex => $"Model evaluation error: {ex.Message}",
            TransactionNotFoundException ex => $"Transaction not found: {ex.Message}",
            _ => "An unexpected error occurred"
        };
    }
}