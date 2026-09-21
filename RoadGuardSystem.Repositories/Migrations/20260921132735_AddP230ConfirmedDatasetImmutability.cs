using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddP230ConfirmedDatasetImmutability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_SurveyDataVersions_Immutable]
                ON [dbo].[SurveyDataVersions]
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF UPDATE([SurveyId]) OR UPDATE([VersionNo])
                    BEGIN
                        THROW 51008, 'Survey dataset identity is immutable.', 1;
                    END

                    IF UPDATE([Status]) AND EXISTS
                    (
                        SELECT 1
                        FROM inserted AS [current]
                        INNER JOIN deleted AS [previous]
                            ON [previous].[Id] = [current].[Id]
                        WHERE [previous].[Status] IN (3, 5)
                            AND [current].[Status] NOT IN (3, 5)
                    )
                    BEGIN
                        THROW 51010, 'Confirmed or superseded survey dataset cannot regress status.', 1;
                    END

                    IF UPDATE([SourceManifest]) AND EXISTS
                    (
                        SELECT 1
                        FROM inserted AS [current]
                        INNER JOIN deleted AS [previous]
                            ON [previous].[Id] = [current].[Id]
                        WHERE [previous].[Status] IN (3, 5)
                    )
                    BEGIN
                        THROW 51009, 'Confirmed or superseded survey dataset manifest is immutable.', 1;
                    END
                END
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_SurveyDataVersions_Immutable];");

        }
    }
}
