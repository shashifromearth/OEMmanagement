using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Car.Domain.Agents;
using Car.Domain.Ports;

namespace Car.Infrastructure.Search;

public sealed class AzureAiSearchVectorStore(SearchClient client) : IVectorSearch
{
    public async Task<IReadOnlyList<Citation>> SearchAsync(string query, int k, CancellationToken ct)
    {
        var options = new SearchOptions
        {
            Size = k,
            IncludeTotalCount = false,
            QueryType = SearchQueryType.Semantic
        };
        options.Select.Add("id");
        options.Select.Add("source");
        options.Select.Add("content");

        var response = await client.SearchAsync<SearchDocument>(query, options, ct);
        var hits = new List<Citation>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var source = result.Document.TryGetValue("source", out var s) ? s?.ToString() ?? "unknown" : "unknown";
            var id = result.Document.TryGetValue("id", out var i) ? i?.ToString() ?? "" : "";
            hits.Add(new Citation(source, id, result.Score ?? 0));
        }

        return hits;
    }
}
