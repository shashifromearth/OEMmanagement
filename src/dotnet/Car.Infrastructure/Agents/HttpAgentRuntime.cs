using System.Net.Http.Json;
using System.Text.Json;
using Car.Domain.Agents;
using Car.Domain.Ports;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace Car.Infrastructure.Agents;

public sealed class AgentRuntimeOptions
{
    public string BaseUrl { get; set; } = "http://agent-runtime:8088";
}

public sealed class HttpAgentRuntime(HttpClient http, IOptions<AgentRuntimeOptions> options) : IAgentRuntime
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(45) })
        .AddRetry(new Polly.Retry.RetryStrategyOptions { MaxRetryAttempts = 1, Delay = TimeSpan.FromMilliseconds(150) })
        .Build();

    public async Task<AgentResponse> RunTurnAsync(AgentTurn turn, CancellationToken ct)
    {
        http.BaseAddress ??= new Uri(options.Value.BaseUrl);
        return await Pipeline.ExecuteAsync(async token =>
        {
            var response = await http.PostAsJsonAsync("/v1/turns", turn, Json, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AgentResponse>(Json, token)
                   ?? throw new InvalidOperationException("Empty agent response.");
        }, ct);
    }
}
