using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class H4RepairContinuationSources
{
    private static void ReplaceCanonicalKinds(MigrationBuilder migrationBuilder,bool forward)
    {
        var kinds=forward?"'FIELD_START','FIELD_SUBMISSION','FIELD_ACCEPT','REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH'":"'FIELD_START','FIELD_SUBMISSION'";
        migrationBuilder.DropCheckConstraint("CK_FieldInspectionOperationOrigins_Kind","FieldInspectionOperationOrigins");
        migrationBuilder.AddCheckConstraint("CK_FieldInspectionOperationOrigins_Kind","FieldInspectionOperationOrigins",$"[Kind] IN ({kinds}) AND [SchemaVersion]=1");
        // Preserve every H3 identity, task, schema and hash check. New typed effects
        // are appended after their canonical registry row in the same transaction.
        migrationBuilder.Sql($"""
            CREATE OR ALTER TRIGGER [TR_FieldInspectionOperationOrigins_Scope] ON [dbo].[FieldInspectionOperationOrigins] AFTER INSERT AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM inserted i LEFT JOIN FieldInspectionTasks t ON t.Id=i.TaskId
                    WHERE t.Id IS NULL OR t.ProjectId<>i.ProjectId OR t.LifecycleVersion<>2
                        OR i.Id<>i.EffectId OR i.SchemaVersion<>1 OR i.Kind NOT IN ({kinds})
                        OR LEN(i.ContentHash)<>64 OR i.ContentHash COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-f]%')
                    THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
            END
            """);
    }
}
