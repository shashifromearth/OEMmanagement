using Car.Domain.Ports;
using Car.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health/ready");

app.MapGet("/v1/memory/{sessionId}", async (string sessionId, IConversationStore store, CancellationToken ct) =>
    Results.Ok(await store.GetRecentAsync(sessionId, 20, ct)));

app.MapPost("/v1/memory/{sessionId}", async (string sessionId, MemoryTurn body, IConversationStore store, CancellationToken ct) =>
{
    await store.AppendTurnAsync(sessionId, body.Role, body.Content, ct);
    return Results.Accepted();
});

app.Run();

public sealed record MemoryTurn(string Role, string Content);
