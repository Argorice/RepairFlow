using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using RepairFlow.Api.Common;

namespace RepairFlow.Api.Filters;

/// <summary>
/// Прогоняет каждый аргумент действия через зарегистрированный для него валидатор FluentValidation.
/// Контроллеры не проверяют ModelState — невалидный запрос до них просто не доходит.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                throw new ValidationFailedException(result.ToDictionary());
            }
        }

        await next();
    }
}
