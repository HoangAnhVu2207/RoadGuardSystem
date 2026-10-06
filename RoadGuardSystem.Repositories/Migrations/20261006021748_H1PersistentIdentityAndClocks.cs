using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H1PersistentIdentityAndClocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_ExpiresAt",
                table: "Sessions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "Sessions",
                type: "datetimeoffset(7)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(7)");

            migrationBuilder.AddColumn<string>(
                name: "IssuedRole",
                table: "Sessions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Lifecycle",
                table: "Sessions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "RefreshTokens",
                type: "datetimeoffset(7)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(7)");

            migrationBuilder.CreateTable(
                name: "DeadlineClocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OriginalDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CurrentDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgmentEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineClocks", x => x.Id);
                    table.CheckConstraint("CK_DeadlineClocks_Acknowledgment", "([AcknowledgedAt] IS NULL AND [AcknowledgedByUserId] IS NULL AND [AcknowledgmentEventId] IS NULL) OR ([Kind]=9 AND [AcknowledgedAt] IS NOT NULL AND [AcknowledgedAt]>=[OriginAt] AND [AcknowledgedByUserId] IS NOT NULL AND [AcknowledgmentEventId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [CompletedAt]=[AcknowledgedAt])");
                    table.CheckConstraint("CK_DeadlineClocks_Kind", "[Kind] BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_DeadlineClocks_Times", "[OriginalDueAt]>[OriginAt] AND [CurrentDueAt]>=[OriginalDueAt] AND ([CompletedAt] IS NULL OR [CompletedAt]>=[OriginAt])");
                    table.ForeignKey(
                        name: "FK_DeadlineClocks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineClocks_Users_AcknowledgedByUserId",
                        column: x => x.AcknowledgedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeadlineBreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineBreaches", x => x.Id);
                    table.CheckConstraint("CK_DeadlineBreaches_Times", "[ObservedAt]>=[DueAt]");
                    table.ForeignKey(
                        name: "FK_DeadlineBreaches_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeadlineExtensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PreviousDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NewDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PreviousDeadlineBreached = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineExtensions", x => x.Id);
                    table.CheckConstraint("CK_DeadlineExtensions_Times", "[NewDueAt]>[PreviousDueAt] AND [NewDueAt]>[OccurredAt]");
                    table.ForeignKey(
                        name: "FK_DeadlineExtensions_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineExtensions_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_ExpiresAt",
                table: "Sessions",
                sql: "([Lifecycle] = 0 AND [ExpiresAt] IS NOT NULL AND [ExpiresAt] > [IssuedAt]) OR ([Lifecycle] = 1 AND [ExpiresAt] IS NULL AND [IssuedRole] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineBreaches_ClockId_DueAt",
                table: "DeadlineBreaches",
                columns: new[] { "ClockId", "DueAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_AcknowledgedByUserId",
                table: "DeadlineClocks",
                column: "AcknowledgedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_Kind_TargetId",
                table: "DeadlineClocks",
                columns: new[] { "Kind", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_OriginEventId",
                table: "DeadlineClocks",
                column: "OriginEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_ProjectId_CompletedAt_CurrentDueAt",
                table: "DeadlineClocks",
                columns: new[] { "ProjectId", "CompletedAt", "CurrentDueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineExtensions_ActorUserId",
                table: "DeadlineExtensions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineExtensions_ClockId_NewDueAt",
                table: "DeadlineExtensions",
                columns: new[] { "ClockId", "NewDueAt" },
                unique: true);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DeadlineExtensions_Immutable] ON [DeadlineExtensions] AFTER UPDATE, DELETE AS
                BEGIN SET NOCOUNT ON; THROW 51001, 'Deadline extension history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DeadlineBreaches_Immutable] ON [DeadlineBreaches] AFTER UPDATE, DELETE AS
                BEGIN SET NOCOUNT ON; THROW 51002, 'Deadline breach history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DeadlineClocks_ImmutableOrigin] ON [DeadlineClocks] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT [Id] FROM deleted EXCEPT SELECT [Id] FROM inserted)
                      OR EXISTS (SELECT [Id],[ProjectId],[Kind],[TargetId],[OriginEventId],[OriginAt],[OriginalDueAt] FROM deleted
                        EXCEPT SELECT [Id],[ProjectId],[Kind],[TargetId],[OriginEventId],[OriginAt],[OriginalDueAt] FROM inserted)
                      OR EXISTS (SELECT 1 FROM deleted d JOIN inserted i ON d.Id=i.Id WHERE
                        i.CurrentDueAt<d.CurrentDueAt OR
                        (d.CompletedAt IS NOT NULL AND (i.CompletedAt IS NULL OR i.CompletedAt<>d.CompletedAt)) OR
                        (d.AcknowledgedAt IS NOT NULL AND (i.AcknowledgedAt IS NULL OR i.AcknowledgedAt<>d.AcknowledgedAt OR
                          i.AcknowledgedByUserId IS NULL OR i.AcknowledgedByUserId<>d.AcknowledgedByUserId OR
                          i.AcknowledgmentEventId IS NULL OR i.AcknowledgmentEventId<>d.AcknowledgmentEventId)))
                        THROW 51003, 'Deadline origin and acknowledged/completed history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Sessions_ImmutableLifecycle] ON [Sessions] AFTER UPDATE AS
                BEGIN SET NOCOUNT ON;
                    IF EXISTS (SELECT Id,Lifecycle,IssuedRole FROM deleted EXCEPT SELECT Id,Lifecycle,IssuedRole FROM inserted)
                        THROW 51005, 'Session lifecycle and issued role are immutable; legacy sessions cannot be promoted.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RefreshTokens_LifecycleExpiry] ON [RefreshTokens] AFTER INSERT, UPDATE AS
                BEGIN SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN Sessions s ON i.SessionId=s.Id WHERE i.ExpiresAt IS NULL AND s.Lifecycle<>1)
                        THROW 51006, 'Non-expiring credentials require a persistent session.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [DeadlineClocks]) OR EXISTS (SELECT 1 FROM [DeadlineExtensions]) OR
                   EXISTS (SELECT 1 FROM [DeadlineBreaches]) OR EXISTS (SELECT 1 FROM [Sessions] WHERE [Lifecycle]=1) OR
                   EXISTS (SELECT 1 FROM [RefreshTokens] WHERE [ExpiresAt] IS NULL) OR
                   EXISTS (SELECT 1 FROM [IdempotencyRecords] WHERE [Operation]='RefreshRotation')
                    THROW 51004, 'H1 rollback refuses to discard persistent credentials or immutable clock history.', 1;
                """);
            migrationBuilder.Sql("DROP TRIGGER [TR_DeadlineClocks_ImmutableOrigin]; DROP TRIGGER [TR_DeadlineExtensions_Immutable]; DROP TRIGGER [TR_DeadlineBreaches_Immutable];");

            migrationBuilder.Sql("DROP TRIGGER [TR_Sessions_ImmutableLifecycle]; DROP TRIGGER [TR_RefreshTokens_LifecycleExpiry];");

            migrationBuilder.DropTable(
                name: "DeadlineBreaches");

            migrationBuilder.DropTable(
                name: "DeadlineExtensions");

            migrationBuilder.DropTable(
                name: "DeadlineClocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_ExpiresAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IssuedRole",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Lifecycle",
                table: "Sessions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "Sessions",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "RefreshTokens",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(7)",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_ExpiresAt",
                table: "Sessions",
                sql: "[ExpiresAt] > [IssuedAt]");
        }
    }
}
