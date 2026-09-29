using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P2AllowProcessingJobModelVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ProcessingJobs_Block",
                table: "ProcessingJobs");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_Block",
                table: "ProcessingJobs",
                column: "ProcessingBlockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessingJobs_Block",
                table: "ProcessingJobs");

            migrationBuilder.CreateIndex(
                name: "UX_ProcessingJobs_Block",
                table: "ProcessingJobs",
                column: "ProcessingBlockId",
                unique: true);
        }
    }
}
