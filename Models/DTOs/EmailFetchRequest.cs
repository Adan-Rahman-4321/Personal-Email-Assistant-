namespace Models.DTOs;

/// <summary>
/// Request model for fetching emails from provider
/// </summary>
public class EmailFetchRequest
{
    public string Provider { get; set; } = "Gmail"; // Gmail or Outlook
    public int MaxEmails { get; set; } = 50;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}


