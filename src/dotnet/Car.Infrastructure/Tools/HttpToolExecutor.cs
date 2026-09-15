using System.Net.Http.Json;
using Car.Domain.Ports;
using Polly;
using Polly.CircuitBreaker;

namespace Car.Infrastructure.Tools;

public sealed class HttpToolExecutor(HttpClient http) : IToolExecutor
{
    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 8,
            BreakDuration = TimeSpan.FromSeconds(15)
        })
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(200)
        })
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolInvocation invocation, CancellationToken ct)
    {
        try
        {
            return await Pipeline.ExecuteAsync(async token =>
            {
                var response = await http.PostAsJsonAsync("/mcp/tools/execute", invocation, token);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<ToolExecutionResult>(token);
                return body ?? new ToolExecutionResult(false, "{}", "empty_response");
            }, ct);
        }
        catch (BrokenCircuitException)
        {
            return new ToolExecutionResult(false, "{}", "circuit_open");
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, "{}", ex.Message);
        }
    }
}
