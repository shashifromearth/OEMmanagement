using System.Text.Json;
using Car.Domain.Ports;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Car.Infrastructure.Messaging;

public sealed class KafkaEventBus(IProducer<string, string> producer, ILogger<KafkaEventBus> logger) : IEventBus
{
    public async Task PublishAsync(string topic, string key, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        var message = new Message<string, string> { Key = key, Value = json };
        var result = await producer.ProduceAsync(topic, message, ct);
        logger.LogInformation("Published {Topic} partition {Partition} offset {Offset}", topic, result.Partition.Value, result.Offset.Value);
    }
}

public static class KafkaTopics
{
    public const string AgentTurns = "agent.turns";
    public const string AgentRequests = "agent.requests";
    public const string AgentFailures = "agent.failures";
    public const string ToolExecutions = "tool.executions";
    public const string WorkOrdersScheduled = "workorders.scheduled";
    public const string AuditLogs = "audit.logs";
    public const string IngestionJobs = "ingestion.jobs";
}
