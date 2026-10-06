using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H2NativeGeometryAndPavement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RoadSectionVersions_Geometry_AllowedSrid",
                table: "RoadSectionVersions");

            migrationBuilder.AddColumn<Guid>(
                name: "CrsProfileRevisionId",
                table: "RoadSectionVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Wgs84GeometryJson",
                table: "RoadGeometryMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CrsProfileRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    SourceSrid = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SampleOnly = table.Column<bool>(type: "bit", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrsProfileRevisions", x => x.Id);
                    table.UniqueConstraint("AK_CrsProfileRevisions_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_CrsProfileRevision", "[Revision]>0 AND [SourceSrid]>=0 AND [Status] IN ('CANDIDATE','VERIFIED') AND ISJSON([PayloadJson])=1");
                    table.ForeignKey(
                        name: "FK_CrsProfileRevisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrsProfileRevisions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeometryLocationImpacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousRouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewRouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffectedReferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeometryLocationImpacts", x => x.Id);
                    table.CheckConstraint("CK_GeometryLocationImpacts_Json", "ISJSON([AffectedReferencesJson])=1");
                    table.CheckConstraint("CK_GeometryLocationImpacts_Versions", "[PreviousRouteVersionId]<>[NewRouteVersionId]");
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpacts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpacts_RoadSectionVersions_NewRouteVersionId",
                        column: x => x.NewRouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpacts_RoadSectionVersions_PreviousRouteVersionId",
                        column: x => x.PreviousRouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpacts_Users_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadRouteSystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadRouteSystems", x => x.Id);
                    table.UniqueConstraint("AK_RoadRouteSystems_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_RoadRouteSystems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PavementLayoutRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PavementLayoutRevisions", x => x.Id);
                    table.CheckConstraint("CK_PavementLayoutRevisions_Json", "ISJSON([DefinitionJson])=1 AND ISJSON([SnapshotJson])=1");
                    table.CheckConstraint("CK_PavementLayoutRevisions_Kind", "[Kind] IN ('PLANNED','AS_BUILT') AND (([Kind]='PLANNED' AND [SourcePlanId] IS NULL) OR ([Kind]='AS_BUILT' AND [SourcePlanId] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_CrsProfileRevisions_CrsProfileRevisionId_ProjectId",
                        columns: x => new { x.CrsProfileRevisionId, x.ProjectId },
                        principalTable: "CrsProfileRevisions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_PavementLayoutRevisions_SourcePlanId",
                        column: x => x.SourcePlanId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeometryLocationImpactDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImpactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeometryLocationImpactDecisions", x => x.Id);
                    table.CheckConstraint("CK_GeometryLocationImpactDecisions_Action", "[Action] IN ('VERIFY','CONTINUE','STOP','REASSIGN')");
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpactDecisions_GeometryLocationImpacts_ImpactId",
                        column: x => x.ImpactId,
                        principalTable: "GeometryLocationImpacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpactDecisions_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NativeRouteVersionFacts",
                columns: table => new
                {
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ParentRouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JunctionOffsetMeters = table.Column<double>(type: "float", nullable: true),
                    CanonicalLengthMeters = table.Column<double>(type: "float", nullable: false),
                    DeclaredLengthMeters = table.Column<double>(type: "float", nullable: true),
                    CalibrationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SampleOnly = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NativeRouteVersionFacts", x => x.RoadSectionVersionId);
                    table.CheckConstraint("CK_NativeRouteFacts", "[CanonicalLengthMeters]>0 AND ([DeclaredLengthMeters] IS NULL OR [DeclaredLengthMeters]>0) AND (([RouteKind]='MAIN' AND [ParentRouteVersionId] IS NULL AND [JunctionOffsetMeters] IS NULL) OR ([RouteKind]='BRANCH' AND [ParentRouteVersionId] IS NOT NULL AND [JunctionOffsetMeters] IS NOT NULL AND [JunctionOffsetMeters]>=0)) AND ([CalibrationJson] IS NULL OR ISJSON([CalibrationJson])=1)");
                    table.ForeignKey(
                        name: "FK_NativeRouteVersionFacts_CrsProfileRevisions_CrsProfileRevisionId_ProjectId",
                        columns: x => new { x.CrsProfileRevisionId, x.ProjectId },
                        principalTable: "CrsProfileRevisions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NativeRouteVersionFacts_RoadRouteSystems_RouteSystemId_ProjectId",
                        columns: x => new { x.RouteSystemId, x.ProjectId },
                        principalTable: "RoadRouteSystems",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NativeRouteVersionFacts_RoadSectionVersions_ParentRouteVersionId",
                        column: x => x.ParentRouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NativeRouteVersionFacts_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeometryMapPublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LayoutRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PublicationMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeometryMapPublications", x => x.Id);
                    table.CheckConstraint("CK_GeometryMapPublications_Json", "ISJSON([SnapshotJson])=1");
                    table.CheckConstraint("CK_GeometryMapPublications_Mode", "[PublicationMode] IN ('SAMPLE','OFFICIAL')");
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_CrsProfileRevisions_CrsProfileRevisionId_ProjectId",
                        columns: x => new { x.CrsProfileRevisionId, x.ProjectId },
                        principalTable: "CrsProfileRevisions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_PavementLayoutRevisions_LayoutRevisionId",
                        column: x => x.LayoutRevisionId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_Users_PublishedBy",
                        column: x => x.PublishedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PavementSourceFileReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LayoutRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CaptureFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PavementSourceFileReferences", x => x.Id);
                    table.CheckConstraint("CK_PavementSourceFileReferences_Facts", "LEN([ContentChecksum])=64 AND ISJSON([CaptureFactsJson])=1");
                    table.ForeignKey(
                        name: "FK_PavementSourceFileReferences_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementSourceFileReferences_PavementLayoutRevisions_LayoutRevisionId",
                        column: x => x.LayoutRevisionId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadSectionVersions_CrsProfileRevisionId",
                table: "RoadSectionVersions",
                column: "CrsProfileRevisionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoadSectionVersions_Geometry_AllowedSrid",
                table: "RoadSectionVersions",
                sql: "([CrsProfileRevisionId] IS NULL AND [Geometry].STSrid IN (32648, 32649)) OR ([CrsProfileRevisionId] IS NOT NULL AND [Geometry].STSrid>=0)");

            migrationBuilder.CreateIndex(
                name: "IX_CrsProfileRevisions_CreatedBy",
                table: "CrsProfileRevisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CrsProfileRevisions_ProjectId_Code_Revision",
                table: "CrsProfileRevisions",
                columns: new[] { "ProjectId", "Code", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpactDecisions_ActorId",
                table: "GeometryLocationImpactDecisions",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpactDecisions_ImpactId_TaskId",
                table: "GeometryLocationImpactDecisions",
                columns: new[] { "ImpactId", "TaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpacts_NewRouteVersionId",
                table: "GeometryLocationImpacts",
                column: "NewRouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpacts_PreviousRouteVersionId_NewRouteVersionId",
                table: "GeometryLocationImpacts",
                columns: new[] { "PreviousRouteVersionId", "NewRouteVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpacts_ProjectId",
                table: "GeometryLocationImpacts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpacts_RecordedBy",
                table: "GeometryLocationImpacts",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_CrsProfileRevisionId_ProjectId",
                table: "GeometryMapPublications",
                columns: new[] { "CrsProfileRevisionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_LayoutRevisionId",
                table: "GeometryMapPublications",
                column: "LayoutRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_ProjectId_PublishedAt_Id",
                table: "GeometryMapPublications",
                columns: new[] { "ProjectId", "PublishedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_PublishedBy",
                table: "GeometryMapPublications",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_RouteVersionId",
                table: "GeometryMapPublications",
                column: "RouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_SegmentSetId",
                table: "GeometryMapPublications",
                column: "SegmentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_NativeRouteVersionFacts_CrsProfileRevisionId_ProjectId",
                table: "NativeRouteVersionFacts",
                columns: new[] { "CrsProfileRevisionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_NativeRouteVersionFacts_ParentRouteVersionId",
                table: "NativeRouteVersionFacts",
                column: "ParentRouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_NativeRouteVersionFacts_RouteSystemId_ProjectId",
                table: "NativeRouteVersionFacts",
                columns: new[] { "RouteSystemId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_CreatedBy",
                table: "PavementLayoutRevisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_CrsProfileRevisionId_ProjectId",
                table: "PavementLayoutRevisions",
                columns: new[] { "CrsProfileRevisionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_ProjectId",
                table: "PavementLayoutRevisions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_RouteVersionId_CreatedAt_Id",
                table: "PavementLayoutRevisions",
                columns: new[] { "RouteVersionId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_SegmentSetId",
                table: "PavementLayoutRevisions",
                column: "SegmentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_SourcePlanId",
                table: "PavementLayoutRevisions",
                column: "SourcePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementSourceFileReferences_FileId",
                table: "PavementSourceFileReferences",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementSourceFileReferences_LayoutRevisionId_FileId",
                table: "PavementSourceFileReferences",
                columns: new[] { "LayoutRevisionId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadRouteSystems_ProjectId_Code",
                table: "RoadRouteSystems",
                columns: new[] { "ProjectId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RoadSectionVersions_CrsProfileRevisions_CrsProfileRevisionId",
                table: "RoadSectionVersions",
                column: "CrsProfileRevisionId",
                principalTable: "CrsProfileRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("CREATE TRIGGER [TR_CrsProfileRevisions_Immutable] ON [dbo].[CrsProfileRevisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_NativeRouteVersionFacts_Immutable] ON [dbo].[NativeRouteVersionFacts] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_PavementLayoutRevisions_Immutable] ON [dbo].[PavementLayoutRevisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_GeometryMapPublications_Immutable] ON [dbo].[GeometryMapPublications] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_GeometryLocationImpacts_Immutable] ON [dbo].[GeometryLocationImpacts] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_GeometryLocationImpactDecisions_Immutable] ON [dbo].[GeometryLocationImpactDecisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_PavementSourceFileReferences_Immutable] ON [dbo].[PavementSourceFileReferences] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_RoadSectionVersions_ProfileScope] ON [dbo].[RoadSectionVersions] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM inserted i JOIN RoadSections r ON r.Id=i.RoadSectionId JOIN CrsProfileRevisions p ON p.Id=i.CrsProfileRevisionId WHERE p.ProjectId<>r.ProjectId OR p.SourceSrid<>i.Geometry.STSrid) THROW 51231, 'Native profile scope or SRID mismatch.', 1; END");
            migrationBuilder.Sql("CREATE OR ALTER TRIGGER [TR_RoadSectionVersions_Immutable] ON [dbo].[RoadSectionVersions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF NOT EXISTS(SELECT 1 FROM inserted) THROW 50000, 'RoadSectionVersion records cannot be deleted.', 1; IF UPDATE(RoadSectionId) OR UPDATE(VersionNo) OR UPDATE(Geometry) OR UPDATE(EffectiveFrom) OR UPDATE(ChangeReason) OR UPDATE(CrsProfileRevisionId) THROW 50001, 'RoadSectionVersion history is immutable; only IsCurrent may change.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_NativeRouteVersionFacts_Scope] ON [dbo].[NativeRouteVersionFacts] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN RoadSectionVersions v ON v.Id=i.RoadSectionVersionId JOIN RoadSections r ON r.Id=v.RoadSectionId JOIN CrsProfileRevisions c ON c.Id=i.CrsProfileRevisionId WHERE r.ProjectId<>i.ProjectId OR v.CrsProfileRevisionId IS NULL OR v.CrsProfileRevisionId<>i.CrsProfileRevisionId OR c.SampleOnly<>i.SampleOnly) THROW 51231, 'Native route scope mismatch.', 1; IF EXISTS(SELECT 1 FROM inserted i WHERE i.RouteKind='BRANCH' AND NOT EXISTS(SELECT 1 FROM NativeRouteVersionFacts p JOIN RoadSectionVersions pv ON pv.Id=p.RoadSectionVersionId JOIN RoadSectionVersions cv ON cv.Id=i.RoadSectionVersionId WHERE p.RoadSectionVersionId=i.ParentRouteVersionId AND p.ProjectId=i.ProjectId AND p.RouteSystemId=i.RouteSystemId AND p.CrsProfileRevisionId=i.CrsProfileRevisionId AND pv.RoadSectionId<>cv.RoadSectionId AND pv.IsCurrent=1 AND i.JunctionOffsetMeters<=p.CanonicalLengthMeters)) THROW 51231, 'Native branch scope mismatch.', 1; DECLARE @bad bit=0; WITH walk AS (SELECT i.RoadSectionVersionId AS RootId,v.RoadSectionId AS RootSectionId,i.ParentRouteVersionId AS NextId,CAST('|' + CONVERT(varchar(36),i.RoadSectionVersionId) + '|' AS varchar(max)) AS Seen,CAST('|' + CONVERT(varchar(36),v.RoadSectionId) + '|' AS varchar(max)) AS SeenSections,CAST(0 AS bit) AS Bad,0 AS Depth FROM inserted i JOIN RoadSectionVersions v ON v.Id=i.RoadSectionVersionId UNION ALL SELECT w.RootId,w.RootSectionId,p.ParentRouteVersionId,CAST(w.Seen + CONVERT(varchar(36),p.RoadSectionVersionId) + '|' AS varchar(max)),CAST(w.SeenSections + CONVERT(varchar(36),v.RoadSectionId) + '|' AS varchar(max)),CAST(CASE WHEN CHARINDEX('|' + CONVERT(varchar(36),p.RoadSectionVersionId) + '|',w.Seen)>0 OR CHARINDEX('|' + CONVERT(varchar(36),v.RoadSectionId) + '|',w.SeenSections)>0 THEN 1 ELSE 0 END AS bit),w.Depth+1 FROM walk w JOIN NativeRouteVersionFacts p ON p.RoadSectionVersionId=w.NextId JOIN RoadSectionVersions v ON v.Id=p.RoadSectionVersionId WHERE w.Bad=0 AND w.Depth<100000) SELECT @bad=1 FROM walk WHERE Bad=1 OR Depth>=100000 OPTION(MAXRECURSION 0); IF @bad=1 THROW 51231, 'Native route topology cycle or depth bound exceeded.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_PavementLayoutRevisions_Scope] ON [dbo].[PavementLayoutRevisions] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN RoadSectionVersions v ON v.Id=i.RouteVersionId JOIN RoadSections r ON r.Id=v.RoadSectionId JOIN RoadSegmentSets s ON s.Id=i.SegmentSetId WHERE r.ProjectId<>i.ProjectId OR s.RoadSectionVersionId<>i.RouteVersionId OR NOT EXISTS(SELECT 1 WHERE (v.CrsProfileRevisionId=i.CrsProfileRevisionId) OR (v.CrsProfileRevisionId IS NULL AND i.CrsProfileRevisionId IS NULL))) THROW 51231, 'Layout scope mismatch.', 1; IF EXISTS(SELECT 1 FROM inserted i WHERE i.Kind='AS_BUILT' AND NOT EXISTS(SELECT 1 FROM PavementLayoutRevisions p WHERE p.Id=i.SourcePlanId AND p.Kind='PLANNED' AND p.ProjectId=i.ProjectId AND p.RouteVersionId=i.RouteVersionId AND p.SegmentSetId=i.SegmentSetId AND ((p.CrsProfileRevisionId=i.CrsProfileRevisionId) OR (p.CrsProfileRevisionId IS NULL AND i.CrsProfileRevisionId IS NULL)))) THROW 51231, 'As-built source plan scope mismatch.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_GeometryMapPublications_Scope] ON [dbo].[GeometryMapPublications] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM PavementLayoutRevisions l WHERE l.Id=i.LayoutRevisionId AND l.ProjectId=i.ProjectId AND l.RouteVersionId=i.RouteVersionId AND l.SegmentSetId=i.SegmentSetId AND ((l.CrsProfileRevisionId=i.CrsProfileRevisionId) OR (l.CrsProfileRevisionId IS NULL AND i.CrsProfileRevisionId IS NULL)))) THROW 51231, 'Map source scope mismatch.', 1; IF EXISTS(SELECT 1 FROM inserted i WHERE i.PublicationMode='OFFICIAL' AND NOT EXISTS(SELECT 1 FROM CrsProfileRevisions p WHERE p.Id=i.CrsProfileRevisionId AND p.ProjectId=i.ProjectId AND p.Status='VERIFIED' AND p.SampleOnly=0)) THROW 51231, 'Official profile is not verified.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_GeometryLocationImpacts_Scope] ON [dbo].[GeometryLocationImpacts] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN RoadSectionVersions p ON p.Id=i.PreviousRouteVersionId JOIN RoadSectionVersions n ON n.Id=i.NewRouteVersionId JOIN RoadSections r ON r.Id=p.RoadSectionId WHERE p.RoadSectionId<>n.RoadSectionId OR r.ProjectId<>i.ProjectId) THROW 51231, 'Impact route scope mismatch.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_PavementSourceFileReferences_Scope] ON [dbo].[PavementSourceFileReferences] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN PavementLayoutRevisions l ON l.Id=i.LayoutRevisionId JOIN Files f ON f.Id=i.FileId WHERE f.Checksum<>i.ContentChecksum OR NOT EXISTS(SELECT 1 FROM FileScopes s WHERE s.FileId=i.FileId AND s.ProjectId=l.ProjectId AND s.Purpose<>'REPORT_PHOTO')) THROW 51231, 'Pavement source file scope or checksum mismatch.', 1; END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM CrsProfileRevisions) OR EXISTS(SELECT 1 FROM RoadRouteSystems) OR EXISTS(SELECT 1 FROM NativeRouteVersionFacts) OR EXISTS(SELECT 1 FROM PavementLayoutRevisions) OR EXISTS(SELECT 1 FROM GeometryMapPublications) OR EXISTS(SELECT 1 FROM GeometryLocationImpacts) OR EXISTS(SELECT 1 FROM GeometryLocationImpactDecisions) OR EXISTS(SELECT 1 FROM PavementSourceFileReferences) THROW 51000, 'Cannot downgrade populated H2; preserve geometry and publication history.', 1;");
            migrationBuilder.Sql("DROP TRIGGER [TR_RoadSectionVersions_ProfileScope]");
            migrationBuilder.Sql("CREATE OR ALTER TRIGGER [TR_RoadSectionVersions_Immutable] ON [dbo].[RoadSectionVersions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF NOT EXISTS(SELECT 1 FROM inserted) THROW 50000, 'RoadSectionVersion records cannot be deleted.', 1; IF UPDATE(RoadSectionId) OR UPDATE(VersionNo) OR UPDATE(Geometry) OR UPDATE(EffectiveFrom) OR UPDATE(ChangeReason) THROW 50001, 'RoadSectionVersion history is immutable; only IsCurrent may change.', 1; END");
            migrationBuilder.DropForeignKey(
                name: "FK_RoadSectionVersions_CrsProfileRevisions_CrsProfileRevisionId",
                table: "RoadSectionVersions");

            migrationBuilder.DropTable(
                name: "GeometryLocationImpactDecisions");

            migrationBuilder.DropTable(
                name: "GeometryMapPublications");

            migrationBuilder.DropTable(
                name: "NativeRouteVersionFacts");

            migrationBuilder.DropTable(
                name: "PavementSourceFileReferences");

            migrationBuilder.DropTable(
                name: "GeometryLocationImpacts");

            migrationBuilder.DropTable(
                name: "RoadRouteSystems");

            migrationBuilder.DropTable(
                name: "PavementLayoutRevisions");

            migrationBuilder.DropTable(
                name: "CrsProfileRevisions");

            migrationBuilder.DropIndex(
                name: "IX_RoadSectionVersions_CrsProfileRevisionId",
                table: "RoadSectionVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoadSectionVersions_Geometry_AllowedSrid",
                table: "RoadSectionVersions");

            migrationBuilder.DropColumn(
                name: "CrsProfileRevisionId",
                table: "RoadSectionVersions");

            migrationBuilder.DropColumn(
                name: "Wgs84GeometryJson",
                table: "RoadGeometryMetadata");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoadSectionVersions_Geometry_AllowedSrid",
                table: "RoadSectionVersions",
                sql: "[Geometry].STSrid IN (32648, 32649)");
        }
    }
}
