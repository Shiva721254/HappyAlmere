using HappyCity.Application;
using HappyCity.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyCity.Web.Controllers;

public sealed class HomeController(IDiscoveryService discovery) : Controller
{
    [HttpGet, EnableRateLimiting("search")]
    public async Task<IActionResult> Index(string? category, string? q, CancellationToken cancellationToken)
    {
        DiscoveryFilter filter;
        try { filter = new DiscoveryFilter(category, q); }
        catch (ArgumentException) { return BadRequest(); }

        var items = await discovery.FindAsync(filter, cancellationToken);
        return View(new DiscoveryViewModel(items, filter.Query, filter.Category));
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
