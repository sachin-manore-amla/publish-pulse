namespace ZnodePublishUtility.Models.DTOs;

public class StoreDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public DateTime? LastPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateStoreDto
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
}

public class UpdateStoreDto
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
