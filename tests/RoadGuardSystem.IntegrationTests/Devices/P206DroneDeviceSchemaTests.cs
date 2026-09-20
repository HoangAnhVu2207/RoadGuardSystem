using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Seeding;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Devices;

[Trait("TaskId", "P2-06")]
public sealed class P206DroneDeviceSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P206DroneDeviceSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-06: valid drone device round-trips through SQL Server")]
    public async Task DroneDevice_ValidRecord_RoundTrips()
    {
        var device = DroneDevice.Create(
            Guid.NewGuid(),
            $"P206-SERIAL-{Guid.NewGuid():N}",
            DroneDeviceStatus.Maintenance,
            model: "Fixture model",
            checklistVersion: "v2");

        await using (var context = _fixture.CreateDbContext())
        {
            context.DroneDevices.Add(device);
            await context.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateDbContext();
        var persisted = await readContext.DroneDevices
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == device.Id);

        persisted.SerialNo.Should().Be(device.SerialNo);
        persisted.Status.Should().Be(DroneDeviceStatus.Maintenance);
        persisted.Model.Should().Be("Fixture model");
        persisted.ChecklistVersion.Should().Be("v2");
    }

    [Fact(DisplayName = "P2-06: duplicate drone serial numbers are rejected")]
    public async Task DroneDevice_DuplicateSerialNo_IsRejectedBySqlServer()
    {
        var serialNo = $"P206-SERIAL-{Guid.NewGuid():N}";
        await using (var seedContext = _fixture.CreateDbContext())
        {
            seedContext.DroneDevices.Add(DroneDevice.Create(
                Guid.NewGuid(),
                serialNo,
                DroneDeviceStatus.Active));
            await seedContext.SaveChangesAsync();
        }

        await using var context = _fixture.CreateDbContext();
        context.DroneDevices.Add(DroneDevice.Create(
            Guid.NewGuid(),
            serialNo,
            DroneDeviceStatus.Retired));

        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-06: invalid drone status is rejected by SQL Server")]
    public async Task DroneDevice_InvalidStatus_IsRejectedBySqlServer()
    {
        var id = Guid.NewGuid();
        var serialNo = $"P206-INVALID-{Guid.NewGuid():N}";
        await using var context = _fixture.CreateDbContext();

        var insert = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [DroneDevices] ([Id], [SerialNo], [Model], [Status], [ChecklistVersion])
            VALUES ({id}, {serialNo}, {"Fixture model"}, {99}, {"v1"})
            """);

        await insert.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-06: drone device seed is idempotent")]
    public async Task DroneDeviceSeedStep_RepeatedExecution_PersistsOneFixtureDevice()
    {
        var step = new DroneDeviceSeedStep();
        await using var context = _fixture.CreateDbContext();

        await step.SeedAsync(context);
        await step.SeedAsync(context);

        var matchingDevices = await context.DroneDevices
            .AsNoTracking()
            .Where(device => device.SerialNo == DroneDeviceSeedStep.FixtureSerialNo)
            .ToListAsync();

        matchingDevices.Should().ContainSingle();
        matchingDevices[0].Id.Should().Be(DroneDeviceSeedStep.FixtureDeviceId);
        matchingDevices[0].Status.Should().Be(DroneDeviceStatus.Active);
    }
}
