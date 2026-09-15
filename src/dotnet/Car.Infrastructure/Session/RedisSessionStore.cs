using System.Text.Json;
using Car.Domain.Ports;
using StackExchange.Redis;

namespace Car.Infrastructure.Session;

public sealed class RedisSessionStore(IConnectionMultiplexer redis) : ISessionStore
{
    public async Task SetAsync(string sessionId, IReadOnlyDictionary<string, string> state, TimeSpan ttl, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(state);
        await db.StringSetAsync($"session:{sessionId}", json, ttl);
    }

    public async Task<IReadOnlyDictionary<string, string>?> GetAsync(string sessionId, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync($"session:{sessionId}");
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(value!) ?? new Dictionary<string, string>();
    }
}
