using FluentValidation;
using RepairFlow.Api.Contracts;

namespace RepairFlow.Api.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Укажите почту.")
            .EmailAddress().WithMessage("Почта выглядит некорректно.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Придумайте пароль.")
            .MinimumLength(8).WithMessage("Пароль должен быть не короче 8 символов.")
            .MaximumLength(128);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Укажите имя — так мастеру проще к вам обращаться.")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(32)
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Телефон может содержать только цифры и символы + - ( ).")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Укажите почту.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Укажите пароль.");
    }
}

public sealed class DemoLoginRequestValidator : AbstractValidator<DemoLoginRequest>
{
    public DemoLoginRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum().WithMessage("Неизвестная роль демо-аккаунта.");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Укажите текущий пароль.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Укажите новый пароль.")
            .MinimumLength(8).WithMessage("Пароль должен быть не короче 8 символов.")
            .MaximumLength(128)
            .NotEqual(x => x.CurrentPassword).WithMessage("Новый пароль совпадает со старым.");
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Имя не может быть пустым.")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
