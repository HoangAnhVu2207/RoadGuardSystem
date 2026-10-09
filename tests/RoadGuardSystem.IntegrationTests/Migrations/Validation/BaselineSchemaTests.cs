using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class BaselineSchemaTests
{
    [Fact]
    public void FreshDatabaseStartsWithDirectBaselineContainingEveryCurrentTable()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer("Server=localhost;Database=SchemaMetadataOnly;Integrated Security=True", sql => sql.UseNetTopologySuite()).Options;
        using var context = new RoadGuardDbContext(options);
        var assembly = context.GetService<IMigrationsAssembly>();
        var migration = Assert.Single(assembly.Migrations.Where(item =>
            item.Key.EndsWith("_BaselineCurrentSchema", StringComparison.Ordinal)));
        var operations = assembly.CreateMigration(migration.Value, "Microsoft.EntityFrameworkCore.SqlServer").UpOperations;
        var modelTables = context.GetService<IDesignTimeModel>().Model.GetRelationalModel().Tables.Select(table => table.Name).Order().ToArray();
        Assert.Equal(modelTables, operations.OfType<CreateTableOperation>().Select(table => table.Name).Order().ToArray());
        Assert.DoesNotContain(operations, operation => operation is DropTableOperation or RenameTableOperation or AlterColumnOperation);
    }
}
