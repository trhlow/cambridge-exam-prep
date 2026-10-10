using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class InvigilatorActionConfiguration : IEntityTypeConfiguration<InvigilatorAction>
{
    public void Configure(EntityTypeBuilder<InvigilatorAction> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.PayloadJson).HasColumnType("json");
        builder.Property(a => a.Reason).HasMaxLength(255);
        builder.HasOne(a => a.Exam).WithMany().HasForeignKey(a => a.ExamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Attempt).WithMany().HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.ExamId, a.CreatedAt });
        builder.HasIndex(a => a.BatchId);
    }
}
