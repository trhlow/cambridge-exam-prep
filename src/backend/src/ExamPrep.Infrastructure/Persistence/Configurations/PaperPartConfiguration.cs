using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class PaperPartConfiguration : IEntityTypeConfiguration<PaperPart>
{
    public void Configure(EntityTypeBuilder<PaperPart> builder)
    {
        builder.HasKey(p => p.Id);
        builder.ToTable(t => t.HasCheckConstraint("CK_paper_parts_part_no", "part_no > 0"));
        builder.Property(p => p.Title).HasMaxLength(256);
        builder.Property(p => p.StimulusHtml).HasColumnType("mediumtext");
        builder.HasOne(p => p.TestPaper).WithMany(t => t.Parts).HasForeignKey(p => p.TestPaperId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.AudioAsset).WithMany().HasForeignKey(p => p.AudioAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.TestPaperId, p.PartNo }).IsUnique();
    }
}
