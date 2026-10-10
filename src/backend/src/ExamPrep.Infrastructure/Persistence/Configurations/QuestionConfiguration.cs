using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.PromptHtml).HasColumnType("text");
        builder.Property(q => q.StimulusHtml).HasColumnType("mediumtext");
        builder.Property(q => q.OptionsJson).HasColumnType("json");
        builder.HasOne(q => q.QuestionGroup).WithMany(g => g.Questions).HasForeignKey(q => q.QuestionGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(q => new { q.QuestionGroupId, q.OrderNo }).IsUnique();
    }
}
