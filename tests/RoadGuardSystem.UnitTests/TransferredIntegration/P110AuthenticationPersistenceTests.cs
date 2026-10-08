using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Options;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

public sealed class P110AuthenticationPersistenceTests
{
    [Fact(DisplayName = "P1-10 Positive: persistence DI resolves the authoritative identity repository")]
    public async Task PersistenceDi_ValidConfiguration_ResolvesIdentityRepository()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RoadGuardDatabaseOptions.SectionName}:ConnectionString"] = "Server=localhost;Database=unit_di_only;Trusted_Connection=True;TrustServerCertificate=True",
                [$"{RoadGuardDatabaseOptions.SectionName}:EnableSensitiveDataLogging"] = "false"
            })
            .Build();
        services.AddRoadGuardPersistence(configuration, isProduction: false);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IIdentityRepository>()
            .Should().BeOfType<IdentityRepository>();
    }

}
