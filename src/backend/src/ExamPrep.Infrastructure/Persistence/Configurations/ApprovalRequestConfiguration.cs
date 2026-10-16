using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Summary).HasMaxLength(512);
        builder.Property(r => r.PayloadJson).HasColumnType("text");
        builder.Property(r => r.DecisionNote).HasMaxLength(500);
        builder.Property(r => r.Outcome).HasMaxLength(500);
        // Two administrators deciding at once: the second save finds the status changed and fails.
        builder.Property(r => r.Status).IsConcurrencyToken();
        builder.HasIndex(r => new { r.Status, r.Kind, r.TargetId });
        // Requests are the approval log: staff accounts are switched off, never deleted, so the links stay.
        builder.HasOne(r => r.RequestedBy).WithMany().HasForeignKey(r => r.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.DecidedBy).WithMany().HasForeignKey(r => r.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_approval_requests_decider", "decided_by_user_id IS NULL OR decided_by_user_id <> requested_by_user_id OR status = 'Cancelled'"));
    }
}
