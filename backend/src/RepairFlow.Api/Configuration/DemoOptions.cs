namespace RepairFlow.Api.Configuration;

public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Заполнять ли базу демо-данными при старте.</summary>
    public bool SeedOnStartup { get; set; } = true;

    /// <summary>Пароль всех демо-аккаунтов. Показывается на странице входа фронтенда.</summary>
    public string Password { get; set; } = "demo1234";
}
