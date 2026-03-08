using Microsoft.EntityFrameworkCore;
using Holonix.Server.Domain.Entities;
using System.Globalization;

namespace Holonix.Server.Infrastructure.Data;

public static class ServiceSeeder
{
    private static readonly string[] DeprecatedServiceNames =
    [
        "Crime scene cleanup",
        "Junk hauling",
        "Moving labor help",
        "Interior decorating",
        "Staging services",
        "Landscaping design",
        "Plant care services",
        "Vent cleaning",
    ];

    private static readonly string[] ServiceNames =
    [
        "House cleaning",
        "Carpet cleaning",
        "Window cleaning",
        "Pressure washing",
        "Gutter cleaning",
        "Lawn mowing",
        "Landscaping",
        "Tree trimming",
        "Tree removal",
        "Pest control",
        "Pool cleaning",
        "Pool repair",
        "Junk removal",
        "Moving services",
        "Packing services",
        "Handyman services",
        "Furniture assembly",
        "Appliance repair",
        "Garage door repair",
        "Locksmith services",
        "Plumbing",
        "Electrical work",
        "HVAC repair",
        "HVAC installation",
        "Roofing",
        "Painting",
        "Drywall repair",
        "Flooring installation",
        "Tile installation",
        "Kitchen remodeling",
        "Bathroom remodeling",
        "Deck building",
        "Fence installation",
        "Concrete work",
        "Masonry",
        "Insulation installation",
        "Home inspection",
        "Chimney cleaning",
        "Chimney repair",
        "Fireplace installation",
        "Solar panel installation",
        "Generator installation",
        "Security system installation",
        "Security camera installation",
        "Smart home installation",
        "Internet and network setup",
        "Computer repair",
        "Phone repair",
        "TV mounting",
        "Audio system installation",
        "Car detailing",
        "Mobile car wash",
        "Mobile mechanic",
        "Oil change services",
        "Tire installation",
        "Windshield repair",
        "Window tinting",
        "Paint correction",
        "Towing services",
        "Dog walking",
        "Pet sitting",
        "Pet boarding",
        "Dog grooming",
        "Mobile pet grooming",
        "Pet training",
        "Pet waste removal",
        "Horse grooming",
        "Horse boarding",
        "Babysitting",
        "Nanny services",
        "Elder care",
        "Home health aide services",
        "Personal training",
        "Yoga instruction",
        "Pilates instruction",
        "Massage therapy",
        "Chiropractic services",
        "Physical therapy",
        "Occupational therapy",
        "Nutrition coaching",
        "Life coaching",
        "Tutoring",
        "Music lessons",
        "Language lessons",
        "Driving lessons",
        "Test preparation tutoring",
        "Photography",
        "Videography",
        "Drone photography",
        "Wedding photography",
        "Event planning",
        "DJ services",
        "Live music entertainment",
        "Catering services",
        "Private chef services",
        "Bartending services",
        "Party rentals",
        "Photo booth rentals",
        "Balloon decoration services",
        "Florist services",
        "Graphic design",
        "Web design",
        "App development",
        "Marketing consulting",
        "Social media management",
        "SEO services",
        "Content writing",
        "Copywriting",
        "Resume writing",
        "Bookkeeping",
        "Tax preparation",
        "Accounting services",
        "Financial advising",
        "Insurance consulting",
        "Real estate photography",
        "Real estate staging",
        "Property management",
        "House sitting",
        "Vacation rental cleaning",
        "Laundry services",
        "Dry cleaning pickup and delivery",
        "Tailoring and alterations",
        "Shoe repair",
        "Watch repair",
        "Jewelry repair",
        "Bike repair",
        "Small engine repair",
        "Boat cleaning",
        "Boat repair",
        "Dock installation",
        "Dock repair",
        "Scrap metal pickup",
        "Recycling pickup",
        "Courier services",
        "Grocery delivery",
        "Personal shopping",
        "Errand running",
        "Mobile notary services",
        "Translation services",
        "Interpretation services",
        "Travel planning",
        "Relocation assistance",
        "Home organization",
        "Closet organization",
        "Interior design",
        "Holiday light installation",
        "Snow removal",
        "Ice removal",
        "Firewood delivery",
        "Mulch delivery",
        "Irrigation installation",
        "Irrigation repair",
        "Sod installation",
        "Garden maintenance",
        "Beekeeping services",
        "Compost pickup",
        "Waste bin cleaning",
        "Driveway sealing",
        "Asphalt repair",
        "Parking lot striping",
        "Sign installation",
        "Sign repair",
        "Billboard installation",
        "Window tinting for homes",
        "Window film installation",
        "Glass repair",
        "Mirror installation",
        "Shower door installation",
        "Cabinet installation",
        "Countertop installation",
        "Backsplash installation",
        "Mold remediation",
        "Water damage restoration",
        "Fire damage restoration",
        "Biohazard cleanup",
        "Hoarding cleanup",
        "Carpet installation",
        "Rug cleaning",
        "Upholstery cleaning",
        "Mattress cleaning",
        "Air duct cleaning",
        "Dryer vent cleaning",
        "Attic cleaning",
        "Crawlspace cleaning",
    ];

    private static readonly TextInfo TextInfo = CultureInfo.InvariantCulture.TextInfo;

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var canonicalServiceNames = ServiceNames
            .Select(ToTitleCaseName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var deprecatedServiceNames = DeprecatedServiceNames
            .Select(ToTitleCaseName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingServices = await dbContext.Services
            .ToListAsync(cancellationToken);

        var assignedServiceIds = await dbContext.BusinessServices
            .AsNoTracking()
            .Select(link => link.ServiceId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var assignedServiceIdLookup = assignedServiceIds.ToHashSet();
        var hasChanges = false;

        foreach (var service in existingServices)
        {
            var titleCaseName = ToTitleCaseName(service.Name);
            if (canonicalServiceNames.Contains(titleCaseName, StringComparer.OrdinalIgnoreCase) &&
                !string.Equals(service.Name, titleCaseName, StringComparison.Ordinal))
            {
                service.Name = titleCaseName;
                hasChanges = true;
            }
        }

        var deprecatedServices = existingServices
            .Where(service => deprecatedServiceNames.Contains(ToTitleCaseName(service.Name), StringComparer.OrdinalIgnoreCase))
            .Where(service => !assignedServiceIdLookup.Contains(service.ServiceId))
            .ToList();

        if (deprecatedServices.Count > 0)
        {
            dbContext.Services.RemoveRange(deprecatedServices);
            hasChanges = true;
        }

        var existingNames = existingServices
            .Except(deprecatedServices)
            .Select(service => ToTitleCaseName(service.Name))
            .ToList();

        var existingLookup = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        var missingServices = canonicalServiceNames
            .Where(name => !existingLookup.Contains(name))
            .Select(name => new Service { Name = name })
            .ToList();

        if (missingServices.Count == 0)
        {
            if (hasChanges)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.Services.AddRange(missingServices);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ToTitleCaseName(string value)
    {
        return TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());
    }
}
