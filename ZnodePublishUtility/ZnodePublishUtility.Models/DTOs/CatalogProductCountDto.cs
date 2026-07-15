namespace ZnodePublishUtility.Models.DTOs;

/// <summary>
/// One row from <c>dbo.GetCatalogProductCounts</c>: a catalog's draft (not-yet-published)
/// product count.
/// </summary>
public class CatalogProductCountDto
{
    public string CatalogId { get; set; } = "";
    public int Count { get; set; }
}
