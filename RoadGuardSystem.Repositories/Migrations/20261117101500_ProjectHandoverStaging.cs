using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.Repositories;

namespace RoadGuardSystem.cRepositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20261117101500_ProjectHandoverStaging")]
public sealed class ProjectHandoverStaging : Migration
{
    private const string CheckName = "CK_FileScopes_PrivateShape";
    private const string TableName = "FileScopes";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(CheckName, TableName);
        migrationBuilder.AddCheckConstraint(CheckName, TableName,
            "[ProjectId] IS NOT NULL OR ([Purpose] IN ('REPORT_PHOTO','PROJECT_HANDOVER_STAGING') AND [TargetId] IS NULL)");
        migrationBuilder.Sql("""
            ALTER TRIGGER [TR_FileScopes_Immutable] ON [FileScopes] AFTER UPDATE, DELETE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                  SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id = d.Id
                  WHERE i.Id IS NULL OR d.ProjectId IS NOT NULL OR d.TargetId IS NOT NULL
                    OR d.Purpose <> 'PROJECT_HANDOVER_STAGING'
                    OR i.ProjectId IS NULL OR i.TargetId IS NULL OR i.TargetId <> i.ProjectId OR i.Purpose <> 'DOCUMENT'
                    OR i.FileId <> d.FileId OR i.OwnerUserId <> d.OwnerUserId OR i.CreatedAt <> d.CreatedAt
                    OR NOT EXISTS (
                        SELECT 1 FROM UploadSessions u
                        WHERE u.FileId = i.FileId AND u.OwnerUserId = i.OwnerUserId
                          AND u.Purpose = 'DOCUMENT' AND u.Status = 4)
                    OR NOT EXISTS (
                        SELECT 1 FROM HandoverDocuments h
                        WHERE h.FileId = i.FileId AND h.ProjectId = i.ProjectId AND h.AcceptedByUserId = i.OwnerUserId)
                )
                THROW 51113, 'Immutable integration history cannot be rewritten.', 1;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM [FileScopes] WHERE [ProjectId] IS NULL AND [Purpose] = 'PROJECT_HANDOVER_STAGING')
                THROW 51113, 'Unfinalized project handover files prevent downgrade.', 1;
            """);
        migrationBuilder.Sql("""
            ALTER TRIGGER [TR_FileScopes_Immutable] ON [FileScopes] AFTER UPDATE, DELETE AS
            BEGIN
                SET NOCOUNT ON;
                THROW 51113, 'Immutable integration history cannot be rewritten.', 1;
            END;
            """);
        migrationBuilder.DropCheckConstraint(CheckName, TableName);
        migrationBuilder.AddCheckConstraint(CheckName, TableName,
            "[ProjectId] IS NOT NULL OR ([Purpose] = 'REPORT_PHOTO' AND [TargetId] IS NULL)");
    }
}
