using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ExamPrep.Infrastructure.Persistence;

/// <summary>
/// Every enum is stored as its name in a varchar column (<see cref="ModelConventions"/>). Without a constraint the
/// column would take any text, so each one gets a CHECK listing exactly the names of its enum: a status the code does
/// not know can never reach the database, whoever writes it. The lists come from the enums themselves, so a new enum
/// value needs only a migration.
/// </summary>
internal static class EnumCheckConstraints
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (table is null || entityType.IsOwned())
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!enumType.IsEnum)
                {
                    continue;
                }

                var column = property.GetColumnName(StoreObjectIdentifier.Table(table, entityType.GetSchema()));
                var names = string.Join(", ", Enum.GetNames(enumType).Select(n => $"'{n}'"));
                var sql = property.IsNullable ? $"{column} IS NULL OR {column} IN ({names})" : $"{column} IN ({names})";
                modelBuilder.Entity(entityType.ClrType).ToTable(t => t.HasCheckConstraint($"CK_{table}_{column}_values", sql));
            }
        }
    }
}
