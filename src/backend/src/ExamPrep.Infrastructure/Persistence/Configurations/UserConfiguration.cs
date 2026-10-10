using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(64);
        builder.Property(u => u.PasswordHash).HasMaxLength(256);
        builder.Property(u => u.FullName).HasMaxLength(128);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.HasIndex(u => u.Username).IsUnique();
    }
}
