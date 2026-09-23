namespace FleetRentals.Api.Controllers.Rentals;

public sealed record StartRentalRequest(Guid VehicleId, Guid DriverId);
