using System.Reflection;
using FluentAssertions;
using RoadGuardSystem.Repositories;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

internal static class P202ProductionContract
{
    internal static Type RequireRepositoryType(string relativeName)
    {
        var fullName = $"RoadGuardSystem.Repositories.{relativeName}";
        var type = typeof(RoadGuardDbContext).Assembly.GetType(fullName, throwOnError: false);

        type.Should().NotBeNull($"P2-02 requires production contract {fullName}");
        return type!;
    }

    internal static MethodInfo RequirePublicMethod(Type type, string name, bool isStatic, int parameterCount)
    {
        var method = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .SingleOrDefault(candidate =>
                candidate.Name == name &&
                candidate.IsStatic == isStatic &&
                candidate.GetParameters().Length == parameterCount);

        method.Should().NotBeNull($"{type.FullName}.{name} must expose the assigned P2-02 contract");
        return method!;
    }

    internal static Exception UnwrapInvocation(Exception exception)
    {
        if (exception is TargetInvocationException invocation && invocation.InnerException is not null)
        {
            return invocation.InnerException;
        }

        return exception;
    }
}
