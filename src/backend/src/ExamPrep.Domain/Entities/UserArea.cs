using ExamPrep.Domain.Enums;

namespace ExamPrep.Domain.Entities;

/// <summary>An area a manager account is allowed to work in.</summary>
public class UserArea
{
    public int UserId { get; set; }

    public User? User { get; set; }

    public ManagerArea Area { get; set; }
}
