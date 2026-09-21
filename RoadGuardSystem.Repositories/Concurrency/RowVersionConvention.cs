using Microsoft.EntityFrameworkCore;

namespace RoadGuardSystem.Repositories.Concurrency;

public static class RowVersionConvention
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rowVersionProperty = entityType.FindProperty("RowVersion");
            if (rowVersionProperty?.ClrType != typeof(byte[]))
            {
                continue;
            }

            rowVersionProperty.IsConcurrencyToken = true;
            rowVersionProperty.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
            rowVersionProperty.SetColumnType("rowversion");
        }
    }
}
