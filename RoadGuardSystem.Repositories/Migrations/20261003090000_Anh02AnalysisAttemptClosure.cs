using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.Repositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20261003090000_Anh02AnalysisAttemptClosure")]
public sealed class Anh02AnalysisAttemptClosure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE OR ALTER TRIGGER [dbo].[TR_ProcessingAttempts_AppendOnly]
        ON [dbo].[ProcessingAttempts] AFTER UPDATE, DELETE AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
                THROW 51032, 'ProcessingAttempts are append-only.', 1;
            IF EXISTS (
                SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                WHERE d.EndedAt IS NOT NULL OR d.ErrorType IS NOT NULL OR i.EndedAt IS NULL
                   OR i.EndedAt < d.StartedAt OR i.ErrorType IS NULL OR i.ErrorType NOT IN (2,3)
                   OR EXISTS (SELECT i.ProcessingJobId,i.AttemptNo,i.StartedAt,i.WorkerReference
                              EXCEPT SELECT d.ProcessingJobId,d.AttemptNo,d.StartedAt,d.WorkerReference)
                   OR NOT EXISTS (
                       SELECT 1 FROM dbo.Anh02AiMockRuns r JOIN dbo.ProcessingJobs j ON j.Id=r.ProcessingJobId
                       WHERE r.AttemptId=i.Id AND r.ProcessingJobId=i.ProcessingJobId
                         AND r.Stage='VIDEO_ANALYSIS' AND j.Mode='MOCK' AND r.CompletedAt=i.EndedAt
                         AND ((r.Status='FAILED' AND j.Status=4 AND i.ErrorType=2 AND j.CompletedAt=r.CompletedAt AND j.ErrorCode=r.ErrorCode)
                           OR (r.Status='SUCCEEDED' AND j.Status=5 AND i.ErrorType=3 AND j.CompletedAt<=r.CompletedAt)))
            ) THROW 51032, 'Only terminal ANH-02 analysis may close its pending attempt once.', 1;
        END
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE OR ALTER TRIGGER [dbo].[TR_ProcessingAttempts_AppendOnly]
        ON [dbo].[ProcessingAttempts] AFTER UPDATE, DELETE AS
        BEGIN
            SET NOCOUNT ON;
            THROW 51032, 'ProcessingAttempts are append-only.', 1;
        END
        """);
}
