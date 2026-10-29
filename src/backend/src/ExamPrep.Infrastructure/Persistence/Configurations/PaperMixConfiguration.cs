using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Papers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class PaperMixConfiguration : IEntityTypeConfiguration<PaperMix>
{
    public void Configure(EntityTypeBuilder<PaperMix> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).HasMaxLength(200);
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_paper_mixes_version_count",
            $"version_count BETWEEN {PaperMixPlan.MinVersions} AND {PaperMixPlan.VersionsPerSource * PaperMixPlan.MaxSources}"));
        builder.HasOne(m => m.SectionTemplate).WithMany().HasForeignKey(m => m.SectionTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(m => m.Sources).WithOne().HasForeignKey(s => s.PaperMixId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
