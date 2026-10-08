using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories;
using System.Reflection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Seeding;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

public sealed class IdentityPersistenceNegativeTests
{
    [Fact(DisplayName = "P2-10 Negative: PermitRoleMutationScope is non-public and cannot be called by external callers")]
    public void PermitRoleMutationScope_IsNonPublic()
    {
        var method = typeof(RoadGuardDbContext).GetMethod("PermitRoleMutationScope", BindingFlags.Public | BindingFlags.Instance);
        method.Should().BeNull("PermitRoleMutationScope must be internal to Repositories and not exposed publicly on RoadGuardDbContext");
    }

}
