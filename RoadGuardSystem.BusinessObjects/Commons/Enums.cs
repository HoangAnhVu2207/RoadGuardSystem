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
}
