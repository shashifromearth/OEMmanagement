using System.Text.Json;
using Car.Domain.Ports;
using Microsoft.Azure.Cosmos;

namespace Car.Infrastructure.Memory;

public sealed class CosmosConversationStore(CosmosClient client, string databaseId, string containerId) : IConversationStore
{
    private readonly Container _container = client.GetContainer(databaseId, containerId);

    public async Task AppendTurnAsync(string sessionId, string role, string content, CancellationToken ct)
    {
        var item = new
        {
            id = Guid.NewGuid().ToString(),
            sessionId,
            role,
            content,
            ts = DateTimeOffset.UtcNow
        };
        await _container.CreateItemAsync(item, new PartitionKey(sessionId), cancellationToken: ct);
    }

    public async Task<IReadOnlyList<(string Role, string Content)>> GetRecentAsync(string sessionId, int take, CancellationToken ct)
    {
        var query = new QueryDefinition(
                "SELECT TOP @take c.role, c.content FROM c WHERE c.sessionId = @sid ORDER BY c.ts DESC")
            .WithParameter("@take", take)
            .WithParameter("@sid", sessionId);

        var results = new List<(string, string)>();
        using var iterator = _container.GetItemQueryIterator<JsonElement>(query);
        while (iterator.HasMoreResults)
        {
            foreach (var doc in await iterator.ReadNextAsync(ct))
                results.Add((doc.GetProperty("role").GetString()!, doc.GetProperty("content").GetString()!));
        }

        results.Reverse();
        return results;
    }
}
