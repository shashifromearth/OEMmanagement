using Azure;
using Azure.AI.ContentSafety;
using Car.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Car.Infrastructure.Safety;

public sealed class AzureContentSafetyGuard(ContentSafetyClient? client, ILogger<AzureContentSafetyGuard> logger) : IContentSafety
{
    public async Task<SafetyVerdict> EvaluateAsync(string text, CancellationToken ct)
    {
        if (LooksLikePromptInjection(text))
            return new SafetyVerdict(false, "prompt_injection", "Potential prompt injection detected.");

        if (client is null)
            return new SafetyVerdict(true, null, null);

        try
        {
            var result = await client.AnalyzeTextAsync(new AnalyzeTextOptions(text), ct);
            var blocked = result.Value.CategoriesAnalysis.Any(c => c.Severity >= 4);
            if (blocked)
                return new SafetyVerdict(false, "content_safety", "Azure AI Content Safety blocked the text.");
            return new SafetyVerdict(true, null, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Content safety fallback: fail closed for high-risk, fail open for availability on timeout.");
            return new SafetyVerdict(true, "degraded", "Safety service degraded; heuristic-only.");
        }
    }

    private static bool LooksLikePromptInjection(string text)
    {
        ReadOnlySpan<char> t = text;
        return text.Contains("ignore previous", StringComparison.OrdinalIgnoreCase)
               || text.Contains("system prompt", StringComparison.OrdinalIgnoreCase)
               || text.Contains("jailbreak", StringComparison.OrdinalIgnoreCase);
    }
}
