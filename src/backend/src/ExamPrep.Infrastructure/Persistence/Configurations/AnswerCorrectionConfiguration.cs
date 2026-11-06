using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class AnswerCorrectionConfiguration : IEntityTypeConfiguration<AnswerCorrection>
{
    public void Configure(EntityTypeBuilder<AnswerCorrection> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.OldAnswers).HasMaxLength(2000);
        builder.Property(c => c.NewAnswers).HasMaxLength(2000);
        builder.ToTable(t => t.HasCheckConstraint("CK_answer_corrections_counts", "attempts_remarked >= 0 AND attempts_skipped >= 0"));
        // The log goes with its paper; the person may leave (SetNull) without losing what they did.
        builder.HasOne(c => c.TestPaper).WithMany().HasForeignKey(c => c.TestPaperId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.CorrectedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(c => new { c.TestPaperId, c.QuestionNumber });
    }
}
