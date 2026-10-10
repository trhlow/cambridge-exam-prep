using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ManualMarkConfiguration : IEntityTypeConfiguration<ManualMark>
{
    public void Configure(EntityTypeBuilder<ManualMark> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.CriteriaJson).HasColumnType("json");
        builder.Property(m => m.Comment).HasMaxLength(2000);
        builder.HasOne(m => m.Response).WithMany(r => r.ManualMarks).HasForeignKey(m => m.ResponseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Marker).WithMany().HasForeignKey(m => m.MarkerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
