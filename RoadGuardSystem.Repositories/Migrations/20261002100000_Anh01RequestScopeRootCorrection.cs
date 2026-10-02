using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.Repositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20261002100000_Anh01RequestScopeRootCorrection")]
public sealed class Anh01RequestScopeRootCorrection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 090000 validated JSON_QUERY's first scope property, while the runtime JSON
        // reader selects the last. Inspect the raw root before trusting either value.
        // Revoke unsafe adoption only; preserve snapshots, relational refs and history.
        migrationBuilder.Sql("""
            UPDATE r SET ScopeFormatVersion=NULL
            FROM SurveyRequests r
            CROSS APPLY (SELECT CASE WHEN ISJSON(r.OutputRequirements)=1
              AND LEFT(LTRIM(REPLACE(REPLACE(REPLACE(r.OutputRequirements,
                CHAR(9),' '),CHAR(10),' '),CHAR(13),' ')),1)='{' THEN r.OutputRequirements ELSE N'{}' END body) root
            WHERE r.ScopeFormatVersion='BAND_V1'
              AND ((SELECT COUNT(*) FROM OPENJSON(root.body)
                    WHERE [key] COLLATE Latin1_General_100_BIN2=N'scope' AND DATALENGTH([key])=10)<>1
                OR (SELECT COUNT(*) FROM OPENJSON(root.body)
                    WHERE [key] COLLATE Latin1_General_100_BIN2=N'scope' AND DATALENGTH([key])=10 AND [type]=4)<>1);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Do not reactivate writes against ambiguous historical snapshots on rollback.
        migrationBuilder.Sql("THROW 51027, 'Request root scope correction cannot be undone without an owner-reviewed data preservation plan.', 1;");
    }
}
