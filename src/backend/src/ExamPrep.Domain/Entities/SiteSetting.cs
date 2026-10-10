namespace ExamPrep.Domain.Entities;

/// <summary>Single-row centre settings (name and logo shown in the exam header).</summary>
public class SiteSetting
{
    /// <summary>Always 1.</summary>
    public int Id { get; set; }

    public required string CentreName { get; set; }

    public int? LogoAssetId { get; set; }

    public Asset? LogoAsset { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }
}
