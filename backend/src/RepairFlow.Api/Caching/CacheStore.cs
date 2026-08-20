using MessagePack;
using Microsoft.Extensions.Caching.Distributed;
using RepairFlow.Api.Serialization;

namespace RepairFlow.Api.Caching;

/// <summary>
/// Кеш поверх <see cref="IDistributedCache"/> с MessagePack вместо JSON: тот же объект занимает
/// заметно меньше места, а времени на сериализацию уходит меньше.
/// </summary>
public interface ICacheStore
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}

public sealed class MessagePackCacheStore : ICacheStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<MessagePackCacheStore> _logger;

    public MessagePackCacheStore(IDistributedCache cache, ILogger<MessagePackCacheStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync(key, ct);

        if (cached is { Length: > 0 })
        {
            try
            {
                return MessagePackSerializer.Deserialize<T>(cached, MessagePackConfig.Cache, ct);
            }
            catch (MessagePackSerializationException exception)
            {
                // Контракт DTO поменялся между деплоями — считаем это промахом кеша, а не ошибкой.
                _logger.LogWarning(exception, "Не удалось прочитать кеш по ключу {Key}, пересчитываю.", key);
                await _cache.RemoveAsync(key, ct);
            }
        }

        var value = await factory(ct);

        var payload = MessagePackSerializer.Serialize(value, MessagePackConfig.Cache, ct);
        await _cache.SetAsync(
            key,
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct);

        _logger.LogDebug("Кеш {Key} обновлён, {Bytes} байт.", key, payload.Length);

        return value;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) => _cache.RemoveAsync(key, ct);
}
