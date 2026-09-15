using Car.Domain.Ports;
using Car.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health/ready");

app.MapPost("/v1/route", async (LlmRouteRequest request, ILlmRouter router, CancellationToken ct) =>
    Results.Ok(await router.SelectAsync(request, ct)));

app.Run();
