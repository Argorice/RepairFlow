namespace RepairFlow.Api.Common;

/// <summary>
/// Базовое исключение приложения. Каждое несёт свой HTTP-код, поэтому контроллеры не разбирают
/// ошибки руками — этим занимается один middleware, который превращает исключение в ProblemDetails.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }

    public abstract int StatusCode { get; }

    public abstract string Title { get; }
}

/// <summary>404 — запрошенный объект не существует (или недоступен, и мы не подтверждаем его существование).</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException For(string entity, object id) =>
        new($"{entity} с идентификатором {id} не найден.");

    public override int StatusCode => StatusCodes.Status404NotFound;

    public override string Title => "Не найдено";
}

/// <summary>403 — пользователь аутентифицирован, но операция ему не разрешена.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status403Forbidden;

    public override string Title => "Доступ запрещён";
}

/// <summary>401 — не аутентифицирован либо неверные учётные данные.</summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status401Unauthorized;

    public override string Title => "Требуется авторизация";
}

/// <summary>409 — запрос корректен, но нарушает бизнес-правило (например, недопустимый переход статуса).</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status409Conflict;

    public override string Title => "Операция недопустима";
}

/// <summary>400 — ошибки валидации полей, сгруппированные по именам полей.</summary>
public sealed class ValidationFailedException : AppException
{
    public ValidationFailedException(IDictionary<string, string[]> errors)
        : base("Запрос не прошёл валидацию.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }

    public override int StatusCode => StatusCodes.Status400BadRequest;

    public override string Title => "Ошибка валидации";
}
