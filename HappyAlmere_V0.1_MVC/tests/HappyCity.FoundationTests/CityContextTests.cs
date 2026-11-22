using HappyCity.Application;
using HappyCity.Web.CityContext;
using HappyCity.Web.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace HappyCity.FoundationTests;

public sealed class CityContextTests
{
    internal static CityResolution Resolution(string slug = "almere") => new(
        Guid.Parse("60000000-0000-4000-8000-000000000001"), slug, "Synthetic city",
        "NL", "nl", "Europe/Amsterdam", "almere.test", "almere.test", false, false);

    [Fact]
    public void Context_rejects_uninitialized_access_and_second_initialization()
    {
        var context = new RequestCityContext();
        foreach (var property in typeof(ICityContext).GetProperties())
        {
            Assert.Null(property.SetMethod);
            var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => property.GetValue(context));
            Assert.IsType<InvalidOperationException>(error.InnerException);
        }
        var value = Resolution();
        context.Initialize(value);
        Assert.Equal(value.CityId, context.CityId);
        Assert.Equal(value.CitySlug, context.CitySlug);
        Assert.Equal(value.CityName, context.CityName);
        Assert.Equal(value.CountryCode, context.CountryCode);
        Assert.Equal(value.DefaultLanguage, context.DefaultLanguage);
        Assert.Equal(value.TimeZone, context.TimeZone);
        Assert.Equal(value.RequestedHost, context.RequestedHost);
        Assert.Equal(value.CanonicalHost, context.CanonicalHost);
        Assert.False(context.IsLocalEntryPoint);
        Assert.False(context.RequiresCanonicalRedirect);
        Assert.Throws<InvalidOperationException>(() => context.Initialize(Resolution("utrecht")));
        Assert.Equal("almere", context.CitySlug);
    }

    [Fact]
    public void Scoped_interface_and_holder_are_identical_and_scopes_are_distinct()
    {
        var services = new ServiceCollection();
        services.AddScoped<RequestCityContext>();
        services.AddScoped<ICityContext>(provider => provider.GetRequiredService<RequestCityContext>());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var a = first.ServiceProvider.GetRequiredService<RequestCityContext>();
        Assert.Same(a, first.ServiceProvider.GetRequiredService<ICityContext>());
        Assert.NotSame(a, second.ServiceProvider.GetRequiredService<ICityContext>());
        a.Initialize(Resolution());
        Assert.Throws<InvalidOperationException>(() => second.ServiceProvider.GetRequiredService<ICityContext>().CityId);
        Assert.DoesNotContain(typeof(ICityContext).Assembly.GetReferencedAssemblies(), assembly => assembly.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Only_Index_is_explicitly_city_scoped()
    {
        foreach (var name in new[] { "Index", "Business", "Privacy", "Error" })
            Assert.Equal(name == "Index", Attribute.IsDefined(typeof(HomeController).GetMethod(name)!, typeof(CityScopedAttribute)));
    }
}
