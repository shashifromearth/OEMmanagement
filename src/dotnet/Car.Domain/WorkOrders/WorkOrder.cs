using Car.BuildingBlocks.Domain;

namespace Car.Domain.WorkOrders;

public enum WorkOrderStatus
{
    Draft = 0,
    Scheduled = 1,
    InProgress = 2,
    WaitingParts = 3,
    Completed = 4,
    Cancelled = 5
}

public sealed class WorkOrder : AggregateRoot
{
    public string DealerId { get; private set; } = string.Empty;
    public Guid VehicleId { get; private set; }
    public string CustomerId { get; private set; } = string.Empty;
    public string Concern { get; private set; } = string.Empty;
    public WorkOrderStatus Status { get; private set; }
    public DateTimeOffset? ScheduledAt { get; private set; }
    public string? TechnicianId { get; private set; }
    public decimal EstimatedCost { get; private set; }

    private WorkOrder() { }

    public static WorkOrder Create(string dealerId, Guid vehicleId, string customerId, string concern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dealerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(concern);

        var order = new WorkOrder
        {
            DealerId = dealerId,
            VehicleId = vehicleId,
            CustomerId = customerId,
            Concern = concern,
            Status = WorkOrderStatus.Draft
        };
        order.Raise(new WorkOrderCreated(order.Id, dealerId, vehicleId));
        return order;
    }

    public void Schedule(DateTimeOffset when)
    {
        if (Status is WorkOrderStatus.Cancelled or WorkOrderStatus.Completed)
            throw new InvalidOperationException("Cannot schedule a closed work order.");
        ScheduledAt = when;
        Status = WorkOrderStatus.Scheduled;
        Touch();
        Raise(new WorkOrderScheduled(Id, when));
    }

    public void AssignTechnician(string technicianId)
    {
        TechnicianId = technicianId;
        Status = WorkOrderStatus.InProgress;
        Touch();
    }

    public void WaitForParts() => Status = WorkOrderStatus.WaitingParts;
    public void Complete() => Status = WorkOrderStatus.Completed;
    public void SetEstimate(decimal cost) => EstimatedCost = cost;
}

public sealed record WorkOrderCreated(Guid WorkOrderId, string DealerId, Guid VehicleId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record WorkOrderScheduled(Guid WorkOrderId, DateTimeOffset When) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
