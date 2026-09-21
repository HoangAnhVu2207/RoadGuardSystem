using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Devices;

namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Idempotently provisions a synthetic registry device for local workflow fixtures.
/// It is not a source of operational device inventory.
/// </summary>
public sealed class DroneDeviceSeedStep : ISeedStep
{
    public static readonly Guid FixtureDeviceId = Guid.Parse("5f1e9338-3f89-48f0-9d90-097b4ced4e62");

    public const string FixtureSerialNo = "RG-DRONE-0001";

    public int Order => 20;

    public string Name => "DroneDeviceSeedStep";

    public async Task SeedAsync(RoadGuardDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await context.DroneDevices.AnyAsync(
                device => device.SerialNo == FixtureSerialNo,
                cancellationToken))
        {
            return;
        }

        context.DroneDevices.Add(DroneDevice.Create(
            FixtureDeviceId,
            FixtureSerialNo,
            DroneDeviceStatus.Active,
            model: "RoadGuard fixture device",
            checklistVersion: "v1"));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            if (!await context.DroneDevices.AnyAsync(
                    device => device.SerialNo == FixtureSerialNo,
                    cancellationToken))
            {
                throw;
            }
        }
    }
}
