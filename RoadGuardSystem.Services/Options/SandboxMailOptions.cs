namespace RoadGuardSystem.Services.Options;

/// <summary>Local capture transport. Enabled only by the Development sandbox composition root.</summary>
public sealed class SandboxMailOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "mailpit";
}
