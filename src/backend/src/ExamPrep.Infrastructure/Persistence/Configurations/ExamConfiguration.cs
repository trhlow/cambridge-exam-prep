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
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_exams_seats_per_row", "seats_per_row IS NULL OR seats_per_row BETWEEN 1 AND 50");
            // The PIN the invigilator reads out: 4 to 8 digits. The SEB Config Key: 64 lower-case hex characters.
            t.HasCheckConstraint("CK_exams_session_pin", "REGEXP_LIKE(session_pin, '^[0-9]{4,8}$', 'c')");
            t.HasCheckConstraint("CK_exams_seb_config_key", "seb_config_key_hash IS NULL OR REGEXP_LIKE(seb_config_key_hash, '^[0-9a-f]{64}$', 'c')");
        });
    }
}
