namespace Models.DTOs;

/// <summary>
/// Response model from AI classification
/// </summary>
public class EmailClassificationResponse
{
    public string CategoryName { get; set; } = string.Empty; // Most Important, Important, Casual, Promotional
    public double? ConfidenceScore { get; set; }
}


