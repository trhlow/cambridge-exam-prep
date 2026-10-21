using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_attempts_non_negative",
            "paused_seconds >= 0 AND planned_extra_seconds >= 0 AND incident_extra_seconds >= 0 AND audio_adjust_ms >= 0"));
        builder.Property(a => a.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasOne(a => a.Candidate).WithMany(c => c.Attempts).HasForeignKey(a => a.CandidateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.ExamSection).WithMany(s => s.Attempts).HasForeignKey(a => a.ExamSectionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(a => new { a.CandidateId, a.ExamSectionId }).IsUnique();
        builder.HasOne(a => a.TestPaper).WithMany().HasForeignKey(a => a.TestPaperId).OnDelete(DeleteBehavior.Restrict);
    }
}
