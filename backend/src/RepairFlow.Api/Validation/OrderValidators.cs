using FluentValidation;
using RepairFlow.Api.Contracts;

namespace RepairFlow.Api.Validation;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.DeviceType)
            .NotEmpty().WithMessage("Укажите тип устройства: ноутбук, смартфон, телевизор…")
            .MaximumLength(100);

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Укажите производителя.")
            .MaximumLength(100);

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Укажите модель.")
            .MaximumLength(100);

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100);

        RuleFor(x => x.ProblemDescription)
            .NotEmpty().WithMessage("Опишите, что случилось — чем подробнее, тем быстрее диагностика.")
            .MinimumLength(10).WithMessage("Слишком коротко: опишите проблему хотя бы одним предложением.")
            .MaximumLength(4000);

        RuleFor(x => x.Priority)
            .IsInEnum().When(x => x.Priority is not null);
    }
}

public sealed class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.DeviceType).NotEmpty().MaximumLength(100).When(x => x.DeviceType is not null);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100).When(x => x.Brand is not null);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100).When(x => x.Model is not null);
        RuleFor(x => x.SerialNumber).MaximumLength(100).When(x => x.SerialNumber is not null);

        RuleFor(x => x.ProblemDescription)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(4000)
            .When(x => x.ProblemDescription is not null);

        RuleFor(x => x.Priority).IsInEnum().When(x => x.Priority is not null);

        RuleFor(x => x)
            .Must(HasAnyField)
            .WithMessage("Запрос не содержит ни одного поля для изменения.")
            .OverridePropertyName("request");
    }

    private static bool HasAnyField(UpdateOrderRequest request) =>
        request.DeviceType is not null
        || request.Brand is not null
        || request.Model is not null
        || request.SerialNumber is not null
        || request.ProblemDescription is not null
        || request.Priority is not null;
}

public sealed class ChangeStatusRequestValidator : AbstractValidator<ChangeStatusRequest>
{
    public ChangeStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Неизвестный статус.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public sealed class AssignTechnicianRequestValidator : AbstractValidator<AssignTechnicianRequest>
{
    public AssignTechnicianRequestValidator()
    {
        RuleFor(x => x.TechnicianId)
            .NotEqual(Guid.Empty).WithMessage("Некорректный идентификатор мастера.")
            .When(x => x.TechnicianId is not null);
    }
}

public sealed class SaveOrderItemRequestValidator : AbstractValidator<SaveOrderItemRequest>
{
    public SaveOrderItemRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage("Неизвестный тип позиции.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Укажите название работы или запчасти.")
            .MaximumLength(200);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Количество должно быть больше нуля.")
            .LessThanOrEqualTo(10_000).WithMessage("Количество выглядит неправдоподобно большим.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Цена не может быть отрицательной.")
            .LessThanOrEqualTo(10_000_000).WithMessage("Цена выглядит неправдоподобно большой.");
    }
}

public sealed class RejectEstimateRequestValidator : AbstractValidator<RejectEstimateRequest>
{
    public RejectEstimateRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Комментарий не может быть пустым.")
            .MaximumLength(4000);
    }
}
