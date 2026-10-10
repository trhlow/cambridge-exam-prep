using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ResponseConfiguration : IEntityTypeConfiguration<Response>
{
    public void Configure(EntityTypeBuilder<Response> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Value).HasColumnType("text");
        builder.HasOne(r => r.Attempt).WithMany(a => a.Responses).HasForeignKey(r => r.AttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Question).WithMany().HasForeignKey(r => r.QuestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.AttemptId, r.QuestionId }).IsUnique();
    }
}
