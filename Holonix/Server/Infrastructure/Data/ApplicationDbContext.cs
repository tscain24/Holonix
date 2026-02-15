using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<Team> Teams => Set<Team>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("authentication");

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id)
                .HasColumnName("UsersId");
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        builder.Entity<Sport>(entity =>
        {
            entity.ToTable("Sports", "sport");
            entity.HasKey(sport => sport.SportsId);
            entity.Property(sport => sport.Name)
                .IsRequired()
                .HasMaxLength(120);

            entity.HasData(
                new Sport { SportsId = 1, Name = "NFL" },
                new Sport { SportsId = 2, Name = "MLB" },
                new Sport { SportsId = 3, Name = "NBA" },
                new Sport { SportsId = 4, Name = "MLS" },
                new Sport { SportsId = 5, Name = "NCAA Football" },
                new Sport { SportsId = 6, Name = "NCAA Basketball" },
                new Sport { SportsId = 7, Name = "NCAA Baseball" }
            );
        });

        builder.Entity<Team>(entity =>
        {
            entity.ToTable("Teams", "sport");
            entity.HasKey(team => team.TeamId);
            entity.Property(team => team.LocationName)
                .IsRequired()
                .HasMaxLength(120);
            entity.Property(team => team.TeamName)
                .IsRequired()
                .HasMaxLength(120);

            entity.HasOne(team => team.Sport)
                .WithMany()
                .HasForeignKey(team => team.SportId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(
                new Team { TeamId = 1, SportId = 3, LocationName = "Atlanta", TeamName = "Hawks" },
                new Team { TeamId = 2, SportId = 3, LocationName = "Boston", TeamName = "Celtics" },
                new Team { TeamId = 3, SportId = 3, LocationName = "Brooklyn", TeamName = "Nets" },
                new Team { TeamId = 4, SportId = 3, LocationName = "Charlotte", TeamName = "Hornets" },
                new Team { TeamId = 5, SportId = 3, LocationName = "Chicago", TeamName = "Bulls" },
                new Team { TeamId = 6, SportId = 3, LocationName = "Cleveland", TeamName = "Cavaliers" },
                new Team { TeamId = 7, SportId = 3, LocationName = "Dallas", TeamName = "Mavericks" },
                new Team { TeamId = 8, SportId = 3, LocationName = "Denver", TeamName = "Nuggets" },
                new Team { TeamId = 9, SportId = 3, LocationName = "Detroit", TeamName = "Pistons" },
                new Team { TeamId = 10, SportId = 3, LocationName = "Golden State", TeamName = "Warriors" },
                new Team { TeamId = 11, SportId = 3, LocationName = "Houston", TeamName = "Rockets" },
                new Team { TeamId = 12, SportId = 3, LocationName = "Indiana", TeamName = "Pacers" },
                new Team { TeamId = 13, SportId = 3, LocationName = "Los Angeles", TeamName = "Clippers" },
                new Team { TeamId = 14, SportId = 3, LocationName = "Los Angeles", TeamName = "Lakers" },
                new Team { TeamId = 15, SportId = 3, LocationName = "Memphis", TeamName = "Grizzlies" },
                new Team { TeamId = 16, SportId = 3, LocationName = "Miami", TeamName = "Heat" },
                new Team { TeamId = 17, SportId = 3, LocationName = "Milwaukee", TeamName = "Bucks" },
                new Team { TeamId = 18, SportId = 3, LocationName = "Minnesota", TeamName = "Timberwolves" },
                new Team { TeamId = 19, SportId = 3, LocationName = "New Orleans", TeamName = "Pelicans" },
                new Team { TeamId = 20, SportId = 3, LocationName = "New York", TeamName = "Knicks" },
                new Team { TeamId = 21, SportId = 3, LocationName = "Oklahoma City", TeamName = "Thunder" },
                new Team { TeamId = 22, SportId = 3, LocationName = "Orlando", TeamName = "Magic" },
                new Team { TeamId = 23, SportId = 3, LocationName = "Philadelphia", TeamName = "76ers" },
                new Team { TeamId = 24, SportId = 3, LocationName = "Phoenix", TeamName = "Suns" },
                new Team { TeamId = 25, SportId = 3, LocationName = "Portland", TeamName = "Trail Blazers" },
                new Team { TeamId = 26, SportId = 3, LocationName = "Sacramento", TeamName = "Kings" },
                new Team { TeamId = 27, SportId = 3, LocationName = "San Antonio", TeamName = "Spurs" },
                new Team { TeamId = 28, SportId = 3, LocationName = "Toronto", TeamName = "Raptors" },
                new Team { TeamId = 29, SportId = 3, LocationName = "Utah", TeamName = "Jazz" },
                new Team { TeamId = 30, SportId = 3, LocationName = "Washington", TeamName = "Wizards" },
                new Team { TeamId = 31, SportId = 1, LocationName = "Arizona", TeamName = "Cardinals" },
                new Team { TeamId = 32, SportId = 1, LocationName = "Atlanta", TeamName = "Falcons" },
                new Team { TeamId = 33, SportId = 1, LocationName = "Baltimore", TeamName = "Ravens" },
                new Team { TeamId = 34, SportId = 1, LocationName = "Buffalo", TeamName = "Bills" },
                new Team { TeamId = 35, SportId = 1, LocationName = "Carolina", TeamName = "Panthers" },
                new Team { TeamId = 36, SportId = 1, LocationName = "Chicago", TeamName = "Bears" },
                new Team { TeamId = 37, SportId = 1, LocationName = "Cincinnati", TeamName = "Bengals" },
                new Team { TeamId = 38, SportId = 1, LocationName = "Cleveland", TeamName = "Browns" },
                new Team { TeamId = 39, SportId = 1, LocationName = "Dallas", TeamName = "Cowboys" },
                new Team { TeamId = 40, SportId = 1, LocationName = "Denver", TeamName = "Broncos" },
                new Team { TeamId = 41, SportId = 1, LocationName = "Detroit", TeamName = "Lions" },
                new Team { TeamId = 42, SportId = 1, LocationName = "Green Bay", TeamName = "Packers" },
                new Team { TeamId = 43, SportId = 1, LocationName = "Houston", TeamName = "Texans" },
                new Team { TeamId = 44, SportId = 1, LocationName = "Indianapolis", TeamName = "Colts" },
                new Team { TeamId = 45, SportId = 1, LocationName = "Jacksonville", TeamName = "Jaguars" },
                new Team { TeamId = 46, SportId = 1, LocationName = "Kansas City", TeamName = "Chiefs" },
                new Team { TeamId = 47, SportId = 1, LocationName = "Las Vegas", TeamName = "Raiders" },
                new Team { TeamId = 48, SportId = 1, LocationName = "Los Angeles", TeamName = "Chargers" },
                new Team { TeamId = 49, SportId = 1, LocationName = "Los Angeles", TeamName = "Rams" },
                new Team { TeamId = 50, SportId = 1, LocationName = "Miami", TeamName = "Dolphins" },
                new Team { TeamId = 51, SportId = 1, LocationName = "Minnesota", TeamName = "Vikings" },
                new Team { TeamId = 52, SportId = 1, LocationName = "New England", TeamName = "Patriots" },
                new Team { TeamId = 53, SportId = 1, LocationName = "New Orleans", TeamName = "Saints" },
                new Team { TeamId = 54, SportId = 1, LocationName = "New York", TeamName = "Giants" },
                new Team { TeamId = 55, SportId = 1, LocationName = "New York", TeamName = "Jets" },
                new Team { TeamId = 56, SportId = 1, LocationName = "Philadelphia", TeamName = "Eagles" },
                new Team { TeamId = 57, SportId = 1, LocationName = "Pittsburgh", TeamName = "Steelers" },
                new Team { TeamId = 58, SportId = 1, LocationName = "San Francisco", TeamName = "49ers" },
                new Team { TeamId = 59, SportId = 1, LocationName = "Seattle", TeamName = "Seahawks" },
                new Team { TeamId = 60, SportId = 1, LocationName = "Tampa Bay", TeamName = "Buccaneers" },
                new Team { TeamId = 61, SportId = 1, LocationName = "Tennessee", TeamName = "Titans" },
                new Team { TeamId = 62, SportId = 1, LocationName = "Washington", TeamName = "Commanders" },
                new Team { TeamId = 63, SportId = 2, LocationName = "Arizona", TeamName = "Diamondbacks" },
                new Team { TeamId = 64, SportId = 2, LocationName = "Atlanta", TeamName = "Braves" },
                new Team { TeamId = 65, SportId = 2, LocationName = "Baltimore", TeamName = "Orioles" },
                new Team { TeamId = 66, SportId = 2, LocationName = "Boston", TeamName = "Red Sox" },
                new Team { TeamId = 67, SportId = 2, LocationName = "Chicago", TeamName = "Cubs" },
                new Team { TeamId = 68, SportId = 2, LocationName = "Chicago", TeamName = "White Sox" },
                new Team { TeamId = 69, SportId = 2, LocationName = "Cincinnati", TeamName = "Reds" },
                new Team { TeamId = 70, SportId = 2, LocationName = "Cleveland", TeamName = "Guardians" },
                new Team { TeamId = 71, SportId = 2, LocationName = "Colorado", TeamName = "Rockies" },
                new Team { TeamId = 72, SportId = 2, LocationName = "Detroit", TeamName = "Tigers" },
                new Team { TeamId = 73, SportId = 2, LocationName = "Houston", TeamName = "Astros" },
                new Team { TeamId = 74, SportId = 2, LocationName = "Kansas City", TeamName = "Royals" },
                new Team { TeamId = 75, SportId = 2, LocationName = "Los Angeles", TeamName = "Angels" },
                new Team { TeamId = 76, SportId = 2, LocationName = "Los Angeles", TeamName = "Dodgers" },
                new Team { TeamId = 77, SportId = 2, LocationName = "Miami", TeamName = "Marlins" },
                new Team { TeamId = 78, SportId = 2, LocationName = "Milwaukee", TeamName = "Brewers" },
                new Team { TeamId = 79, SportId = 2, LocationName = "Minnesota", TeamName = "Twins" },
                new Team { TeamId = 80, SportId = 2, LocationName = "New York", TeamName = "Mets" },
                new Team { TeamId = 81, SportId = 2, LocationName = "New York", TeamName = "Yankees" },
                new Team { TeamId = 82, SportId = 2, LocationName = "Oakland", TeamName = "Athletics" },
                new Team { TeamId = 83, SportId = 2, LocationName = "Philadelphia", TeamName = "Phillies" },
                new Team { TeamId = 84, SportId = 2, LocationName = "Pittsburgh", TeamName = "Pirates" },
                new Team { TeamId = 85, SportId = 2, LocationName = "San Diego", TeamName = "Padres" },
                new Team { TeamId = 86, SportId = 2, LocationName = "San Francisco", TeamName = "Giants" },
                new Team { TeamId = 87, SportId = 2, LocationName = "Seattle", TeamName = "Mariners" },
                new Team { TeamId = 88, SportId = 2, LocationName = "St. Louis", TeamName = "Cardinals" },
                new Team { TeamId = 89, SportId = 2, LocationName = "Tampa Bay", TeamName = "Rays" },
                new Team { TeamId = 90, SportId = 2, LocationName = "Texas", TeamName = "Rangers" },
                new Team { TeamId = 91, SportId = 2, LocationName = "Toronto", TeamName = "Blue Jays" },
                new Team { TeamId = 92, SportId = 2, LocationName = "Washington", TeamName = "Nationals" },
                new Team { TeamId = 93, SportId = 4, LocationName = "Atlanta", TeamName = "United" },
                new Team { TeamId = 94, SportId = 4, LocationName = "Austin", TeamName = "FC" },
                new Team { TeamId = 95, SportId = 4, LocationName = "Charlotte", TeamName = "FC" },
                new Team { TeamId = 96, SportId = 4, LocationName = "Chicago", TeamName = "Fire" },
                new Team { TeamId = 97, SportId = 4, LocationName = "Cincinnati", TeamName = "FC" },
                new Team { TeamId = 98, SportId = 4, LocationName = "Colorado", TeamName = "Rapids" },
                new Team { TeamId = 99, SportId = 4, LocationName = "Columbus", TeamName = "Crew" },
                new Team { TeamId = 100, SportId = 4, LocationName = "Dallas", TeamName = "FC" },
                new Team { TeamId = 101, SportId = 4, LocationName = "D.C. United", TeamName = "FC" },
                new Team { TeamId = 102, SportId = 4, LocationName = "Houston", TeamName = "Dynamo" },
                new Team { TeamId = 103, SportId = 4, LocationName = "Kansas City", TeamName = "Sporting" },
                new Team { TeamId = 104, SportId = 4, LocationName = "Los Angeles", TeamName = "Galaxy" },
                new Team { TeamId = 105, SportId = 4, LocationName = "Los Angeles", TeamName = "FC" },
                new Team { TeamId = 106, SportId = 4, LocationName = "Miami", TeamName = "Inter" },
                new Team { TeamId = 107, SportId = 4, LocationName = "Minnesota", TeamName = "United" },
                new Team { TeamId = 108, SportId = 4, LocationName = "Montreal", TeamName = "CF" },
                new Team { TeamId = 109, SportId = 4, LocationName = "Nashville", TeamName = "SC" },
                new Team { TeamId = 110, SportId = 4, LocationName = "New England", TeamName = "Revolution" },
                new Team { TeamId = 111, SportId = 4, LocationName = "New York City", TeamName = "FC" },
                new Team { TeamId = 112, SportId = 4, LocationName = "New York", TeamName = "Red Bulls" },
                new Team { TeamId = 113, SportId = 4, LocationName = "Orlando", TeamName = "City" },
                new Team { TeamId = 114, SportId = 4, LocationName = "Philadelphia", TeamName = "Union" },
                new Team { TeamId = 115, SportId = 4, LocationName = "Portland", TeamName = "Timbers" },
                new Team { TeamId = 116, SportId = 4, LocationName = "Salt Lake", TeamName = "Real" },
                new Team { TeamId = 117, SportId = 4, LocationName = "San Jose", TeamName = "Earthquakes" },
                new Team { TeamId = 118, SportId = 4, LocationName = "Seattle", TeamName = "Sounders" },
                new Team { TeamId = 119, SportId = 4, LocationName = "St. Louis", TeamName = "City" },
                new Team { TeamId = 120, SportId = 4, LocationName = "Toronto", TeamName = "FC" },
                new Team { TeamId = 121, SportId = 4, LocationName = "Vancouver", TeamName = "Whitecaps" }
            );
        });
    }
}


