using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ExamInvigilatorConfiguration : IEntityTypeConfiguration<ExamInvigilator>
{
    public void Configure(EntityTypeBuilder<ExamInvigilator> builder)
    {
        builder.HasKey(i => new { i.ExamId, i.UserId });
        builder.HasOne(i => i.Exam).WithMany(e => e.Invigilators).HasForeignKey(i => i.ExamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.User).WithMany().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
