namespace Car.Domain.Ports;

using Car.Domain.Agents;
using Car.Domain.Vehicles;
using Car.Domain.WorkOrders;

public interface IWorkOrderRepository
{
    Task AddAsync(WorkOrder order, CancellationToken ct);
    Task<WorkOrder?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(WorkOrder order, CancellationToken ct);
}

public interface IVehicleRepository
{
    Task<Vehicle?> GetByVinAsync(string vin, CancellationToken ct);
    Task AddAsync(Vehicle vehicle, CancellationToken ct);
}

public interface IConversationStore
{
    Task AppendTurnAsync(string sessionId, string role, string content, CancellationToken ct);
    Task<IReadOnlyList<(string Role, string Content)>> GetRecentAsync(string sessionId, int take, CancellationToken ct);
}

public interface ISessionStore
{
    Task SetAsync(string sessionId, IReadOnlyDictionary<string, string> state, TimeSpan ttl, CancellationToken ct);
    Task<IReadOnlyDictionary<string, string>?> GetAsync(string sessionId, CancellationToken ct);
}

public interface IVectorSearch
{
    Task<IReadOnlyList<Citation>> SearchAsync(string query, int k, CancellationToken ct);
}

public interface ILlmRouter
{
    Task<RoutedModel> SelectAsync(LlmRouteRequest request, CancellationToken ct);
}

public sealed record LlmRouteRequest(string Intent, int EstimatedTokens, bool RequiresVision, string TenantTier);
public sealed record RoutedModel(string Provider, string Deployment, string Reason);

public interface IToolExecutor
{
    Task<ToolExecutionResult> ExecuteAsync(ToolInvocation invocation, CancellationToken ct);
}

public sealed record ToolInvocation(string ToolName, string PayloadJson, string SessionId, string CorrelationId);
public sealed record ToolExecutionResult(bool Ok, string OutputJson, string? Error);

public interface IEventBus
{
    Task PublishAsync(string topic, string key, object payload, CancellationToken ct);
}

public interface IContentSafety
{
    Task<SafetyVerdict> EvaluateAsync(string text, CancellationToken ct);
}

public sealed record SafetyVerdict(bool Allowed, string? Category, string? Reason);

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public interface IAgentRuntime
{
    Task<AgentResponse> RunTurnAsync(AgentTurn turn, CancellationToken ct);
}
