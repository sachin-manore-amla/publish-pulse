namespace ZnodePublishUtility.Models.DTOs;

public class CatalogDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public DateTime? LastPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateCatalogDto
{
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}

public class UpdateCatalogDto
{
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public bool IsActive { get; set; }
}
