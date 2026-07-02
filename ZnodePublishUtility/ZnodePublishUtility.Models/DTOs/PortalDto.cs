namespace ZnodePublishUtility.Models.DTOs;

public class PortalDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public DateTime? LastPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreatePortalDto
{
    public string Name { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
}

public class UpdatePortalDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
