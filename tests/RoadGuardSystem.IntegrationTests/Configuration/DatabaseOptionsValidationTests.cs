using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Extensions;
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
            ConnectionString = "Server=sql.production.internal;Database=RoadGuard;User Id=app;Password=secret;TrustServerCertificate=true"
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue("TrustServerCertificate=true must be rejected in production configuration");
        result.FailureMessage.Should().Contain("TrustServerCertificate");
    }

    [Fact(DisplayName = "Negative: Encrypt=false in production options is strictly forbidden")]
    public void Encrypt_False_In_Production_Fails_Validation()
    {
        var options = new RoadGuardDatabaseOptions
        {
            ConnectionString = "Server=sql.production.internal;Database=RoadGuard;User Id=app;Password=secret;Encrypt=false;TrustServerCertificate=false"
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue("Encrypt=false must be rejected in production configuration");
        result.FailureMessage.Should().Contain("Encrypt");
    }

    [Fact(DisplayName = "Negative: EnableSensitiveDataLogging=true in production options is strictly forbidden")]
    public void EnableSensitiveDataLogging_In_Production_Fails_Validation()
    {
        var options = new RoadGuardDatabaseOptions
        {
            ConnectionString = "Server=sql.production.internal;Database=RoadGuard;User Id=app;Password=secret;Encrypt=true;TrustServerCertificate=false",
            EnableSensitiveDataLogging = true
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue("EnableSensitiveDataLogging=true must be rejected in production configuration to protect PII");
        result.FailureMessage.Should().Contain("SensitiveDataLogging");
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
            ConnectionString = "Server=sql.internal;Database=RoadGuard;User Id=app;Password=secret;Encrypt=true;TrustServerCertificate=false"
        };

        var result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue("valid secure connection string must pass validation");
    }

    [Fact(DisplayName = "Negative: Configuration cannot bypass production security by setting IsProduction=false")]
    public void Configuration_Cannot_Bypass_Production_Security_By_Setting_IsProduction_False()
    {
        var services = new ServiceCollection();
        // Attacker or misconfiguration tries to disable production checks via configuration JSON
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["RoadGuardDatabase:ConnectionString"] = "Server=sql.production.internal;Database=RoadGuard;User Id=app;Password=secret;Encrypt=false;TrustServerCertificate=true",
            ["RoadGuardDatabase:IsProduction"] = "false",
            ["RoadGuardDatabase:EnableSensitiveDataLogging"] = "true"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Host composition root declares production mode is true
        var act = () => services.AddRoadGuardPersistence(configuration, isProduction: true);

        act.Should().Throw<ArgumentException>("configuration cannot bypass production security rules by supplying IsProduction=false");
    }

    [Fact(DisplayName = "Negative: AddRoadGuardPersistence fails fast when configuration is missing")]
    public void AddRoadGuardPersistence_FailsFast_When_Configuration_Missing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddRoadGuardPersistence(services.BuildServiceProvider().GetService<IConfiguration>() ?? configuration);

        act.Should().Throw<ArgumentException>("missing configuration must fail fast on DI registration");
    }

    [Fact(DisplayName = "Positive: AddRoadGuardPersistence succeeds when valid configuration is supplied")]
    public void AddRoadGuardPersistence_Succeeds_When_Valid_Configuration_Supplied()
    {
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["RoadGuardDatabase:ConnectionString"] = "Server=sql.test.internal;Database=RoadGuard;Integrated Security=True;Encrypt=true;TrustServerCertificate=false"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var act = () => services.AddRoadGuardPersistence(configuration, isProduction: true);

        act.Should().NotThrow();
    }
}
