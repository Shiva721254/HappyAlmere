using HappyCity.Application;
using HappyCity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HappyCity.Infrastructure;

public sealed class PostgresCategoryLookup(HappyCityDbContext database) : ICategoryLookup
{
    public async Task<IReadOnlyList<CategoryLabel>> FindAsync(string citySlug, string language, CancellationToken cancellationToken = default)
    {
        var scope = new DiscoveryFilter(citySlug, null, null);
        var requested = RegionalPolicy.Language(language);
        var fallback = await database.Cities.AsNoTracking().Where(x => x.Slug == scope.CitySlug && x.IsActive)
            .Select(x => x.DefaultLanguage).SingleOrDefaultAsync(cancellationToken);
        if (fallback is null) return [];
        return await database.Categories.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Take(100)
            .Select(x => new CategoryLabel(x.Id, x.Slug,
                database.CategoryTranslations.Where(t => t.CategoryId == x.Id && t.LanguageCode == requested).Select(t => t.Name).FirstOrDefault()
                ?? database.CategoryTranslations.Where(t => t.CategoryId == x.Id && t.LanguageCode == fallback).Select(t => t.Name).FirstOrDefault()
                ?? x.Name)).ToArrayAsync(cancellationToken);
    }
}
