namespace RoadGuardSystem.API.Authentication;

// Opt in only a matched implemented route; business authorization remains in its existing producer.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class WebCookieEligibleAttribute : Attribute { }
