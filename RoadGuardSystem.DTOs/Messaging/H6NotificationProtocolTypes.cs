using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.DTOs.Messaging;

// One exact lease/parser inventory. A registered type still requires its actual source adapter;
// this list does not activate pending business commands or certify a producer as ready.
public static class H6NotificationProtocolTypes
{
    public const string RegistryVersion = NotificationRegisteredTypes.RegistryVersion;
    public static IReadOnlyList<string> All => NotificationRegisteredTypes.All;
    public static bool Owns(string messageType) => NotificationRegisteredTypes.Owns(messageType);
    public static bool IsOwnedUnregistered(string messageType) => NotificationRegisteredTypes.IsOwnedUnregistered(messageType);
}
