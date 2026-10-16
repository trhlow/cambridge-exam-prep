using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class UserAreaConfiguration : IEntityTypeConfiguration<UserArea>
{
    public void Configure(EntityTypeBuilder<UserArea> builder)
    {
        builder.HasKey(a => new { a.UserId, a.Area });
        builder.HasOne(a => a.User).WithMany(u => u.Areas).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
