using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace RoadGuardSystem.Repositories.Seeding.Sandbox;

public static class SandboxSeedGuard
{
    public static async Task ValidateTargetAsync(RoadGuardDbContext db, SandboxSeedTarget target, CancellationToken ct = default)
    {
        var connection = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        var session = target.Mode == "Session" && connection.DataSource == "sql" &&
            target.DatabaseName == $"RoadGuard_FE_{target.OwnerId:N}";
        var test = target.Mode == "Test" && Regex.IsMatch(connection.DataSource, @"^(127\.0\.0\.1|localhost),[0-9]{1,5}$") &&
            Regex.IsMatch(target.DatabaseName, @"^RoadGuard_(Test|ApiTest)_[a-f0-9]{32}$");
        if ((!session && !test) || target.OwnerId == Guid.Empty || connection.InitialCatalog != target.DatabaseName ||
            !Regex.IsMatch(target.ExpectedContainerId, "^[a-f0-9]{64}$") || string.IsNullOrWhiteSpace(target.ExpectedServer))
            throw new InvalidOperationException("TargetRejected");
        var server = await ScalarAsync(db, "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'));", ct);
        var name = await ScalarAsync(db, "SELECT DB_NAME();", ct);
        var owner = await ScalarAsync(db, "SELECT CONVERT(nvarchar(128),value) FROM sys.extended_properties WHERE class=0 AND name=N'RoadGuardSandboxOwner';", ct);
        if (server != target.ExpectedServer || name != target.DatabaseName || !Guid.TryParse(owner, out var parsed) || parsed != target.OwnerId)
            throw new InvalidOperationException("TargetRejected");
    }

    public static async Task<SandboxSchemaFacts> ReadSchemaAsync(RoadGuardDbContext db, CancellationToken ct = default)
    {
        var tables = db.Model.GetRelationalModel().Tables.Select(t => (t.Schema ?? "dbo") + "." + t.Name).Order(StringComparer.Ordinal).ToArray();
        var actual = await ScalarAsync(db, "SELECT STRING_AGG(CONVERT(nvarchar(max),s.name+'.'+t.name),N'|') WITHIN GROUP(ORDER BY s.name,t.name) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.name<>N'__EFMigrationsHistory';", ct);
        if (!tables.SequenceEqual((actual ?? "").Split('|').Order(StringComparer.Ordinal))) throw new InvalidOperationException("SchemaMismatch");
        var migrations = (await db.Database.GetAppliedMigrationsAsync(ct)).ToArray();
        if (!(db.Database.GetMigrations()).SequenceEqual(migrations)) throw new InvalidOperationException("SchemaMismatch");
        // Include column, key, constraint, relationship and index definitions, without data.
        var structure = await ScalarAsync(db, """
SELECT (SELECT s.name AS [schema],t.name AS [table],
 (SELECT c.name,ty.name AS [type],c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,c.is_computed,
    dc.definition AS [default],cc.definition AS computed
  FROM sys.columns c JOIN sys.types ty ON c.user_type_id=ty.user_type_id
  LEFT JOIN sys.default_constraints dc ON c.default_object_id=dc.object_id
  LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
  WHERE c.object_id=t.object_id ORDER BY c.column_id FOR JSON PATH) AS columns,
 (SELECT i.name,i.is_unique,i.is_primary_key,i.filter_definition,
   (SELECT c.name,ic.key_ordinal,ic.is_descending_key,ic.is_included_column FROM sys.index_columns ic
    JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
    WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id ORDER BY ic.index_column_id FOR JSON PATH) AS columns
  FROM sys.indexes i WHERE i.object_id=t.object_id AND i.index_id>0 ORDER BY i.name FOR JSON PATH) AS indexes,
 (SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints WHERE parent_object_id=t.object_id ORDER BY name FOR JSON PATH) AS checks,
 (SELECT fk.name,rs.name AS target_schema,rt.name AS target_table,fk.delete_referential_action,fk.update_referential_action,fk.is_disabled,fk.is_not_trusted,
  (SELECT pc.name AS source_column,rc.name AS target_column FROM sys.foreign_key_columns fc
   JOIN sys.columns pc ON pc.object_id=fc.parent_object_id AND pc.column_id=fc.parent_column_id
   JOIN sys.columns rc ON rc.object_id=fc.referenced_object_id AND rc.column_id=fc.referenced_column_id
   WHERE fc.constraint_object_id=fk.object_id ORDER BY fc.constraint_column_id FOR JSON PATH) AS columns
  FROM sys.foreign_keys fk JOIN sys.tables rt ON rt.object_id=fk.referenced_object_id JOIN sys.schemas rs ON rs.schema_id=rt.schema_id
  WHERE fk.parent_object_id=t.object_id ORDER BY fk.name FOR JSON PATH) AS foreign_keys
 FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.name<>N'__EFMigrationsHistory' ORDER BY s.name,t.name FOR JSON PATH);
""", ct);
        return new(tables, migrations, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(structure ?? ""))));
    }

    internal static async Task<string?> ScalarAsync(RoadGuardDbContext db, string sql, CancellationToken ct)
    {
        if (db.Database.GetDbConnection().State != ConnectionState.Open) await db.Database.OpenConnectionAsync(ct);
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql; cmd.CommandTimeout = 60;
        cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        var value = await cmd.ExecuteScalarAsync(ct);
        return value is null or DBNull ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
