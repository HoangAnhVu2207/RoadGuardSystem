namespace RoadGuardSystem.BusinessObjects.Messaging;

// Generic text contains no protected source payload and is shared by first delivery and retry.
public static class NotificationMessageContent
{
    public static (string Title, string Body) For(NotificationEventKind kind) => kind switch
    {
        NotificationEventKind.FieldAssigned or NotificationEventKind.RepairAssigned => ("Task assigned", "A task has been assigned. Open the protected task to review its current scope."),
        NotificationEventKind.FieldSubmitted or NotificationEventKind.RepairSubmitted => ("Submission awaiting review", "A task submission is awaiting project manager review."),
        NotificationEventKind.FieldSupplementRequested or NotificationEventKind.RepairReworkRequested => ("Supplement or rework requested", "A task requires additional evidence or rework. Open the protected task for details."),
        NotificationEventKind.SupervisorApprovalRequired => ("Supervisor review required", "A current project responsibility requires Supervisor review."),
        NotificationEventKind.FastTrackConfirmedInformation => ("Fast-track confirmation recorded", "The project manager has confirmed fast-track work. This information does not request normal-mode approval."),
        NotificationEventKind.DeadlineBreached => ("Deadline overdue", "A business deadline has passed. This notification does not approve or close any work."),
        NotificationEventKind.SafetyMeasureAssigned => ("Temporary safety assigned", "A temporary safety responsibility was assigned under an approved repair plan."),
        NotificationEventKind.SafetyWarning or NotificationEventKind.SafetyInspectionDue => ("Safety responsibility requires attention", "A safety responsibility requires attention. Reading this notification does not acknowledge the danger."),
        NotificationEventKind.WeeklyReviewPending => ("Weekly pending review", "Review responsibilities remain pending for this calendar occurrence."),
        NotificationEventKind.ProjectObligationTransferred or NotificationEventKind.ProjectHandlingRenewed => ("Project responsibility updated", "A protected project responsibility has a recorded update."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
