using MessagePack;
using MessagePack.AspNetCoreMvcFormatter;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc;

namespace RepairFlow.Api.Serialization;

/// <summary>
/// Единая настройка MessagePack для всего приложения: HTTP-ответы, SignalR и кеш пользуются
/// одним и тем же резолвером, поэтому данные, записанные одним каналом, читаются другим.
/// </summary>
public static class MessagePackConfig
{
    public const string ContentType = "application/x-msgpack";

    /// <summary>
    /// Контракты нигде не размечены атрибутами: DTO — обычные record'ы, а contractless-резолвер
    /// сопоставляет поля по именам. Enum'ы едут строками — так же, как в JSON, чтобы два формата
    /// одного API не расходились в мелочах.
    /// </summary>
    private static readonly IFormatterResolver Resolver = CompositeResolver.Create(
        new IMessagePackFormatter[] { DateOnlyFormatter.Instance },
        new IFormatterResolver[]
        {
            DynamicEnumAsStringResolver.Instance,
            ContractlessStandardResolver.Instance
        });

    /// <summary>Для HTTP и SignalR — без сжатия: наружу должен уходить обычный, читаемый любым клиентом msgpack.</summary>
    public static readonly MessagePackSerializerOptions Wire = MessagePackSerializerOptions.Standard
        .WithResolver(Resolver)
        .WithSecurity(MessagePackSecurity.UntrustedData);

    /// <summary>Для кеша — с LZ4: данные никто, кроме нас, не читает, а места занимают втрое меньше.</summary>
    public static readonly MessagePackSerializerOptions Cache = MessagePackSerializerOptions.Standard
        .WithResolver(Resolver)
        .WithCompression(MessagePackCompression.Lz4BlockArray);

    /// <summary>
    /// Подключает MessagePack как второй формат REST API. JSON остаётся форматом по умолчанию,
    /// а клиент, которому важен трафик, просто присылает Accept: application/x-msgpack.
    /// </summary>
    public static void AddMessagePackFormatters(MvcOptions options)
    {
        options.InputFormatters.Add(new MessagePackInputFormatter(Wire));
        options.OutputFormatters.Add(new MessagePackOutputFormatter(Wire));
        options.FormatterMappings.SetMediaTypeMappingForFormat("msgpack", ContentType);
    }
}
