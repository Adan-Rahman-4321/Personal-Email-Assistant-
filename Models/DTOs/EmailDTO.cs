namespace Models.DTOs;

/// <summary>
/// Data Transfer Object for Email display
/// </summary>
public class EmailDTO
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public DateTime ReceivedTime { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public string? CategoryColorCode { get; set; }
    public string? EmailProvider { get; set; }
}


