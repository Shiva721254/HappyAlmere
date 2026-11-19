using HappyCity.Application;
using HappyCity.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyCity.Web.Controllers;

public sealed class HomeController(IDiscoveryService discovery) : Controller
{
    [HttpGet, EnableRateLimiting("search")]
    public async Task<IActionResult> Index(string? category, string? q, CancellationToken cancellationToken, string? cursor = null)
    {
        DiscoveryFilter filter;
        // This deployment serves Almere. Request parameters cannot override the city.
        try { filter = new DiscoveryFilter("almere", category, q); }
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
