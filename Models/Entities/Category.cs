using System.ComponentModel.DataAnnotations;

namespace Models.Entities;

/// <summary>
/// Category entity for email classification
/// </summary>
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string DisplayName { get; set; } = string.Empty; // 🔥 Most Important, ⭐ Important, etc.

    [MaxLength(100)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? ColorCode { get; set; } // For UI styling (red, yellow, etc.)

    public int Priority { get; set; } // For sorting

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<Email> Emails { get; set; } = new List<Email>();
}


