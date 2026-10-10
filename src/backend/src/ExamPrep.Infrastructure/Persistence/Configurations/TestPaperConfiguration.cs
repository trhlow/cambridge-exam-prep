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
    }
}
