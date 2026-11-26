using System.Globalization;

namespace RepairFlow.Tests;

/// <summary>
/// Временно переключает язык интерфейса — так делает middleware локализации на каждый запрос.
/// Без этого тексты зависели бы от языка машины, на которой гоняют тесты.
/// </summary>
internal sealed class UiCulture : IDisposable
{
    private readonly CultureInfo _previous;

    private UiCulture(string name)
    {
        _previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(name);
    }

    public static IDisposable Use(string name) => new UiCulture(name);

    public void Dispose() => CultureInfo.CurrentUICulture = _previous;
}
