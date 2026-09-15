using Car.BuildingBlocks.Results;
using Car.Domain.Agents;
using Car.Domain.Ports;
using Car.Domain.WorkOrders;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Car.Application.AgentTurns;

public sealed record HandleAgentTurnCommand(
    string SessionId,
    string UserId,
    string Channel,
    string DealerId,
    string Message,
    IReadOnlyDictionary<string, string>? Metadata) : IRequest<Result<AgentResponse>>;

public sealed class HandleAgentTurnValidator : AbstractValidator<HandleAgentTurnCommand>
{
    public HandleAgentTurnValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DealerId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Channel).NotEmpty();
    }
}

public sealed class HandleAgentTurnHandler(
    IContentSafety safety,
    ISessionStore sessions,
    IConversationStore conversations,
    IAgentRuntime runtime,
    IEventBus events,
    ILogger<HandleAgentTurnHandler> logger) : IRequestHandler<HandleAgentTurnCommand, Result<AgentResponse>>
{
    public async Task<Result<AgentResponse>> Handle(HandleAgentTurnCommand request, CancellationToken ct)
    {
        var verdict = await safety.EvaluateAsync(request.Message, ct);
        if (!verdict.Allowed)
            return Result<AgentResponse>.Failure(verdict.Reason ?? "Blocked by content safety.", "content_safety");

        var turn = new AgentTurn(
            request.SessionId,
            request.UserId,
            request.Channel,
            request.DealerId,
            request.Message,
            request.Metadata ?? new Dictionary<string, string>());

        await conversations.AppendTurnAsync(request.SessionId, "user", request.Message, ct);

        AgentResponse response;
        try
        {
            response = await runtime.RunTurnAsync(turn, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent runtime failed for session {SessionId}", request.SessionId);
            await events.PublishAsync("agent.failures", request.SessionId, new { request.SessionId, ex.Message }, ct);
            return Result<AgentResponse>.Failure("Agent runtime unavailable. A technician will follow up.", "runtime_down");
        }

        await conversations.AppendTurnAsync(request.SessionId, "assistant", response.Content, ct);
        await sessions.SetAsync(
            request.SessionId,
            new Dictionary<string, string>
            {
                ["lastAgent"] = response.ProducedBy.ToString(),
                ["userId"] = request.UserId,
                ["dealerId"] = request.DealerId
            },
            TimeSpan.FromHours(4),
            ct);

        await events.PublishAsync("agent.turns", request.SessionId, response, ct);
        return Result<AgentResponse>.Success(response);
    }
}
