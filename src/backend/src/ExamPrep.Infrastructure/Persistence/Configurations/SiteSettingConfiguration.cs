using ExamPrep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPrep.Infrastructure.Persistence.Configurations;

internal sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        // One centre, one row of settings.
        builder.ToTable(t => t.HasCheckConstraint("CK_site_settings_single_row", "id = 1"));
        builder.Property(s => s.CentreName).HasMaxLength(128);
        builder.HasOne(s => s.LogoAsset).WithMany().HasForeignKey(s => s.LogoAssetId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasData(SeedData.DefaultSiteSetting);
    }
}
