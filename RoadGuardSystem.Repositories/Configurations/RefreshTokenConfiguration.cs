using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", table =>
        {
            table.HasCheckConstraint("CK_RefreshTokens_TokenHash_NotEmpty", "LEN(LTRIM(RTRIM([TokenHash]))) >= 32");
        });

        builder.HasKey(token => token.Id);

        builder.Property(token => token.SessionId)
            .IsRequired();

        builder.HasOne(token => token.Session)
            .WithMany(session => session.RefreshTokens)
            .HasForeignKey(token => token.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");

        builder.Property(token => token.ExpiresAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();

        builder.Property(token => token.RevokedAt)
            .HasColumnType("datetimeoffset(7)");

        builder.HasIndex(token => token.SessionId)
            .HasDatabaseName("IX_RefreshTokens_SessionId");
    }
}
