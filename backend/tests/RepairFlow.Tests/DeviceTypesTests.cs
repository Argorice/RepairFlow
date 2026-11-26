using RepairFlow.Api.Domain;
using Xunit;

namespace RepairFlow.Tests;

/// <summary>
/// Тип устройства хранится кодом. Эти тесты держат главное: слово на любом из языков
/// интерфейса сводится к одному коду, а неизвестное устройство остаётся свободным текстом.
/// </summary>
public class DeviceTypesTests
{
    [Theory]
    [InlineData("Laptop", DeviceTypes.Laptop)]
    [InlineData("  ноутбук ", DeviceTypes.Laptop)]
    [InlineData("Ноутбук", DeviceTypes.Laptop)]
    [InlineData("Смартфон", DeviceTypes.Phone)]
    [InlineData("TV", DeviceTypes.Tv)]
    [InlineData("Игровая консоль", DeviceTypes.Console)]
    [InlineData("laptop", DeviceTypes.Laptop)]
    public void Known_words_in_either_language_become_one_code(string input, string expected)
    {
        Assert.Equal(expected, DeviceTypes.Normalize(input));
    }

    [Fact]
    public void Unknown_device_stays_free_text()
    {
        Assert.Equal("Кофемашина", DeviceTypes.Normalize("  Кофемашина "));
    }

    [Fact]
    public void Search_word_finds_the_code_whatever_the_language()
    {
        Assert.Contains(DeviceTypes.Laptop, DeviceTypes.MatchingSearch("ноут"));
        Assert.Contains(DeviceTypes.Laptop, DeviceTypes.MatchingSearch("Laptop"));
        Assert.Empty(DeviceTypes.MatchingSearch("RF-2026-0025"));
    }
}
