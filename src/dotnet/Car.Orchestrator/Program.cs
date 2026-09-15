using Car.Application;
using Car.Application.AgentTurns;
using Car.Application.Knowledge;
using Car.Application.WorkOrders;
using Car.Infrastructure;
using MediatR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapPost("/v1/agent/turns", async (HandleAgentTurnCommand cmd, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(cmd, ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { result.Error, result.Code });
});

app.MapPost("/v1/work-orders/schedule", async (ScheduleServiceCommand cmd, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(cmd, ct);
    return result.IsSuccess ? Results.Created($"/v1/work-orders/{result.Value}", new { id = result.Value }) : Results.BadRequest(new { result.Error });
});

app.MapGet("/v1/knowledge/search", async (string q, int k, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new SearchKnowledgeQuery(q, k == 0 ? 5 : k), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { result.Error });
});

app.Run();
