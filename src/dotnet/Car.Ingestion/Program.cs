using Car.Infrastructure;
using Car.Infrastructure.Messaging;
using Confluent.Kafka;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddInfrastructure(ctx.Configuration);
        services.AddHostedService<IngestionConsumer>();
    })
    .Build();

await host.RunAsync();

public sealed class IngestionConsumer(IConfiguration config, ILogger<IngestionConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = config["KAFKA_BOOTSTRAP"] ?? "localhost:9092",
            GroupId = "car-ingestion",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            IsolationLevel = IsolationLevel.ReadCommitted
        }).Build();

        consumer.Subscribe(KafkaTopics.IngestionJobs);
        logger.LogInformation("Ingestion worker subscribed to {Topic}", KafkaTopics.IngestionJobs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                logger.LogInformation("Ingest job {Key}: {Value}", cr.Message.Key, cr.Message.Value);
                consumer.Commit(cr);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume failed");
            }
        }
    }
}
