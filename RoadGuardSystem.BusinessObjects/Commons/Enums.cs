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
}
