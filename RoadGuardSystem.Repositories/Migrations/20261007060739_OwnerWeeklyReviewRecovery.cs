using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class OwnerWeeklyReviewRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyReviewRecoveryPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecoveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsLatestAtRecovery = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReviewRecoveryPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewRecoveryPeriods_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReviewDigests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecoveryPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecoveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecipientKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientRole = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReviewDigests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigests_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigests_WeeklyReviewRecoveryPeriods_RecoveryPeriodId",
                        column: x => x.RecoveryPeriodId,
                        principalTable: "WeeklyReviewRecoveryPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReviewDigestDuties",
                columns: table => new
                {
                    DigestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    OriginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueAtRecoveryUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReviewDigestDuties", x => new { x.DigestId, x.ClockId });
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigestDuties_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigestDuties_WeeklyReviewDigests_DigestId",
                        column: x => x.DigestId,
                        principalTable: "WeeklyReviewDigests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewDigestDuties_ClockId",
                table: "WeeklyReviewDigestDuties",
                column: "ClockId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewDigests_ProjectId_ScheduledAtUtc_RecipientKey",
                table: "WeeklyReviewDigests",
                columns: new[] { "ProjectId", "ScheduledAtUtc", "RecipientKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewDigests_RecipientId",
                table: "WeeklyReviewDigests",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewDigests_RecoveryPeriodId",
                table: "WeeklyReviewDigests",
                column: "RecoveryPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewRecoveryPeriods_ProjectId_ScheduledAtUtc",
                table: "WeeklyReviewRecoveryPeriods",
                columns: new[] { "ProjectId", "ScheduledAtUtc" },
                unique: true);
            foreach (var table in new[] { "WeeklyReviewRecoveryPeriods", "WeeklyReviewDigests", "WeeklyReviewDigestDuties" })
                migrationBuilder.Sql($"CREATE TRIGGER [TR_{table}_Immutable] ON [{table}] AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51109, 'Weekly recovery facts are immutable.', 1; END;");
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_WeeklyReviewDigests_Source] ON [WeeklyReviewDigests] AFTER INSERT AS
                BEGIN SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM inserted d WHERE NOT EXISTS(SELECT 1 FROM [WeeklyReviewRecoveryPeriods] p
                  WHERE p.Id=d.RecoveryPeriodId AND p.ProjectId=d.ProjectId AND p.ScheduledAtUtc=d.ScheduledAtUtc
                    AND p.RecoveredAtUtc=d.RecoveredAtUtc AND p.IsLatestAtRecovery=1))
                  THROW 51110, 'Weekly digest must bind the exact latest recovery period.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_WeeklyReviewDigestDuties_Source] ON [WeeklyReviewDigestDuties] AFTER INSERT AS
                BEGIN SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM inserted d WHERE NOT EXISTS(SELECT 1 FROM [WeeklyReviewDigests] g
                  JOIN [DeadlineClocks] c ON c.Id=d.ClockId WHERE g.Id=d.DigestId AND c.ProjectId=g.ProjectId
                    AND c.TargetId=d.TargetId AND c.OriginEventId=d.OriginEventId AND c.OriginAt=d.OriginAtUtc AND c.CurrentDueAt=d.DueAtRecoveryUtc
                    AND c.OriginAt<=g.RecoveredAtUtc AND (c.CompletedAt IS NULL OR c.CompletedAt>g.RecoveredAtUtc)))
                  THROW 51111, 'Weekly duty must bind actual pending-at-recovery source facts.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM [WeeklyReviewRecoveryPeriods]) OR EXISTS(SELECT 1 FROM [WeeklyReviewDigests]) THROW 51108, 'Populated weekly recovery downgrade is forbidden.', 1;");
            migrationBuilder.DropTable(
                name: "WeeklyReviewDigestDuties");

            migrationBuilder.DropTable(
                name: "WeeklyReviewDigests");

            migrationBuilder.DropTable(
                name: "WeeklyReviewRecoveryPeriods");
        }
    }
}
