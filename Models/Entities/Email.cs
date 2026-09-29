using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities;

/// <summary>
/// Email entity representing an email message
/// </summary>
public class Email
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Body { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SenderName { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string SenderEmail { get; set; } = string.Empty;

    [Required]
    public DateTime ReceivedTime { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; } = null!;

    [MaxLength(50)]
    public string? EmailProvider { get; set; } // Gmail, Outlook, etc.

    [MaxLength(100)]
    public string? ExternalEmailId { get; set; } // Email ID from provider API

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}


