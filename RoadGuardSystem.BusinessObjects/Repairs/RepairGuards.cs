using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Repairs;

internal static class RepairGuards
{
    internal static void Id(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("A non-empty identity is required.");
    }
    internal static string Text(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 2000) throw new ArgumentException("The fact exceeds its maximum length.");
        return value.Trim();
    }
    internal static DateTimeOffset Time(DateTimeOffset value)
    {
        if (value == default) throw new ArgumentException("An explicit event timestamp is required.");
        return value.ToUniversalTime();
    }
    internal static void Actor(Guid actor, UserRoleCode actual, UserRoleCode required)
    {
        Id(actor);
        if (actual != required) throw new InvalidOperationException("This role cannot perform the transition.");
    }
    internal static void EnumValue<T>(T value) where T : struct, Enum
    {
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }
}
