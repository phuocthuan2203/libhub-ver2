namespace LibHub.CatalogService.Application.DTOs;

public class CreateBookDto
{
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalCopies { get; set; }
}
