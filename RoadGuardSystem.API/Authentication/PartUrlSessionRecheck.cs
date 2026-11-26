using System.Security.Claims;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Services.Authentication;

namespace RoadGuardSystem.API.Authentication;

// Local to the two multipart part-URL actions: signing may outlive session authority.
internal static class PartUrlSessionRecheck
{
    internal static async Task<AuthoritativeSessionValidation> ValidateAsync(HttpContext context,
        Guid actor, UserRoleCode role, CancellationToken token)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("sid"), out var session))
            return AuthoritativeSessionValidation.Unauthorized;
        var now = context.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
        if (long.TryParse(context.User.FindFirstValue("exp"), out var expiry) && now.ToUnixTimeSeconds() >= expiry)
            return AuthoritativeSessionValidation.Unauthorized;
        return await context.RequestServices.GetRequiredService<AuthoritativeSessionValidator>()
            .ValidateAsync(actor, session, role, now,
                context.User.Identity?.AuthenticationType == WebCookieConfiguration.Scheme ? SessionTransport.Web : null,
                token);
    }
}
