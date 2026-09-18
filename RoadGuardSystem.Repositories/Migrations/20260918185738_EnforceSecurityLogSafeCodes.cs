using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSecurityLogSafeCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_PasswordResetLogs_Reason_SafeCode",
                table: "PasswordResetLogs",
                sql: "[Reason] IS NULL OR [Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PasswordResetLogs_Source_SafeCode",
                table: "PasswordResetLogs",
                sql: "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccountStatusChangeLogs_Reason_SafeCode",
                table: "AccountStatusChangeLogs",
                sql: "[Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccountStatusChangeLogs_Source_SafeCode",
                table: "AccountStatusChangeLogs",
                sql: "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PasswordResetLogs_Reason_SafeCode",
                table: "PasswordResetLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PasswordResetLogs_Source_SafeCode",
                table: "PasswordResetLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccountStatusChangeLogs_Reason_SafeCode",
                table: "AccountStatusChangeLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccountStatusChangeLogs_Source_SafeCode",
                table: "AccountStatusChangeLogs");
        }
    }
}
