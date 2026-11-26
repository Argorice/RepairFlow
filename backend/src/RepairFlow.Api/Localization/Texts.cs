using System.Globalization;

namespace RepairFlow.Api.Localization;

/// <summary>
/// Тексты, которые API отдаёт людям: ошибки, отказы, подписи статусов. Язык выбирает middleware
/// локализации по заголовку Accept-Language, по умолчанию — английский.
///
/// Языка два, поэтому оба варианта стоят рядом прямо в коде: так их невозможно рассинхронизировать,
/// и перевод виден там же, где фраза используется. Появится третий язык — это сигнал переехать
/// на .resx и IStringLocalizer; места вызова при этом останутся теми же.
/// </summary>
public static class Texts
{
    public const string DefaultLanguage = "en";

    public static readonly string[] SupportedLanguages = ["en", "ru"];

    public static bool IsRussian =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

    /// <summary>Код языка текущего запроса — для ключей кеша, где лежат уже переведённые подписи.</summary>
    public static string Language => IsRussian ? "ru" : DefaultLanguage;

    public static string Pick(string en, string ru) => IsRussian ? ru : en;
}
