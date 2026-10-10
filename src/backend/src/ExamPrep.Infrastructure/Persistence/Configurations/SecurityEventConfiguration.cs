using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Detail).HasMaxLength(500);
        builder.HasOne(e => e.Attempt).WithMany().HasForeignKey(e => e.AttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.ResolvedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => new { e.AttemptId, e.DetectedAt });
    }
}
