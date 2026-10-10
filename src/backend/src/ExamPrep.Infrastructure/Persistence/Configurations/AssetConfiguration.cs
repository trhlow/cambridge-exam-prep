using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.OriginalFileName).HasMaxLength(255);
        builder.Property(a => a.StoredPath).HasMaxLength(512);
        builder.Property(a => a.ContentType).HasMaxLength(128);
        builder.Property(a => a.Sha256).HasMaxLength(64).IsFixedLength();
        builder.HasIndex(a => a.Sha256);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
