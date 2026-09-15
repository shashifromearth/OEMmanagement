using Car.BuildingBlocks.Results;
using Car.Domain.Ports;
using Car.Domain.WorkOrders;
using FluentValidation;
using MediatR;

namespace Car.Application.WorkOrders;

public sealed record ScheduleServiceCommand(
    string DealerId,
    Guid VehicleId,
    string CustomerId,
    string Concern,
    DateTimeOffset When) : IRequest<Result<Guid>>;

public sealed class ScheduleServiceValidator : AbstractValidator<ScheduleServiceCommand>
{
    public ScheduleServiceValidator()
    {
        RuleFor(x => x.DealerId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Concern).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.When).Must(w => w > DateTimeOffset.UtcNow).WithMessage("Appointment must be in the future.");
    }
}

public sealed class ScheduleServiceHandler(
    IWorkOrderRepository orders,
    IUnitOfWork uow,
    IEventBus events) : IRequestHandler<ScheduleServiceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ScheduleServiceCommand request, CancellationToken ct)
    {
        var order = WorkOrder.Create(request.DealerId, request.VehicleId, request.CustomerId, request.Concern);
        order.Schedule(request.When);
        await orders.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        await events.PublishAsync("workorders.scheduled", order.Id.ToString(), new
        {
            order.Id,
            request.DealerId,
            request.When,
            request.Concern
        }, ct);
        return Result<Guid>.Success(order.Id);
    }
}
