using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoadGuardSystem.aBusinessObjects.Commons
{
    public enum UserRoleCode : byte
    {
        Unknown = 0,
        Supervisor = 1,
        ProjectManager = 2,
        DroneOperator = 3,
        RepairCrew = 4
    }

    public enum UserStatus : byte
    {
        Unknown = 0,
        Active = 1,
        Suspended = 2,
        Pending = 3
    }

    public enum PasswordResetResult : byte
    {
        Unknown = 0,
        Success = 1,
        Failed = 2,
        Rejected = 3
    }

    public enum ProjectStatus : byte
    {
        Planning = 1,
        Active = 2,
        Closed = 3
    }

    public enum ProjectMemberStatus : byte
    {
        Active = 1,
        Ended = 2
    }

    public enum DroneDeviceStatus : byte
    {
        Active = 1,
        Maintenance = 2,
        Retired = 3
    }

    public enum WarrantyScope : byte
    {
        Unknown = 0,
        Project = 1,
        RoadSection = 2,
        ContractItem = 3,
        Other = 4
    }

    public enum WarrantyStatus : byte
    {
        Unknown = 0,
        Planned = 1,
        Active = 2,
        Expired = 3,
        Suspended = 4
    }

    public enum SurveyType : byte
    {
        Unknown = 0,
        Original = 1,
        Periodic = 2,
        Supplementary = 3
    }

    public enum SurveyPlanStatus : byte
    {
        Unknown = 0,
        Planned = 1,
        Postponed = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum SurveyRequestStatus : byte
    {
        Unknown = 0,
        NewAssigned = 1,
        Accepted = 2,
        Rejected = 3,
        Reassigned = 4,
        InProgress = 5,
        Submitted = 6,
        SupplementRequired = 7,
        Completed = 8,
        Cancelled = 9,
        Postponed = 10
    }

    public enum SurveyStatus : byte
    {
        Unknown = 0,
        Draft = 1,
        InProgress = 2,
        Submitted = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum SurveyFileType : byte
    {
        Unknown = 0,
        Video = 1,
        Srt = 2,
        Photo = 3,
        Other = 4
    }

    public enum SurveyFileSyncStatus : byte
    {
        Unknown = 0,
        Local = 1,
        Queued = 2,
        Uploading = 3,
        ServerConfirmed = 4,
        Invalid = 5
    }

    public enum SurveyDataVersionStatus : byte
    {
        Unknown = 0,
        Draft = 1,
        Uploading = 2,
        ServerConfirmed = 3,
        Invalid = 4,
        Superseded = 5
    }

    public enum SurveyDataIntegrityStatus : byte
    {
        Unknown = 0,
        Pending = 1,
        Passed = 2,
        Failed = 3
    }

    public enum SurveyDataConfirmationActor : byte
    {
        Unknown = 0,
        Backend = 1
    }

    public enum ProcessingJobStatus : byte
    {
        Unknown = 0,
        Queued = 1,
        Running = 2,
        RetryableFailure = 3,
        DataFailure = 4,
        Completed = 5,
        Cancelled = 6
    }

    public enum ProcessingAttemptErrorType : byte
    {
        Unknown = 0,
        Infrastructure = 1,
        Data = 2,
        None = 3
    }

    public enum AIModelVersionStatus : byte
    {
        Unknown = 0,
        Draft = 1,
        Released = 2,
        Retired = 3
    }

    public enum DefectSeverity : byte
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum DefectStatus : byte
    {
        Unknown = 0,
        Open = 1,
        Verified = 2,
        Rejected = 3,
        Resolved = 4
    }

    public enum DefectVerificationAction : byte
    {
        Unknown = 0,
        PreliminaryKeep = 1,
        Adjust = 2,
        Confirm = 3,
        Reject = 4,
        Merge = 5
    }

    public enum FieldInspectionTaskStatus : byte
    {
        Unknown = 0,
        NewAssigned = 1,
        Accepted = 2,
        Rejected = 3,
        InProgress = 4,
        SupplementRequired = 5,
        Submitted = 6,
        Completed = 7
    }

    public enum FieldInspectionReviewDecision : byte
    {
        Unknown = 0,
        DefectConfirmed = 1,
        NoDefect = 2
    }

    public enum FieldInspectionAssignmentStatus : byte
    {
        Unknown = 0,
        Active = 1,
        Rejected = 2,
        Ended = 3
    }

    public enum FieldInspectionPurpose : byte
    {
        Unknown = 0,
        DefectVerification = 1,
        ResearchValidation = 2
    }

    public enum FieldInspectionSessionStatus : byte
    {
        Unknown = 0,
        Draft = 1,
        Completed = 2,
        Imported = 3,
        Locked = 4
    }

    public enum MeasurementType : byte
    {
        Unknown = 0,
        DepressionDepth = 1,
        SlabFaultingHeight = 2,
        ShoulderErosionExtent = 3
    }

    public enum QualityCheckScope : byte
    {
        Unknown = 0,
        SurveyFile = 1,
        SurveyDataset = 2
    }

    public enum QualityCheckExecutionStage : byte
    {
        Unknown = 0,
        ClientPrecheck = 1,
        ServerValidation = 2
    }

    public enum QualityCheckType : byte
    {
        Unknown = 0,
        Format = 1,
        Geolocation = 2,
        TimeSync = 3,
        Clarity = 4,
        Lighting = 5,
        Coverage = 6,
        Overlap = 7,
        Completeness = 8,
        Other = 9
    }

    public enum QualityCheckStatus : byte
    {
        Unknown = 0,
        Pending = 1,
        Passed = 2,
        Failed = 3,
        Warning = 4
    }

    public enum QualityCheckActor : byte
    {
        Unknown = 0,
        DroneApp = 1,
        Backend = 2
    }

    public enum SupplementarySurveyRequestStatus : byte
    {
        Unknown = 0,
        Requested = 1,
        Approved = 2,
        Assigned = 3,
        InProgress = 4,
        Submitted = 5,
        Rejected = 6,
        Cancelled = 7
    }

    public enum OutboxDeliveryStatus : byte
    {
        Pending = 1,
        Leased = 2,
        Completed = 3,
        DeadLetter = 4
    }
}
