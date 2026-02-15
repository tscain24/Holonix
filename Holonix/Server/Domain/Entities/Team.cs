namespace Holonix.Server.Domain.Entities;

public class Team
{
    public int TeamId { get; set; }
    public int SportId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;

    public Sport? Sport { get; set; }
}

