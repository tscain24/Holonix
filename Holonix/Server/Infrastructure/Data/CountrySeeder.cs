using Microsoft.EntityFrameworkCore;
using Holonix.Server.Domain.Entities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Holonix.Server.Infrastructure.Data;

public static class CountrySeeder
{
    private static readonly Regex TwoLetterCodePattern = new("^[A-Z]{2}$", RegexOptions.Compiled);
    private static readonly Regex ThreeLetterCodePattern = new("^[A-Z]{3}$", RegexOptions.Compiled);

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existingCodes = await dbContext.Countries
            .AsNoTracking()
            .Select(country => country.TwoLetterIsoCode)
            .ToListAsync(cancellationToken);

        var existingCodeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (existingCodeSet.Count > 0 && existingCodeSet.Contains("US"))
        {
            // Proceed to add any missing countries; keep this fast for subsequent startups.
        }

        var countriesToAdd = GetCultureCountries()
            .Where(country => !existingCodeSet.Contains(country.TwoLetterIsoCode))
            .ToList();

        if (countriesToAdd.Count == 0)
        {
            return;
        }

        dbContext.Countries.AddRange(countriesToAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<Country> GetCultureCountries()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            RegionInfo region;
            try
            {
                region = new RegionInfo(culture.Name);
            }
            catch
            {
                continue;
            }

            var twoLetter = (region.TwoLetterISORegionName ?? string.Empty).Trim().ToUpperInvariant();
            if (!TwoLetterCodePattern.IsMatch(twoLetter))
            {
                continue;
            }

            if (!seen.Add(twoLetter))
            {
                continue;
            }

            var name = (region.EnglishName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var threeLetter = (region.ThreeLetterISORegionName ?? string.Empty).Trim().ToUpperInvariant();
            if (!ThreeLetterCodePattern.IsMatch(threeLetter))
            {
                threeLetter = string.Empty;
            }

            yield return new Country
            {
                Name = name,
                TwoLetterIsoCode = twoLetter,
                ThreeLetterIsoCode = string.IsNullOrWhiteSpace(threeLetter) ? null : threeLetter,
            };
        }
    }
}

