using RepairFlow.Api.Domain;
using Xunit;

namespace RepairFlow.Tests;

public class OrderNumberGeneratorTests
{
    [Fact]
    public void Number_is_padded_to_four_digits()
    {
        Assert.Equal("RF-2026-0001", OrderNumberGenerator.Format(2026, 1));
    }

    [Fact]
    public void Number_grows_beyond_four_digits_without_breaking()
    {
        Assert.Equal("RF-2026-12345", OrderNumberGenerator.Format(2026, 12345));
    }

    [Fact]
    public void First_number_of_the_year_starts_from_one()
    {
        Assert.Equal("RF-2026-0001", OrderNumberGenerator.Next(2026, null));
    }

    [Fact]
    public void Next_number_increments_the_sequence()
    {
        Assert.Equal("RF-2026-0042", OrderNumberGenerator.Next(2026, "RF-2026-0041"));
    }

    [Fact]
    public void Sequence_restarts_in_a_new_year()
    {
        Assert.Equal("RF-2026-0001", OrderNumberGenerator.Next(2026, "RF-2025-0099"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2026-0001")]
    [InlineData("XX-2026-0001")]
    [InlineData("RF-2026")]
    [InlineData("RF-abcd-0001")]
    public void Unknown_format_falls_back_to_the_first_number(string garbage)
    {
        Assert.Equal("RF-2026-0001", OrderNumberGenerator.Next(2026, garbage));
    }

    [Fact]
    public void Sequence_is_parsed_back_from_the_number()
    {
        Assert.True(OrderNumberGenerator.TryParseSequence("RF-2026-0007", out var year, out var sequence));
        Assert.Equal(2026, year);
        Assert.Equal(7, sequence);
    }

    [Fact]
    public void Year_prefix_matches_the_number_format()
    {
        var prefix = OrderNumberGenerator.YearPrefix(2026);

        Assert.Equal("RF-2026-", prefix);
        Assert.StartsWith(prefix, OrderNumberGenerator.Format(2026, 15));
    }

    [Fact]
    public void Zero_sequence_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderNumberGenerator.Format(2026, 0));
    }
}
