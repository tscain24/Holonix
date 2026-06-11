namespace Holonix.Server.Domain.Entities;

public class UserSavedLocation
{
    public long UserSavedLocationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? InactiveDate { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
