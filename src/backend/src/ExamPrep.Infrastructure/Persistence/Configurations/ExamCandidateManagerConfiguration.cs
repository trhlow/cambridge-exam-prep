using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ExamCandidateManagerConfiguration : IEntityTypeConfiguration<ExamCandidateManager>
{
    public void Configure(EntityTypeBuilder<ExamCandidateManager> builder)
    {
        builder.HasKey(m => new { m.ExamId, m.UserId });
        builder.HasOne(m => m.Exam).WithMany(e => e.CandidateManagers).HasForeignKey(m => m.ExamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
