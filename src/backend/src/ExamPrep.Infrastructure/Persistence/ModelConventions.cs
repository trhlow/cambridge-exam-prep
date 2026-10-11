using ExamPrep.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExamPrep.Infrastructure.Persistence;

/// <summary>Store-wide mapping rules: enums as readable strings, UTC timestamps, money-like decimals.</summary>
internal static class ModelConventions
{
    private const int EnumMaxLength = 32;

    public static void Apply(ModelConfigurationBuilder builder)
    {
        var enumTypes = typeof(UserRole).Assembly.GetTypes()
            .Where(t => t.IsEnum && t.Namespace == typeof(UserRole).Namespace);

        foreach (var enumType in enumTypes)
        {
            builder.Properties(enumType).HaveConversion<string>().HaveMaxLength(EnumMaxLength);
        }

        builder.Properties<decimal>().HavePrecision(8, 2);
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
        builder.Properties<DateOnly>().HaveConversion<DateOnlyConverter>().HaveColumnType("date");
        builder.Properties<DateOnly?>().HaveConversion<NullableDateOnlyConverter>().HaveColumnType("date");
    }

    /// <summary>
    /// Oracle's MySQL provider reads DATE columns as <see cref="DateTime"/> and cannot materialise
    /// <see cref="DateOnly"/> itself, so the conversion is done here.
    /// </summary>
    private sealed class DateOnlyConverter() : ValueConverter<DateOnly, DateTime>(
        v => v.ToDateTime(TimeOnly.MinValue),
        v => DateOnly.FromDateTime(v));

    private sealed class NullableDateOnlyConverter() : ValueConverter<DateOnly?, DateTime?>(
        v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : null,
        v => v.HasValue ? DateOnly.FromDateTime(v.Value) : null);

    /// <summary>MySQL DATETIME has no time zone; every value is written and read back as UTC.</summary>
    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
}
