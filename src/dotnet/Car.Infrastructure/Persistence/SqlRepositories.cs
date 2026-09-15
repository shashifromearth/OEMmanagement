using Car.Domain.Ports;
using Car.Domain.Vehicles;
using Car.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace Car.Infrastructure.Persistence;

public sealed class SqlWorkOrderRepository(CarOpsDbContext db) : IWorkOrderRepository
{
    public Task AddAsync(WorkOrder order, CancellationToken ct) =>
        db.WorkOrders.AddAsync(order, ct).AsTask();

    public Task<WorkOrder?> GetAsync(Guid id, CancellationToken ct) =>
        db.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task SaveAsync(WorkOrder order, CancellationToken ct)
    {
        db.WorkOrders.Update(order);
        return Task.CompletedTask;
    }
}

public sealed class SqlVehicleRepository(CarOpsDbContext db) : IVehicleRepository
{
    public Task<Vehicle?> GetByVinAsync(string vin, CancellationToken ct) =>
        db.Vehicles.FirstOrDefaultAsync(x => x.Vin == vin, ct);

    public Task AddAsync(Vehicle vehicle, CancellationToken ct) =>
        db.Vehicles.AddAsync(vehicle, ct).AsTask();
}

public sealed class EfUnitOfWork(CarOpsDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
