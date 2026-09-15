using Car.BuildingBlocks.Results;
using Car.Domain.Agents;
using Car.Domain.Ports;
using MediatR;

namespace Car.Application.Knowledge;

public sealed record SearchKnowledgeQuery(string Query, int K = 5) : IRequest<Result<IReadOnlyList<Citation>>>;

public sealed class SearchKnowledgeHandler(IVectorSearch search)
    : IRequestHandler<SearchKnowledgeQuery, Result<IReadOnlyList<Citation>>>
{
    public async Task<Result<IReadOnlyList<Citation>>> Handle(SearchKnowledgeQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return Result<IReadOnlyList<Citation>>.Failure("Query is required.");

        var hits = await search.SearchAsync(request.Query, Math.Clamp(request.K, 1, 20), ct);
        return Result<IReadOnlyList<Citation>>.Success(hits);
    }
}
