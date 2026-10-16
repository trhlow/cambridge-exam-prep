using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ExamExaminerConfiguration : IEntityTypeConfiguration<ExamExaminer>
{
    public void Configure(EntityTypeBuilder<ExamExaminer> builder)
    {
        builder.HasKey(e => new { e.ExamId, e.UserId });
        builder.HasOne(e => e.Exam).WithMany(x => x.Examiners).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
