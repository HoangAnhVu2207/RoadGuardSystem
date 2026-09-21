namespace RoadGuardSystem.Repositories.Identity;

public static class SecurityLogSafeValueCodes
{
    public static class Reasons
    {
        public const string AdministratorInitiated = "ADMINISTRATOR_INITIATED";
        public const string SelfServiceAccountRecovery = "SELF_SERVICE_ACCOUNT_RECOVERY";
        public const string RegistrationApproved = "REGISTRATION_APPROVED";
        public const string SafetyPolicyViolation = "SAFETY_POLICY_VIOLATION";
        public const string NoStatusChange = "NO_STATUS_CHANGE";
        public const string AdministrativeLock = "ADMINISTRATIVE_LOCK";
        public const string SecurityIncident = "SECURITY_INCIDENT";
        public const string AccountReactivated = "ACCOUNT_REACTIVATED";
    }

    public static class Sources
    {
        public const string AdminApi = "ADMIN_API";
        public const string SelfService = "SELF_SERVICE";
        public const string IdentityService = "IDENTITY_SERVICE";
        public const string ComplianceReview = "COMPLIANCE_REVIEW";
        public const string System = "SYSTEM";
    }

    public static bool IsAllowedReason(string value) => value is
        Reasons.AdministratorInitiated or
        Reasons.SelfServiceAccountRecovery or
        Reasons.RegistrationApproved or
        Reasons.SafetyPolicyViolation or
        Reasons.NoStatusChange or
        Reasons.AdministrativeLock or
        Reasons.SecurityIncident or
        Reasons.AccountReactivated;

    public static bool IsAllowedSource(string value) => value is
        Sources.AdminApi or
        Sources.SelfService or
        Sources.IdentityService or
        Sources.ComplianceReview or
        Sources.System;
}
