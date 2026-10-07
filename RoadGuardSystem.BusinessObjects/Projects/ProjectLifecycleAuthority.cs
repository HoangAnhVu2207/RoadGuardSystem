using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Projects;

public enum ProjectLifecycleCommandKind : byte
{
    DefectClose = 1, LinkedRecurrenceCreate = 2, ConstructionComplete = 3, OperationalClose = 4,
    ObligationTransferGrant = 5, ObligationTransferAccept = 6, RenewedHandlingScope = 7
}

/// <summary>Policy seam. Current membership alone never grants an unassigned lifecycle command.
/// The renewed-handling source facts must come from locked actual project history, not caller selectors.</summary>
public static class ProjectLifecycleAuthority
{
    public static string Evaluate(ProjectLifecycleCommandKind command, UserRoleCode role, bool currentProjectAuthority,
        bool verifiedOperationalClosure)
    {
        if (!Enum.IsDefined(command)) return "lifecycle_command_unregistered";
        if (!currentProjectAuthority || role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.RepairCrew))
            return "access_forbidden";
        if (command is ProjectLifecycleCommandKind.LinkedRecurrenceCreate or ProjectLifecycleCommandKind.ConstructionComplete)
            return role == UserRoleCode.ProjectManager ? "success" : "access_forbidden";
        if (command != ProjectLifecycleCommandKind.RenewedHandlingScope)
            return role == UserRoleCode.Supervisor ? "success" : "access_forbidden";
        if (role != UserRoleCode.Supervisor) return "access_forbidden";
        return verifiedOperationalClosure ? "success" : "operational_closure_source_unavailable";
    }
}
