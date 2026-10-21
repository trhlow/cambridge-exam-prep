using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class PaperMixConfiguration : IEntityTypeConfiguration<PaperMix>
{
    public void Configure(EntityTypeBuilder<PaperMix> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).HasMaxLength(200);
        builder.ToTable(t => t.HasCheckConstraint("CK_paper_mixes_sources", "source_a_paper_id <> source_b_paper_id"));
        builder.HasOne(m => m.SectionTemplate).WithMany().HasForeignKey(m => m.SectionTemplateId).OnDelete(DeleteBehavior.Restrict);
        // A source paper stays while a mix was made from it: the mix records where its parts came from.
        builder.HasOne(m => m.SourceAPaper).WithMany().HasForeignKey(m => m.SourceAPaperId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.SourceBPaper).WithMany().HasForeignKey(m => m.SourceBPaperId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
