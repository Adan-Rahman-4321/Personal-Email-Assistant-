namespace Models.DTOs;

/// <summary>
/// Request model for updating email
/// </summary>
public class UpdateEmailRequest
{
    public int Id { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public int? CategoryId { get; set; }
}


