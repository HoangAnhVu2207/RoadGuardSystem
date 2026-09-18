using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Idempotency;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords", table =>
        {
            table.HasCheckConstraint(
                "CK_IdempotencyRecords_OutcomeJson_Json",
                "ISJSON([OutcomeJson]) = 1");
        });

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Operation).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(record => record.IdempotencyKey).HasMaxLength(200).IsUnicode(false).IsRequired();
        builder.Property(record => record.RequestFingerprint).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(record => record.OutcomeJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(record => record.CreatedAtUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.HasIndex(record => new
        {
            record.ActorUserId,
            record.ProjectId,
            record.Operation,
            record.IdempotencyKey
        })
            .IsUnique()
            .HasFilter(null)
            .HasDatabaseName("UX_IdempotencyRecords_ScopeKey");
        builder.HasIndex(record => record.OperationId)
            .HasDatabaseName("IX_IdempotencyRecords_OperationId");
    }
}
