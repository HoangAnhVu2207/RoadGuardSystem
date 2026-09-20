using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

[Trait("TaskId", "P2-04")]
public sealed class FileSchemaContractTests
{
    [Fact]
    public void StoredFile_Create_RejectsInvalidCanonicalMetadata()
    {
        var create = typeof(StoredFile).GetMethod("Create");

        create.Should().NotBeNull("File metadata needs one validated creation boundary");
        var act = () => create!.Invoke(null,
        [
            Guid.NewGuid(), "opaque-key", "evidence.pdf", "application/pdf", -1,
            new string('A', 64), null, DateTimeOffset.UtcNow, null
        ]);

        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("../evidence.pdf", "application/pdf", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("evidence.pdf", "not-a-mime", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("evidence.pdf", "application/pdf", null)]
    public void StoredFile_Create_RejectsPathMimeAndNullChecksum(
        string originalName,
        string mimeType,
        string? checksum)
    {
        var create = () => StoredFile.Create(
            Guid.NewGuid(),
            "opaque-key",
            originalName,
            mimeType,
            1,
            checksum!,
            null,
            DateTimeOffset.UtcNow,
            null);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StoredFile_PersistedContentIdentity_CannotBeModifiedOrDeleted()
    {
        using var context = CreateContext();
        var file = CreateValidFile();
        context.Attach(file);
        context.Entry(file).Property(candidate => candidate.StorageUri).CurrentValue = "replacement-key";
        context.Entry(file).Property(candidate => candidate.StorageUri).IsModified = true;

        var update = () => context.SaveChanges();
        update.Should().Throw<InvalidOperationException>()
            .WithMessage("*immutable*");

        context.ChangeTracker.Clear();
        context.Attach(file);
        context.Remove(file);
        var delete = () => context.SaveChanges();
        delete.Should().Throw<InvalidOperationException>()
            .WithMessage("*retention*");
    }

    [Fact]
    public void FileModel_RejectsNegativeSizeAndNonCanonicalChecksum()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var file = model.GetEntityTypes().SingleOrDefault(entity => entity.GetTableName() == "Files");
        var checks = file?.GetCheckConstraints().Select(constraint => constraint.Name).ToArray() ?? [];

        checks.Should().Contain("CK_Files_SizeBytes_NonNegative");
        checks.Should().Contain("CK_Files_Checksum_Sha256Lowercase");
    }

    [Fact]
    public void FileModel_PreservesCanonicalMetadataAndNullableUploader()
    {
        using var context = CreateContext();
        var file = context.Model.GetEntityTypes().SingleOrDefault(entity => entity.GetTableName() == "Files");

        file.Should().NotBeNull();
        file!.FindProperty("StorageUri")!.GetMaxLength().Should().Be(2048);
        file.FindProperty("OriginalName")!.GetMaxLength().Should().Be(255);
        file.FindProperty("MimeType")!.GetMaxLength().Should().Be(120);
        file.FindProperty("SizeBytes")!.ClrType.Should().Be<int>();
        file.FindProperty("UploadedByUserId")!.IsNullable.Should().BeTrue();
        file.GetForeignKeys().Should().ContainSingle(key => key.PrincipalEntityType.GetTableName() == "Users");
        file.GetIndexes().Should().ContainSingle(index =>
            index.IsUnique && index.Properties.Count == 1 && index.Properties[0].Name == "StorageUri");
    }

    private static RoadGuardDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=unused_model_only;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.UseNetTopologySuite())
            .Options;
        return new RoadGuardDbContext(options);
    }

    private static StoredFile CreateValidFile()
    {
        var create = typeof(StoredFile).GetMethod("Create")
            ?? throw new InvalidOperationException("StoredFile.Create is missing.");
        return (StoredFile)create.Invoke(null,
        [
            Guid.NewGuid(), "opaque-key", "evidence.pdf", "application/pdf", 4,
            new string('a', 64), null, DateTimeOffset.UtcNow, null
        ])!;
    }
}
