using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.ToTable("Flights", table =>
        {
            table.HasTrigger("TR_Flights_ImmutableSurvey");
            table.HasCheckConstraint("CK_Flights_TimestampOrder", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
        });

        builder.HasKey(flight => flight.Id);
        builder.Property(flight => flight.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(flight => flight.SurveyId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(flight => flight.DroneDeviceId).HasColumnType("uniqueidentifier");
        builder.Property(flight => flight.OperatorUserId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(flight => flight.StartedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(flight => flight.EndedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(flight => flight.FlightNo).HasMaxLength(80).IsUnicode(false).IsRequired();
        builder.HasIndex(flight => new { flight.SurveyId, flight.FlightNo })
            .IsUnique()
            .HasDatabaseName("UX_Flights_SurveyFlightNo");
        builder.HasOne<Survey>().WithMany().HasForeignKey(flight => flight.SurveyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DroneDevice>().WithMany().HasForeignKey(flight => flight.DroneDeviceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(flight => flight.OperatorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
