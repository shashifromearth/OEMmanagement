using Azure;
using Azure.AI.ContentSafety;
using Azure.Search.Documents;
using Car.BuildingBlocks.Time;
using Car.Domain.Ports;
using Car.Infrastructure.Agents;
using Car.Infrastructure.Llm;
using Car.Infrastructure.Memory;
using Car.Infrastructure.Messaging;
using Car.Infrastructure.Persistence;
using Car.Infrastructure.Safety;
using Car.Infrastructure.Search;
using Car.Infrastructure.Session;
using Car.Infrastructure.Tools;
using Confluent.Kafka;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Car.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddDbContext<CarOpsDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Sql") ?? config["SQL_CONNECTION"]));

        services.AddScoped<IWorkOrderRepository, SqlWorkOrderRepository>();
        services.AddScoped<IVehicleRepository, SqlVehicleRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(config["REDIS_CONNECTION"] ?? "localhost:6379"));
        services.AddSingleton<ISessionStore, RedisSessionStore>();

        services.AddSingleton(sp =>
        {
            var endpoint = config["COSMOS_ENDPOINT"] ?? "https://localhost:8081";
            var key = config["COSMOS_KEY"] ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            return new CosmosClient(endpoint, key, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                MaxRetryAttemptsOnRateLimitedRequests = 9,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
            });
        });
        services.AddSingleton<IConversationStore>(sp =>
            new CosmosConversationStore(sp.GetRequiredService<CosmosClient>(),
                config["COSMOS_DATABASE"] ?? "car",
                config["COSMOS_CONTAINER"] ?? "conversations"));

        services.AddSingleton<IProducer<string, string>>(_ =>
        {
            var bootstrap = config["KAFKA_BOOTSTRAP"] ?? "localhost:9092";
            return new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = bootstrap,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 5,
                LingerMs = 5
            }).Build();
        });
        services.AddSingleton<IEventBus, KafkaEventBus>();

        var searchEndpoint = config["AZURE_SEARCH_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(searchEndpoint))
        {
            services.AddSingleton(_ => new SearchClient(
                new Uri(searchEndpoint),
                config["AZURE_SEARCH_INDEX"] ?? "vehicle-knowledge",
                new AzureKeyCredential(config["AZURE_SEARCH_KEY"] ?? "")));
            services.AddSingleton<IVectorSearch, AzureAiSearchVectorStore>();
        }
        else
        {
            services.AddSingleton<IVectorSearch, NoOpVectorSearch>();
        }

        services.Configure<LlmRouterOptions>(config.GetSection("LlmRouter"));
        services.AddSingleton<IRoutingPolicy, CostLatencyRoutingPolicy>();
        services.AddSingleton<ILlmRouter, PolicyLlmRouter>();

        services.AddHttpClient<IToolExecutor, HttpToolExecutor>(c =>
        {
            c.BaseAddress = new Uri(config["TOOL_EXECUTOR_URL"] ?? "http://tool-executor:5082");
            c.Timeout = TimeSpan.FromSeconds(20);
        });

        services.Configure<AgentRuntimeOptions>(config.GetSection("AgentRuntime"));
        services.AddHttpClient<IAgentRuntime, HttpAgentRuntime>(c =>
        {
            c.BaseAddress = new Uri(config["AGENT_RUNTIME_URL"] ?? "http://agent-runtime:8088");
            c.Timeout = TimeSpan.FromSeconds(50);
        });

        var safetyEndpoint = config["CONTENT_SAFETY_ENDPOINT"];
        services.AddSingleton<IContentSafety>(sp =>
        {
            ContentSafetyClient? client = null;
            if (!string.IsNullOrWhiteSpace(safetyEndpoint))
                client = new ContentSafetyClient(new Uri(safetyEndpoint), new AzureKeyCredential(config["CONTENT_SAFETY_KEY"] ?? ""));
            return new AzureContentSafetyGuard(client, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AzureContentSafetyGuard>>());
        });

        return services;
    }
}

internal sealed class NoOpVectorSearch : IVectorSearch
{
    public Task<IReadOnlyList<Car.Domain.Agents.Citation>> SearchAsync(string query, int k, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Car.Domain.Agents.Citation>>([]);
}
