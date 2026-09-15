using Car.BuildingBlocks.Domain;

namespace Car.Domain.Vehicles;

public sealed class Vehicle : AggregateRoot
{
    public string Vin { get; private set; } = string.Empty;
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string CustomerId { get; private set; } = string.Empty;
    public int Mileage { get; private set; }

    private Vehicle() { }

    public static Vehicle Register(string vin, string make, string model, int year, string customerId, int mileage)
    {
        if (string.IsNullOrWhiteSpace(vin) || vin.Length is < 11 or > 17)
            throw new ArgumentException("VIN is invalid.", nameof(vin));

        return new Vehicle
        {
            Vin = vin.ToUpperInvariant(),
            Make = make,
            Model = model,
            Year = year,
            CustomerId = customerId,
            Mileage = mileage
        };
    }

    public void UpdateMileage(int mileage)
    {
        if (mileage < Mileage)
            throw new InvalidOperationException("Mileage cannot decrease.");
        Mileage = mileage;
        Touch();
    }
}
