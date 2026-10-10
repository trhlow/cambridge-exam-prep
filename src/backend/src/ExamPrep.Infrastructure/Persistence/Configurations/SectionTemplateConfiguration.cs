using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class SectionTemplateConfiguration : IEntityTypeConfiguration<SectionTemplate>
{
    public void Configure(EntityTypeBuilder<SectionTemplate> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).HasMaxLength(128);
        builder.HasOne(s => s.ExamType).WithMany(t => t.Sections).HasForeignKey(s => s.ExamTypeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.ExamTypeId, s.OrderNo }).IsUnique();
        builder.HasData(SeedData.SectionTemplates);
    }
}
