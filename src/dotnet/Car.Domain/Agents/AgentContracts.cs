namespace Car.Domain.Agents;

public enum AgentKind
{
    Supervisor = 0,
    Intent = 1,
    Booking = 2,
    Diagnostics = 3,
    Parts = 4,
    CustomerCare = 5,
    Safety = 6
}

public sealed record AgentTurn(
    string SessionId,
    string UserId,
    string Channel,
    string DealerId,
    string Message,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record AgentDecision(
    AgentKind NextAgent,
    string Intent,
    double Confidence,
    IReadOnlyList<string> RequiredTools);

public sealed record AgentResponse(
    string SessionId,
    string Content,
    AgentKind ProducedBy,
    IReadOnlyList<Citation> Citations,
    bool RequiresHumanApproval);
