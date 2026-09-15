using System.Text.Json;
using Car.Domain.Ports;
using Car.Infrastructure;
using Car.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health/ready");

app.MapPost("/mcp/tools/execute", async (ToolInvocation invocation, IEventBus bus, CancellationToken ct) =>
{
    var result = invocation.ToolName switch
    {
        "dms.lookup_vehicle" => new ToolExecutionResult(true, JsonSerializer.Serialize(new { vin = "1HGCM82633A004352", make = "Honda", model = "Accord" }), null),
        "crm.get_customer" => new ToolExecutionResult(true, JsonSerializer.Serialize(new { loyalty = "gold", lastVisit = "2026-08-01" }), null),
        "erp.check_parts" => new ToolExecutionResult(true, JsonSerializer.Serialize(new { sku = "BRK-001", qty = 4, warehouse = "A1" }), null),
        "booking.create_slot" => new ToolExecutionResult(true, JsonSerializer.Serialize(new { slot = "2026-09-16T09:00:00Z", bay = 3 }), null),
        "graph.send_email" => new ToolExecutionResult(true, JsonSerializer.Serialize(new { sent = true }), null),
        _ => new ToolExecutionResult(false, "{}", $"unknown_tool:{invocation.ToolName}")
    };

    await bus.PublishAsync(KafkaTopics.ToolExecutions, invocation.CorrelationId, new
    {
        invocation.ToolName,
        invocation.SessionId,
        result.Ok
    }, ct);

    return Results.Ok(result);
});

app.Run();
