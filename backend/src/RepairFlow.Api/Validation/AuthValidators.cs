using FluentValidation;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Localization;

namespace RepairFlow.Api.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => Texts.Pick("Enter your email.", "Укажите почту."))
            .EmailAddress().WithMessage(_ => Texts.Pick("That email doesn’t look right.", "Почта выглядит некорректно."))
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => Texts.Pick("Choose a password.", "Придумайте пароль."))
            .MinimumLength(8).WithMessage(_ => Texts.Pick("The password must be at least 8 characters.", "Пароль должен быть не короче 8 символов."))
            .MaximumLength(128);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(_ => Texts.Pick("Enter your name — it helps the technician address you.", "Укажите имя — так мастеру проще к вам обращаться."))
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(32)
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage(_ => Texts.Pick("A phone number may only contain digits and + - ( ).", "Телефон может содержать только цифры и символы + - ( )."))
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage(_ => Texts.Pick("Enter your email.", "Укажите почту."));
        RuleFor(x => x.Password).NotEmpty().WithMessage(_ => Texts.Pick("Enter your password.", "Укажите пароль."));
    }
}

public sealed class DemoLoginRequestValidator : AbstractValidator<DemoLoginRequest>
{
    public DemoLoginRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum().WithMessage(_ => Texts.Pick("Unknown demo account role.", "Неизвестная роль демо-аккаунта."));
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage(_ => Texts.Pick("Enter your current password.", "Укажите текущий пароль."));

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(_ => Texts.Pick("Enter a new password.", "Укажите новый пароль."))
            .MinimumLength(8).WithMessage(_ => Texts.Pick("The password must be at least 8 characters.", "Пароль должен быть не короче 8 символов."))
            .MaximumLength(128)
            .NotEqual(x => x.CurrentPassword).WithMessage(_ => Texts.Pick("The new password is the same as the old one.", "Новый пароль совпадает со старым."));
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(_ => Texts.Pick("The name can’t be empty.", "Имя не может быть пустым."))
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
