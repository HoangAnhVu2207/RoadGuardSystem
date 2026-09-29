using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P2ValidationMeasurementProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ValidationRuns",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.CreateTable(
                name: "DerivedMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyDataVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SampleId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MeasurementType = table.Column<byte>(type: "tinyint", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    UncertaintyEstimate = table.Column<decimal>(type: "decimal(19,6)", nullable: true),
                    SourceType = table.Column<byte>(type: "tinyint", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DerivedMeasurements", x => x.Id);
                    table.CheckConstraint("CK_DerivedMeasurements_MeasurementType", "[MeasurementType] IN (1, 2, 3)");
                    table.CheckConstraint("CK_DerivedMeasurements_SourceType", "[SourceType] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_DerivedMeasurements_Status", "[Status] IN (1, 2, 3)");
                    table.CheckConstraint("CK_DerivedMeasurements_UncertaintyEstimate", "[UncertaintyEstimate] IS NULL OR [UncertaintyEstimate] >= 0");
                    table.CheckConstraint("CK_DerivedMeasurements_Unit", "LOWER([Unit]) IN ('mm', 'cm', 'm')");
                    table.CheckConstraint("CK_DerivedMeasurements_Value", "[Value] >= 0");
                    table.ForeignKey(
                        name: "FK_DerivedMeasurements_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DerivedMeasurements_SurveyDataVersions_SurveyDataVersionId",
                        column: x => x.SurveyDataVersionId,
                        principalTable: "SurveyDataVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementValidationSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroundTruthMeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DerivedMeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignedError = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    AbsoluteError = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    InclusionStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementValidationSamples", x => x.Id);
                    table.CheckConstraint("CK_MeasurementValidationSamples_InclusionStatus", "[InclusionStatus] IN (1, 2, 3)");
                    table.CheckConstraint("CK_MeasurementValidationSamples_Reason", "[InclusionStatus] = 1 OR LEN(LTRIM(RTRIM([ExclusionReason]))) > 0");
                    table.ForeignKey(
                        name: "FK_MeasurementValidationSamples_DerivedMeasurements_DerivedMeasurementId",
                        column: x => x.DerivedMeasurementId,
                        principalTable: "DerivedMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeasurementValidationSamples_GroundTruthMeasurements_GroundTruthMeasurementId",
                        column: x => x.GroundTruthMeasurementId,
                        principalTable: "GroundTruthMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeasurementValidationSamples_ValidationRuns_ValidationRunId",
                        column: x => x.ValidationRunId,
                        principalTable: "ValidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DerivedMeasurements_DataSampleTypeStatus",
                table: "DerivedMeasurements",
                columns: new[] { "SurveyDataVersionId", "SampleId", "MeasurementType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DerivedMeasurements_RoadSectionVersionId",
                table: "DerivedMeasurements",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementValidationSamples_DerivedMeasurementId",
                table: "MeasurementValidationSamples",
                column: "DerivedMeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementValidationSamples_GroundTruthMeasurementId",
                table: "MeasurementValidationSamples",
                column: "GroundTruthMeasurementId");

            migrationBuilder.CreateIndex(
                name: "UX_MeasurementValidationSamples_RunPair",
                table: "MeasurementValidationSamples",
                columns: new[] { "ValidationRunId", "GroundTruthMeasurementId", "DerivedMeasurementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeasurementValidationSamples");

            migrationBuilder.DropTable(
                name: "DerivedMeasurements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ValidationRuns");
        }
    }
}
