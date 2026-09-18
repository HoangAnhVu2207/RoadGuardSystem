using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Concurrency;

namespace RoadGuardSystem.Repositories.Concurrency;

public static class RowVersionConvention
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rowVersionProperty = entityType.FindProperty(nameof(IHasRowVersion.RowVersion));
            if (rowVersionProperty?.ClrType != typeof(byte[]))
            {
                continue;
            }

            if (!typeof(IHasRowVersion).IsAssignableFrom(entityType.ClrType) &&
                !string.Equals(rowVersionProperty.Name, "RowVersion", StringComparison.Ordinal))
            {
                continue;
            }

            rowVersionProperty.IsConcurrencyToken = true;
            rowVersionProperty.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
            rowVersionProperty.SetColumnType("rowversion");
        }
    }
}
