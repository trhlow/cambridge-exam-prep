using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ExamSectionConfiguration : IEntityTypeConfiguration<ExamSection>
{
    public void Configure(EntityTypeBuilder<ExamSection> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasOne(s => s.Exam).WithMany(e => e.Sections).HasForeignKey(s => s.ExamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.SectionTemplate).WithMany().HasForeignKey(s => s.SectionTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.TestPaper).WithMany().HasForeignKey(s => s.TestPaperId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.ExamId, s.OrderNo }).IsUnique();
    }
}
