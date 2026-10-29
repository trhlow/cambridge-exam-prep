using ExamPrep.Domain.Entities;
using ExamPrep.Domain.Papers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class PaperMixSourceConfiguration : IEntityTypeConfiguration<PaperMixSource>
{
    public void Configure(EntityTypeBuilder<PaperMixSource> builder)
    {
        builder.HasKey(s => new { s.PaperMixId, s.Position });
        builder.Ignore(s => s.Letter);
        builder.HasIndex(s => new { s.PaperMixId, s.TestPaperId }).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_paper_mix_sources_position", $"position BETWEEN 1 AND {PaperMixPlan.MaxSources}"));
        // A source paper stays while a mix was made from it: the mix records where its parts came from.
        builder.HasOne(s => s.TestPaper).WithMany().HasForeignKey(s => s.TestPaperId).OnDelete(DeleteBehavior.Restrict);
    }
}
