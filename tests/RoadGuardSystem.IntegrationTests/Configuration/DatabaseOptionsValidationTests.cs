using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Options;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Configuration;

[Trait("TaskId", "P2-00")]
public sealed class DatabaseOptionsValidationTests
{
    private readonly RoadGuardDatabaseOptionsValidator _validator = new();

    [Fact(DisplayName = "Negative: Missing connection string fails options validation")]
    public void Missing_ConnectionString_Fails_Validation()
    {
        var options = new RoadGuardDatabaseOptions
        {
            ConnectionString = string.Empty
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue("empty connection string must fail validation");
        result.FailureMessage.Should().Contain("ConnectionString");
    }

    [Fact(DisplayName = "Negative: Whitespace connection string fails options validation")]
    public void Whitespace_ConnectionString_Fails_Validation()
    {
        var options = new RoadGuardDatabaseOptions
        {
            ConnectionString = "   "
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue("whitespace connection string must fail validation");
    }

    [Fact(DisplayName = "Negative: Malformed connection string fails validation")]
    public void Malformed_ConnectionString_Fails_Validation()
    {
        var options = new RoadGuardDatabaseOptions
        {
            ConnectionString = "This is not a valid connection string;;; == 123"
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue("malformed connection string must fail validation");
    }

    [Fact(DisplayName = "Negative: TrustServerCertificate=true in production options is strictly forbidden")]
    public void TrustServerCertificate_In_Production_Fails_Validation()
    {
        var options = new RoadGuardDatabaseOptions
        {
            ConnectionString = "Server=sql.production.internal;Database=RoadGuard;User Id=app;Password=secret;TrustServerCertificate=true",
            IsProduction = true
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue("TrustServerCertificate=true must be rejected in production configuration");
        result.FailureMessage.Should().Contain("TrustServerCertificate");
    }

    [Fact(DisplayName = "Negative: ValidateOrThrow throws on invalid options immediately")]
    public void ValidateOrThrow_Throws_Immediately_On_Invalid_Options()
    {
        var options = new RoadGuardDatabaseOptions
        {
            ConnectionString = ""
        };

        var act = () => RoadGuardDatabaseOptionsValidator.ValidateOrThrow(options);

        act.Should().Throw<ArgumentException>("invalid database configuration must fail fast on startup");
    }

    [Fact(DisplayName = "Positive: Valid secure connection string passes validation")]
    public void Valid_ConnectionString_Passes_Validation()
    {
        var options = new RoadGuardDatabaseOptions
        {
            ConnectionString = "Server=sql.internal;Database=RoadGuard;User Id=app;Password=secret;Encrypt=true;TrustServerCertificate=false",
            IsProduction = true
        };

        var result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue("valid secure connection string must pass validation");
    }

    [Fact(DisplayName = "Negative: AddRoadGuardPersistence fails fast when configuration is missing")]
    public void AddRoadGuardPersistence_FailsFast_When_Configuration_Missing()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

        var act = () => RoadGuardSystem.Repositories.Extensions.RoadGuardPersistenceExtensions.AddRoadGuardPersistence(
            services, configuration);

        act.Should().Throw<ArgumentException>("missing configuration must fail fast on DI registration");
    }

    [Fact(DisplayName = "Positive: AddRoadGuardPersistence succeeds when valid configuration is supplied")]
    public void AddRoadGuardPersistence_Succeeds_When_Valid_Configuration_Supplied()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["RoadGuardDatabase:ConnectionString"] = "Server=sql.test.internal;Database=RoadGuard;Integrated Security=True;TrustServerCertificate=false",
            ["RoadGuardDatabase:IsProduction"] = "false"
        };
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var act = () => RoadGuardSystem.Repositories.Extensions.RoadGuardPersistenceExtensions.AddRoadGuardPersistence(
            services, configuration);

        act.Should().NotThrow();
    }
}
