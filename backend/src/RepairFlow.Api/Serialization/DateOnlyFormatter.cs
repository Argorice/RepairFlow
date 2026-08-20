using System.Globalization;
using MessagePack;
using MessagePack.Formatters;

namespace RepairFlow.Api.Serialization;

/// <summary>
/// MessagePack не знает про <see cref="DateOnly"/> из коробки, поэтому дата едет строкой ISO-8601.
/// Строка, а не число дней, — чтобы поток можно было прочитать любым клиентом, хоть питоновским.
/// </summary>
public sealed class DateOnlyFormatter : IMessagePackFormatter<DateOnly>
{
    public static readonly DateOnlyFormatter Instance = new();

    private const string Format = "yyyy-MM-dd";

    public void Serialize(ref MessagePackWriter writer, DateOnly value, MessagePackSerializerOptions options) =>
        writer.Write(value.ToString(Format, CultureInfo.InvariantCulture));

    public DateOnly Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var raw = reader.ReadString();

        if (raw is null)
        {
            throw new MessagePackSerializationException("Ожидалась дата в формате yyyy-MM-dd, получен nil.");
        }

        return DateOnly.ParseExact(raw, Format, CultureInfo.InvariantCulture);
    }
}
