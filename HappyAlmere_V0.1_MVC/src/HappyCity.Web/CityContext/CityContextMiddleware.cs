using System.Data.Common;
using HappyCity.Application;
using Microsoft.AspNetCore.WebUtilities;

namespace HappyCity.Web.CityContext;

public sealed class CityContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http)
    {
        if (http.GetEndpoint()?.Metadata.GetMetadata<CityScopedAttribute>() is null)
        {
            await next(http);
            return;
        }

        CityResolution? city;
        try
        {
            city = await http.RequestServices.GetRequiredService<ICityResolver>()
                .ResolveAsync(http.Request.Host.Host, http.RequestAborted);
        }
        catch (DbException)
        {
            // No fallback city or database diagnostics in the response.
            http.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }
        if (city is null)
        {
            http.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        http.RequestServices.GetRequiredService<RequestCityContext>().Initialize(city);
        if (!city.RequiresCanonicalRedirect)
        {
            await next(http);
            return;
        }
        if (!HttpMethods.IsGet(http.Request.Method))
        {
            http.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }
        try
        {
            var query = http.Request.Query;
            string? Value(string name)
            {
                if (!query.TryGetValue(name, out var values)) return null;
                if (values.Count != 1) throw new ArgumentException("Duplicate query value.");
                return values[0];
            }
            // Query parsers can retain malformed percent escapes. Reject before redirect.
            var raw = http.Request.QueryString.Value ?? "";
            for (var i = 0; i < raw.Length; i++)
                if (raw[i] == '%' && (i + 2 >= raw.Length || !Uri.IsHexDigit(raw[i + 1]) || !Uri.IsHexDigit(raw[i + 2])))
                    throw new ArgumentException("Malformed query encoding.");
            var filter = new DiscoveryFilter(city.CitySlug, Value("category"), Value("q"));
            var cursor = Value("cursor");
            DiscoveryCursor.Parse(cursor, filter, DiscoveryFilter.MaximumResults);
            var path = (http.Request.PathBase + http.Request.Path).ToUriComponent();
            if (!path.StartsWith('/') || path.Contains('\\') || path.Any(char.IsControl))
                throw new ArgumentException("Invalid local path.");
            var authority = new UriBuilder(Uri.UriSchemeHttps, city.CanonicalHost!).Uri.GetLeftPart(UriPartial.Authority);
            var location = QueryHelpers.AddQueryString(authority + path,
                new Dictionary<string, string?> { ["q"] = filter.Query, ["category"] = filter.Category, ["cursor"] = cursor });
            http.Response.Redirect(location, permanent: true);
        }
        catch (ArgumentException)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
