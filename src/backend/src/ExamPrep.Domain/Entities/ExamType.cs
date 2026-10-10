namespace ExamPrep.Domain.Entities;

/// <summary>An exam product, e.g. A2 Key for Schools. Its sections are data, not code.</summary>
public class ExamType
{
    public int Id { get; set; }

    /// <summary>Stable code, e.g. <c>A2_KEY_FS</c>.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<SectionTemplate> Sections { get; set; } = [];
}
