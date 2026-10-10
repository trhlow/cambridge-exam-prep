using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Room).HasMaxLength(64);
        builder.Property(e => e.SessionPin).HasMaxLength(16);
        builder.Property(e => e.InvigilatorPassword).HasMaxLength(64);
        builder.Property(e => e.SebConfigKeyHash).HasMaxLength(64).IsFixedLength();
        builder.HasOne(e => e.ExamType).WithMany().HasForeignKey(e => e.ExamTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.ScheduledDate);
    }
}
