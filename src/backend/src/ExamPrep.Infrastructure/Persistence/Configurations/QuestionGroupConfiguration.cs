using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class QuestionGroupConfiguration : IEntityTypeConfiguration<QuestionGroup>
{
    public void Configure(EntityTypeBuilder<QuestionGroup> builder)
    {
        builder.HasKey(g => g.Id);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_question_groups_order", "order_no > 0");
            t.HasCheckConstraint("CK_question_groups_max_words", "max_words_per_answer IS NULL OR max_words_per_answer > 0");
        });
        builder.Property(g => g.Instructions).HasMaxLength(1000);
        builder.Property(g => g.BodyHtml).HasColumnType("mediumtext");
        builder.Property(g => g.OptionPoolJson).HasColumnType("json");
        builder.HasOne(g => g.PaperPart).WithMany(p => p.Groups).HasForeignKey(g => g.PaperPartId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(g => new { g.PaperPartId, g.OrderNo }).IsUnique();
    }
}
