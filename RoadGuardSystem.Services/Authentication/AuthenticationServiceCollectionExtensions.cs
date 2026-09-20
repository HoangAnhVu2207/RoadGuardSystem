using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Services.Identity;

namespace RoadGuardSystem.Services.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddRoadGuardAuthenticationApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction)
    {
        services.AddRoadGuardPersistence(configuration, isProduction);
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetRequiredSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<PasswordChangeFingerprintOptions>()
            .Bind(configuration.GetRequiredSection(PasswordChangeFingerprintOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PasswordChangeFingerprintOptions>, PasswordChangeFingerprintOptionsValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICredentialVerifier, IdentityCredentialVerifier>();
        services.AddScoped<AuthoritativeSessionValidator>();
        services.AddScoped<IIdentityService>(provider =>
            new IdentityService(
                provider.GetRequiredService<Repositories.Identity.IIdentityRepository>(),
                provider.GetRequiredService<IPasswordHasher<ApplicationUser>>()));
        services.AddScoped<AccessTokenFactory>(provider =>
            new AccessTokenFactory(provider.GetRequiredService<IOptions<JwtOptions>>().Value));
        services.AddScoped<PasswordChangeFingerprintFactory>(provider =>
            new PasswordChangeFingerprintFactory(
                provider.GetRequiredService<IOptions<PasswordChangeFingerprintOptions>>().Value));
        services.AddScoped<IAuthService>(provider =>
            new AuthService(
                provider.GetRequiredService<Repositories.Identity.IIdentityRepository>(),
                provider.GetRequiredService<ICredentialVerifier>(),
                provider.GetRequiredService<AccessTokenFactory>(),
                provider.GetRequiredService<IOptions<JwtOptions>>().Value,
                provider.GetRequiredService<TimeProvider>(),
                provider.GetRequiredService<PasswordChangeFingerprintFactory>(),
                provider.GetRequiredService<IOptions<SessionDeviceMetadataOptions>>().Value));

        return services;
    }
}
