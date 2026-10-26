using System.Text.Json;
using RoadGuardSystem.IntegrationTests.Reporting;

// Test-only child. Credentials arrive through a redirected private pipe, never
// arguments or logs. There is no connection-string/environment fallback.
try
{
    var input = await Console.In.ReadToEndAsync();
    var request = JsonSerializer.Deserialize<ExportRendererRecoverySqlTests.WorkerInput>(input)
        ?? throw new InvalidOperationException();
    await ExportRendererRecoverySqlTests.RunOwnedWorker(request);
    return 2; // The selected durable boundary must terminate the child with 73.
}
catch (Exception exception)
{
    Console.Error.WriteLine("Test-owned export worker failed before its selected boundary.");
    Console.Error.WriteLine(exception.GetType().FullName);
    if (exception is InvalidOperationException && exception.Message.StartsWith("Not an owned loopback test worker request", StringComparison.Ordinal))
        Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(exception.StackTrace);
    return 3;
}
