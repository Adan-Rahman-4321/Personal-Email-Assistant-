namespace Models.DTOs;

/// <summary>
/// Request model for AI email classification
/// </summary>
public class EmailClassificationRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
}


