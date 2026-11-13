using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class CrewFieldMeasurementDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_MeasurementType",
                table: "GroundTruthMeasurements");
            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_Unit",
                table: "GroundTruthMeasurements");
            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_MeasurementType",
                table: "GroundTruthMeasurements",
                sql: "[MeasurementType] IN (1, 2, 3, 4, 5, 6)");
            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_Unit",
                table: "GroundTruthMeasurements",
                sql: "([MeasurementType] IN (1,2,3,5,6) AND [Dimension]='LENGTH' AND LOWER([Unit]) IN ('mm','cm','m')) OR ([MeasurementType]=4 AND [Dimension]='AREA' AND [Unit]=N'm²')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_MeasurementType",
                table: "GroundTruthMeasurements");
            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_Unit",
                table: "GroundTruthMeasurements");
            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_MeasurementType",
                table: "GroundTruthMeasurements",
                sql: "[MeasurementType] IN (1, 2, 3, 4)");
            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_Unit",
                table: "GroundTruthMeasurements",
                sql: "([MeasurementType] IN (1,2,3) AND [Dimension]='LENGTH' AND LOWER([Unit]) IN ('mm','cm','m')) OR ([MeasurementType]=4 AND [Dimension]='AREA' AND [Unit]=N'm²')");
        }
    }
}
