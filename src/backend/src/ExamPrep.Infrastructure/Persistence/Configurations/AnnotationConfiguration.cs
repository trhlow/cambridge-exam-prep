using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class AnnotationConfiguration : IEntityTypeConfiguration<Annotation>
{
    public void Configure(EntityTypeBuilder<Annotation> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AnchorJson).HasColumnType("json");
        builder.Property(a => a.NoteText).HasMaxLength(2000);
        builder.HasOne(a => a.Attempt).WithMany(t => t.Annotations).HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.PaperPart).WithMany().HasForeignKey(a => a.PaperPartId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Question>().WithMany().HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}
