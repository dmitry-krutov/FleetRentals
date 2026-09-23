using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;
using Xunit;

namespace FleetRentals.UnitTests.Domain;

public sealed class RentalTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_rental_is_active_and_finishing_it_changes_status()
    {
        var rental = Rental.Start(RentalId.NewId(), VehicleId.NewId(), DriverId.NewId(), StartedAt);

        Assert.Equal(RentalStatus.Active, rental.Status);
        Assert.Null(rental.FinishedAtUtc);
        Assert.True(rental.Finish(StartedAt.AddHours(1)).IsSuccess);
        Assert.Equal(RentalStatus.Finished, rental.Status);
        Assert.Equal(StartedAt.AddHours(1), rental.FinishedAtUtc);
    }

    [Fact]
    public void Repeated_finish_is_refused_without_changing_original_finish_time()
    {
        var rental = Rental.Start(RentalId.NewId(), VehicleId.NewId(), DriverId.NewId(), StartedAt);
        var firstFinish = StartedAt.AddMinutes(15);
        Assert.True(rental.Finish(firstFinish).IsSuccess);

        var secondFinish = rental.Finish(firstFinish.AddMinutes(15));

        Assert.True(secondFinish.IsFailure);
        Assert.Equal("rental.already_finished", secondFinish.Error.Code);
        Assert.Equal(firstFinish, rental.FinishedAtUtc);
    }

    [Fact]
    public void Finish_before_start_is_refused_without_changing_active_rental()
    {
        var rental = Rental.Start(RentalId.NewId(), VehicleId.NewId(), DriverId.NewId(), StartedAt);

        var finish = rental.Finish(StartedAt.AddSeconds(-1));

        Assert.True(finish.IsFailure);
        Assert.Equal("rental.finish_before_start", finish.Error.Code);
        Assert.Equal(RentalStatus.Active, rental.Status);
        Assert.Null(rental.FinishedAtUtc);
    }

    [Fact]
    public void Restore_validates_time_order_and_status_is_derived_from_finish_time()
    {
        var id = RentalId.NewId();
        var vehicleId = VehicleId.NewId();
        var driverId = DriverId.NewId();

        var invalid = Rental.Restore(id, vehicleId, driverId, StartedAt, StartedAt.AddMinutes(-1));
        var finished = Rental.Restore(id, vehicleId, driverId, StartedAt, StartedAt.AddMinutes(1));

        Assert.Equal("rental.finish_before_start", invalid.Error.Code);
        Assert.True(finished.IsSuccess);
        Assert.Equal(RentalStatus.Finished, finished.Value.Status);
    }

    [Fact]
    public void Time_is_converted_to_utc_and_rental_id_cannot_be_empty()
    {
        var localTime = StartedAt.ToOffset(TimeSpan.FromHours(2));
        var rental = Rental.Start(RentalId.NewId(), VehicleId.NewId(), DriverId.NewId(), localTime);

        Assert.Equal(TimeSpan.Zero, rental.StartedAtUtc.Offset);
        Assert.Equal(StartedAt, rental.StartedAtUtc);
        Assert.Equal("rental.id.invalid", RentalId.Create(Guid.Empty).Error.Code);
    }
}
