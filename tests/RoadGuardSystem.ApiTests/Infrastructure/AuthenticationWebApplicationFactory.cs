using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RoadGuardSystem.ApiTests.Infrastructure;

public sealed class AuthenticationWebApplicationFactory : WebApplicationFactory<Program>
{
    internal const string TestIssuer = "roadguard-api-tests";
    internal const string TestAudience = "roadguard-clients";
    internal const string CurrentKeyId = "test-current";
    internal static readonly byte[] CurrentSigningKey =
        Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();

    private readonly string _connectionString;
    private readonly int? _maxPlatformLength;
    private readonly string _activeKeyId;
    private readonly int _accessTokenLifetimeMinutes;
    private readonly int _sessionLifetimeHours;
    private readonly int _refreshTokenLifetimeDays;
    private readonly ILoggerProvider? _loggerProvider;
    private readonly Action<IServiceCollection>? _configureTestServices;

    public AuthenticationWebApplicationFactory(
        string connectionString,
        int? maxPlatformLength = null,
        string activeKeyId = CurrentKeyId,
        int accessTokenLifetimeMinutes = 10,
        int sessionLifetimeHours = 8,
        int refreshTokenLifetimeDays = 30,
        ILoggerProvider? loggerProvider = null,
        Action<IServiceCollection>? configureTestServices = null)
    {
        _connectionString = connectionString;
        _maxPlatformLength = maxPlatformLength;
        _activeKeyId = activeKeyId;
        _accessTokenLifetimeMinutes = accessTokenLifetimeMinutes;
        _sessionLifetimeHours = sessionLifetimeHours;
        _refreshTokenLifetimeDays = refreshTokenLifetimeDays;
        _loggerProvider = loggerProvider;
        _configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("RoadGuardDatabase:ConnectionString", _connectionString);
        builder.UseSetting("RoadGuardDatabase:EnableSensitiveDataLogging", "false");
        builder.UseSetting("RoadGuardDatabase:MaxRetryCount", "3");
        builder.UseSetting("Jwt:Issuer", TestIssuer);
        builder.UseSetting("Jwt:Audience", TestAudience);
        builder.UseSetting("Jwt:ActiveKeyId", _activeKeyId);
        builder.UseSetting(
            $"Jwt:SigningKeys:{CurrentKeyId}",
            Convert.ToBase64String(CurrentSigningKey));
        builder.UseSetting(
            "Jwt:SigningKeys:test-next",
            Convert.ToBase64String(Enumerable.Range(65, 32).Select(value => (byte)value).ToArray()));
        builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", _accessTokenLifetimeMinutes.ToString());
        builder.UseSetting("Jwt:SessionLifetimeHours", _sessionLifetimeHours.ToString());
        builder.UseSetting("Jwt:RefreshTokenLifetimeDays", _refreshTokenLifetimeDays.ToString());
        builder.UseSetting(
            "PasswordChangeFingerprint:Key",
            Convert.ToBase64String(Enumerable.Repeat((byte)91, 32).ToArray()));
        if (_maxPlatformLength.HasValue)
        {
            builder.UseSetting("SessionDeviceMetadata:MaxPlatformLength", _maxPlatformLength.Value.ToString());
        }
        if (_loggerProvider is not null)
        {
            builder.ConfigureLogging(logging => logging.AddProvider(_loggerProvider));
        }

        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(AuthenticationWebApplicationFactory).Assembly);
            services.Configure<PasswordHasherOptions>(options => options.IterationCount = 10_000);
            _configureTestServices?.Invoke(services);
        });
    }
}
