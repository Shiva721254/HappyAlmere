using HappyCity.Application;
using HappyCity.Domain;
using HappyCity.Web.ViewModels;
using HappyCity.Web.CityContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyCity.Web.Controllers;

public sealed class HomeController(IDiscoveryService discovery, ICityContext city) : Controller
{
    [HttpGet, EnableRateLimiting("search"), CityScoped]
    public async Task<IActionResult> Index(string? category, string? q, string? district, CancellationToken cancellationToken, string? cursor = null)
    {
        DiscoveryFilter filter;
        try { filter = new DiscoveryFilter(city.CitySlug, category, q, district); }
        catch (ArgumentException) { return BadRequest(); }

        try
        {
            IReadOnlyList<DiscoveryItem> items;
            string? nextCursor = null;
            if (cursor is null && filter.Query is not null)
            {
                items = await discovery.FindAsync(filter, cancellationToken);
            }
            else
            {
                var page = await discovery.FindPageAsync(filter, DiscoveryFilter.MaximumResults, cursor, cancellationToken);
                items = page.Items;
                nextCursor = page.NextCursor;
            }

            var districts = await discovery.FindDistrictsAsync(city.CitySlug, cancellationToken);
            return View(new DiscoveryViewModel(items, filter.Query, filter.Category, nextCursor, filter.District, districts));
        }
        catch (ArgumentException) { return BadRequest(); }
    }

    [HttpGet("/ontdek/{slug}"), EnableRateLimiting("search"), CityScoped]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        if (!DiscoveryFilter.IsValidSlug(slug))
            return NotFoundPage();

        var item = await discovery.FindBySlugAsync(city.CitySlug, slug, cancellationToken);
        if (item is null)
            return NotFoundPage();

        var relatedPool = await discovery.FindAsync(new DiscoveryFilter(city.CitySlug, item.Category, null), cancellationToken);
        var related = relatedPool
            .Where(x => !string.Equals(x.Slug, item.Slug, StringComparison.Ordinal))
            .Take(3)
            .ToArray();

        return View(new DiscoveryDetailViewModel(item, related));
    }

    [HttpGet("/Home/Over"), HttpGet("/over")]
    public IActionResult Over() => View();

    [HttpGet]
    public IActionResult Business() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet("/Home/NotFoundPage"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }

    [Route("Home/Error"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }
}
