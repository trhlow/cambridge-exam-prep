using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ExamTypeConfiguration : IEntityTypeConfiguration<ExamType>
{
    public void Configure(EntityTypeBuilder<ExamType> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(32);
        builder.Property(t => t.Name).HasMaxLength(128);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasData(SeedData.ExamTypes);
    }
}
