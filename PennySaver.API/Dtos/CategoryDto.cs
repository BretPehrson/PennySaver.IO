namespace PennySaver.API.Dtos;

public class CategoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorCode { get; set; }
    public bool IsActive { get; set; }
}

public class CategoryCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(7)]
    public string ColorCode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}