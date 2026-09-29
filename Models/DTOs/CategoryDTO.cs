namespace Models.DTOs;

/// <summary>
/// Data Transfer Object for Category
/// </summary>
public class CategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorCode { get; set; }
    public int Priority { get; set; }
    public int EmailCount { get; set; }
}


