using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class CandidateMessageConfiguration : IEntityTypeConfiguration<CandidateMessage>
{
    public void Configure(EntityTypeBuilder<CandidateMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Text).HasMaxLength(1000);
        builder.HasOne(m => m.Exam).WithMany().HasForeignKey(m => m.ExamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Candidate).WithMany().HasForeignKey(m => m.CandidateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.SentByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.ExamId, m.SentAt });
    }
}
