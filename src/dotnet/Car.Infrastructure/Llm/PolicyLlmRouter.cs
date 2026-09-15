using Car.Domain.Ports;
using Microsoft.Extensions.Options;

namespace Car.Infrastructure.Llm;

public sealed class LlmRouterOptions
{
    public string DefaultDeployment { get; set; } = "gpt-4o";
    public string FastDeployment { get; set; } = "gpt-4o-mini";
    public string ReasoningDeployment { get; set; } = "gpt-4o";
}

/// <summary>
/// OCP: new routing rules can be added via IRoutingPolicy without changing callers.
/// </summary>
public interface IRoutingPolicy
{
    RoutedModel Route(LlmRouteRequest request);
}

public sealed class CostLatencyRoutingPolicy(IOptions<LlmRouterOptions> options) : IRoutingPolicy
{
    public RoutedModel Route(LlmRouteRequest request)
    {
        var o = options.Value;
        if (request.RequiresVision)
            return new RoutedModel("azure-openai", o.DefaultDeployment, "vision capability required");
        if (request.Intent is "smalltalk" or "status" || request.EstimatedTokens < 800)
            return new RoutedModel("azure-openai", o.FastDeployment, "low-complexity intent");
        if (request.TenantTier == "premium" && request.Intent is "diagnostics" or "parts")
            return new RoutedModel("azure-openai", o.ReasoningDeployment, "premium diagnostic workload");
        return new RoutedModel("azure-openai", o.DefaultDeployment, "default policy");
    }
}

public sealed class PolicyLlmRouter(IRoutingPolicy policy) : ILlmRouter
{
    public Task<RoutedModel> SelectAsync(LlmRouteRequest request, CancellationToken ct) =>
        Task.FromResult(policy.Route(request));
}
