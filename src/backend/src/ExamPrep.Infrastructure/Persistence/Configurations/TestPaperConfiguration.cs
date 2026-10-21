using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class TestPaperConfiguration : IEntityTypeConfiguration<TestPaper>
{
    public void Configure(EntityTypeBuilder<TestPaper> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(256);
        builder.HasOne(p => p.SectionTemplate).WithMany().HasForeignKey(p => p.SectionTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(p => p.MixPattern).HasMaxLength(16);
        builder.HasOne(p => p.PaperMix).WithMany(m => m.Versions).HasForeignKey(p => p.PaperMixId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.PaperMixId, p.MixVersion }).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_test_papers_mix_version",
            "(paper_mix_id IS NULL AND mix_version IS NULL AND mix_pattern IS NULL) OR (paper_mix_id IS NOT NULL AND mix_version BETWEEN 1 AND 4 AND mix_pattern IS NOT NULL)"));
    }
}
