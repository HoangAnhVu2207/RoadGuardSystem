using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Seeding;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Transactions;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Repositories.Extensions;

/// <summary>
/// Service collection extension methods for registering RoadGuard persistence and SQL Server + NetTopologySuite DbContext.
/// </summary>
public static class RoadGuardPersistenceExtensions
{
    /// <summary>
    /// Registers RoadGuard database options with fail-fast validation and configures RoadGuardDbContext with SQL Server and NetTopologySuite.
    /// Production security mode is passed explicitly from the host environment, preventing configuration tampering.
    /// Missing or malformed configuration fails immediately on registration.
    /// </summary>
    public static IServiceCollection AddRoadGuardPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction = true,
        Action<RoadGuardDatabaseOptions>? configure = null)
    {
        var options = new RoadGuardDatabaseOptions();
        var section = configuration.GetSection(RoadGuardDatabaseOptions.SectionName);
        if (section.Exists())
        {
            section.Bind(options);
        }

        configure?.Invoke(options);

        // Fail-fast: validate immediately during registration with isProduction from host environment
        RoadGuardDatabaseOptionsValidator.ValidateOrThrow(options, isProduction);

        services.Configure<RoadGuardDatabaseOptions>(opt =>
        {
            if (section.Exists())
            {
                section.Bind(opt);
            }
            configure?.Invoke(opt);
        });

        services.AddSingleton<IValidateOptions<RoadGuardDatabaseOptions>>(
            new RoadGuardDatabaseOptionsValidator(isProduction));

        services.Configure<SessionDeviceMetadataOptions>(opt =>
        {
            var metadataSection = configuration.GetSection(SessionDeviceMetadataOptions.SectionName);
            if (metadataSection.Exists())
            {
                metadataSection.Bind(opt);
            }
        });

        services.Configure<FileStorageOptions>(opt =>
        {
            var fileStorageSection = configuration.GetSection(FileStorageOptions.SectionName);
            if (fileStorageSection.Exists())
            {
                fileStorageSection.Bind(opt);
            }
        });

        services.AddScoped<RoadGuardTransactionService>();
        services.AddScoped<IdempotencyOperationService>();
        services.AddScoped<ConsumerEffectService>();
        services.AddScoped<NotificationOutboxConsumer>();
        services.AddScoped<ProjectMembershipReadModel>();
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddSingleton<IFileContentStore>(provider =>
            new LocalFileContentStore(provider.GetRequiredService<IOptions<FileStorageOptions>>().Value));
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<Microsoft.AspNetCore.Identity.IUserStore<RoadGuardSystem.BusinessObjects.Identity.ApplicationUser>, RoadGuardUserStore>();
        services.AddScoped<Microsoft.AspNetCore.Identity.IRoleStore<RoadGuardSystem.BusinessObjects.Identity.ApplicationRole>, RoadGuardRoleStore>();

        services.AddDbContext<RoadGuardDbContext>((sp, dbContextOptions) =>
        {
            var dbOpts = sp.GetRequiredService<IOptions<RoadGuardDatabaseOptions>>().Value;
            dbContextOptions.UseSqlServer(dbOpts.ConnectionString, sqlOptions =>
            {
                sqlOptions.UseNetTopologySuite();
                sqlOptions.CommandTimeout(dbOpts.CommandTimeoutSeconds);
                if (dbOpts.MaxRetryCount > 0)
                {
                    sqlOptions.EnableRetryOnFailure(dbOpts.MaxRetryCount);
                }
            });

            if (dbOpts.EnableDetailedErrors)
            {
                dbContextOptions.EnableDetailedErrors();
            }

            if (dbOpts.EnableSensitiveDataLogging)
            {
                dbContextOptions.EnableSensitiveDataLogging();
            }
        });

        return services;
    }

    /// <summary>
    /// Registers the database seeder framework and any registered seed steps.
    /// </summary>
    public static IServiceCollection AddRoadGuardSeeding(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddScoped<ISeedStep, IdentityRoleSeedStep>();
        services.AddScoped<ISeedStep, DroneDeviceSeedStep>();
        return services;
    }
}
