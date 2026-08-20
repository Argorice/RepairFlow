using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RepairFlow.Api.Common;

namespace RepairFlow.Api.Middleware;

/// <summary>
/// Единственное место, где исключение превращается в ответ. Благодаря этому в контроллерах нет
/// ни одного try/catch, а формат ошибки везде одинаковый — ProblemDetails по RFC 7807.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteAsync(context, exception);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Ошибка после начала отправки ответа — перехватить её уже нельзя.");
            throw exception;
        }

        var problem = Build(context, exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Необработанная ошибка при {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogInformation(
                "Запрос отклонён ({Status}) при {Method} {Path}: {Message}",
                problem.Status,
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }

    private ProblemDetails Build(HttpContext context, Exception exception)
    {
        switch (exception)
        {
            case ValidationFailedException validation:
            {
                var problem = new ValidationProblemDetails(validation.Errors.ToDictionary(e => e.Key, e => e.Value))
                {
                    Status = validation.StatusCode,
                    Title = validation.Title,
                    Detail = validation.Message,
                    Instance = context.Request.Path
                };

                return problem;
            }

            case AppException app:
                return new ProblemDetails
                {
                    Status = app.StatusCode,
                    Title = app.Title,
                    Detail = app.Message,
                    Instance = context.Request.Path
                };

            case OperationCanceledException:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status499ClientClosedRequest,
                    Title = "Запрос отменён",
                    Detail = "Клиент разорвал соединение до завершения обработки.",
                    Instance = context.Request.Path
                };

            default:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Внутренняя ошибка сервера",
                    // Наружу текст исключения уезжает только в разработке.
                    Detail = _environment.IsDevelopment()
                        ? exception.ToString()
                        : "Что-то пошло не так. Мы уже знаем и разбираемся.",
                    Instance = context.Request.Path
                };
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseAppExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
