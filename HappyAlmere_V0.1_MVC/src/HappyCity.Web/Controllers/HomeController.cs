using HappyCity.Application;
using HappyCity.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyCity.Web.Controllers;

public sealed class HomeController(IDiscoveryService discovery, ICityResolver cities) : Controller
{
    [HttpGet, EnableRateLimiting("search")]
    public async Task<IActionResult> Index(string? category, string? q, CancellationToken cancellationToken, string? cursor = null)
    {
        var city = await cities.ResolveAsync(Request.Host.Host, cancellationToken);
        if (city is null) return NotFound();
        if (city.CanonicalHost is not null)
        {
            var canonical = new UriBuilder("https", city.CanonicalHost);
            // Host is from validated database configuration; no forwarded-host redirect input.
            return RedirectPermanent(canonical.Uri.GetLeftPart(UriPartial.Authority) + Request.PathBase + Request.Path + Request.QueryString);
        }
        DiscoveryFilter filter;
        try { filter = new DiscoveryFilter(city.CitySlug, category, q); }
        catch (ArgumentException) { return BadRequest(); }

        try
        {
            var page = await discovery.FindPageAsync(filter, DiscoveryFilter.MaximumResults, cursor, cancellationToken);
            return View(new DiscoveryViewModel(page.Items, filter.Query, filter.Category, page.NextCursor));
        }
        catch (ArgumentException) { return BadRequest(); }
    }

    [HttpGet]
    public IActionResult Business() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    [Route("Home/Error"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }
}
