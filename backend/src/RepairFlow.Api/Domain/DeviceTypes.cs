namespace RepairFlow.Api.Domain;

/// <summary>
/// Тип устройства хранится кодом, а не словом: «laptop», а не «Laptop» или «Ноутбук».
/// Слово зависит от языка того, кто создавал заявку, — с таким значением английский мастер
/// увидел бы «Ноутбук», а поиск «laptop» не нашёл бы русские заявки. Код переводит интерфейс.
///
/// Для устройства, которого нет в списке, остаётся свободный текст: сервис принимает что угодно,
/// и выдумывать код для каждой кофемашины незачем.
/// </summary>
public static class DeviceTypes
{
    public const string Laptop = "laptop";
    public const string Phone = "phone";
    public const string Tablet = "tablet";
    public const string Tv = "tv";
    public const string Monitor = "monitor";
    public const string Console = "console";
    public const string Printer = "printer";

    /// <summary>
    /// Код → слова, которыми его называют люди на обоих языках интерфейса. По ним старые
    /// заявки приводятся к кодам, а API принимает и «Laptop», и «ноутбук» от сторонних клиентов.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> Aliases = new Dictionary<string, string[]>
    {
        [Laptop] = ["laptop", "notebook", "ноутбук"],
        [Phone] = ["phone", "smartphone", "mobile phone", "смартфон", "телефон"],
        [Tablet] = ["tablet", "планшет"],
        [Tv] = ["tv", "television", "телевизор"],
        [Monitor] = ["monitor", "display", "монитор"],
        [Console] = ["console", "game console", "игровая консоль", "консоль", "приставка"],
        [Printer] = ["printer", "принтер"]
    };

    public static IEnumerable<string> Known => Aliases.Keys;

    /// <summary>
    /// Приводит ввод к коду, если это известный тип, иначе возвращает аккуратно обрезанный текст.
    /// </summary>
    public static string Normalize(string input)
    {
        var trimmed = input.Trim();
        var lower = trimmed.ToLowerInvariant();

        foreach (var (code, aliases) in Aliases)
        {
            if (aliases.Contains(lower))
            {
                return code;
            }
        }

        return trimmed;
    }

    /// <summary>
    /// Коды, которые подходят под поисковую строку: «ноут» найдёт ноутбуки, хотя в базе лежит «laptop».
    /// </summary>
    public static IReadOnlyList<string> MatchingSearch(string search)
    {
        var lower = search.Trim().ToLowerInvariant();

        return lower.Length == 0
            ? []
            : Aliases
                .Where(pair => pair.Value.Any(alias => alias.Contains(lower)))
                .Select(pair => pair.Key)
                .ToList();
    }
}
