using Car.Domain.Vehicles;
using Car.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace Car.Infrastructure.Persistence;

public sealed class CarOpsDbContext(DbContextOptions<CarOpsDbContext> options) : DbContext(options)
{
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>(b =>
        {
            b.ToTable("WorkOrders");
            b.HasKey(x => x.Id);
            b.Property(x => x.DealerId).HasMaxLength(64).IsRequired();
            b.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Concern).HasMaxLength(2000).IsRequired();
            b.Property(x => x.TechnicianId).HasMaxLength(64);
            b.Ignore(x => x.DomainEvents);
            b.HasIndex(x => new { x.DealerId, x.Status });
        });

        modelBuilder.Entity<Vehicle>(b =>
        {
            b.ToTable("Vehicles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Vin).HasMaxLength(17).IsRequired();
            b.HasIndex(x => x.Vin).IsUnique();
            b.Ignore(x => x.DomainEvents);
        });
    }
}
