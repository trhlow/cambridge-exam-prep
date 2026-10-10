using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class AnswerKeyConfiguration : IEntityTypeConfiguration<AnswerKey>
{
    public void Configure(EntityTypeBuilder<AnswerKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Value).HasMaxLength(255);
        builder.HasOne(k => k.Question).WithMany(q => q.AnswerKeys).HasForeignKey(k => k.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
