using FluentValidation;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Localization;

namespace RepairFlow.Api.Validation;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => Texts.Pick("Enter your email.", "Укажите почту."))
            .EmailAddress().WithMessage(_ => Texts.Pick("That email doesn’t look right.", "Почта выглядит некорректно."))
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage(_ => Texts.Pick("The password must be at least 8 characters.", "Пароль должен быть не короче 8 символов."))
            .MaximumLength(128);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(_ => Texts.Pick("Enter the staff member’s name.", "Укажите имя сотрудника."))
            .MaximumLength(200);

        RuleFor(x => x.Phone).MaximumLength(32);

        RuleFor(x => x.Role).IsInEnum().WithMessage(_ => Texts.Pick("Unknown role.", "Неизвестная роль."));
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum().When(x => x.Role is not null);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).When(x => x.FullName is not null);
        RuleFor(x => x.Phone).MaximumLength(32).When(x => x.Phone is not null);

        RuleFor(x => x)
            .Must(r => r.Role is not null || r.IsActive is not null || r.FullName is not null || r.Phone is not null)
            .WithMessage(_ => Texts.Pick("The request contains no fields to change.", "Запрос не содержит ни одного поля для изменения."))
            .OverridePropertyName("request");
    }
}
