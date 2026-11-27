using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.aBusinessObjects.Commons;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Controllers;
using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.Services.Files;
using Xunit;

namespace RoadGuardSystem.ApiTests.Files;

public sealed class Anh01UploadContractTests
{
    [Fact]
    public async Task PartUrlReplay_Returns200WithOriginalParts()
    {
        var service = DispatchProxy.Create<IUploadService, ReplayProxy>();
        var parts = new UploadPartUrlsResponseDto([new(1, "https://storage.invalid/part", DateTimeOffset.UtcNow.AddMinutes(1))]);
        ((ReplayProxy)(object)service).Result = new(UploadServiceStatus.Replayed, PartUrls: parts);
        var controller = new UploadsController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var actor = Guid.NewGuid(); var session = Guid.NewGuid();
        var identity = DispatchProxy.Create<IIdentityRepository, IdentityProxy>();
        ((IdentityProxy)(object)identity).Actor = actor;
        using var services = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton(new AuthoritativeSessionValidator(identity)).BuildServiceProvider();
        controller.HttpContext.RequestServices = services;
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, actor.ToString()), new Claim("sid", session.ToString()), new Claim("role", "PM")], "test"));
        var result = await controller.GetPartUrls(Guid.NewGuid(), new([1]), "replay", CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(parts);
    }

    [Theory]
    [InlineData(2147483648L)]
    [InlineData(8589934592L)]
    public void Int64Size_ValidJson_ReachesSemanticValidation(long bytes)
    {
        var request = new UploadCreateRequestDto("SURVEY_VIDEO", Guid.NewGuid(), Guid.NewGuid(), "source.mp4", "video/mp4", bytes, new string('a', 64));
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, true).Should().BeTrue();
    }

    public class IdentityProxy : DispatchProxy
    {
        public Guid Actor { get; set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            nameof(IIdentityRepository.GetSessionSecurityStateAsync) => Task.FromResult<SessionSecurityState?>(
                new((Guid)args![0]!, Actor, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1), null, true, [1])),
            nameof(IIdentityRepository.GetUserSecurityStateAsync) => Task.FromResult<UserSecurityState?>(
                new(Actor, "synthetic", "Synthetic", UserRoleCode.ProjectManager, UserStatus.Active, false, [1])),
            nameof(IIdentityRepository.IsRoleActiveAsync) => Task.FromResult(true),
            _ => throw new NotSupportedException(targetMethod?.Name)
        };
    }

    public class ReplayProxy : DispatchProxy
    {
        public UploadServiceResult Result { get; set; } = new(UploadServiceStatus.InvalidInput);
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Task.FromResult(Result);
    }
}
