using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HappyCity.Application;
namespace HappyCity.Web.Controllers;
public sealed class HomeController(IDiscoveryService discovery) : Controller {
 [HttpGet, EnableRateLimiting("search")]
 public IActionResult Index(string? category, string? q) {
  if ((q?.Length ?? 0) > 100 || (category?.Length ?? 0) > 30) return BadRequest();
  ViewData["Query"] = q; ViewData["Category"] = category;
  return View(discovery.Find(category,q));
 }
 [HttpGet] public IActionResult Business() => View();
 [HttpGet] public IActionResult Privacy() => View();
 [Route("Home/Error")] public IActionResult Error() { Response.StatusCode=500; return View(); }
}
