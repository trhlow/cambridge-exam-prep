using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Marking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.HasKey(c => c.Id);
        builder.ToTable(t => t.HasCheckConstraint("CK_candidates_planned_extra", "planned_extra_percent BETWEEN 0 AND 100"));
        builder.Property(c => c.CandidateNumber).HasMaxLength(16);
        builder.Property(c => c.FirstName).HasMaxLength(100);
        builder.Property(c => c.LastName).HasMaxLength(100);
        builder.Property(c => c.Username).HasMaxLength(64);
        builder.Property(c => c.PasswordHash).HasMaxLength(256);
        builder.Property(c => c.PasswordProtected).HasMaxLength(1024);
        builder.Property(c => c.SeatNo).HasMaxLength(16);
        builder.HasOne(c => c.Exam).WithMany(e => e.Candidates).HasForeignKey(c => c.ExamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.Username).IsUnique();
        builder.HasIndex(c => new { c.ExamId, c.CandidateNumber }).IsUnique();
        builder.Property(c => c.MarkingCode).HasMaxLength(MarkingAllocation.CodeLength).IsFixedLength();
        builder.HasIndex(c => new { c.ExamId, c.MarkingCode }).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_candidates_marking_code", "marking_code IS NULL OR REGEXP_LIKE(marking_code, '^[A-HJ-NP-Z2-9]{6}$', 'c')"));
        // An examiner account is deactivated, never deleted; a deleted one would only leave the script to deal again.
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.ExaminerUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
