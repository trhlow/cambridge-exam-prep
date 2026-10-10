using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.HasKey(c => c.Id);
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
    }
}
