using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_attempts_non_negative",
                "paused_seconds >= 0 AND planned_extra_seconds >= 0 AND incident_extra_seconds >= 0 AND audio_adjust_ms >= 0");
            // The state and its timestamps move together (AttemptClock): started has a start time, paused exactly when
            // paused, submitted exactly when it has a submit time and a reason, and only a submitted attempt is graded.
            t.HasCheckConstraint(
                "CK_attempts_state",
                "(status = 'NotStarted' OR started_at IS NOT NULL) " +
                "AND ((status = 'Paused') = (paused_at IS NOT NULL)) " +
                "AND ((status = 'Submitted') = (submitted_at IS NOT NULL AND submit_reason IS NOT NULL)) " +
                "AND (grading_status = 'NotGraded' OR status = 'Submitted')");
            t.HasCheckConstraint(
                "CK_attempts_score",
                "(raw_score IS NULL OR max_score IS NOT NULL) AND (raw_score IS NULL OR (raw_score >= 0 AND raw_score <= max_score)) " +
                "AND (max_score IS NULL OR max_score >= 0)");
        });
        builder.Property(a => a.ConcurrencyStamp).IsConcurrencyToken();
        // An attempt is the record of a candidate's work: deleting the candidate or the section must not take it along.
        builder.HasOne(a => a.Candidate).WithMany(c => c.Attempts).HasForeignKey(a => a.CandidateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.ExamSection).WithMany(s => s.Attempts).HasForeignKey(a => a.ExamSectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.CandidateId, a.ExamSectionId }).IsUnique();
        builder.HasOne(a => a.TestPaper).WithMany().HasForeignKey(a => a.TestPaperId).OnDelete(DeleteBehavior.Restrict);
    }
}
